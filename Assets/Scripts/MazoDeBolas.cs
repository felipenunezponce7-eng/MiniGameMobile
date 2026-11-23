using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
public class MazoDeBolas : MonoBehaviour
{
    [SerializeField] Bola bolaPrefab;
    [SerializeField] List<TipoDeBola> startingDeck = new();

    public event Action BallReturned;

    public int InFlight { get; private set; }     // bolas lanzadas que siguen en juego
    public int Remaining => drawPile.Count;       // bolas por robar en este nivel

    readonly List<BallSlot> drawPile = new();
    ObjectPool<Bola> pool;

    void Awake()
    {
        // Las bolas no chocan entre sí (si no, las extras se empujarían con la original)
        int ballLayer = bolaPrefab.gameObject.layer;
        if (ballLayer != 0) Physics.IgnoreLayerCollision(ballLayer, ballLayer, true);
        else Debug.LogWarning("El prefab de la bola está en la capa Default: ponlo en la capa 'Ball' para que las bolas extra no choquen entre sí.", this);

        // La primera vez crea el mazo de la run; en niveles siguientes reutiliza el existente
        RunState.EnsureDeck(startingDeck);

        pool = new ObjectPool<Bola>(
            createFunc: () =>
            {
                var b = Instantiate(bolaPrefab);
                b.gameObject.SetActive(false);
                return b;
            },
            actionOnGet: b => b.gameObject.SetActive(true),
            actionOnRelease: b => b.gameObject.SetActive(false),
            actionOnDestroy: b => Destroy(b.gameObject),
            defaultCapacity: 10,
            maxSize: 50);
    }

    void Start() => ResetAndShuffle();

    // Llamar al empezar cada nivel
    public void ResetAndShuffle()
    {
        drawPile.Clear();
        drawPile.AddRange(RunState.Deck);
        InFlight = 0;

        for (int i = drawPile.Count - 1; i > 0; i--)   // Fisher-Yates
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (drawPile[i], drawPile[j]) = (drawPile[j], drawPile[i]);
        }
    }

    // Para agregar bolas al mazo (por ejemplo, una futura mejora que aumente su tamaño)
    public void AddBall(TipoDeBola type)
    {
        RunState.EnsureDeck(startingDeck);
        RunState.Deck.Add(new BallSlot(type, type.isUpgrade ? 1 : 0));
    }

    public Bola Draw(Vector3 position)
    {
        if (drawPile.Count == 0) return null;

        int last = drawPile.Count - 1;
        BallSlot slot = drawPile[last];
        drawPile.RemoveAt(last);

        position.z = 0f; // siempre en el plano de juego
        Bola bola = pool.Get();
        bola.transform.position = position;
        bola.Setup(slot.type, slot.level);
        bola.Returned = Return;
        bola.Owner = this;
        return bola;
    }

    // Bola extra de la mejora Multibola: copia el aspecto de la original,
    // nace ya lanzada y cuenta como "en juego"
    public void SpawnExtra(Vector3 position, Vector2 velocity, TipoDeBola type, int level)
    {
        position.z = 0f;

        Bola extra = pool.Get();
        extra.transform.position = position;
        extra.Setup(type, level, true);
        extra.Returned = Return;
        extra.Owner = this;
        extra.Launch(new Vector3(velocity.x, velocity.y, 0f));
        InFlight++;
    }

    public void MarkLaunched(Bola bola) => InFlight++;

    void Return(Bola bola)
    {
        if (bola.IsLaunched) InFlight--;
        pool.Release(bola);
        BallReturned?.Invoke();
    }
}
