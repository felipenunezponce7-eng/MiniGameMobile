using UnityEngine;

// Ponlo en la cámara. Sacude la vista un instante: ShakeCamara.Sacudir(intensidad, duracion);
// Si no hay uno en la escena, la llamada no hace nada.
public class ShakeCamara : MonoBehaviour
{
    static ShakeCamara instancia;

    Vector3 posicionBase;
    float restante;
    float duracion = 0.01f;
    float intensidad;

    void Awake()
    {
        instancia = this;
        posicionBase = transform.localPosition;
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    public static void Sacudir(float fuerza, float segundos)
    {
        if (instancia == null) return;
        instancia.Iniciar(fuerza, segundos);
    }

    void Iniciar(float fuerza, float segundos)
    {
        // Si ya hay una sacudida, se queda la más fuerte
        if (fuerza < intensidad * (restante / duracion)) return;
        intensidad = fuerza;
        duracion = Mathf.Max(0.01f, segundos);
        restante = duracion;
    }

    void LateUpdate()
    {
        if (restante > 0f)
        {
            restante -= Time.unscaledDeltaTime;
            float k = Mathf.Max(0f, restante / duracion);
            Vector2 desplazamiento = Random.insideUnitCircle * (intensidad * k);
            transform.localPosition = posicionBase + new Vector3(desplazamiento.x, desplazamiento.y, 0f);
        }
        else if (transform.localPosition != posicionBase)
        {
            transform.localPosition = posicionBase;
        }
    }
}