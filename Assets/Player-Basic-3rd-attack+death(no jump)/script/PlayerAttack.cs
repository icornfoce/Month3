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
    public PlayerHealth playerHealth;

    private Animator anim;
    private bool isLightAttack = true;
    private bool isAttackingFlag = false;
    private Coroutine attackCoroutine;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        
        if (attackPoint == null)
        {
            GameObject point = new GameObject("AttackPoint");
            point.transform.parent = transform;
            point.transform.localPosition = new Vector3(0f, 1f, 1f);
            attackPoint = point.transform;
        }
    }

    void Update()
    {
        // ถ้ากำลังติด Lock จากการหลบ กำลังโจมตีอยู่ หรือกำลังโดนดาเมจ จะไม่รับ Input ใหม่
        bool isDodgeLocked = playerMovement != null && playerMovement.isDodgeLockingMovement;
        bool isTakingDMG = playerHealth != null && playerHealth.isTakingDamage;

        if (isAttackingFlag || isDodgeLocked || isTakingDMG || anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack") || anim.IsInTransition(0)) return;

        if (Input.GetMouseButtonDown(0)) attackCoroutine = StartCoroutine(PerformAttack(true));
        else if (Input.GetMouseButtonDown(1)) attackCoroutine = StartCoroutine(PerformAttack(false));
    }

    IEnumerator PerformAttack(bool isLight)
    {
        isAttackingFlag = true;
        // Stamina integration for attacking
        if (staminaManager != null)
        {
            float cost = isLight ? staminaManager.lightAttackStaminaCost : staminaManager.heavyAttackStaminaCost;
            if (!staminaManager.UseStamina(cost))
            {
                // Not enough stamina
                isAttackingFlag = false;
                yield break;
            }
        }

        isLightAttack = isLight;

        // Reset Trigger เก่าก่อนเพื่อป้องกันบัค
        anim.ResetTrigger("Is attack light");
        anim.ResetTrigger("Is attack heavy");

        string triggerName = isLight ? "Is attack light" : "Is attack heavy";
        anim.SetTrigger(triggerName);

        // รอให้ดาเมจเกิดตามดีเลย์
        yield return new WaitForSeconds(attackHitDelay);
        DealDamageToEnemy();

        // รออีกสักพักเพื่อให้แอนิเมชันเริ่มเล่นหรือจบช่วงที่กันการกดซ้ำซ้อน
        // หรือรอจนกว่าจะพ้นช่วง Attack Tag (เราจะใช้ cooldown สั้นๆ กันไว้ด้วย)
        yield return new WaitForSeconds(0.3f); 
        isAttackingFlag = false;
        attackCoroutine = null;
    }

    public void CancelAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        
        isAttackingFlag = false;
        
        // Reset Triggers and Tag impacts
        anim.ResetTrigger("Is attack light");
        anim.ResetTrigger("Is attack heavy");
        
        // เราไม่สามารถหยุด Animator animation ทันทีได้ในวิธีที่ง่ายที่สุด แต่การ Reset Trigger และ Flag 
        // จะทำให้ PlayerMovement สามารถข้ามการเช็ค Tag Attack ได้ถ้าเราปรับโค้ดฝั่งนั้น
    }

    public void DealDamageToEnemy()
    {
        if (attackPoint == null) return;
        float damage = isLightAttack ? lightDamage : heavyDamage;
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            var boss = enemy.GetComponent<BossAI>();
            if (boss != null) boss.TakeDamage(damage);
        }
    }
}