using UnityEngine;
using UnityEngine.Events;

public enum LevelState { Playing, Won, Lost }

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] MazoDeBolas Mazo;

    [Header("Eventos (conecta UI desde el Inspector)")]
    public UnityEvent OnWon;
    public UnityEvent OnLost;

    public LevelState State { get; private set; } = LevelState.Playing;
    public int BlocksLeft { get; private set; }

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        Block.OnDestroyed += HandleBlockDestroyed;
        Mazo.BallReturned += HandleBallReturned;
    }

    void OnDisable()
    {
        Block.OnDestroyed -= HandleBlockDestroyed;
        Mazo.BallReturned -= HandleBallReturned;
    }

    void Start() => CountBlocks();

    // Si generas bloques por código, llama esto después de crearlos
    public void CountBlocks()
    {
        BlocksLeft = FindObjectsByType<Block>(FindObjectsSortMode.None).Length;
    }

    void HandleBlockDestroyed(Block block)
    {
        if (State != LevelState.Playing) return;

        BlocksLeft--;
        if (BlocksLeft > 0) return;

        State = LevelState.Won;
        Debug.Log("Nivel completado");
        OnWon?.Invoke();
    }

    // Derrota: no quedan bolas por robar, ninguna en juego y aún hay bloques
    void HandleBallReturned()
    {
        if (State != LevelState.Playing) return;
        if (Mazo.Remaining > 0 || Mazo.InFlight > 0 || BlocksLeft <= 0) return;

        State = LevelState.Lost;
        Debug.Log("Sin bolas: nivel perdido");
        OnLost?.Invoke();
    }
}
