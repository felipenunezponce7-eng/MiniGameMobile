using UnityEngine;
using UnityEngine.InputSystem;

// Apuntar: mantener el dedo (o mouse) presionado. Disparar: soltar.
[RequireComponent(typeof(LineRenderer))]
public class Launcher : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] MazoDeBolas Mazo;
    [SerializeField] Transform muzzle;      // punto de salida de la bola
    [SerializeField] Camera cam;

    [Header("Disparo")]
    [SerializeField] float launchSpeed = 14f;
    [SerializeField, Range(10f, 85f)] float maxAngle = 75f; // límite respecto a "recto hacia abajo"

    [Header("Línea de trayectoria")]
    [Tooltip("Excluye la capa 'Ball' para que la línea no choque con la propia bola")]
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField, Min(2)] int maxSteps = 120;

    LineRenderer line;
    Vector3[] buffer;
    Bola current;
    Vector2 lastDir = Vector2.down;
    bool aiming;
    readonly Plane plane = new Plane(Vector3.back, Vector3.zero); // plano de juego z = 0

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 0;
        buffer = new Vector3[maxSteps + 1];
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        var level = LevelManager.Instance;
        if (level != null && level.State != LevelState.Playing)
        {
            ClearAim();
            return;
        }

        // Cargar la siguiente bola solo cuando no hay ninguna en juego
        if (current == null && Mazo.InFlight == 0)
            current = Mazo.Draw(muzzle.position);

        var pointer = Pointer.current; // cubre mouse, touch y lápiz
        if (current == null || pointer == null)
        {
            ClearAim();
            return;
        }

        if (pointer.press.isPressed)
        {
            aiming = true;
            lastDir = GetAimDirection(pointer.position.ReadValue());
            DrawTrajectory(lastDir);
        }
        else if (aiming) // se soltó: disparar
        {
            aiming = false;
            ClearAim();
            Fire(lastDir);
        }
    }

    Vector2 GetAimDirection(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!plane.Raycast(ray, out float enter)) return lastDir;

        Vector2 target = ray.GetPoint(enter);
        Vector2 dir = target - (Vector2)muzzle.position;
        if (dir.sqrMagnitude < 0.01f) return lastDir;

        // Limitar el ángulo para no apuntar hacia arriba o muy horizontal
        float angle = Mathf.Clamp(Vector2.SignedAngle(Vector2.down, dir), -maxAngle, maxAngle);
        Vector2 result = Quaternion.Euler(0f, 0f, angle) * Vector3.down;
        return result;
    }

    // Simula la misma física que PhysX (gravedad + paso fijo) hasta el primer impacto
    void DrawTrajectory(Vector2 dir)
    {
        float dt = Time.fixedDeltaTime;
        float radius = current.Radius;
        Vector3 p = muzzle.position;
        Vector3 v = (Vector3)dir * launchSpeed;

        int n = 0;
        buffer[n++] = p;

        for (int i = 0; i < maxSteps; i++)
        {
            v += Physics.gravity * dt;
            Vector3 delta = v * dt;
            float dist = delta.magnitude;
            Vector3 step = delta / dist;

            if (Physics.SphereCast(p, radius, step, out RaycastHit hit, dist,
                                   hitMask, QueryTriggerInteraction.Ignore))
            {
                buffer[n++] = p + step * hit.distance;
                break;
            }

            p += delta;
            buffer[n++] = p;
        }

        line.positionCount = n;
        line.SetPositions(buffer);
    }

    void ClearAim()
    {
        aiming = false;
        line.positionCount = 0;
    }

    void Fire(Vector2 dir)
    {
        current.Launch(dir * launchSpeed);
        Mazo.MarkLaunched(current);
        current = null;
    }
}
