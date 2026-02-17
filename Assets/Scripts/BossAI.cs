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

    [Header("Skill Settings (ตั้งค่าสกิล)")]
    public float skillCooldown = 15.0f;     // เวลาหน่วงสกิล (วินาที)
    public float skillDamage = 25f;         // ดาเมจต่อลูกกระสุน
    public float skillBackupSpeed = 12f;    // ความเร็วถอยหลัง
    public float skillBackupDistance = 6f;  // ระยะถอยหลัง
    public float skillRange = 10f;          // ระยะที่จะเริ่มใช้สกิล
    public float projectileSpeed = 14f;     // ความเร็วกระสุนตอนพุ่ง
    public float skillAnimDelay = 0.5f;     // เวลารอก่อนสร้างกระสุน (หลังเริ่ม animation)
    public float projectileHoverTime = 5f;  // เวลาที่กระสุนลอยอยู่ก่อนพุ่ง (วินาที)
    public int projectileCount = 2;         // จำนวนกระสุนที่สร้าง
    public float spawnSpread = 2f;          // ระยะห่างระหว่างกระสุนแต่ละลูก (เมตร)
    public float spawnHeight = 1.5f;        // ความสูงของจุดสร้างกระสุน
    public GameObject projectilePrefab;     // ลาก Prefab กระสุนใส่ตรงนี้

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
    private float lastSkillTime;
    private bool isAttacking = false;
    private bool isUsingPower = false;
    private bool isUsingSkill = false;      // กำลังใช้สกิลอยู่
    private bool lastAttackWasPower = false; // เก็บว่าท่าล่าสุดเป็น Power ไหม
    
    // Animation Parameter Hash
    private int animSpeedID;
    private int animAttackID;
    private int animPowerID;
    private int animIsGroundedID;
    private int animIsJumpingID;
    private int animHitID;
    private int animDeathID;
    private int animSkillID;

    [Header("Audio Settings")]
    public AudioClip skillSound;            // เสียงตอนใช้สกิล
    private AudioSource audioSource;

    void Start()
    {
        // 1. รับ Component
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // ถ้าไม่มี AudioSource ให้ใส่ให้
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

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
        animSkillID = Animator.StringToHash("Skill");
    }

    void Update()
    {
        if (player == null || isDead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // เช็คว่ากำลังเล่น Animation โจมตีอยู่ไหม
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        isAttacking = stateInfo.IsName("attack") || stateInfo.IsName("Use power") || stateInfo.IsTag("Skill"); 

        if (isAttacking || isUsingSkill)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            if (!isUsingSkill) FaceTarget(player.position);
            agent.velocity = Vector3.zero;
        }
        else
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
                // อัปเดต stoppingDistance เผื่อมีการปรับ attackRange
                agent.stoppingDistance = attackRange - 0.5f; 
            }

            CheckAttack(distanceToPlayer);
        }

        UpdateAnimator();
    }

    void CheckAttack(float distance)
    {
        // ไม่รับคำสั่งโจมตีซ้อนถ้ากำลังโจมตีหรือใช้สกิลอยู่
        if (isAttacking || isUsingSkill) return;

        // 1. สกิล (Skill) — ถอยหลังแล้วยิง Projectile
        if (distance <= skillRange && Time.time >= lastSkillTime + skillCooldown && projectilePrefab != null)
        {
            PerformSkill();
            return;
        }

        // 2. ท่าพิเศษ (Use Power)
        if (distance <= powerRange && distance > attackRange && Time.time >= lastPowerTime + powerCooldown)
        {
             PerformUsePower();
             return;
        }

        // 3. ท่าโจมตีปกติ (Attack)
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

    // ========== สกิล: ถอยหลัง → เล่น Animation → ยิง Projectile 2 ลูก ==========
    void PerformSkill()
    {
        lastSkillTime = Time.time;
        isUsingSkill = true;
        Debug.Log("Boss: Skill Start! — Backing up...");
        StartCoroutine(SkillSequence());
    }

    IEnumerator SkillSequence()
    {
        // --- 1. ถอยหลังจาก Player อย่างรวดเร็ว ---
        if (agent.isOnNavMesh) agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // หันหน้าเข้าหา Player ก่อนถอย
        FaceTarget(player.position);
        yield return null;

        Vector3 backupDirection = (transform.position - player.position).normalized;
        backupDirection.y = 0;
        float distanceMoved = 0f;

        while (distanceMoved < skillBackupDistance)
        {
            if (isDead) { isUsingSkill = false; yield break; }

            float step = skillBackupSpeed * Time.deltaTime;
            transform.position += backupDirection * step;
            distanceMoved += step;
            yield return null;
        }

        // --- 2. หันหน้ากลับเข้าหา Player แล้วเล่น Animation ---
        FaceTarget(player.position);
        animator.SetTrigger(animSkillID);
        Debug.Log("Boss: Skill Animation Playing...");

        // เล่นเสียงสกิล
        if (skillSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(skillSound);
        }

        // --- 3. รอจังหวะ animation แล้วยิง Projectile ---
        yield return new WaitForSeconds(skillAnimDelay);

        if (!isDead)
        {
            SpawnProjectiles();
        }

        // --- 4. รอให้ animation เล่นจบ ---
        yield return new WaitForSeconds(0.5f);

        // --- 5. กลับสู่สถานะปกติ ---
        isUsingSkill = false;
        if (!isDead && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    void SpawnProjectiles()
    {
        if (projectilePrefab == null || player == null) return;

        // จุดกึ่งกลาง: หน้าบอส
        Vector3 centerPos = transform.position + Vector3.up * spawnHeight + transform.forward * 2f;

        // ทิศขวางของบอส (ใช้กระจายกระสุน)
        Vector3 rightDir = transform.right;

        for (int i = 0; i < projectileCount; i++)
        {
            // คำนวณตำแหน่งกระจายจากกึ่งกลาง
            float offset = 0f;
            if (projectileCount > 1)
            {
                // กระจายเท่าๆ กัน เช่น 2 ลูก = -1, +1  |  3 ลูก = -1, 0, +1
                float t = (float)i / (projectileCount - 1); // 0 → 1
                offset = Mathf.Lerp(-spawnSpread, spawnSpread, t);
            }

            Vector3 spawnPos = centerPos + rightDir * offset;

            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            BossProjectile bp = proj.GetComponent<BossProjectile>();
            if (bp != null)
            {
                bp.speed = projectileSpeed;
                bp.damage = skillDamage;
                bp.hoverDuration = projectileHoverTime;
                bp.target = player;
            }

            Debug.Log($"Boss: Projectile {i + 1}/{projectileCount} spawned! (Hover for {projectileHoverTime}s)");
        }
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
        if (agent.isOnNavMesh) agent.isStopped = true;
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

            // เปลี่ยนจาก PlayerHealth เป็น PlayerHealth (เพื่อให้ใช้ระบบ stagger ได้)
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                Debug.Log($"Boss: Hit Player! Dealt {damage} damage. (Dist: {currentDistance:F1})");
            }
            else
            {
                // Fallback เผื่อใช้ HealthManager
                HealthManager hm = player.GetComponent<HealthManager>();
                if (hm != null) hm.TakeDamage(damage);
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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, skillRange);  // วงฟ้า = ระยะใช้สกิล
    }
}
