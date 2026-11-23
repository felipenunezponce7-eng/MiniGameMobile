using UnityEngine;

// Puente para botones de la UI (los botones no pueden llamar métodos estáticos).
public class RunFlow : MonoBehaviour
{
    public void RestartRun() => RunState.RestartRun();
}
