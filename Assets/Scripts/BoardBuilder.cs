using UnityEngine;

// Crea paredes laterales, techo y KillZone pegados a los bordes de la cámara ortográfica.
// Se recalcula al iniciar, así que se adapta a cualquier relación de aspecto (celulares distintos).
// Úsalo en un objeto vacío DEDICADO en (0,0,0) con escala (1,1,1):
// al construir, borra todos sus hijos y los vuelve a crear.
public class BoardBuilder : MonoBehaviour
{
    [SerializeField] Camera cam;

    [Header("Paredes")]
    [SerializeField, Min(0.1f)] float thickness = 1f;
    [SerializeField, Min(0.1f)] float depth = 2f;
    [Tooltip("Acerca las paredes laterales hacia el centro")]
    [SerializeField, Min(0f)] float sideInset = 0f;
    [Tooltip("Baja el techo (deja espacio para la UI superior)")]
    [SerializeField, Min(0f)] float topInset = 0f;
    [Tooltip("Opcional: si lo dejas vacío, las paredes son invisibles")]
    [SerializeField] Material wallMaterial;

    [Header("Zona de pérdida")]
    [SerializeField] bool buildKillZone = true;
    [Tooltip("Distancia bajo el borde inferior de la pantalla, para que la bola salga completa")]
    [SerializeField, Min(0f)] float killZoneOffset = 1f;

    void Awake() => Build();

    // También disponible con clic derecho en el componente, para verlo en el editor
    [ContextMenu("Construir tablero")]
    public void Build()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || !cam.orthographic)
        {
            Debug.LogWarning("BoardBuilder necesita una cámara ortográfica.", this);
            return;
        }

        Clear();

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;

        // Bordes internos del área de juego
        float left = c.x - halfW + sideInset;
        float right = c.x + halfW - sideInset;
        float top = c.y + halfH - topInset;
        float bottom = c.y - halfH;

        float midX = (left + right) * 0.5f;
        float midY = (top + bottom) * 0.5f;
        float width = right - left;
        float height = top - bottom;

        // Paredes: su cara interior queda justo en el borde del área
        CreateBox("Pared Izquierda", new Vector2(left - thickness * 0.5f, midY),
                  new Vector2(thickness, height + thickness * 2f), true);
        CreateBox("Pared Derecha", new Vector2(right + thickness * 0.5f, midY),
                  new Vector2(thickness, height + thickness * 2f), true);
        CreateBox("Techo", new Vector2(midX, top + thickness * 0.5f),
                  new Vector2(width + thickness * 2f, thickness), true);

        if (buildKillZone)
        {
            var kz = CreateBox("KillZone", new Vector2(midX, bottom - killZoneOffset - thickness * 0.5f),
                               new Vector2(width + thickness * 2f, thickness), false);
            kz.GetComponent<BoxCollider>().isTrigger = true;
            kz.AddComponent<KillZone>();
        }
    }

    GameObject CreateBox(string objName, Vector2 center, Vector2 size, bool useMaterial)
    {
        // El cubo primitivo ya trae BoxCollider, MeshFilter y MeshRenderer
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objName;
        go.transform.position = new Vector3(center.x, center.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, depth);
        go.transform.SetParent(transform, true);

        var r = go.GetComponent<MeshRenderer>();
        if (useMaterial && wallMaterial != null) r.sharedMaterial = wallMaterial;
        else r.enabled = false; // invisible, solo colisión

        return go;
    }

    void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                child.SetActive(false); // Destroy es diferido: así no colisiona mientras tanto
                Destroy(child);
            }
            else DestroyImmediate(child);
        }
    }

    // Rectángulo cian = área de juego, visible al seleccionar el objeto
    void OnDrawGizmosSelected()
    {
        var c = cam != null ? cam : Camera.main;
        if (c == null || !c.orthographic) return;

        float halfH = c.orthographicSize;
        float halfW = halfH * c.aspect;
        Vector3 p = c.transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(p.x, p.y - topInset * 0.5f, 0f),
                            new Vector3(halfW * 2f - sideInset * 2f, halfH * 2f - topInset, 0f));
    }
}
