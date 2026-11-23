using System;
using UnityEngine;

public enum Sonido { Clic, Lanzar, Rebote, GolpeBloque, RompeBloque, Explosion, Victoria, Derrota }

// Efectos de sonido sin archivos de audio: se generan por código la primera vez que se usan.
// Uso: Sonidos.Reproducir(Sonido.Clic);
// Para poner los tuyos, guarda clips en Assets/Resources/Audio con el nombre del sonido
// (Clic, Lanzar, Rebote, GolpeBloque, RompeBloque, Explosion, Victoria, Derrota) y reemplazan a los generados.
public static class Sonidos
{
    const int Tasa = 44100;
    const int CantidadFuentes = 12;
    const float IntervaloMinimo = 0.03f;   // evita que el mismo sonido se apile decenas de veces

    static readonly int Total = Enum.GetValues(typeof(Sonido)).Length;
    static readonly AudioClip[] clips = new AudioClip[Total];
    static readonly float[] ultimaVez = new float[Total];
    static AudioSource[] fuentes;
    static int siguiente;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        Array.Clear(clips, 0, clips.Length);
        Array.Clear(ultimaVez, 0, ultimaVez.Length);
        fuentes = null;
    }

    public static void Reproducir(Sonido sonido, float volumen = 1f, float pitch = 1f)
    {
        int i = (int)sonido;
        float ahora = Time.unscaledTime;
        if (ahora - ultimaVez[i] < IntervaloMinimo) return;
        ultimaVez[i] = ahora;

        AudioClip clip = Obtener(sonido);
        if (clip == null) return;

        AudioSource fuente = SiguienteFuente();
        fuente.clip = clip;
        fuente.volume = Mathf.Clamp01(volumen);
        fuente.pitch = pitch;
        fuente.Play();
    }

    static AudioSource SiguienteFuente()
    {
        if (fuentes == null || fuentes.Length == 0 || fuentes[0] == null)
        {
            var go = new GameObject("Sonidos");
            UnityEngine.Object.DontDestroyOnLoad(go);

            fuentes = new AudioSource[CantidadFuentes];
            for (int i = 0; i < fuentes.Length; i++)
            {
                fuentes[i] = go.AddComponent<AudioSource>();
                fuentes[i].playOnAwake = false;
                fuentes[i].spatialBlend = 0f;
            }
        }

        siguiente = (siguiente + 1) % fuentes.Length;
        return fuentes[siguiente];
    }

    static AudioClip Obtener(Sonido sonido)
    {
        int i = (int)sonido;
        if (clips[i] != null) return clips[i];

        var propio = Resources.Load<AudioClip>("Audio/" + sonido);
        clips[i] = propio != null ? propio : Sintetizar(sonido);
        return clips[i];
    }

    // ---------- Síntesis ----------

    static AudioClip Sintetizar(Sonido sonido)
    {
        switch (sonido)
        {
            case Sonido.Clic:
                return Crear("Clic", 0.06f, Tono(1000f, 700f, 0.06f, 0.5f, 30f, 2));

            case Sonido.Lanzar:
                return Crear("Lanzar", 0.16f, Tono(220f, 640f, 0.16f, 0.6f, 8f, 0));

            case Sonido.Rebote:
                return Crear("Rebote", 0.08f, Tono(520f, 380f, 0.08f, 0.5f, 30f, 0));

            case Sonido.GolpeBloque:
                return Crear("GolpeBloque", 0.07f, Tono(760f, 560f, 0.07f, 0.5f, 35f, 2));

            case Sonido.RompeBloque:
                return Crear("RompeBloque", 0.16f,
                    Tono(900f, 180f, 0.16f, 0.4f, 20f, 1),
                    Ruido(28f, 0.2f, 0.6f));

            case Sonido.Explosion:
                return Crear("Explosion", 0.7f,
                    Ruido(5f, 0.88f, 2.4f),
                    Tono(110f, 35f, 0.7f, 0.9f, 4f, 0));

            case Sonido.Victoria:
                return Crear("Victoria", 0.9f,
                    Retardo(Tono(523.25f, 523.25f, 0.5f, 0.5f, 6f, 0), 0f),
                    Retardo(Tono(659.25f, 659.25f, 0.5f, 0.5f, 6f, 0), 0.12f),
                    Retardo(Tono(783.99f, 783.99f, 0.5f, 0.5f, 6f, 0), 0.24f),
                    Retardo(Tono(1046.5f, 1046.5f, 0.5f, 0.5f, 5f, 0), 0.36f));

            default: // Derrota
                return Crear("Derrota", 0.8f,
                    Tono(330f, 110f, 0.7f, 0.4f, 3f, 1),
                    Retardo(Tono(247f, 82f, 0.6f, 0.4f, 3f, 1), 0.15f));
        }
    }

    // Suma las capas y las convierte en un clip mono
    static AudioClip Crear(string nombre, float duracion, params Func<float, float>[] capas)
    {
        int n = Mathf.CeilToInt(duracion * Tasa);
        var datos = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Tasa;
            float s = 0f;
            for (int c = 0; c < capas.Length; c++) s += capas[c](t);

            float fin = Mathf.Clamp01((duracion - t) / 0.01f); // fundido final: sin clics al terminar
            datos[i] = Mathf.Clamp(s * fin, -1f, 1f);
        }

        var clip = AudioClip.Create(nombre, n, 1, Tasa, false);
        clip.SetData(datos, 0);
        return clip;
    }

    // Tono con barrido de frecuencia. forma: 0 = seno, 1 = cuadrada, 2 = triangular
    static Func<float, float> Tono(float f0, float f1, float dur, float vol, float decay, int forma)
    {
        float fase = 0f;
        return t =>
        {
            float f = Mathf.Lerp(f0, f1, Mathf.Clamp01(t / dur));
            fase += 2f * Mathf.PI * f / Tasa;
            float env = Mathf.Exp(-t * decay) * Mathf.Min(1f, t / 0.003f);

            float s;
            switch (forma)
            {
                case 1: s = Mathf.Sin(fase) >= 0f ? 0.45f : -0.45f; break;
                case 2: s = Mathf.PingPong(fase / Mathf.PI, 1f) * 2f - 1f; break;
                default: s = Mathf.Sin(fase); break;
            }
            return s * env * vol;
        };
    }

    // Ruido blanco con filtro paso bajo: suavizado cercano a 1 = sonido grave y sordo
    static Func<float, float> Ruido(float decay, float suavizado, float vol)
    {
        var rng = new System.Random(7);
        float y = 0f;
        float a = 1f - Mathf.Clamp01(suavizado);
        return t =>
        {
            float x = (float)(rng.NextDouble() * 2.0 - 1.0);
            y += a * (x - y);
            return y * Mathf.Exp(-t * decay) * vol;
        };
    }

    static Func<float, float> Retardo(Func<float, float> f, float inicio)
        => t => t < inicio ? 0f : f(t - inicio);
}