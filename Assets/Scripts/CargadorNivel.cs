using UnityEngine;
using System.Collections.Generic;

// Crea los bloques del nivel al iniciar la escena. Ponlo en un objeto vacío en (0,0,0), escala 1.
// Usa un NivelData hecho a mano si existe para ese nivel; si no, lo genera según RunState.Level.
// Reemplaza a los bloques que tenías puestos a mano en la escena (puedes borrarlos).
public class CargadorNivel : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Tu prefab de bloque (cubo con BoxCollider y el script Block). Vacío = crea cubos simples")]
    [SerializeField] Block bloquePrefab;
    [SerializeField] Camera cam;

    [Header("Niveles hechos a mano (opcional)")]
    [Tooltip("El elemento 0 se usa en el nivel 1, el 1 en el nivel 2, etc. Los niveles que falten se generan solos")]
    [SerializeField] NivelData[] nivelesFijos;

    [Header("Cuadrícula generada")]
    [SerializeField, Min(4)] int columnas = 9;
    [SerializeField, Min(3)] int filasMaximas = 14;

    [Header("Área de bloques (unidades de Unity)")]
    [Tooltip("Espacio libre arriba, donde está el lanzador")]
    [SerializeField, Min(0f)] float margenSuperior = 3.5f;
    [Tooltip("Espacio libre abajo, antes de la KillZone")]
    [SerializeField, Min(0f)] float margenInferior = 2.5f;
    [SerializeField, Min(0f)] float margenLateral = 0.5f;

    [Header("Aspecto")]
    [Tooltip("Tamaño del bloque respecto a su celda (menos de 1 deja un pequeño espacio entre bloques)")]
    [SerializeField, Range(0.5f, 1f)] float escalaBloque = 0.92f;
    [SerializeField, Min(0.1f)] float profundidad = 1f;
    [Tooltip("Los bloques aparecen escalonados fila por fila")]
    [SerializeField, Min(0f)] float retrasoPorFila = 0.04f;

    void Awake() => Construir();

    // También con clic derecho en el componente, para ver una vista previa en el editor
    [ContextMenu("Construir nivel (vista previa)")]
    public void Construir()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || !cam.orthographic)
        {
            Debug.LogError("CargadorNivel necesita una cámara ortográfica.", this);
            return;
        }

        Limpiar();

        float mitadAlto = cam.orthographicSize;
        float mitadAncho = mitadAlto * cam.aspect;
        Vector3 c = cam.transform.position;

        float izquierda = c.x - mitadAncho + margenLateral;
        float derecha = c.x + mitadAncho - margenLateral;
        float arriba = c.y + mitadAlto - margenSuperior;
        float abajo = c.y - mitadAlto + margenInferior;
        float ancho = derecha - izquierda;
        float alto = arriba - abajo;

        if (ancho <= 0f || alto <= 0f)
        {
            Debug.LogError("CargadorNivel: los márgenes no dejan espacio para bloques.", this);
            return;
        }

        int nivel = RunState.Level;
        NivelData fijo = ElegirFijo(nivel);

        int cols;
        int filas;
        List<BloqueDef> bloques;

        if (fijo != null)
        {
            cols = fijo.columnas;
            filas = fijo.filas;
            bloques = fijo.bloques;
        }
        else
        {
            // Columnas fijas: el nivel se ve igual en cualquier pantalla; las filas dependen del alto
            cols = columnas;
            float celdaAncho = ancho / cols;
            filas = Mathf.Clamp(Mathf.FloorToInt(alto / celdaAncho), 3, filasMaximas);
            bloques = GeneradorNiveles.Generar(nivel, cols, filas, RunState.Seed + nivel * 7919);
        }

        // Cuadrícula centrada en horizontal y pegada arriba
        float celda = Mathf.Min(ancho / cols, alto / filas);
        float origenX = izquierda + (ancho - celda * cols) * 0.5f;

        foreach (var def in bloques)
        {
            if (def.celda.x < 0 || def.celda.x >= cols || def.celda.y < 0 || def.celda.y >= filas) continue;

            var pos = new Vector3(
                origenX + (def.celda.x + 0.5f) * celda,
                arriba - (def.celda.y + 0.5f) * celda,
                0f);

            Block bloque = Crear(pos);
            bloque.transform.localScale = new Vector3(celda * escalaBloque, celda * escalaBloque, profundidad);
            bloque.Configurar(Mathf.Max(1, def.vida), def.celda.y * retrasoPorFila + def.celda.x * 0.005f);
        }
    }

    NivelData ElegirFijo(int nivel)
    {
        if (nivelesFijos == null) return null;
        int i = nivel - 1;
        return i >= 0 && i < nivelesFijos.Length ? nivelesFijos[i] : null;
    }

    Block Crear(Vector3 posicion)
    {
        if (bloquePrefab != null)
            return Instantiate(bloquePrefab, posicion, Quaternion.identity, transform);

        // Sin prefab: cubo simple con su BoxCollider y el script Block
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Bloque";
        go.transform.position = posicion;
        go.transform.SetParent(transform, true);
        return go.AddComponent<Block>();
    }

    void Limpiar()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var hijo = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                hijo.SetActive(false); // Destroy es diferido: así no cuenta como bloque este frame
                Destroy(hijo);
            }
            else DestroyImmediate(hijo);
        }
    }
}