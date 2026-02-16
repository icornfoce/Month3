using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class BossAI : MonoBehaviour
{
    [Header("Boss Settings (ตั้งค่าบอส)")]
    public float moveSpeed = 3.5f;          // ความเร็วเดิน
    public float attackRange = 2.0f;        // ระยะโจมตีปกติ
    public float powerRange = 6.0f;         // ระยะใช้ท่าพิเศษ (Power)
    public float attackCooldown = 1.5f;     // เวลาหน่วงการโจมตี (วินาที)
    public float powerCooldown = 10.0f;     // เวลาหน่วงท่าพิเศษ (วินาที)

    [Header("Damage Settings (ค่าดาเมจ)")]
    public float attackDamage = 15f;        // ดาเมจท่าปกติ
    public float powerDamage = 30f;         // ดาเมจท่าพิเศษ

    [Header("Animation Timing (เวลาที่ดาเมจจะเข้า)")]
    public float attackHitDelay = 0.5f;     // เวลาหลังจากเริ่มท่าโจมตี จนถึงจังหวะที่ดาบโดน (วินาที)
    public float powerHitDelay = 0.8f;      // เวลาหลังจากเริ่มท่า Power จนถึงจังหวะที่โดน (วินาที)

    [Header("Health Settings (ค่าพลังชีวิตบอส)")]
    public float maxHealth = 200f;
    public float currentHealth;
    public bool isDead = false;
    
    [Header("Jump Settings (ตั้งค่ากระโดด)")]
    public float jumpForce = 5.0f;          // แรงกระโดด
    public bool enableJumping = true;       // เปิดปิดระบบกระโดด

    [Header("Setup (การเชื่อมต่อ)")]
    public string playerTag = "Player";     // Tag ของผู้เล่นที่บอสจะวิ่งตาม

    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    
    // สถานะภายใน (Internal State)
    private float lastAttackTime;
    private float lastPowerTime;
    private bool isAttacking = false;
    private bool isUsingPower = false;
    private bool lastAttackWasPower = false; // เก็บว่าท่าล่าสุดเป็น Power ไหม
    
    // Animation Parameter Hash
    private int animSpeedID;
    private int animAttackID;
    private int animPowerID;
    private int animIsGroundedID;
    private int animIsJumpingID;
    private int animHitID;
    private int animDeathID;

    void Start()
    {
        // 1. รับ Component
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // ถ้าไม่มี NavMeshAgent ให้ใส่ให้ (กัน error)
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();

        // 2. ตั้งค่า NavMeshAgent
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange - 0.5f;

        // 3. ตั้งค่า HP
        currentHealth = maxHealth;

        // 4. หาตัวผู้เล่น
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"BossAI: Found Player at {player.position}");
        }
        else
        {
            Debug.LogError("BossAI: ไม่พบผู้เล่น! กรุณาตั้ง Tag 'Player' ให้ตัวละครผู้เล่นด้วยครับ");
        }

        // 5. Cache Animation IDs
        animSpeedID = Animator.StringToHash("Speed");
        animAttackID = Animator.StringToHash("Attack");
        animPowerID = Animator.StringToHash("UsePower");
        animIsGroundedID = Animator.StringToHash("IsGrounded");
        animIsJumpingID = Animator.StringToHash("IsJumping");
        animHitID = Animator.StringToHash("Hit");
        animDeathID = Animator.StringToHash("Death");
    }

    void Update()
    {
        if (player == null || isDead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // เช็คว่ากำลังเล่น Animation โจมตีอยู่ไหม
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        isAttacking = stateInfo.IsName("attack") || stateInfo.IsName("Use power"); 

        if (isAttacking)
        {
            agent.isStopped = true;
            FaceTarget(player.position);
            agent.velocity = Vector3.zero;
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            // อัปเดต stoppingDistance เผื่อมีการปรับ attackRange
            agent.stoppingDistance = attackRange - 0.5f; 

            CheckAttack(distanceToPlayer);
        }

        UpdateAnimator();
    }

    void CheckAttack(float distance)
    {
        // ไม่รับคำสั่งโจมตีซ้อนถ้ากำลังโจมตีอยู่
        if (isAttacking) return;

        // 1. ท่าพิเศษ (Use Power)
        if (distance <= powerRange && distance > attackRange && Time.time >= lastPowerTime + powerCooldown)
        {
             PerformUsePower();
             return;
        }

        // 2. ท่าโจมตีปกติ (Attack)
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
            return;
        }
    }

    void PerformAttack()
    {
        animator.SetTrigger(animAttackID);
        lastAttackTime = Time.time;
        lastAttackWasPower = false;
        
        Debug.Log("Boss: Attack Start!");
        // Auto-trigger damage (fallback logic for easier use)
        StartCoroutine(DelayDealDamage(attackHitDelay));
    }

    void PerformUsePower()
    {
        animator.SetTrigger(animPowerID);
        lastPowerTime = Time.time;
        lastAttackWasPower = true;
        
        Debug.Log("Boss: Use Power Start!");
        StartCoroutine(DelayDealDamage(powerHitDelay));
    }

    IEnumerator DelayDealDamage(float delay)
    {
        yield return new WaitForSeconds(delay);
        // ตรวจสอบสถานะอีกครั้งก่อนทำดาเมจ (เผื่อบอสตายไปแล้วระหว่างรอ)
        if (!isDead)
        {
            DealDamage();
        }
    }

    void UpdateAnimator()
    {
        float speed = agent.velocity.magnitude;
        animator.SetFloat(animSpeedID, speed);
        animator.SetBool(animIsGroundedID, true);
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    // ========== ฟังก์ชันรับดาเมจ (เรียกจาก PlayerAttack) ==========
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"Boss HP: {currentHealth}/{maxHealth}");

        // เล่น Animation โดนตี
        animator.SetTrigger(animHitID);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Boss Died!");

        // เล่น Death animation
        animator.SetTrigger(animDeathID);

        // หยุด NavMeshAgent
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // ปิดสคริปต์ (ไม่ให้ Update ทำงานต่อ)
        this.enabled = false;
        
        // ปิด Collider ด้วยก็ได้ถ้าไม่อยากให้ตีศพต่อ
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    // ========== ฟังก์ชันทำดาเมจใส่ Player (Auto-called by Coroutine) ==========
    public void DealDamage()
    {
        if (player == null || isDead) return;

        // เช็คระยะอีกที (เผื่อผู้เล่นหนีทัน)
        // บวกระยะเพิ่มเล็กน้อยเผื่อ Animation ก้าวเท้า
        float hitDistanceCheck = attackRange + 2.0f;
        float currentDistance = Vector3.Distance(transform.position, player.position);

        if (currentDistance <= hitDistanceCheck)
        {
            // เลือกดาเมจตามท่าที่ใช้
            float damage = lastAttackWasPower ? powerDamage : attackDamage;

            // เปลี่ยนจาก PlayerHealth เป็น HealthManager
            HealthManager ph = player.GetComponent<HealthManager>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                Debug.Log($"Boss: Hit Player! Dealt {damage} damage. (Dist: {currentDistance:F1})");
            }
        }
        else
        {
            Debug.Log($"Boss: Missed Player! (Dist: {currentDistance:F1} > {hitDistanceCheck})");
        }
    }
    
    // วาด Gizmos ใน Editor เพื่อดูระยะ
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange); // วงแดง = ระยะโจมตีธรรมดา
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, powerRange);  // วงเหลือง = ระยะท่าพิเศษ
    }
}
