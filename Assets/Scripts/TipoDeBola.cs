using UnityEngine;

public enum UpgradeEffect
{
    None,      
    Giant,    
    MultiBall,   
    Fire,       
    Bouncy,    
    Bomb        
}

[CreateAssetMenu(menuName = "Roguelike/Ball Type")]
public class TipoDeBola : ScriptableObject
{
    [Header("Carta de recompensa")]
    public string displayName = "Normal";
    [TextArea] public string description; 
    public Sprite icon;                     
    [Min(0f)] public float weight = 60f;   

    [Header("Bola")]

    public bool isUpgrade;
    public Color color = Color.white;
    [Min(0.1f)] public float scale = 1f;
    [Min(1)] public int damage = 1;
    public PhysicsMaterial physicsMaterial;

    [Header("Efecto (solo mejoras)")]
    public UpgradeEffect effect = UpgradeEffect.None;

    [Header("Gigante")]
    [Min(0f)]

    public float sizeGrowth = 0.2f;

    [Header("Fuego")]
    [Min(0)]

    public int damagePerLevel = 1;

    [Header("Saltarina")]
    [Range(0f, 1f)]
    public float bouncePerLevel = 0.1f;

    [Header("Bomba")]
    [Min(0.1f)]
    public float explosionRadius = 2f;
    [Min(0f)]
    public float radiusPerLevel = 0.5f;
    [Min(1)]
    public int explosionDamage = 2;
    public GameObject explosionEffectPrefab;

    [Header("Efecto visual que acompaña a la bola (opcional)")]
    public GameObject trailEffectPrefab;

    [Header("Fade de color (solo mejoras)")]
    public Color fadeColor = Color.white;

    [Range(0f, 1f)]
    public float fadeAmount = 0.35f;
}