using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Transición con fundido a negro entre escenas. Se crea sola la primera vez que se usa:
// FundidoEscena.Cargar("NombreDeEscena");
public class FundidoEscena : MonoBehaviour
{
    const float Duracion = 0.35f;

    static FundidoEscena instancia;

    CanvasGroup grupo;
    bool cargando;

    public static void Cargar(string escena)
    {
        var fundido = Obtener();
        fundido.StartCoroutine(fundido.Rutina(escena));
    }

    static FundidoEscena Obtener()
    {
        if (instancia != null) return instancia;

        var go = new GameObject("FundidoEscena");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<FundidoEscena>();
        instancia.Construir();
        return instancia;
    }

    void Construir()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        gameObject.AddComponent<GraphicRaycaster>();

        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;   // solo bloquea clics mientras dura la transición

        var negro = new GameObject("Negro", typeof(RectTransform), typeof(Image));
        negro.transform.SetParent(transform, false);
        var rt = (RectTransform)negro.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        negro.GetComponent<Image>().color = Color.black;
    }

    IEnumerator Rutina(string escena)
    {
        if (cargando) yield break;
        cargando = true;
        grupo.blocksRaycasts = true;

        yield return Fundir(grupo.alpha, 1f);

        var operacion = SceneManager.LoadSceneAsync(escena);
        while (operacion != null && !operacion.isDone) yield return null;
        yield return null; // un frame para que la escena nueva termine de iniciar

        Time.timeScale = 1f; // por si se cargó en medio de una cámara lenta
        yield return Fundir(1f, 0f);

        grupo.blocksRaycasts = false;
        cargando = false;
    }

    // Usa tiempo sin escalar para funcionar aunque el juego esté en cámara lenta
    IEnumerator Fundir(float desde, float hasta)
    {
        float t = 0f;
        while (t < Duracion)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, t / Duracion);
            yield return null;
        }
        grupo.alpha = hasta;
    }
}