using UnityEngine;
using UnityEngine.Pool;

public class BolaSpawner : MonoBehaviour
{
    [SerializeField] Bola bolaPrefab;
    [SerializeField] TipoDeBola[] tipos;
    [SerializeField] int cantidad = 5;
    [SerializeField] float radio = 5f;

    ObjectPool<Bola> pool;
    int nextId;

    void Awake()
    {
        pool = new ObjectPool<Bola>(
            createFunc: () =>
            {
                var b = Instantiate(bolaPrefab, transform);
                b.gameObject.SetActive(false);
                nextId++;
                return b;
            },
            actionOnGet: b => b.gameObject.SetActive(true),
            actionOnRelease: b => b.gameObject.SetActive(false),
            actionOnDestroy: b => Destroy(b.gameObject),
            defaultCapacity: 10,
            maxSize: 50);
    }

    void Start()
    {
        for (int i = 0; i < cantidad; i++)
            Spawn();
    }

    public Bola Spawn()
    {
        var bola = pool.Get();
        bola.transform.position = transform.position + Random.insideUnitSphere * radio;
        bola.Setup(ElegirTipo(), 0);
        bola.GetComponent<Rigidbody>().isKinematic = true;
        return bola;
    }

    public void Devolver(Bola bola) => pool.Release(bola);

    // Selección por pesos: sin huecos y fácil de balancear desde el Inspector
    TipoDeBola ElegirTipo()
    {
        float total = 0f;
        foreach (var t in tipos) total += t.weight;

        float r = Random.value * total;
        foreach (var t in tipos)
        {
            r -= t.weight;
            if (r <= 0f) return t;
        }
        return tipos[0];
    }
}