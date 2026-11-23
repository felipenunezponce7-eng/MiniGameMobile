using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD sin configuración: crea solo su Canvas y sus textos (nivel, bloques restantes y bolas).
// Ponlo en cualquier objeto de la escena de juego. Usa el nombre MazoDeCartas (tu clase renombrada).
public class HudJuego : MonoBehaviour
{
    [SerializeField] MazoDeBolas mazo;
    [SerializeField] Color colorTexto = new Color(1f, 1f, 1f, 0.92f);
    [SerializeField, Min(10f)] float tamanoTexto = 46f;

    TMP_Text textoNivel;
    TMP_Text textoBloques;
    TMP_Text textoBolas;
    RectTransform zona;

    int ultimosBloques = -1;
    int ultimasBolas = -1;
    Vector2Int ultimaResolucion;
    Rect ultimaZonaSegura;

    void Awake()
    {
        if (mazo == null) mazo = FindFirstObjectByType<MazoDeBolas>();
        Construir();
    }

    void Construir()
    {
        var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Sin Graphic Raycaster: el HUD nunca bloquea los toques del lanzador
        var zonaGo = new GameObject("ZonaSegura", typeof(RectTransform));
        zonaGo.transform.SetParent(canvasGo.transform, false);
        zona = (RectTransform)zonaGo.transform;

        textoNivel = CrearTexto("Nivel", TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f), new Vector2(40f, -30f));
        textoBloques = CrearTexto("Bloques", TextAlignmentOptions.TopRight,
            new Vector2(1f, 1f), new Vector2(-40f, -30f));
        textoBolas = CrearTexto("Bolas", TextAlignmentOptions.Bottom,
            new Vector2(0.5f, 0f), new Vector2(0f, 40f));

        textoNivel.text = $"NIVEL {RunState.Level}";
        AplicarZonaSegura();
    }

    TMP_Text CrearTexto(string nombre, TextAlignmentOptions alineacion, Vector2 anclaje, Vector2 posicion)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(zona, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anclaje;
        rt.pivot = anclaje;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = new Vector2(500f, 90f);

        var t = go.GetComponent<TextMeshProUGUI>();
        t.fontSize = tamanoTexto;
        t.fontStyle = FontStyles.Bold;
        t.alignment = alineacion;
        t.color = colorTexto;
        t.raycastTarget = false;
        return t;
    }

    // LateUpdate: así corre después del lanzador y la cuenta de bolas ya está actualizada
    void LateUpdate()
    {
        if (Screen.width != ultimaResolucion.x || Screen.height != ultimaResolucion.y
            || Screen.safeArea != ultimaZonaSegura)
            AplicarZonaSegura();

        var nivel = LevelManager.Instance;
        int bloques = nivel != null ? nivel.BlocksLeft : 0;

        // Bolas por lanzar: las del mazo más la que está cargada en el lanzador (si no hay ninguna en juego)
        int bolas = 0;
        if (mazo != null && (nivel == null || nivel.State == LevelState.Playing))
            bolas = mazo.Remaining + (mazo.InFlight == 0 ? 1 : 0);

        if (bloques != ultimosBloques)
        {
            ultimosBloques = bloques;
            textoBloques.text = $"BLOQUES {bloques}";
        }
        if (bolas != ultimasBolas)
        {
            ultimasBolas = bolas;
            textoBolas.text = $"BOLAS {bolas}";
        }
    }

    // Mantiene los textos dentro de la zona segura (notch y barras del sistema)
    void AplicarZonaSegura()
    {
        ultimaResolucion = new Vector2Int(Screen.width, Screen.height);
        ultimaZonaSegura = Screen.safeArea;
        if (Screen.width == 0 || Screen.height == 0) return;

        Rect safe = Screen.safeArea;
        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        zona.anchorMin = min;
        zona.anchorMax = max;
        zona.offsetMin = Vector2.zero;
        zona.offsetMax = Vector2.zero;
    }
}