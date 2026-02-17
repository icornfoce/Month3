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

    [Header("Dash Attack Settings (พุ่งชาร์จ)")]
    public float dashCooldown = 10f;        // คูลดาวน์ Dash (วินาที)
    public float dashSpeed = 20f;           // ความเร็วตอนพุ่ง
    public float dashDamage = 35f;          // ดาเมจตอนพุ่งชน
    public float dashRange = 12f;           // ระยะที่จะเริ่มใช้ Dash
    public float dashMinRange = 5f;         // ระยะขั้นต่ำ (ไม่ Dash ถ้าอยู่ใกล้เกิน)
    public float dashHitRadius = 2f;        // รัศมีที่ถือว่าชน Player

    [Header("Projectile Rain Settings (ฝนกระสุน)")]
    public float rainCooldown = 20f;        // คูลดาวน์ฝนกระสุน (วินาที)
    public int rainProjectileCount = 5;     // จำนวนกระสุนต่อรอบ
    public float rainSpreadAngle = 60f;     // มุมกระจายของพัด (องศา)
    public float rainProjectileSpeed = 18f; // ความเร็วกระสุน Rain
    public float rainDamage = 15f;          // ดาเมจต่อลูก
    public float rainRange = 14f;           // ระยะที่จะเริ่มใช้ Rain

    [Header("Damage Settings (ค่าดาเมจ)")]
    public float attackDamage = 15f;        // ดาเมจท่าปกติ
    public float powerDamage = 30f;         // ดาเมจท่าพิเศษ

    [Header("Animation Timing (เวลาที่ดาเมจจะเข้า)")]
    public float attackHitDelay = 1.25f;    // เวลาหลังจากเริ่มท่าโจมตี จนถึงจังหวะที่ดาบโดน (วินาที)
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
    private float lastDashTime;
    private float lastRainTime;
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

        // 1. ฝนกระสุน (Projectile Rain) — ไกลสุด ยิงพัด 5 ลูก
        if (distance <= rainRange && Time.time >= lastRainTime + rainCooldown && projectilePrefab != null)
        {
            PerformProjectileRain();
            return;
        }

        // 2. สกิล (Skill) — ถอยหลังแล้วยิง Projectile
        if (distance <= skillRange && Time.time >= lastSkillTime + skillCooldown && projectilePrefab != null)
        {
            PerformSkill();
            return;
        }

        // 3. พุ่งชาร์จ (Dash Attack) — ระยะกลาง
        if (distance <= dashRange && distance >= dashMinRange && Time.time >= lastDashTime + dashCooldown)
        {
            PerformDashAttack();
            return;
        }

        // 4. ท่าพิเศษ (Use Power)
        if (distance <= powerRange && distance > attackRange && Time.time >= lastPowerTime + powerCooldown)
        {
             PerformUsePower();
             return;
        }

        // 5. ท่าโจมตีปกติ (Attack)
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

        // หมุนตาม Player ระหว่างรอ Animation (2.5 วินาที)
        float skillTimer = 0f;
        while (skillTimer < 2.5f)
        {
            if (isDead) { isUsingSkill = false; yield break; }
            FaceTarget(player.position);
            skillTimer += Time.deltaTime;
            yield return null;
        }

        // ยิง Projectile
        if (!isDead)
        {
            FaceTargetInstant(player.position); // หันหน้าเข้าหา Player ทันทีก่อนยิง
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

        // คำนวณทิศทางไป Player โดยตรง (ไม่พึ่ง transform.forward)
        Vector3 toPlayer = (player.position - transform.position).normalized;
        toPlayer.y = 0;
        Vector3 rightOfPlayer = Vector3.Cross(Vector3.up, toPlayer); // ทิศขวาของทิศที่ไป Player

        for (int i = 0; i < projectileCount; i++)
        {
            Vector3 spawnPos;

            if (projectileSpawnPoint != null)
            {
                spawnPos = projectileSpawnPoint.position;
            }
            else
            {
                // Spawn หน้าบอส ไปทาง Player
                Vector3 centerPos = transform.position + Vector3.up * spawnHeight + toPlayer * 2f;

                float offset = 0f;
                if (projectileCount > 1)
                {
                    float t = (float)i / (projectileCount - 1);
                    offset = Mathf.Lerp(-spawnSpread, spawnSpread, t);
                }
                spawnPos = centerPos + rightOfPlayer * offset;
            }

            // Spawn โดยหันไปทาง Player
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(toPlayer));
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

    // ========== Dash Attack: พุ่งชาร์จใส่ Player ==========
    void PerformDashAttack()
    {
        lastDashTime = Time.time;
        isUsingSkill = true;
        Debug.Log("Boss: Dash Attack! — Charging at Player!");
        StartCoroutine(DashAttackSequence());
    }

    IEnumerator DashAttackSequence()
    {
        // หยุดก่อน
        if (agent.isOnNavMesh) agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // หันหน้าเข้าหา Player
        FaceTargetInstant(player.position);
        yield return null;

        // === Phase 1: พุ่งไปหา Player ก่อน ===
        Vector3 dashDir = (player.position - transform.position).normalized;
        dashDir.y = 0;
        Vector3 targetPos = player.position;

        float dashTime = 0f;
        float maxDashTime = 1.5f;
        bool hitPlayer = false;

        while (dashTime < maxDashTime)
        {
            if (isDead) { isUsingSkill = false; yield break; }

            // หมุนตาม Player ตลอดตอนพุ่ง
            FaceTarget(player.position);

            float step = dashSpeed * Time.deltaTime;
            agent.Move(dashDir * step);

            // เช็คว่าถึงตัว Player หรือยัง
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer <= dashHitRadius)
            {
                hitPlayer = true;
                break;
            }

            // ถ้าพุ่งเลย target ไปแล้วก็หยุด
            float distToTarget = Vector3.Distance(transform.position, targetPos);
            if (distToTarget < 1f)
            {
                break;
            }

            dashTime += Time.deltaTime;
            yield return null;
        }

        // === Phase 2: ถึงตัวแล้ว → เล่น Animation ตี ===
        FaceTargetInstant(player.position);
        animator.SetTrigger(animAttackID);
        Debug.Log("Boss: Dash reached target! Playing attack animation...");

        // รอจังหวะดาเมจ (1.25 วิ ตรงกับ attackHitDelay)
        yield return new WaitForSeconds(attackHitDelay);

        // === Phase 3: ทำดาเมจ ===
        if (hitPlayer && !isDead)
        {
            // เช็คระยะอีกทีว่า Player ยังอยู่ใกล้ไหม
            float finalDist = Vector3.Distance(transform.position, player.position);
            if (finalDist <= dashHitRadius + 1f)
            {
                HealthManager hm = player.GetComponent<HealthManager>();
                if (hm != null)
                {
                    hm.TakeDamage(dashDamage);
                    Debug.Log($"Boss: Dash Attack hit Player! Dealt {dashDamage} damage!");
                }
            }
        }

        // กลับสู่สถานะปกติ
        Debug.Log("Boss: Dash Attack Complete.");
        isUsingSkill = false;
        if (!isDead && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    // ========== Projectile Rain: ยิงกระสุนเป็นพัด ==========
    void PerformProjectileRain()
    {
        lastRainTime = Time.time;
        isUsingSkill = true;
        Debug.Log("Boss: Projectile Rain! — Firing fan of projectiles!");
        StartCoroutine(ProjectileRainSequence());
    }

    IEnumerator ProjectileRainSequence()
    {
        // หยุดก่อน
        if (agent.isOnNavMesh) agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // หันหน้าเข้าหา Player
        FaceTarget(player.position);
        yield return null;
        FaceTarget(player.position);

        // เล่น Animation Use Power
        animator.SetTrigger(animPowerID);

        // หมุนตาม Player ระหว่างรอ Animation (2.5 วินาที)
        float rainTimer = 0f;
        while (rainTimer < 2.5f)
        {
            if (isDead) { isUsingSkill = false; yield break; }
            FaceTarget(player.position);
            rainTimer += Time.deltaTime;
            yield return null;
        }

        // สุ่มจำนวนรอบ 1-3 รอบ
        int rounds = Random.Range(1, 4);
        Debug.Log($"Boss: Projectile Rain — {rounds} rounds!");

        for (int r = 0; r < rounds; r++)
        {
            if (isDead) break;

            FaceTargetInstant(player.position);
            SpawnRainProjectiles();

            // รอระหว่างรอบ (ถ้ายังไม่ใช่รอบสุดท้าย)
            if (r < rounds - 1)
            {
                yield return new WaitForSeconds(0.8f);
            }
        }

        // รอกระสุนบินไป
        yield return new WaitForSeconds(1.5f);

        // กลับสู่ปกติ
        Debug.Log("Boss: Projectile Rain Complete.");
        isUsingSkill = false;
        if (!isDead && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    void SpawnRainProjectiles()
    {
        if (projectilePrefab == null || player == null) return;

        // คำนวณทิศทางกลาง (ตรงไปหา Player)
        Vector3 centerDir = (player.position - transform.position).normalized;
        centerDir.y = 0;

        float halfSpread = rainSpreadAngle / 2f;

        for (int i = 0; i < rainProjectileCount; i++)
        {
            // คำนวณมุมของลูกนี้
            float angle = 0f;
            if (rainProjectileCount > 1)
            {
                float t = (float)i / (rainProjectileCount - 1);
                angle = Mathf.Lerp(-halfSpread, halfSpread, t);
            }

            // หมุนทิศทางตามมุม
            Vector3 dir = Quaternion.Euler(0, angle, 0) * centerDir;

            // ตำแหน่ง Spawn หน้าบอส
            Vector3 spawnPos = transform.position + Vector3.up * spawnHeight + dir * 1.5f;

            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));
            BossProjectile bp = proj.GetComponent<BossProjectile>();
            if (bp != null)
            {
                bp.speed = rainProjectileSpeed;
                bp.damage = rainDamage;
                bp.hoverDuration = 0f; // ไม่ต้อง hover — ยิงตรงไปเลย
                bp.target = null;      // ไม่ต้อง track Player — ยิงตรงตามทิศ
            }

            Debug.Log($"Boss: Rain Projectile {i + 1}/{rainProjectileCount} fired! (angle: {angle:F1}°)");
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

    // หันหน้าเข้าหาเป้าหมายทันที (ไม่ Slerp — snap เลย)
    void FaceTargetInstant(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
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

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, dashRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, rainRange);
    }
}
