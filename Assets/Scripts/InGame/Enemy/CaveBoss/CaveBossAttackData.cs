using UnityEngine;

[System.Serializable]
public class CaveBossAttackData
{
    [Header("Attack Settings")]
    public string attackName = "Basic Attack";
    public string animatorTrigger = "Attack1";
    public int damage = 25;
    public float cooldown = 3.0f;
    public float animationDuration = 1.0f;
    public float attackDelay = 0.4f;
    
    [Header("Attack Type")]
    public AttackType attackType = AttackType.Melee;
    
    [Header("Melee Attack Settings")]
    public Vector2 attackColliderOffset = new Vector2(2f, 0f);
    public float attackColliderRadius = 1.5f;
    
    [Header("Warp Hand Attack Settings")] // UPDATED: Changed from "Spell Cast"
    public GameObject spellPrefab; // Warp hand prefab
    public float spellSpawnDelay = 0.8f; // Delay after animation starts before warp spawns
    public float spellSpawnRadius = 3f; // How far from target to spawn warp
    public int spellSpawnCount = 1; // How many warp hands to spawn
    public float spellLifetime = 3f; // How long warp hand lasts
    
    public enum AttackType
    {
        Melee,    // Direct contact damage (claw swipe)
        SpellCast // Spawns warp with hand near target
    }
}