using UnityEngine;

// Va en el prefab "Explosion". Anima el anillo que marca el radio real del daño en área.
// La Bola escala todo el prefab al radio de la bomba, así que el anillo (de radio 1) coincide con el daño.
public class EfectoExplosion : MonoBehaviour
{
    public LineRenderer anillo;
    [Min(0.05f)] public float duracionExpansion = 0.25f;
    [Min(0.05f)] public float duracionFade = 0.6f;

    float t;
    Color colorBase = Color.white;

    void Awake()
    {
        if (anillo != null) colorBase = anillo.startColor;
    }

    void Update()
    {
        if (anillo == null || !anillo.enabled) return;
        t += Time.deltaTime;

        // El anillo se expande rápido hasta el radio completo
        float k = Mathf.Clamp01(t / duracionExpansion);
        float ease = 1f - (1f - k) * (1f - k);
        anillo.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, ease);

        // y luego se desvanece
        float fade = Mathf.Clamp01(t / duracionFade);
        Color c = colorBase;
        c.a = colorBase.a * (1f - fade);
        anillo.startColor = c;
        anillo.endColor = c;

        if (fade >= 1f) anillo.enabled = false;
    }
}
