using System;
using UnityEngine;

// Usa los nombres TipoDeBola y MazoDeCartas (tus clases renombradas).
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider), typeof(MeshRenderer))]
public class Bola : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit/Unlit
    static MaterialPropertyBlock mpb;

    [Header("Anti-atasco")]
    [SerializeField] float stuckSpeed = 0.3f;
    [SerializeField] float stuckTime = 2.5f;

    [Header("Multibola")]
    [Tooltip("Ángulo mínimo y máximo (en grados) entre la dirección actual y la de la bola extra")]
    [SerializeField, Range(5f, 90f)] float extraAngleMin = 25f;
    [SerializeField, Range(5f, 90f)] float extraAngleMax = 60f;
    [SerializeField] float extraMinSpeed = 6f;
    [Tooltip("Evita gastar dos extras en el mismo roce")]
    [SerializeField] float extraCooldown = 0.1f;

    public TipoDeBola Type { get; private set; }
    public int Level { get; private set; }
    public int Damage { get; private set; }
    public bool IsLaunched { get; private set; }
    public bool IsExtra { get; private set; }
    public float Radius => col.radius * transform.lossyScale.x;

    // Las asigna el MazoDeCartas
    public Action<Bola> Returned;
    public MazoDeBolas Owner;

    Rigidbody rb;
    MeshRenderer rend;
    SphereCollider col;
    float stuckTimer;
    bool returned;

    // Multibola
    int extraCharges;      // bolas extra que aún puede soltar esta bola
    float lastExtraTime;
    float ultimoRebote;    // para que el sonido de rebote no se repita en cada roce

    // Bomba
    bool bombArmed;
    float bombRadius;

    // Recursos propios de la bola, que se reutilizan cuando vuelve del pool
    PhysicsMaterial bouncyMaterial;
    GameObject trailInstance;
    GameObject trailSource;
    ParticleSystem[] trailSystems;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<MeshRenderer>();
        col = GetComponent<SphereCollider>();
        mpb ??= new MaterialPropertyBlock();

        // Juego 2D con modelos 3D: bloqueamos Z y la rotación en X/Y
        rb.constraints = RigidbodyConstraints.FreezePositionZ
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnDestroy()
    {
        if (bouncyMaterial != null) Destroy(bouncyMaterial);
    }

    // Prepara la bola (recién sacada del pool) según su tipo y su nivel de mejora.
    // Las bolas extra tienen el mismo aspecto pero no sueltan más bolas ni explotan.
    public void Setup(TipoDeBola type, int level, bool isExtra = false)
    {
        Type = type;
        Level = level;
        IsExtra = isExtra;
        name = isExtra
            ? $"Bola {type.displayName} Nv{level} (extra)"
            : $"Bola {type.displayName} Nv{level}";

        float scale = type.scale;
        Color color = type.color;
        int damage = type.damage;
        int charges = 0;
        bool bouncy = false;
        bool bomb = false;
        float radius = 0f;

        if (type.isUpgrade && level > 0)
        {
            switch (type.effect)
            {
                case UpgradeEffect.Giant:
                    scale *= Mathf.Pow(1f + type.sizeGrowth, level);   // +20% sobre el tamaño actual, por nivel
                    break;
                case UpgradeEffect.MultiBall:
                    charges = level;                                   // una extra por nivel
                    break;
                case UpgradeEffect.Fire:
                    damage += type.damagePerLevel * level;             // +1 de daño por nivel
                    break;
                case UpgradeEffect.Bouncy:
                    bouncy = true;                                     // rebote propio de esta bola
                    break;
                case UpgradeEffect.Bomb:
                    bomb = true;
                    radius = type.explosionRadius + type.radiusPerLevel * (level - 1);
                    break;
            }

            // Fade progresivo: cada nivel la acerca más al color elegido
            color = Color.Lerp(color, type.fadeColor, Mathf.Clamp01(type.fadeAmount * level));
        }

        Damage = damage;
        extraCharges = isExtra ? 0 : charges;
        lastExtraTime = -999f;
        bombArmed = bomb && !isExtra;
        bombRadius = radius;

        transform.localScale = Vector3.one * scale;
        ApplyPhysicsMaterial(type, bouncy ? level : 0);
        ApplyTrail(type.trailEffectPrefab);
        SetColor(color);

        IsLaunched = false;
        returned = false;
        stuckTimer = 0f;

        // Primero limpiar velocidades (no se puede en kinemático), luego congelar
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    void SetColor(Color color)
    {
        rend.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, color);
        rend.SetPropertyBlock(mpb);
    }

    // Saltarina: cada bola tiene su propia copia del material, para no afectar a las demás
    void ApplyPhysicsMaterial(TipoDeBola type, int bouncyLevel)
    {
        if (bouncyLevel <= 0)
        {
            col.sharedMaterial = type.physicsMaterial;
            return;
        }

        bouncyMaterial ??= new PhysicsMaterial("BolaSaltarina");

        var src = type.physicsMaterial;
        if (src != null)
        {
            bouncyMaterial.dynamicFriction = src.dynamicFriction;
            bouncyMaterial.staticFriction = src.staticFriction;
            bouncyMaterial.frictionCombine = src.frictionCombine;
            bouncyMaterial.bounceCombine = src.bounceCombine;
        }
        else
        {
            bouncyMaterial.dynamicFriction = 0f;
            bouncyMaterial.staticFriction = 0f;
        }

        float baseBounce = src != null ? src.bounciness : 0f;
        bouncyMaterial.bounciness = Mathf.Clamp01(baseBounce + type.bouncePerLevel * bouncyLevel);
        col.sharedMaterial = bouncyMaterial;
    }

    // Efecto visual que acompaña a la bola (ej. fuego). Se crea una vez y se reutiliza.
    void ApplyTrail(GameObject prefab)
    {
        if (prefab == null)
        {
            if (trailInstance != null) trailInstance.SetActive(false);
            return;
        }

        if (trailInstance == null || trailSource != prefab)
        {
            if (trailInstance != null) Destroy(trailInstance);

            trailInstance = Instantiate(prefab, transform);
            trailInstance.transform.localPosition = Vector3.zero;
            trailSource = prefab;
            trailSystems = trailInstance.GetComponentsInChildren<ParticleSystem>(true);

            // En espacio mundo, las partículas se quedan atrás y forman la estela
            foreach (var ps in trailSystems)
            {
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
            }
        }

        trailInstance.SetActive(true);
        foreach (var ps in trailSystems)
        {
            ps.Clear();
            ps.Play();
        }
    }

    public void Launch(Vector3 velocity)
    {
        rb.isKinematic = false;
        rb.linearVelocity = velocity;
        if (!IsExtra) Sonidos.Reproducir(Sonido.Lanzar, 0.8f);
        IsLaunched = true;
    }

    // Llamar cuando la bola sale del tablero (KillZone) o se atasca
    public void Lose()
    {
        if (returned) return;
        returned = true;
        Returned?.Invoke(this);
    }

    void OnCollisionEnter(Collision c)
    {
        if (!IsLaunched) return;

        if (c.collider.TryGetComponent(out IDamageable target))
        {
            // La bomba explota al primer contacto con un bloque (la explosión ya lo incluye)
            if (bombArmed) Explode();
            else target.TakeDamage(Damage, this);
        }

        // Rebote contra paredes y techo (los bloques ya suenan por su cuenta)
        if (!c.collider.TryGetComponent(out IDamageable _) && Time.time - ultimoRebote > 0.06f)
        {
            ultimoRebote = Time.time;
            Sonidos.Reproducir(Sonido.Rebote, 0.5f, UnityEngine.Random.Range(0.9f, 1.1f));
        }

        TrySpawnExtra(c);
    }

    // Daño en área. Después de explotar, la bola es normal el resto del nivel.
    void Explode()
    {
        bombArmed = false;
        Vector3 center = transform.position;

        Sonidos.Reproducir(Sonido.Explosion);
        ShakeCamara.Sacudir(0.25f, 0.3f);

        if (Type.explosionEffectPrefab != null)
        {
            var fx = Instantiate(Type.explosionEffectPrefab, center, Quaternion.identity);
            // El efecto se dibuja con radio 1: al escalarlo, el anillo marca el radio real del daño
            fx.transform.localScale = Vector3.one * bombRadius;
            Destroy(fx, 3f);
        }

        Collider[] hits = Physics.OverlapSphere(center, bombRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(Type.explosionDamage, this);
        }

        // Vuelve a verse y comportarse como una bola normal
        Damage = Type.damage;
        SetColor(Type.color);
        if (trailInstance != null) trailInstance.SetActive(false);
    }

    // Gizmo: en la vista Scene muestra el radio de la bomba mientras sigue armada
    void OnDrawGizmos()
    {
        if (!bombArmed || bombRadius <= 0f) return;
        Gizmos.color = new Color(1f, 0.45f, 0f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, bombRadius);
    }

    // Multibola: cada rebote con cualquier superficie gasta un nivel y suelta una bola extra
    void TrySpawnExtra(Collision c)
    {
        if (extraCharges <= 0 || Owner == null) return;
        if (Time.time - lastExtraTime < extraCooldown) return;

        extraCharges--;
        lastExtraTime = Time.time;

        // Hacia dónde "sale" la bola de la superficie
        Vector2 away = transform.position - c.GetContact(0).point;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.up;

        Vector2 vel = rb.linearVelocity;
        float speed = Mathf.Max(vel.magnitude, extraMinSpeed);
        Vector2 dir = vel.sqrMagnitude > 0.01f ? vel.normalized : away;

        // Dirección distinta a la actual: giro aleatorio hacia un lado u otro
        float sign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float angle = UnityEngine.Random.Range(extraAngleMin, extraAngleMax) * sign;
        Vector2 extraDir = Quaternion.Euler(0f, 0f, angle) * (Vector3)dir;

        // Si apunta hacia dentro de la superficie, se espeja para que salga hacia afuera
        if (Vector2.Dot(extraDir, away) < 0f) extraDir = Vector2.Reflect(extraDir, away);

        Owner.SpawnExtra(transform.position, extraDir.normalized * speed, Type, Level);
    }

    // Si la bola queda casi quieta (ej. encajada entre bloques), se pierde
    void FixedUpdate()
    {
        if (!IsLaunched || returned) return;

        if (rb.linearVelocity.magnitude < stuckSpeed) stuckTimer += Time.fixedDeltaTime;
        else stuckTimer = 0f;

        if (stuckTimer >= stuckTime) Lose();
    }
}