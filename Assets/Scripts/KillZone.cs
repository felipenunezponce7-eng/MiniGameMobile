using UnityEngine;

// Zona bajo el tablero: la bola que entra se devuelve al mazo/pool.
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    void Reset() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Bola bola))
            bola.Lose();
    }
}
