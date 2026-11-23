#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Va en Assets/Editor. Menú: Roguelike > Crear menú principal (o Assets > Create > Roguelike).
// Ábrelo con tu ESCENA DE JUEGO abierta: crea Assets/Scenes/MenuPrincipal.unity con el título,
// los botones y los ajustes ya conectados, y deja las dos escenas en Build Settings
// (menú primero, tu escena de juego después).
public static class MenuSetup
{
    const string CarpetaEscenas = "Assets/Scenes";
    const string RutaMenu = "Assets/Scenes/MenuPrincipal.unity";

    [MenuItem("Assets/Create/Roguelike/Crear menú principal")]
    static void CrearDesdeAssets() => Crear();

    [MenuItem("Roguelike/Crear menú principal")]
    public static void Crear()
    {
        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog("Falta TextMeshPro",
                "Importa los recursos con Window > TextMeshPro > Import TMP Essential Resources y vuelve a intentarlo.",
                "Entendido");
            return;
        }

        Scene actual = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(actual.path))
        {
            EditorUtility.DisplayDialog("Guarda tu escena",
                "Guarda primero tu escena de juego (File > Save) y vuelve a ejecutarlo con ella abierta.", "Entendido");
            return;
        }
        if (actual.path == RutaMenu)
        {
            EditorUtility.DisplayDialog("Abre tu escena de juego",
                "Ahora mismo está abierta la escena del menú. Abre tu escena de juego y vuelve a ejecutarlo.", "Entendido");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string rutaJuego = actual.path;
        string nombreJuego = Path.GetFileNameWithoutExtension(rutaJuego);

        if (!AssetDatabase.IsValidFolder(CarpetaEscenas)) AssetDatabase.CreateFolder("Assets", "Scenes");

        Scene menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ConstruirEscena(nombreJuego);
        EditorSceneManager.SaveScene(menu, RutaMenu);

        // Build Settings: menú en la posición 0 y el juego en la 1
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        lista.RemoveAll(s => s.path == RutaMenu || s.path == rutaJuego);
        lista.Insert(0, new EditorBuildSettingsScene(rutaJuego, true));
        lista.Insert(0, new EditorBuildSettingsScene(RutaMenu, true));
        EditorBuildSettings.scenes = lista.ToArray();

        Debug.Log($"Menú creado en {RutaMenu}. Escena de juego: '{nombreJuego}'. Ambas están en Build Settings.");
        EditorUtility.DisplayDialog("Menú creado",
            $"Escena: {RutaMenu}\nEl botón Jugar carga '{nombreJuego}'.\n\nDale Play para probarlo.", "OK");
    }

    // ---------- Escena ----------

    static void ConstruirEscena(string nombreEscenaJuego)
    {
        var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var fondoSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // Cámara con el fondo animado
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.08f, 0.2f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();

        var fondo = camGo.AddComponent<FondoTablero>();
        var soFondo = new SerializedObject(fondo);
        soFondo.FindProperty("cam").objectReferenceValue = cam;
        soFondo.FindProperty("paletaPorNivel").boolValue = false;
        soFondo.FindProperty("indicePaleta").intValue = 0;
        soFondo.ApplyModifiedPropertiesWithoutUndo();

        // Canvas
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Panel principal
        RectTransform principal = Rect("PanelPrincipal", canvasGo.transform);
        Estirar(principal);

        Texto(principal, "Titulo", "BOLAS\nROGUELIKE", 130f, Color.white, new Vector2(0f, 500f), new Vector2(980f, 340f));
        TMP_Text mejor = Texto(principal, "MejorNivel", "", 44f, new Color(0.8f, 0.9f, 1f, 0.9f),
            new Vector2(0f, 270f), new Vector2(900f, 70f), FontStyles.Normal);

        Button jugar = Boton(principal, "BotonJugar", "JUGAR", new Vector2(0f, 60f), new Vector2(640f, 170f),
            new Color(0.25f, 0.8f, 0.55f), 70f, sprite);
        Button ajustesBtn = Boton(principal, "BotonAjustes", "AJUSTES", new Vector2(0f, -150f), new Vector2(640f, 130f),
            new Color(0.3f, 0.5f, 0.9f), 52f, sprite);
        Button salir = Boton(principal, "BotonSalir", "SALIR", new Vector2(0f, -310f), new Vector2(640f, 130f),
            new Color(0.85f, 0.35f, 0.4f), 52f, sprite);

        // Panel de ajustes (empieza oculto)
        RectTransform ajustes = Rect("PanelAjustes", canvasGo.transform);
        Estirar(ajustes);
        ajustes.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        RectTransform caja = Rect("Caja", ajustes);
        caja.anchorMin = caja.anchorMax = new Vector2(0.5f, 0.5f);
        caja.sizeDelta = new Vector2(860f, 620f);
        var imgCaja = caja.gameObject.AddComponent<Image>();
        imgCaja.sprite = sprite;
        imgCaja.type = Image.Type.Sliced;
        imgCaja.color = new Color(0.12f, 0.14f, 0.24f, 0.98f);

        Texto(caja, "TituloAjustes", "AJUSTES", 70f, Color.white, new Vector2(0f, 220f), new Vector2(700f, 100f));
        Texto(caja, "EtiquetaVolumen", "Volumen", 46f, new Color(0.85f, 0.9f, 1f), new Vector2(0f, 90f),
            new Vector2(700f, 70f), FontStyles.Normal);

        var recursos = new DefaultControls.Resources { standard = sprite, background = fondoSprite, knob = knob };
        GameObject sliderGo = DefaultControls.CreateSlider(recursos);
        sliderGo.name = "SliderVolumen";
        sliderGo.transform.SetParent(caja, false);
        var srt = (RectTransform)sliderGo.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = new Vector2(0f, -10f);
        srt.sizeDelta = new Vector2(620f, 60f);
        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        Transform relleno = sliderGo.transform.Find("Fill Area/Fill");
        if (relleno != null) relleno.GetComponent<Image>().color = new Color(0.25f, 0.8f, 0.55f);

        Button volver = Boton(caja, "BotonVolver", "VOLVER", new Vector2(0f, -190f), new Vector2(520f, 130f),
            new Color(0.3f, 0.5f, 0.9f), 52f, sprite);
        ajustes.gameObject.SetActive(false);

        // Script del menú, con todos los campos asignados
        var menuGo = new GameObject("Menu");
        var menu = menuGo.AddComponent<MenuPrincipal>();
        var so = new SerializedObject(menu);
        so.FindProperty("escenaJuego").stringValue = nombreEscenaJuego;
        so.FindProperty("panelPrincipal").objectReferenceValue = principal.gameObject;
        so.FindProperty("panelAjustes").objectReferenceValue = ajustes.gameObject;
        so.FindProperty("botonJugar").objectReferenceValue = jugar;
        so.FindProperty("botonAjustes").objectReferenceValue = ajustesBtn;
        so.FindProperty("botonSalir").objectReferenceValue = salir;
        so.FindProperty("botonVolver").objectReferenceValue = volver;
        so.FindProperty("textoMejorNivel").objectReferenceValue = mejor;
        so.FindProperty("sliderVolumen").objectReferenceValue = slider;
        so.ApplyModifiedPropertiesWithoutUndo();

        // EventSystem (con el módulo del Input System si está instalado)
        var es = new GameObject("EventSystem", typeof(EventSystem));
        Type modulo = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (modulo != null) es.AddComponent(modulo);
        else es.AddComponent<StandaloneInputModule>();
    }

    // ---------- Ayudas para crear UI ----------

    static RectTransform Rect(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return (RectTransform)go.transform;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TMP_Text Texto(Transform padre, string nombre, string contenido, float tamano, Color color,
        Vector2 posicion, Vector2 dimensiones, FontStyles estilo = FontStyles.Bold)
    {
        RectTransform rt = Rect(nombre, padre);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = contenido;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = estilo;
        t.raycastTarget = false;

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = dimensiones;
        return t;
    }

    static Button Boton(Transform padre, string nombre, string texto, Vector2 posicion, Vector2 dimensiones,
        Color colorFondo, float tamanoTexto, Sprite sprite)
    {
        RectTransform rt = Rect(nombre, padre);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = dimensiones;

        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = colorFondo;

        var boton = rt.gameObject.AddComponent<Button>();
        boton.targetGraphic = img;
        var colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colores.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colores.selectedColor = Color.white;
        colores.fadeDuration = 0.08f;
        boton.colors = colores;

        TMP_Text etiqueta = Texto(rt, "Texto", texto, tamanoTexto, Color.white, Vector2.zero, Vector2.zero);
        Estirar((RectTransform)etiqueta.transform);
        return boton;
    }
}
#endif