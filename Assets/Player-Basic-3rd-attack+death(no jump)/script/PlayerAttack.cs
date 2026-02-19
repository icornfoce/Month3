using UnityEngine;
using System.Collections; // เพิ่มบรรทัดนี้เพื่อแก้ Error CS0103

public class PlayerAttack : MonoBehaviour
{
    [Header("Damage Settings")]
    public float lightDamage = 15f;
    public float heavyDamage = 30f;

    [Header("Attack Detection")]
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public LayerMask enemyLayer;

    [Header("Attack Timing")]
    public float attackHitDelay = 0.2f;

    [Header("Stamina Reference")]
    public StaminaManager staminaManager;

    [Header("Movement Reference")]
    public PlayerMovement playerMovement;

    [Header("Health Reference")]
    public HealthManager healthManager;

    [Header("Weapon Trigger")]
    public PlayerWeaponTrigger weaponTrigger;

    private Animator anim;
    private bool isAttackingFlag = false;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim == null) anim = GetComponent<Animator>();

        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (healthManager == null) healthManager = GetComponent<HealthManager>();

        // พยายามหา WeaponTrigger ถ้ายังไม่ได้ลากใส่
        if (weaponTrigger == null) weaponTrigger = GetComponentInChildren<PlayerWeaponTrigger>();
        
        Debug.Log($"PlayerAttack: Start complete. Anim: {anim != null}, WeaponTrigger: {weaponTrigger != null}");
    }

    void Update()
    {
        if (anim == null || isAttackingFlag) return;

        bool isDodgeLocked = playerMovement != null && playerMovement.isDodgeLockingMovement;
        bool isTakingDMG = healthManager != null && healthManager.isTakingDamage;

        if (isDodgeLocked || isTakingDMG || anim.IsInTransition(0)) return;

        if (Input.GetMouseButtonDown(0)) StartCoroutine(PerformAttack(true));
        else if (Input.GetMouseButtonDown(1)) StartCoroutine(PerformAttack(false));
    }

    IEnumerator PerformAttack(bool isLight)
    {
        isAttackingFlag = true;

        if (staminaManager != null)
        {
            float cost = isLight ? staminaManager.lightAttackStaminaCost : staminaManager.heavyAttackStaminaCost;
            if (!staminaManager.UseStamina(cost))
            {
                isAttackingFlag = false;
                yield break;
            }
        }

        anim.SetTrigger(isLight ? "Is attack light" : "Is attack heavy");

        // 1. เปิด Trigger (รอจังหวะที่ดาบเหวี่ยง)
        yield return new WaitForSeconds(attackHitDelay);
        if (weaponTrigger != null) 
            weaponTrigger.EnableTrigger(isLight ? lightDamage : heavyDamage);

        // 2. ปิด Trigger (รอจนกว่าจะจบวงสวิง)
        yield return new WaitForSeconds(0.4f); 
        if (weaponTrigger != null) weaponTrigger.DisableTrigger();

        isAttackingFlag = false;
    }

    public void CancelAttack()
    {
        StopAllCoroutines();
        if (weaponTrigger != null) weaponTrigger.DisableTrigger();
        isAttackingFlag = false;
        anim.ResetTrigger("Is attack light");
        anim.ResetTrigger("Is attack heavy");
    }
}
