using System.Collections;
using UnityEngine;

// Sonido, sacudida y cámara lenta cuando ganas o pierdes un nivel.
// Ponlo en cualquier objeto de la escena de juego (por ejemplo, el del LevelManager).
public class FeedbackNivel : MonoBehaviour
{
    [Header("Cámara lenta al ganar")]
    [SerializeField] bool camaraLentaAlGanar = true;
    [SerializeField, Range(0.05f, 1f)] float escalaTiempo = 0.3f;
    [SerializeField, Min(0.05f)] float duracionLenta = 0.5f;   // en segundos reales

    LevelManager nivel;

    void Start()
    {
        nivel = LevelManager.Instance;
        if (nivel == null)
        {
            Debug.LogWarning("FeedbackNivel: no hay LevelManager en la escena.", this);
            return;
        }
        nivel.OnWon.AddListener(AlGanar);
        nivel.OnLost.AddListener(AlPerder);
    }

    void OnDestroy()
    {
        if (nivel != null)
        {
            nivel.OnWon.RemoveListener(AlGanar);
            nivel.OnLost.RemoveListener(AlPerder);
        }
        Time.timeScale = 1f; // nunca dejar el juego en cámara lenta al cambiar de escena
    }

    void AlGanar()
    {
        Sonidos.Reproducir(Sonido.Victoria);
        ShakeCamara.Sacudir(0.12f, 0.3f);
        if (camaraLentaAlGanar) StartCoroutine(CamaraLenta());
    }

    void AlPerder()
    {
        Sonidos.Reproducir(Sonido.Derrota);
        ShakeCamara.Sacudir(0.1f, 0.3f);
    }

    IEnumerator CamaraLenta()
    {
        Time.timeScale = escalaTiempo;
        yield return new WaitForSecondsRealtime(duracionLenta);
        Time.timeScale = 1f;
    }
}