using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class BossAI : MonoBehaviour
{
    [Header("Global Settings (ปรับความเร็วรวม)")]
    [Range(0.1f, 2.0f)]
    public float globalSpeedMultiplier = 1.0f; // ตัวคูณความเร็วทุกอย่าง (ลดเหลือ 0.5 คือช้าลงครึ่งนึง)

    [Header("Boss Settings (ตั้งค่าบอส)")]
    public float moveSpeed = 1.5f;          // เดินช้ามาก (เต่ากัดยาง)
    public float attackRange = 2.0f;        
    public float powerRange = 6.0f;         
    public float attackCooldown = 4.0f;     // ตีเสร็จพัก 4 วิ
    public float powerCooldown = 12.0f;     

    [Header("Projectile Rain Settings (ฝนกระสุน)")]
    public float rainCooldown = 20f;        
    public int rainProjectileCount = 5;     
    public float rainSpreadAngle = 60f;     
    public float rainProjectileSpeed = 10f; // ลดจาก 18 (กระสุนช้าลง)
    public float rainDamage = 15f;          // ดาเมจต่อลูก
    public float rainBackSpeed = 6f;        // ความเร็วถอยหลังก่อนยิง
    public float rainBackDuration = 1.0f;   // ระยะเวลาถอยหลัง (วินาที)
    public float rainRange = 14f;           // ระยะที่จะเริ่มใช้ Rain
    public float spawnHeight = 1.5f;        // ความสูงของจุดสร้างกระสุน
    public GameObject projectilePrefab;     // ลาก Prefab กระสุนใส่ตรงนี้
    public Transform rainSpawnPoint;        // (Optional) จุดที่กระสุนจะออก — ลาก Transform ใส่

    [Header("Dash Attack Settings (พุ่งชาร์จ)")]
    public float dashCooldown = 15f;        
    public float dashSpeed = 8f;            // พุ่งช้าลงอีก
    public float dashDamage = 35f;          
    public float dashRange = 12f;           
    public float dashMinRange = 5f;         
    public float dashHitRadius = 2f;        

    [Header("Homing Projectile Settings (กระสุนติดตาม)")]
    public float homingCooldown = 15f;      
    public int homingCount = 3;             
    public float homingSpeed = 3.5f;        // บินช้าเต่ากัด
    public float homingTurnSpeed = 0.5f;    // เลี้ยวแทบไม่ไป (หลบง่ายสุดๆ)
    public float homingDuration = 8f;       

    public float homingRange = 15f;         

    [Header("Burst Skill Settings (ระเบิดรอบทิศ)")]
    public float burstCooldown = 25f;       
    public int burstCount = 12;             
    public float burstSpeed = 5f;          // ระเบิดช้าลงอีก
    public float burstDamage = 20f;         // ดาเมจ
    public float burstRange = 8f;           // ระยะเริ่มใช้ (ใกล้-กลาง)

    [Header("Damage Settings (ค่าดาเมจ)")]
    public float attackDamage = 15f;        // ดาเมจท่าปกติ
    public float powerDamage = 30f;         // ดาเมจท่าพิเศษ

    [Header("Animation Timing (เวลาที่ดาเมจจะเข้า)")]
    public float attackHitDelay = 1.25f;    // เวลาหลังจากเริ่มท่าโจมตี จนถึงจังหวะที่ดาบโดน (วินาที)
    public float powerHitDelay = 0.8f;      // เวลาหลังจากเริ่มท่า Power จนถึงจังหวะที่โดน (วินาที)

    [Header("Health Settings (ค่าพลังชีวิตบอส)")]
    public float maxHealth = 200f;
    [Header("Death Settings (การตาย)")]
    public GameObject deathEffectPrefab;     // Prefab ระเบิด/ควันตอนตาย
    public float destroyDelay = 5.0f;        // เวลาที่จะทำลายซากทิ้ง (0 = ไม่ทำลาย)

    [Header("Audio Settings (เสียง)")]
    public AudioClip attackSound;   // เสียงโจมตีปกติ
    public AudioClip powerSound;    // เสียงท่าพิเศษ (รวมๆ)
    public AudioClip dashSound;     // เสียง Dash
    public AudioClip rainSound;     // เสียง Rain Proj
    public AudioClip homingSound;   // เสียง Homing
    public AudioClip burstSound;    // เสียง Burst
    public AudioClip hitSound;      // เสียงโดนตี/Parry
    public AudioClip deathSound;    // เสียงตาย

    [Header("Sound Timing (เวลาหน่วงเสียง) หน่วย:วินาที")]
    public float attackSoundDelay = 0.2f;   // ดีเลย์เสียงโจมตี
    public float powerSoundDelay = 0.5f;    // ดีเลย์เสียง Power
    public float dashSoundDelay = 0.1f;     // ดีเลย์เสียง Dash
    public float rainSoundDelay = 0.5f;     // ดีเลย์เสียง Rain
    public float homingSoundDelay = 0.5f;   // ดีเลย์เสียง Homing
    public float burstSoundDelay = 0.8f;    // ดีเลย์เสียง Burst
    public float currentHealth;
    public bool isDead = false;
    private bool hasDied = false; // ตัวเช็คว่าตายจริงหรือยัง (กันซ้ำ)

    /*[Header("Jump Settings (ตั้งค่ากระโดด)")]
    public float jumpForce = 5.0f;          // แรงกระโดด
    public bool enableJumping = true;
            */

    [Header("Setup (การเชื่อมต่อ)")]
    public string playerTag = "Player";     // Tag ของผู้เล่นที่บอสจะวิ่งตาม

    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    
    [Header("Stun Settings (Parry)")]
    public float stunDuration = 3.0f;       // ระยะเวลาติดสตั้น
    public bool isStunned = false;
    private Coroutine currentSkillCoroutine;
    
    // สถานะภายใน (Internal State)
    private float lastAttackTime;
    private float lastPowerTime;
    private float lastDashTime;
    private float lastRainTime;
    private float lastHomingTime;
    private float lastBurstTime;
    private bool isAttacking = false;
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
    private int animParryHitID;


    private AudioSource audioSource;

    void Start()
    {
        // 1. รับ Component
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // ถ้าไม่มี AudioSource ให้ใส่ให้
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Auto-find RainSpawnPoint ถ้ายังไม่ได้ใส่
        if (rainSpawnPoint == null)
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
            {
                if (child.name == "RainSpawnPoint")
                {
                    rainSpawnPoint = child;
                    Debug.Log($"Boss: Auto-found RainSpawnPoint at {child.position}");
                    break;
                }
            }
        }

        // ถ้าไม่มี NavMeshAgent ให้ใส่ให้ (กัน error)
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();

        // 2. ตั้งค่า NavMeshAgent
        agent.speed = moveSpeed * globalSpeedMultiplier; // Apply multiplier
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
        animParryHitID = Animator.StringToHash("isParryHit");
    }

    void Update()
    {
        if (player == null) return;
        
        // ถ้าติ๊ก isDead ใน Inspector แต่ยังไม่ตายจริง -> สั่งตายเลย
        if (isDead && !hasDied)
        {
            Die();
            return;
        }

        if (hasDied || isStunned) return; // ถ้าตายแล้วหรือมึนอยู่ ไม่ต้องทำอะไร

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // เช็คว่ากำลังเล่น Animation โจมตีอยู่ไหม
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        isAttacking = stateInfo.IsName("attack") || stateInfo.IsName("Use power"); 

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

        // 2. พุ่งชาร์จ (Dash Attack) — ระยะกลาง
        if (distance <= dashRange && distance >= dashMinRange && Time.time >= lastDashTime + dashCooldown)
        {
            PerformDashAttack();
            return;
        }

        // 3. กระสุนติดตาม (Homing) — ระยะไกล/กลาง (กวน Player)
        if (distance <= homingRange && Time.time >= lastHomingTime + homingCooldown && projectilePrefab != null)
        {
            PerformHomingSkill();
            return;
        }

        // 4. ระเบิดรอบทิศ (Burst) — ระยะใกล้-กลาง (ไล่ Player)
        if (distance <= burstRange && Time.time >= lastBurstTime + burstCooldown && projectilePrefab != null)
        {
            PerformBurstSkill();
            return;
        }

        // 5. ท่าพิเศษ (Use Power) (เปลี่ยนเป็นลำดับท้ายๆ หรือเอาไว้ใช้ท่าอื่นแทนได้)
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
        PlaySound(attackSound, attackSoundDelay); // เล่นเสียงโจมตี (มีดีเลย์)
        StartCoroutine(DelayDealDamage(attackHitDelay));
    }

    void PerformUsePower()
    {
        animator.SetTrigger(animPowerID);
        lastPowerTime = Time.time;
        lastAttackWasPower = true;
        
        Debug.Log("Boss: Use Power Start!");
        PlaySound(powerSound, powerSoundDelay); // เล่นเสียง Power (มีดีเลย์)
        StartCoroutine(DelayDealDamage(powerHitDelay));
    }


    // ========== Dash Attack: พุ่งชาร์จใส่ Player ==========
    void PerformDashAttack()
    {
        lastDashTime = Time.time;
        isUsingSkill = true;
        Debug.Log("Boss: Dash Attack! — Charging at Player!");
        PlaySound(dashSound, dashSoundDelay); // เล่นเสียง Dash
        currentSkillCoroutine = StartCoroutine(DashAttackSequence());
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

            float step = dashSpeed * globalSpeedMultiplier * Time.deltaTime; // Apply multiplier
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
        Debug.Log("Boss: Dash Attack Complete. Recovering...");
        yield return new WaitForSeconds(1.5f); // **เพิ่มช่องว่างให้ผู้เล่นตีสวน**
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
        PlaySound(rainSound, rainSoundDelay); // เล่นเสียง Rain
        currentSkillCoroutine = StartCoroutine(ProjectileRainSequence());
    }

    IEnumerator ProjectileRainSequence()
    {
        // หยุด NavMesh ชั่วคราว (เราจะใช้ agent.Move เองตอนถอย)
        if (agent.isOnNavMesh) agent.isStopped = true;
        
        // === Phase 0: ถอยหลังตั้งหลัก (Backstep) ===
        // ถอยหลังเพื่อให้มีระยะห่างก่อนยิง (Kiting)
        Debug.Log("Boss: Projectile Rain — Backstepping...");
        float backTimer = 0f;
        while (backTimer < rainBackDuration)
        {
             if (isDead) { isUsingSkill = false; yield break; }
             
             // หาทางหนี (ทิศตรงข้าม Player)
             Vector3 dirAway = (transform.position - player.position).normalized;
             dirAway.y = 0; // ไม่เหาะขึ้นฟ้า
             
             // ใช้ agent.Move เพื่อไม่ให้ทะลุกำแพง (ดีกว่า transform.position)
             agent.Move(dirAway * rainBackSpeed * globalSpeedMultiplier * Time.deltaTime);
             
             // หันหน้ามอง Player ตลอด (เดินถอยหลังเท่ๆ)
             FaceTarget(player.position);
             
             backTimer += Time.deltaTime;
             yield return null;
        }
        
        // หยุดและเตรียมยิง
        agent.velocity = Vector3.zero;

        // หันหน้าเข้าหา Player ให้ชัวร์อีกที
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
        Debug.Log("Boss: Projectile Rain Complete. Recovering...");
        yield return new WaitForSeconds(1.2f); // **เพิ่มช่องว่างให้ผู้เล่นตีสวน**
        isUsingSkill = false;
        if (!isDead && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    void SpawnRainProjectiles()
    {
        if (projectilePrefab == null || player == null) return;

        // จุดเริ่มต้น — ใช้ rainSpawnPoint ถ้ามี ไม่งั้นใช้หน้าบอส
        Vector3 origin;
        if (rainSpawnPoint != null)
        {
            origin = rainSpawnPoint.position;
        }
        else
        {
            origin = transform.position + Vector3.up * spawnHeight;
        }

        Debug.Log($"Boss: SpawnRainProjectiles from origin={origin}, rainSpawnPoint={(rainSpawnPoint != null ? rainSpawnPoint.name : "NULL")}");

        // คำนวณทิศทางกลาง (จาก origin ไปหา Player)
        Vector3 toPlayer = player.position - origin;
        toPlayer.y = 0;
        Vector3 centerDir = toPlayer.normalized;
        if (centerDir.sqrMagnitude < 0.01f) centerDir = transform.forward;

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
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;

            // Spawn ตรงจุด origin
            GameObject proj = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));
            BossProjectile bp = proj.GetComponent<BossProjectile>();
            if (bp != null)
            {
                bp.speed = rainProjectileSpeed * globalSpeedMultiplier; // Apply multiplier
                bp.damage = rainDamage;
                bp.hoverDuration = 0f;
                bp.target = null;
                bp.Initialize();
            }
            else
            {
                Debug.LogError("Boss: ERROR! Rain Projectile Prefab ไม่มี BossProjectile script!");
            }

            Debug.Log($"Boss: Rain Projectile {i + 1}/{rainProjectileCount} fired from {origin} (angle: {angle:F1}°)");
        }
    }

    // ========== Homing Skill (กระสุนติดตาม) ==========
    void PerformHomingSkill()
    {
        lastHomingTime = Time.time;
        isUsingSkill = true;
        Debug.Log("Boss: Homing Skill Start!");
        PlaySound(homingSound, homingSoundDelay); // เล่นเสียง Homing
        currentSkillCoroutine = StartCoroutine(HomingSequence());
    }

    IEnumerator HomingSequence()
    {
        if (agent.isOnNavMesh) agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // หันหา Player
        FaceTarget(player.position);
        yield return null;

        // เล่นท่าเดียวกับ Rain (Use Power)
        animator.SetTrigger(animPowerID);

        // รอ Animation
        float waitTimer = 0f;
        while (waitTimer < 1.0f)
        {
            if (isDead) { isUsingSkill = false; yield break; }
            FaceTarget(player.position);
            waitTimer += Time.deltaTime;
            yield return null;
        }

        // ยิงกระสุนทีละลูก (ยิง 3 ลูกเว้นช่วงนิดหน่อย)
        for (int i = 0; i < homingCount; i++)
        {
            if (isDead) break;
            FaceTargetInstant(player.position);
            SpawnHomingProjectile();
            yield return new WaitForSeconds(0.5f); // ยิงรัวๆ เว้น 0.5 วิ
        }

        yield return new WaitForSeconds(1.0f);

        Debug.Log("Boss: Homing Skill Complete. Recovering...");
        yield return new WaitForSeconds(1.5f); // **เพิ่มช่องว่างให้ผู้เล่นตีสวน**

        isUsingSkill = false;
        if (!isDead && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    void SpawnHomingProjectile()
    {
        if (projectilePrefab == null || player == null) return;

        // จุดยิง: ใช้ RainSpawnPoint หรือหน้าบอส
        Vector3 origin = (rainSpawnPoint != null) ? rainSpawnPoint.position : (transform.position + Vector3.up * spawnHeight);
        Vector3 dir = (player.position - origin).normalized;
        if (dir == Vector3.zero) dir = transform.forward;

        GameObject proj = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));
        BossProjectile bp = proj.GetComponent<BossProjectile>();

        if (bp != null)
        {
            bp.speed = homingSpeed * globalSpeedMultiplier;       // Apply multiplier
            bp.damage = rainDamage;       // ดาเมจ (ใช้ค่าเดียวกับ Rain หรือแยกก็ได้)
            bp.hoverDuration = 0f;        // ไม่ต้อง Hover
            bp.target = player;           // **ส่งเป้าหมายให้มันตาม**
            
            // เปิดโหมดติดตาม!
            bp.isHoming = true;
            bp.turnSpeed = homingTurnSpeed;
            bp.lifetimeAfterLaunch = homingDuration; // อยู่นานตามที่ตั้ง

            bp.Initialize();
        }

        Debug.Log("Boss: Fired Homing Projectile!");
    }



    // ========== Burst Skill (ระเบิดรอบทิศ) ==========
    void PerformBurstSkill()
    {
        lastBurstTime = Time.time;
        isUsingSkill = true;
        Debug.Log("Boss: Burst Skill Start!");
        PlaySound(burstSound, burstSoundDelay); // เล่นเสียง Burst
        currentSkillCoroutine = StartCoroutine(BurstSequence());
    }

    IEnumerator BurstSequence()
    {
        if (agent.isOnNavMesh) agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // หันหน้าหา Player ก่อนชาร์จ
        FaceTarget(player.position);
        
        // เล่น Animation
        animator.SetTrigger(animPowerID);

        // รอจังหวะชาร์จ (1.5 วินาที)
        float chargeTime = 1.5f;
        float t = 0;
        while (t < chargeTime)
        {
             if (isDead) { isUsingSkill = false; yield break; }
             // FaceTarget(player.position); // จะให้หมุนตาม หรือยืนนิ่งๆ ก้ได้ (ยืนนิ่งเท่กว่าตอนระเบิด)
             t += Time.deltaTime;
             yield return null;
        }

        if (projectilePrefab != null)
        {
            // คำนวณจุดปล่อย (รอบตัว)
            Vector3 center = transform.position + Vector3.up * spawnHeight;
            if (rainSpawnPoint != null) center = rainSpawnPoint.position;

            float angleStep = 360f / burstCount;
            
            for (int i = 0; i < burstCount; i++)
            {
                float angle = i * angleStep;
                // หมุนทิศทางรอบแกน Y
                Quaternion rot = Quaternion.Euler(0, angle, 0);
                Vector3 dir = rot * transform.forward; // อิงจากหน้าบอส (หรือ Vector3.forward ก็ได้ เพราะครบวงกลมอยู่ดี)

                GameObject proj = Instantiate(projectilePrefab, center, Quaternion.LookRotation(dir));
                BossProjectile bp = proj.GetComponent<BossProjectile>();
                if (bp != null)
                {
                    bp.speed = burstSpeed * globalSpeedMultiplier; // Apply multiplier
                    bp.damage = burstDamage;
                    bp.hoverDuration = 0f; // ยิงเลย
                    bp.target = null;      // ไม่ติดตาม
                    bp.isHoming = false;   // ยิงตรงๆ
                    bp.lifetimeAfterLaunch = 5f;
                    bp.Initialize();
                }
            }
            Debug.Log("Boss: BOOM! Burst Fired.");
        }

        yield return new WaitForSeconds(1.0f); // ค้างท่านิดนึง

        Debug.Log("Boss: Burst Skill Complete. Recovering...");
        yield return new WaitForSeconds(2.0f); // **เพิ่มช่องว่างยาวหน่อยเพราะท่าใหญ่**

        isUsingSkill = false;
        if (!isDead && agent.isOnNavMesh) agent.isStopped = false;
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

    // ========== ฟังก์ชันรับ Parrry (เรียกจาก Script อื่น) ==========
    // ========== ฟังก์ชันรับ Parry (เรียกจาก Script อื่น) ==========
    public void OnParried()
    {
        if (isDead) return;

        // 1. หยุดการทำงานทุกอย่าง
        isStunned = true;
        isUsingSkill = false;
        isAttacking = false;
        StopAllCoroutines();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // 2. บังคับเปลี่ยนท่าทันทีด้วย CrossFade
        if (animator != null)
        {
            animator.SetBool("isParryHit", true);
            // "ParryHitState" คือชื่อกล่อง Animation ใน Animator
            // 0.1f คือเวลาในการเบลนด์ท่า (ยิ่งน้อยยิ่งเปลี่ยนไว)
            animator.CrossFade("parry-hit", 0.05f);
        }

        // หมายเหตุ: ไม่ต้องใช้ StartCoroutine(StunRoutine) เพื่อ Set false แล้ว
        // เราจะใช้ระบบอัตโนมัติใน Animator แทน (ดูข้อ 2)
    }

    /*IEnumerator StunRoutine()
    {
        // ในช่วงเวลานี้ Update() จะถูกบล็อกด้วย isStunned ทำให้บอสขยับไม่ได้เลย
        yield return new WaitForSeconds(stunDuration);

        // --- จบช่วงเวลาติดสตั้น ---
        isStunned = false;
        Debug.Log("Boss: Recovered from Stun. Resuming AI...");

        // ปิด Animation Parry Hit (กลับสู่ท่าปกติ)
        if (animator != null)
        {
            animator.SetBool(animParryHitID, false);
        }

        // กลับมาเดินต่อถ้ายังไม่ตาย
        if (!isDead && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    } */

    // ========== ฟังก์ชันรับดาเมจ (เรียกจาก PlayerAttack) ==========
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"<color=orange>Boss took {amount} damage! Current HP: {currentHealth}/{maxHealth}</color>");

        // เล่น Animation โดนตี
        animator.SetTrigger(animHitID);
        PlaySound(hitSound); // เล่นเสียงโดนตี

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (hasDied) return;
        hasDied = true;
        isDead = true; // ย้ำให้เป็น true
        Debug.Log("Boss Died!");

        // 1. หยุดสกิลทั้งหมด
        StopAllCoroutines();

        // 2. หยุด NavMeshAgent (ปิดไปเลยเพื่อความชัวร์)
        if (agent != null)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
        }

        // 3. ปิด Collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 4. เริ่ม Sequence
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // 1. เล่นเสียง
        PlaySound(deathSound);

        // 2. บังคับเล่น Animation ตายทันที (Force Play)
        if (animator != null)
        {
            // ล้างค่า Trigger อื่นๆ ที่อาจค้างอยู่
            animator.ResetTrigger(animAttackID);
            animator.ResetTrigger(animPowerID);
            animator.ResetTrigger(animHitID);

            // ใช้ CrossFade เพื่อบังคับเปลี่ยนท่าไปที่ "Death" ทันที (ขอแค่มี State ชื่อ Death)
            animator.CrossFade("Death", 0.1f);
            Debug.Log("Boss: Force playing 'Death' animation...");
        }

        // 3. แสดง Effect
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position + Vector3.up * 1.0f, Quaternion.identity);
        }

        // 4. รอเวลา
        yield return new WaitForSeconds(3.0f); // รอให้ท่าตายเล่นจบ

        // 5. จัดการซาก
        if (destroyDelay > 0)
        {
             Destroy(gameObject);
        }
        else
        {
             this.enabled = false;
        }
    }

    // ========== ฟังก์ชันทำดาเมจใส่ Player ==========
    public void DealDamage()
    {
        if (player == null || isDead) return;

        float hitDistanceCheck = attackRange + 2.0f;
        float currentDistance = Vector3.Distance(transform.position, player.position);

        if (currentDistance <= hitDistanceCheck)
        {
            // เช็คว่า Player กำลัง Parry อยู่หรือไม่
            PlayerParry pp = player.GetComponent<PlayerParry>();
            if (pp != null && pp.isParryingState)
            {
                Debug.Log("<color=green>Parry SUCCESSFUL! Damage blocked by Player.</color>");
                pp.SuccessfulParry(); // สั่งให้เล่นแอนิเมชันสำเร็จทันที
                OnParried(); // ทำให้บอสมึน
                return;
            }

            HealthManager hm = player.GetComponent<HealthManager>();
            if (hm != null)
            {
                // เช็คว่า Player กำลังหลบอยู่หรือไม่ (Invincible)
                PlayerMovement movement = player.GetComponent<PlayerMovement>();
                if (movement != null && movement.IsInvincible)
                {
                    Debug.Log("<color=blue>Player DODGED the attack!</color>");
                    return; // ไม่ทำดาเมจ และไม่ log ว่า parry failed
                }

                float damage = lastAttackWasPower ? powerDamage : attackDamage;
                hm.TakeDamage(damage);
                Debug.Log($"<color=red>Parry FAILED! Boss dealt {damage} damage to Player!</color>");
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


        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, dashRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, rainRange);
    }

    void PlaySound(AudioClip clip, float delay = 0f)
    {
        if (clip != null && audioSource != null)
        {
            if (delay > 0)
            {
                StartCoroutine(PlaySoundRoutine(clip, delay));
            }
            else
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    IEnumerator PlaySoundRoutine(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource != null && !isDead)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
