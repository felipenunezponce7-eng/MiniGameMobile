using UnityEngine;

// Para los botones de los paneles de la escena de juego (ponlo, por ejemplo, en el Canvas).
// Conecta cada botón en su On Click: Reintentar() o IrAlMenu().
public class BotonesDeFlujo : MonoBehaviour
{
    public void Reintentar()
    {
        Sonidos.Reproducir(Sonido.Clic);
        RunState.RestartRun();
    }

    public void IrAlMenu()
    {
        Sonidos.Reproducir(Sonido.Clic);
        RunState.GoToMenu();
    }
}