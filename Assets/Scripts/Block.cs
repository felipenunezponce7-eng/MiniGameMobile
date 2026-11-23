using System;
using UnityEngine;

public class Block : MonoBehaviour, IDamageable
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit/Unlit
    static MaterialPropertyBlock mpb;
    static GameObject prefabRomper;
    static bool prefabBuscado;

    [SerializeField, Min(1)] int hp = 1;

    [Header("Aspecto")]
    [Tooltip("Color según la vida que le queda (el último se usa para vidas mayores)")]
    [SerializeField]
    Color[] coloresPorVida =
    {
        new Color(0.25f, 0.85f, 0.75f),   // 1 turquesa
        new Color(0.45f, 0.90f, 0.35f),   // 2 verde
        new Color(1.00f, 0.90f, 0.30f),   // 3 amarillo
        new Color(1.00f, 0.60f, 0.20f),   // 4 naranja
        new Color(1.00f, 0.35f, 0.30f),   // 5 rojo
        new Color(0.95f, 0.35f, 0.75f),   // 6 magenta
        new Color(0.60f, 0.40f, 1.00f),   // 7 violeta
        new Color(0.95f, 0.95f, 1.00f)    // 8 o más: casi blanco
    };

    [Header("Animación")]
    [SerializeField, Min(0.05f)] float duracionAparicion = 0.3f;
    [SerializeField, Range(0f, 0.5f)] float intensidadGolpe = 0.15f;
    [SerializeField, Min(0.05f)] float duracionGolpe = 0.15f;

    // Lo escucha el LevelManager para contar los bloques restantes
    public static event Action<Block> OnDestroyed;

    // Evita eventos "fantasma" si desactivas Domain Reload en el editor
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        OnDestroyed = null;
        prefabRomper = null;
        prefabBuscado = false;
    }

    public int Hp => hp;

    Renderer rend;
    Vector3 escalaBase;
    int vidaInicial;
    float retraso;
    float tAparicion = 99f;   // 99 = animación terminada
    float tGolpe = 99f;
    float flash;

    void Awake()
    {
        escalaBase = transform.localScale;
        vidaInicial = hp;
        AplicarColor();
        enabled = false; // el Update solo corre mientras hay una animación
    }

    // Lo llama el CargadorNivel justo después de crear el bloque (con la escala ya puesta)
    public void Configurar(int vida, float retrasoAparicion)
    {
        hp = Mathf.Max(1, vida);
        vidaInicial = hp;
        escalaBase = transform.localScale;
        AplicarColor();

        if (!Application.isPlaying) return; // vista previa en el editor: sin animación

        retraso = retrasoAparicion;
        tAparicion = 0f;
        tGolpe = 99f;
        flash = 0f;
        transform.localScale = escalaBase * 0.001f;
        enabled = true;
    }

    public void TakeDamage(int amount, Bola source)
    {
        if (hp <= 0) return; // ya fue destruido este frame

        Color colorAntes = ColorActual();
        hp -= amount;

        if (hp > 0)
        {
            tGolpe = 0f;
            flash = 1f;
            enabled = true;
            AplicarColor();

            // Cuanto menos vida le queda, más agudo suena el golpe
            float ratio = hp / (float)Mathf.Max(1, vidaInicial);
            Sonidos.Reproducir(Sonido.GolpeBloque, 0.7f, Mathf.Lerp(1.4f, 0.9f, ratio));
            return;
        }

        hp = 0;
        OnDestroyed?.Invoke(this);

        Sonidos.Reproducir(Sonido.RompeBloque, 0.9f, UnityEngine.Random.Range(0.9f, 1.15f));
        ShakeCamara.Sacudir(0.04f, 0.08f);
        LanzarParticulas(colorAntes);
        Destroy(gameObject);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (retraso > 0f)
        {
            retraso -= dt;
            return;
        }

        float factor = 1f;
        bool activo = false;

        if (tAparicion < duracionAparicion)
        {
            tAparicion += dt;
            factor = EaseOutBack(Mathf.Clamp01(tAparicion / duracionAparicion));
            activo = true;
        }

        if (tGolpe < duracionGolpe)
        {
            tGolpe += dt;
            factor *= 1f + intensidadGolpe * Mathf.Sin(Mathf.Clamp01(tGolpe / duracionGolpe) * Mathf.PI);
            activo = true;
        }

        if (flash > 0f)
        {
            flash = Mathf.Max(0f, flash - dt / 0.12f);
            AplicarColor();
            activo = true;
        }

        transform.localScale = escalaBase * Mathf.Max(factor, 0.001f);
        if (!activo) enabled = false;
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    Color ColorActual()
    {
        if (coloresPorVida == null || coloresPorVida.Length == 0) return Color.white;
        int i = Mathf.Clamp(hp - 1, 0, coloresPorVida.Length - 1);
        return coloresPorVida[i];
    }

    void AplicarColor()
    {
        if (rend == null) rend = GetComponentInChildren<Renderer>();
        if (rend == null) return;

        mpb ??= new MaterialPropertyBlock();
        Color c = ColorActual();
        if (flash > 0f) c = Color.Lerp(c, Color.white, flash);

        rend.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, c);
        rend.SetPropertyBlock(mpb);
    }

    // Ráfaga de partículas del color del bloque (prefab que crea Roguelike > Crear efectos y materiales)
    void LanzarParticulas(Color color)
    {
        if (!prefabBuscado)
        {
            prefabBuscado = true;
            prefabRomper = Resources.Load<GameObject>("Efectos/RompeBloque");
        }
        if (prefabRomper == null) return;

        var fx = Instantiate(prefabRomper, transform.position, Quaternion.identity);
        fx.transform.localScale = Vector3.one * escalaBase.x;

        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.startColor = color;
        }
        Destroy(fx, 1.5f);
    }
}