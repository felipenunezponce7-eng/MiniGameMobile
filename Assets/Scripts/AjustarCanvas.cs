using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ponlo en el Canvas. Hace que la UI se estire según el tamaño de la pantalla (ventana,
// pantalla completa, cualquier celular) para que los botones queden donde se ven y se puedan presionar.
[RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
public class AjustarCanvas : MonoBehaviour
{
    [Header("Escalado")]
    [SerializeField] Vector2 resolucionReferencia = new Vector2(1080f, 1920f);
    [Tooltip("0 = se adapta al ancho, 1 = al alto, 0.5 = punto medio")]
    [SerializeField, Range(0f, 1f)] float coincidencia = 0.5f;

    [Header("Zona segura (opcional)")]
    [Tooltip("Contenedor hijo del Canvas donde pones los paneles. Evita el notch y las barras del sistema")]
    [SerializeField] RectTransform zonaSegura;

    Vector2Int ultimaResolucion;
    Rect ultimaZonaSegura;

    void Awake()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas.renderMode == RenderMode.WorldSpace)
            Debug.LogWarning("AjustarCanvas: el Canvas está en World Space y no se escala con la pantalla. Usa Screen Space - Overlay.", this);

        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = resolucionReferencia;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = coincidencia;

        // Sin Graphic Raycaster los botones no reciben clics; "Blocking Objects" debe estar en None
        // para que los colliders 3D (paredes) no bloqueen la UI
        if (!TryGetComponent(out GraphicRaycaster raycaster))
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

        if (FindFirstObjectByType<EventSystem>() == null)
            Debug.LogError("No hay EventSystem en la escena: los botones no funcionarán (GameObject > UI > Event System).", this);

        AplicarZonaSegura();
    }

    // Reaplica si cambia la resolución o la zona segura (pantalla completa, rotación...)
    void Update()
    {
        if (Screen.width != ultimaResolucion.x || Screen.height != ultimaResolucion.y
            || Screen.safeArea != ultimaZonaSegura)
            AplicarZonaSegura();
    }

    void AplicarZonaSegura()
    {
        ultimaResolucion = new Vector2Int(Screen.width, Screen.height);
        ultimaZonaSegura = Screen.safeArea;
        if (zonaSegura == null || Screen.width == 0 || Screen.height == 0) return;

        Rect safe = Screen.safeArea;
        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        zonaSegura.anchorMin = min;
        zonaSegura.anchorMax = max;
        zonaSegura.offsetMin = Vector2.zero;
        zonaSegura.offsetMax = Vector2.zero;
    }
}
