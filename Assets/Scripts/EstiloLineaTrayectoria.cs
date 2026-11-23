using UnityEngine;

// Ponlo en el mismo objeto que el LineRenderer del Launcher.
// Cambia solo el aspecto (material, ancho, color y animación); la forma de la línea la sigue
// calculando el Launcher. La línea rosada era un LineRenderer sin material.
[RequireComponent(typeof(LineRenderer))]
public class EstiloLineaTrayectoria : MonoBehaviour
{
    [Tooltip("Vacío = usa Resources/Efectos/Mat_Linea (lo crea Roguelike > Crear efectos y materiales)")]
    [SerializeField] Material material;

    [Header("Forma")]
    [SerializeField, Min(0.01f)] float ancho = 0.18f;
    [Tooltip("Ancho al final de la línea respecto al inicio (0.4 = se afina hasta el 40%)")]
    [SerializeField, Range(0f, 1f)] float anchoFinal = 0.4f;
    [Tooltip("Distancia entre puntos, en múltiplos del ancho (2 = un punto de ancho, uno de espacio)")]
    [SerializeField, Min(1f)] float separacion = 2f;

    [Header("Color")]
    [SerializeField] Color colorInicio = new Color(0.65f, 0.95f, 1f, 1f);
    [SerializeField] Color colorFinal = new Color(0.25f, 0.6f, 1f, 0f);

    [Header("Animación")]
    [Tooltip("Puntos que avanzan por segundo hacia afuera (0 = línea quieta)")]
    [SerializeField] float velocidad = 1.5f;

    LineRenderer line;
    Material instancia;
    Vector2 offset;

    void Awake()
    {
        line = GetComponent<LineRenderer>();

        if (material == null) material = Resources.Load<Material>("Efectos/Mat_Linea");
        if (material != null)
        {
            instancia = new Material(material);   // copia propia: no toca el asset al animarla
            line.sharedMaterial = instancia;
        }
        else
        {
            Debug.LogWarning("EstiloLineaTrayectoria: no hay material. Ejecuta Roguelike > Crear efectos y materiales o asigna uno.", this);
        }

        line.textureMode = LineTextureMode.Tile;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 6;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        // Ancho que se afina hacia el final
        line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, anchoFinal));
        line.widthMultiplier = ancho;

        // Color que se desvanece hacia el final
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(colorInicio, 0f), new GradientColorKey(colorFinal, 1f) },
            new[] { new GradientAlphaKey(colorInicio.a, 0f), new GradientAlphaKey(colorFinal.a, 1f) });
        line.colorGradient = gradient;

        // El material repite el punto una vez por "separacion x ancho" unidades
        line.textureScale = new Vector2(1f / (ancho * separacion), 1f);
    }

    void Update()
    {
        if (instancia == null || velocidad == 0f) return;

        // Restar el offset hace que los puntos avancen hacia el final de la línea
        offset.x = (offset.x - velocidad * Time.deltaTime) % 1f;
        instancia.mainTextureOffset = offset;
    }

    void OnDestroy()
    {
        if (instancia != null) Destroy(instancia);
    }
}
