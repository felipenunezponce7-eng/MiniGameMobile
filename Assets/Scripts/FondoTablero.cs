using UnityEngine;

// Fondo con degradado y partículas suaves que flotan. Ponlo en la cámara (o en cualquier objeto).
// La paleta cambia cada 3 niveles de la run. Usa los materiales que crea
// Roguelike > Crear efectos y materiales; si faltan, solo cambia el color de fondo de la cámara.
public class FondoTablero : MonoBehaviour
{
    [System.Serializable]
    public struct Paleta
    {
        public Color arriba;
        public Color abajo;
        public Color particulas;
    }

    [SerializeField] Camera cam;

    [Header("Paleta")]
    [Tooltip("Cambia de paleta cada 3 niveles de la run (si no, usa Indice Paleta)")]
    [SerializeField] bool paletaPorNivel = true;
    [SerializeField] int indicePaleta;
    [Tooltip("Vacío = usa 5 paletas por defecto")]
    [SerializeField] Paleta[] paletas;

    [Header("Partículas")]
    [SerializeField] bool particulasAmbiente = true;
    [SerializeField, Min(0f)] float particulasPorSegundo = 6f;

    Transform fondo;
    ParticleSystem particulas;
    Material material;
    Texture2D textura;
    float ultimoAspecto;
    float ultimoTamano;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || !cam.orthographic)
        {
            Debug.LogWarning("FondoTablero necesita una cámara ortográfica.", this);
            enabled = false;
            return;
        }

        if (paletas == null || paletas.Length == 0) paletas = PaletasPorDefecto();

        int indice = paletaPorNivel ? ((RunState.Level - 1) / 3) % paletas.Length : indicePaleta;
        Paleta p = paletas[Mathf.Clamp(indice, 0, paletas.Length - 1)];

        // Respaldo si faltan los materiales: al menos un color liso
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = p.abajo;

        CrearDegradado(p);
        if (particulasAmbiente) CrearParticulas(p);
        AjustarTamano();
    }

    void LateUpdate()
    {
        if (cam.aspect != ultimoAspecto || cam.orthographicSize != ultimoTamano) AjustarTamano();
    }

    void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (textura != null) Destroy(textura);
    }

    void CrearDegradado(Paleta p)
    {
        var plantilla = Resources.Load<Material>("Efectos/Mat_Fondo");
        if (plantilla == null)
        {
            Debug.LogWarning("FondoTablero: falta Resources/Efectos/Mat_Fondo. Ejecuta Roguelike > Crear efectos y materiales.", this);
            return;
        }

        const int alto = 64;
        textura = new Texture2D(1, alto, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        for (int y = 0; y < alto; y++)
            textura.SetPixel(0, y, Color.Lerp(p.abajo, p.arriba, y / (alto - 1f)));
        textura.Apply();

        material = new Material(plantilla) { mainTexture = textura };

        // Un quad detrás de todo, pegado a la cámara (así también queda quieto con el screen shake)
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Fondo";
        Destroy(go.GetComponent<Collider>());

        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        fondo = go.transform;
        fondo.SetParent(cam.transform, false);
        fondo.localPosition = new Vector3(0f, 0f, 18f);
        fondo.localRotation = Quaternion.identity;
    }

    void CrearParticulas(Paleta p)
    {
        var mat = Resources.Load<Material>("Efectos/Mat_Fuego");
        if (mat == null) return;

        var go = new GameObject("Particulas Ambiente");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 17.5f);

        particulas = go.AddComponent<ParticleSystem>();
        var rend = go.GetComponent<ParticleSystemRenderer>();
        particulas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particulas.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
        main.startColor = p.particulas;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 80;

        var emision = particulas.emission;
        emision.rateOverTime = particulasPorSegundo;

        var forma = particulas.shape;
        forma.shapeType = ParticleSystemShapeType.Box;

        // Deriva lenta hacia arriba (los tres ejes en el mismo modo, como exige Unity)
        var velocidad = particulas.velocityOverLifetime;
        velocidad.enabled = true;
        velocidad.space = ParticleSystemSimulationSpace.Local;
        velocidad.x = 0f;
        velocidad.y = 0.15f;
        velocidad.z = 0f;

        // Aparecen y se desvanecen suavemente
        var gradiente = new Gradient();
        gradiente.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        var color = particulas.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(gradiente);

        rend.sharedMaterial = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        particulas.Play();
    }

    // Cubre siempre toda la vista, aunque cambie el tamaño de la pantalla
    void AjustarTamano()
    {
        ultimoAspecto = cam.aspect;
        ultimoTamano = cam.orthographicSize;

        float alto = cam.orthographicSize * 2f;
        float ancho = alto * cam.aspect;

        if (fondo != null) fondo.localScale = new Vector3(ancho, alto, 1f);
        if (particulas != null)
        {
            var forma = particulas.shape;
            forma.scale = new Vector3(ancho, alto, 0.5f);
        }
    }

    static Paleta[] PaletasPorDefecto()
    {
        return new[]
        {
            Crear(new Color(0.05f, 0.08f, 0.20f), new Color(0.12f, 0.25f, 0.45f), new Color(0.6f, 0.8f, 1.0f, 0.35f)),   // noche azul
            Crear(new Color(0.12f, 0.05f, 0.22f), new Color(0.35f, 0.12f, 0.45f), new Color(0.9f, 0.6f, 1.0f, 0.35f)),   // violeta
            Crear(new Color(0.03f, 0.14f, 0.15f), new Color(0.08f, 0.38f, 0.35f), new Color(0.6f, 1.0f, 0.9f, 0.35f)),   // mar
            Crear(new Color(0.20f, 0.06f, 0.12f), new Color(0.55f, 0.22f, 0.20f), new Color(1.0f, 0.8f, 0.6f, 0.35f)),   // atardecer
            Crear(new Color(0.06f, 0.06f, 0.09f), new Color(0.18f, 0.18f, 0.25f), new Color(0.8f, 0.8f, 1.0f, 0.30f))    // carbón
        };
    }

    static Paleta Crear(Color arriba, Color abajo, Color particulas)
        => new Paleta { arriba = arriba, abajo = abajo, particulas = particulas };
}