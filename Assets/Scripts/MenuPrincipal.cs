using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Controla la escena del menú. La escena la crea Roguelike > Crear menú principal,
// que además deja asignados todos estos campos.
public class MenuPrincipal : MonoBehaviour
{
    const string ClaveVolumen = "Volumen";

    [SerializeField] string escenaJuego = "Juego";

    [Header("Paneles")]
    [SerializeField] GameObject panelPrincipal;
    [SerializeField] GameObject panelAjustes;

    [Header("Botones")]
    [SerializeField] Button botonJugar;
    [SerializeField] Button botonAjustes;
    [SerializeField] Button botonSalir;
    [SerializeField] Button botonVolver;

    [Header("Textos y ajustes")]
    [SerializeField] TMP_Text textoMejorNivel;
    [SerializeField] Slider sliderVolumen;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.volume = PlayerPrefs.GetFloat(ClaveVolumen, 1f);

        // Los botones se conectan por código: no hace falta configurar On Click
        if (botonJugar != null) botonJugar.onClick.AddListener(Jugar);
        if (botonAjustes != null) botonAjustes.onClick.AddListener(AbrirAjustes);
        if (botonSalir != null) botonSalir.onClick.AddListener(Salir);
        if (botonVolver != null) botonVolver.onClick.AddListener(MostrarPrincipal);

        if (sliderVolumen != null)
        {
            sliderVolumen.SetValueWithoutNotify(AudioListener.volume);
            sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
        }

        if (textoMejorNivel != null)
        {
            int mejor = RunState.MejorNivel;
            textoMejorNivel.text = mejor > 0 ? $"Mejor nivel: {mejor}" : string.Empty;
        }

        MostrarPrincipal();
    }

    void Update()
    {
        // El botón "atrás" de Android llega como Escape: cierra los ajustes
        var teclado = Keyboard.current;
        if (teclado != null && teclado.escapeKey.wasPressedThisFrame
            && panelAjustes != null && panelAjustes.activeSelf)
            MostrarPrincipal();
    }

    public void Jugar()
    {
        Sonidos.Reproducir(Sonido.Clic);
        RunState.NewRun();
        FundidoEscena.Cargar(escenaJuego);
    }

    public void AbrirAjustes()
    {
        Sonidos.Reproducir(Sonido.Clic);
        if (panelPrincipal != null) panelPrincipal.SetActive(false);
        if (panelAjustes != null) panelAjustes.SetActive(true);
    }

    public void MostrarPrincipal()
    {
        if (panelAjustes != null) panelAjustes.SetActive(false);
        if (panelPrincipal != null) panelPrincipal.SetActive(true);
    }

    public void Salir()
    {
        Sonidos.Reproducir(Sonido.Clic);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void CambiarVolumen(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat(ClaveVolumen, valor);
    }
}