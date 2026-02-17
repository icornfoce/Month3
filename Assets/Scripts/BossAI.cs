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
    public float projectileSpeed = 14f;     // ความเร็วกระสุน
    public float skillAnimDelay = 0.5f;     // เวลารอก่อนสร้างกระสุน
    public float projectileHoverTime = 5f;  // เวลาที่กระสุนลอยอยู่ก่อนพุ่ง
    public int projectileCount = 2;         // จำนวนกระสุนที่สร้าง
    public float spawnSpread = 2f;          // ระยะห่างระหว่างกระสุนแต่ละลูก (เมตร)
    public float spawnHeight = 1.5f;        // ความสูงของจุดสร้างกระสุน
    public GameObject projectilePrefab;     // ลาก Prefab กระสุนใส่ตรงนี้
    public Transform projectileSpawnPoint;  // (Optional) จุดที่อยากให้กระสุนเกิด

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
                agent.stoppingDistance = attackRange - 0.5f; 
            }

            CheckAttack(distanceToPlayer);
        }

        if (!isUsingSkill) UpdateAnimator();
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

    // ========== สกิล: ถอยหลัง → เล่น Animation → ยิง Projectile ==========
    void PerformSkill()
    {
        lastSkillTime = Time.time;
        isUsingSkill = true;
        Debug.Log("Boss: Skill Start! — Backing up...");
        StartCoroutine(SkillSequence());
    }

    IEnumerator SkillSequence()
    {
        // หยุดเดินก่อน
        if (agent.isOnNavMesh) agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Force Idle — บังคับให้เล่น Idle ทันที ไม่ให้มี Animation ใดๆ ค้างอยู่
        animator.SetFloat(animSpeedID, 0f);
        animator.ResetTrigger(animAttackID);
        animator.ResetTrigger(animPowerID);
        animator.ResetTrigger(animSkillID);
        animator.ResetTrigger(animHitID);
        animator.Play("Idle", 0, 0f); // บังคับเล่น Idle state โดยตรง

        // รอ 1 เฟรมให้ Animator ประมวลผลการ Reset ให้เสร็จก่อน
        yield return null;

        // ถอยหลังจาก Player
        FaceTarget(player.position);
        Vector3 backupDir = (transform.position - player.position).normalized;
        backupDir.y = 0;

        float currentDist = 0f;

        while (currentDist < skillBackupDistance)
        {
            if (isDead) { isUsingSkill = false; yield break; }

            FaceTarget(player.position);
            float step = skillBackupSpeed * Time.deltaTime;
            agent.Move(backupDir * step);
            currentDist += step;
            yield return null;
        }

        Debug.Log("Boss: Backup Complete. Playing Animation...");

        // เล่น Animation "Use power" หลังถอยเสร็จ
        animator.SetTrigger(animPowerID);

        // หันเข้าหา Player แล้วเล่น Skill Animation
        FaceTarget(player.position);
        animator.SetTrigger(animSkillID);

        if (skillSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(skillSound);
        }

        // รอจังหวะ Animation ก่อนยิง Projectile
        yield return new WaitForSeconds(skillAnimDelay);

        // ยิง Projectile
        if (!isDead)
        {
            Debug.Log("Boss: Spawning Projectiles...");
            SpawnProjectiles();
        }

        // รอจนกว่า Projectile จะพุ่งไป (hover time)
        yield return new WaitForSeconds(projectileHoverTime);

        // กลับสู่สถานะปกติ
        Debug.Log("Boss: Skill Complete. Resuming Action.");
        isUsingSkill = false;
        if (!isDead && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    void SpawnProjectiles()
    {
        if (projectilePrefab == null || player == null) return;

        for (int i = 0; i < projectileCount; i++)
        {
            Vector3 spawnPos;

            if (projectileSpawnPoint != null)
            {
                // ใช้ตำแหน่งจาก Spawn Point ที่กำหนด
                spawnPos = projectileSpawnPoint.position;
            }
            else
            {
                // คำนวณตำแหน่ง Spawn ขนาบข้างซ้าย-ขวาของบอส
                Vector3 centerPos = transform.position + Vector3.up * spawnHeight;
                Vector3 rightDir = transform.right;

                float offset = 0f;
                if (projectileCount > 1)
                {
                    float t = (float)i / (projectileCount - 1);
                    offset = Mathf.Lerp(-spawnSpread, spawnSpread, t);
                }
                spawnPos = centerPos + rightDir * offset;
            }

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

        // ปิดสคริปต์
        this.enabled = false;
        
        // ปิด Collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    // ========== ฟังก์ชันทำดาเมจใส่ Player ==========
    public void DealDamage()
    {
        if (player == null || isDead) return;

        float hitDistanceCheck = attackRange + 2.0f;
        float currentDistance = Vector3.Distance(transform.position, player.position);

        if (currentDistance <= hitDistanceCheck)
        {
            HealthManager hm = player.GetComponent<HealthManager>();
            if (hm != null)
            {
                float damage = lastAttackWasPower ? powerDamage : attackDamage;
                hm.TakeDamage(damage);
                Debug.Log($"Boss dealt {damage} damage to Player!");
            }
        }
    }

    // วาด Gizmos ใน Editor เพื่อดูระยะ
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, powerRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, skillRange);
    }
}
