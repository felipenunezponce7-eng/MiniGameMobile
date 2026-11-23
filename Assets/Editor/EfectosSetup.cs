#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Va en una carpeta llamada Editor (Assets/Editor).
// Menú: Roguelike > Crear efectos y materiales (arriba, o en Assets > Create > Roguelike)
// Crea texturas, materiales y los prefabs Fuego, Explosion y RompeBloque en Assets/Resources/Efectos,
// más el material del fondo, y asigna Fuego/Explosion a los assets de bolas con Effect = Fire / Bomb
// que tengan el campo vacío.
// Si lo ejecutas de nuevo, regenera todo con los valores originales.
public static class EfectosSetup
{
    const string Raiz = "Assets/Resources";
    const string Carpeta = "Assets/Resources/Efectos";

    // También aparece en Assets > Create > Roguelike (y con clic derecho en la ventana Project)
    [MenuItem("Assets/Create/Roguelike/Crear efectos y materiales")]
    static void CrearDesdeAssets() => Crear();

    // Y como menú propio en la barra superior: Roguelike > Crear efectos y materiales
    [MenuItem("Roguelike/Crear efectos y materiales")]
    public static void Crear()
    {
        if (!AssetDatabase.IsValidFolder(Raiz)) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder(Raiz, "Efectos");

        Texture2D glow = CrearTextura("Glow", 64, 0f);      // degradado suave, para partículas y anillo
        Texture2D punto = CrearTextura("Punto", 64, 0.6f);  // punto de borde definido, para la línea
        Material matFuego = CrearMaterial("Mat_Fuego", glow, true);
        CrearMaterial("Mat_Linea", punto, false);
        CrearMaterialFondo();

        GameObject fuego = CrearFuego(matFuego);
        GameObject explosion = CrearExplosion(matFuego);
        CrearRompeBloque(matFuego);
        int asignados = AsignarAEfectos(fuego, explosion);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Efectos creados en {Carpeta}. Asignados automáticamente a {asignados} asset(s) de bola.");
    }

    // ---------- Texturas y materiales ----------

    static Texture2D CrearTextura(string nombre, int size, float dureza)
    {
        string path = $"{Carpeta}/{nombre}.png";
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r; // 0 centro, 1 borde
                float a = Mathf.Clamp01((1f - d) / (1f - dureza));
                a = a * a * (3f - 2f * a); // smoothstep
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Repeat; // necesario para repetir el punto en la línea
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material CrearMaterial(string nombre, Texture2D tex, bool aditivo)
    {
        // Shader de partículas de URP; si no existe, uno básico que funciona en cualquier pipeline
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        bool urp = shader != null;
        if (!urp) shader = Shader.Find("Sprites/Default");

        var mat = new Material(shader) { name = nombre };
        mat.mainTexture = tex;

        if (urp)
        {
            mat.SetFloat("_Surface", 1f);                                   // Transparent
            mat.SetFloat("_Blend", aditivo ? 2f : 0f);                      // 2 = Additive, 0 = Alpha
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)(aditivo ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)(aditivo ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetShaderPassEnabled("ShadowCaster", false);
        }

        return GuardarMaterial(mat, $"{Carpeta}/{nombre}.mat");
    }

    // Material opaco sin iluminación para el fondo con degradado (la textura se pone al jugar)
    static Material CrearMaterialFondo()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        var mat = new Material(shader) { name = "Mat_Fondo" };
        return GuardarMaterial(mat, $"{Carpeta}/Mat_Fondo.mat");
    }

    // Si ya existía, se reemplaza su contenido para no romper las referencias
    static Material GuardarMaterial(Material mat, string path)
    {
        var existente = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existente != null)
        {
            EditorUtility.CopySerialized(mat, existente);
            Object.DestroyImmediate(mat);
            return existente;
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ---------- Prefabs ----------

    static GameObject CrearFuego(Material mat)
    {
        var go = new GameObject("Fuego");
        var ps = go.AddComponent<ParticleSystem>();
        var rend = go.GetComponent<ParticleSystemRenderer>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        main.startColor = Color.white;                       // el color lo da Color over Lifetime
        main.simulationSpace = ParticleSystemSimulationSpace.World;   // deja estela
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.gravityModifier = -0.15f;                       // las llamas suben un poco
        main.maxParticles = 120;

        var emission = ps.emission;
        emission.rateOverTime = 55f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        ConfigurarColorYTamano(ps,
            Gradiente(new Color(1f, 0.9f, 0.35f), new Color(1f, 0.4f, 0.05f), new Color(0.5f, 0.05f, 0f)));

        ConfigurarRenderer(rend, mat);
        return GuardarPrefab(go, "Fuego");
    }

    static GameObject CrearExplosion(Material mat)
    {
        var root = new GameObject("Explosion");

        // El prefab se dibuja con radio 1: la Bola lo escala al radio real de la bomba
        CrearRafaga(root.transform, "Fogonazo", mat, 28, 0.3f, 0.5f, 1.2f, 2.2f, 0.45f, 0.9f,
            Gradiente(new Color(1f, 0.95f, 0.6f), new Color(1f, 0.45f, 0.1f), new Color(0.4f, 0.05f, 0f)));
        CrearRafaga(root.transform, "Chispas", mat, 24, 0.2f, 0.4f, 1.5f, 3f, 0.08f, 0.16f,
            Gradiente(new Color(1f, 1f, 0.8f), new Color(1f, 0.7f, 0.2f), new Color(1f, 0.3f, 0f)));

        // Anillo: marca el radio real del daño
        var anilloGo = new GameObject("Anillo");
        anilloGo.transform.SetParent(root.transform, false);
        var lr = anilloGo.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        const int puntos = 48;
        lr.positionCount = puntos;
        for (int i = 0; i < puntos; i++)
        {
            float a = i * Mathf.PI * 2f / puntos;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));
        }
        lr.widthMultiplier = 0.1f;
        lr.textureMode = LineTextureMode.Stretch;
        lr.numCapVertices = 4;
        lr.sharedMaterial = mat;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.startColor = new Color(1f, 0.55f, 0.1f, 1f);
        lr.endColor = new Color(1f, 0.55f, 0.1f, 1f);

        var efecto = root.AddComponent<EfectoExplosion>();
        efecto.anillo = lr;

        return GuardarPrefab(root, "Explosion");
    }

    // Ráfaga de partículas al romperse un bloque. El bloque la escala a su tamaño y le pone su color.
    static GameObject CrearRompeBloque(Material mat)
    {
        var go = new GameObject("RompeBloque");
        var ps = go.AddComponent<ParticleSystem>();
        var rend = go.GetComponent<ParticleSystemRenderer>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.startColor = Color.white;                       // el bloque lo cambia por su color
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.gravityModifier = 0.8f;
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)16) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        ConfigurarColorYTamano(ps, Gradiente(Color.white, Color.white, Color.white));
        ConfigurarRenderer(rend, mat);
        return GuardarPrefab(go, "RompeBloque");
    }

    static void CrearRafaga(Transform padre, string nombre, Material mat, int cantidad,
        float vidaMin, float vidaMax, float velMin, float velMax, float tamMin, float tamMax, Gradient colores)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var ps = go.AddComponent<ParticleSystem>();
        var rend = go.GetComponent<ParticleSystemRenderer>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vidaMin, vidaMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velMin, velMax);
        main.startSize = new ParticleSystem.MinMaxCurve(tamMin, tamMax);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;       // escala con el radio
        main.maxParticles = cantidad * 2;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)cantidad) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        ConfigurarColorYTamano(ps, colores);
        ConfigurarRenderer(rend, mat);
    }

    // ---------- Utilidades ----------

    static Gradient Gradiente(Color a, Color b, Color c)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.5f), new GradientColorKey(c, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f)
            });
        return g;
    }

    static void ConfigurarColorYTamano(ParticleSystem ps, Gradient colores)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(colores);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));
    }

    static void ConfigurarRenderer(ParticleSystemRenderer rend, Material mat)
    {
        rend.sharedMaterial = mat;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows = false;
    }

    static GameObject GuardarPrefab(GameObject go, string nombre)
    {
        string path = $"{Carpeta}/{nombre}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // Busca assets de bola por sus campos (sin depender del nombre de la clase)
    // y rellena los efectos vacíos de los que tengan Effect = Fire o Bomb.
    static int AsignarAEfectos(GameObject fuego, GameObject explosion)
    {
        int asignados = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (obj == null) continue;

            var so = new SerializedObject(obj);
            var efecto = so.FindProperty("effect");
            if (efecto == null || efecto.propertyType != SerializedPropertyType.Enum) continue;
            if (efecto.enumValueIndex < 0 || efecto.enumValueIndex >= efecto.enumNames.Length) continue;

            string nombreEfecto = efecto.enumNames[efecto.enumValueIndex];
            SerializedProperty destino = null;
            GameObject prefab = null;

            if (nombreEfecto == "Fire")
            {
                destino = so.FindProperty("trailEffectPrefab");
                prefab = fuego;
            }
            else if (nombreEfecto == "Bomb")
            {
                destino = so.FindProperty("explosionEffectPrefab");
                prefab = explosion;
            }

            if (destino == null || destino.objectReferenceValue != null) continue;

            destino.objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(obj);
            asignados++;
        }

        return asignados;
    }
}
#endif