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

    private Animator anim;
    private bool isLightAttack = true;
    private bool isAttackingFlag = false;

    void Start()
    {
        anim = GetComponent<Animator>();
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
        // ถ้ากำลังโจมตีอยู่ (จะเช็คทั้งจาก Tag และ Boolean Flag) จะไม่รับ Input ใหม่
        if (isAttackingFlag || anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack") || anim.IsInTransition(0)) return;

        if (Input.GetMouseButtonDown(0)) StartCoroutine(PerformAttack(true));
        else if (Input.GetMouseButtonDown(1)) StartCoroutine(PerformAttack(false));
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