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

    private Animator anim;
    private bool isLightAttack = true;

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
        // ถ้ากำลังโจมตีอยู่ (Tag Attack) จะไม่รับ Input ใหม่
        if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack")) return;

        if (Input.GetMouseButtonDown(0)) PerformAttack(true);
        else if (Input.GetMouseButtonDown(1)) PerformAttack(false);
    }

    void PerformAttack(bool isLight)
    {
        isLightAttack = isLight;

        // Reset Trigger เก่าก่อนเพื่อป้องกันบัค
        anim.ResetTrigger("Is attack light");
        anim.ResetTrigger("Is attack heavy");

        string triggerName = isLight ? "Is attack light" : "Is attack heavy";
        anim.SetTrigger(triggerName);

        StopAllCoroutines();
        StartCoroutine(DelayDealDamage(attackHitDelay));
    }

    // ฟังก์ชันที่แก้ไข Error เรื่อง Context
    IEnumerator DelayDealDamage(float delay)
    {
        yield return new WaitForSeconds(delay);
        DealDamageToEnemy();
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