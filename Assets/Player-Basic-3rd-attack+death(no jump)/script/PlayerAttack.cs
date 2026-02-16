using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Damage Settings (ค่าดาเมจ)")]
    public float lightDamage = 15f;         // ดาเมจท่าตีเบา (คลิกซ้าย)
    public float heavyDamage = 30f;         // ดาเมจท่าตีหนัก (คลิกขวา)

    [Header("Attack Detection (การตรวจจับศัตรู)")]
    public Transform attackPoint;           // จุดศูนย์กลางการตรวจจับ (ลากใส่ใน Inspector)
    public float attackRange = 1.5f;        // รัศมีการตรวจจับ
    public LayerMask enemyLayer;            // Layer ของศัตรู (ตั้งเป็น "Enemy")


    [Header("Attack Timing (เวลาที่ดาเมจจะเข้า)")]
    public float attackHitDelay = 0.2f;     // เวลาหลังจากกดตี จนถึงจังหวะที่ดาบโดน (วินาที) - ปรับได้ใน Inspector

    private Animator anim;
    private bool isLightAttack = true;      // เก็บว่าเป็นท่าเบาหรือหนัก

    void Start()
    {
        anim = GetComponent<Animator>();

        // สร้าง AttackPoint อัตโนมัติถ้ายังไม่ได้ลากใส่
        if (attackPoint == null)
        {
            GameObject point = new GameObject("AttackPoint");
            point.transform.parent = transform;
            point.transform.localPosition = new Vector3(0f, 1f, 1f); // หน้าตัวละคร
            attackPoint = point.transform;
            Debug.LogWarning("PlayerAttack: สร้าง AttackPoint อัตโนมัติแล้ว — ลากใส่เองใน Inspector จะดีกว่านะครับ");
        }
    }

    void Update()
    {
        // ถ้ากำลังโจมตี (Animation เล่นอยู่) จะไม่รับ Input ใหม่
        if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            return;
        }

        bool leftClick = Input.GetMouseButtonDown(0);
        bool rightClick = Input.GetMouseButtonDown(1);

        // ป้องกันการกดพร้อมกัน
        if (leftClick && rightClick) return;

        // สั่ง Trigger โจมตี
        if (leftClick)
        {
            PerformAttack(true);
        }
        else if (rightClick)
        {
            PerformAttack(false);
        }
    }

    void PerformAttack(bool isLight)
    {
        isLightAttack = isLight;
        
        string triggerName = isLight ? "Is attack light" : "Is attack heavy";
        anim.SetTrigger(triggerName);

        // ใช้ Coroutine หน่วงเวลาดาเมจแทนการใช้ Animation Event (แก้ปัญหาคนลืมใส่ Event)
        StartCoroutine(DelayDealDamage(attackHitDelay));
    }

    System.Collections.IEnumerator DelayDealDamage(float delay)
    {
        yield return new WaitForSeconds(delay);
        DealDamageToEnemy();
    }

    // ========== เรียกจาก Animation Event ตอน animation ตีโดน ==========
    public void DealDamageToEnemy()
    {
        if (attackPoint == null) return;

        float damage = isLightAttack ? lightDamage : heavyDamage;

        // หาศัตรูทั้งหมดในรัศมี
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);

        if (hitEnemies.Length == 0)
        {
            Debug.Log($"PlayerAttack: No enemies found in range.");
        }

        bool hitSomething = false;
        foreach (Collider enemy in hitEnemies)
        {
            BossAI boss = enemy.GetComponent<BossAI>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
                hitSomething = true;
                Debug.Log($"Player: Deal {damage} Damage to Boss!");
            }
        }
    }

    // วาด Gizmo ให้เห็นรัศมีโจมตีใน Scene View (สะดวกตอนปรับค่า)
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}