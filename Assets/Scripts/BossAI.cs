using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI; // สำหรับใช้ RawImage
using TMPro; // สำหรับใช้ TextMeshPro
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


    [Header("Damage Settings (ค่าดาเมจ)")]
    public float attackDamage = 15f;        // ดาเมจท่าปกติ
    public float powerDamage = 30f;         // ดาเมจท่าพิเศษ

    [Header("Animation Timing (เวลาที่ดาเมจจะเข้า)")]
    public float attackHitDelay = 1.25f;    // เวลาหลังจากเริ่มท่าโจมตี จนถึงจังหวะที่ดาบโดน (วินาที)
    public float powerHitDelay = 0.8f;      // เวลาหลังจากเริ่มท่า Power จนถึงจังหวะที่โดน (วินาที)

    [Header("Health Settings (ค่าพลังชีวิตบอส)")]
    public float maxHealth = 200f;
    
    [Header("UI Reference (Enhanced)")]
    public string bossDisplayName = "Ancient Boss";
    public TextMeshProUGUI bossNameText;
    public RawImage hpBarImage;
    public CanvasGroup bossUIContainer; // สำหรับ Fade เข้า-ออก
    public float maxWidth = 400f;
    public float uiShowDistance = 25f;
    public float hpLerpSpeed = 5f;

    private float targetHPWidth;

    [Header("You Win Settings")]
    public CanvasGroup youWinCG;
    public AudioSource youWinAudio;
    public float youWinFadeSpeed = 0.5f;

    [Header("Death Settings (การตาย)")]
    public GameObject deathEffectPrefab;     // Prefab ระเบิด/ควันตอนตาย
    public float destroyDelay = 5.0f;        // เวลาที่จะทำลายซากทิ้ง (0 = ไม่ทำลาย)

    [Header("Audio Settings (เสียง)")]
    public AudioClip attackSound;   // เสียงโจมตีปกติ
    public AudioClip powerSound;    // เสียงท่าพิเศษ (รวมๆ)
    public AudioClip dashSound;     // เสียง Dash
    public AudioClip rainSound;     // เสียง Rain Proj
    public AudioClip homingSound;   // เสียง Homing
    public AudioClip hitSound;      // เสียงโดนตี/Parry
    public AudioClip deathSound;    // เสียงตาย

    [Header("Sound Timing (เวลาหน่วงเสียง) หน่วย:วินาที")]
    public float attackSoundDelay = 0.2f;   // ดีเลย์เสียงโจมตี
    public float powerSoundDelay = 0.5f;    // ดีเลย์เสียง Power
    public float dashSoundDelay = 0.1f;     // ดีเลย์เสียง Dash
    public float rainSoundDelay = 0.5f;     // ดีเลย์เสียง Rain
    public float homingSoundDelay = 0.5f;   // ดีเลย์เสียง Homing
    [Header("Phase Settings")]
    public int currentPhase = 1;
    [Range(0.1f, 0.9f)]
    public float phase2Threshold = 0.5f; // เปลี่ยนเฟสที่ 50%

    public float currentHealth;
    public bool isDead = false;
    private bool hasDied = false; // ตัวเช็คว่าตายจริงหรือยัง (กันซ้ำ)

    [Header("Charge Effect Settings")]
    public GameObject chargingEffectPrefab;
    public Transform chargingEffectSpawnPoint;
    private GameObject activeChargingEffect;

    [Header("Phase 2 Transformation Settings")]
    public GameObject phase2TransformationVFX;
    public GameObject phase2ContinuousVFXPrefab;
    public string phase2TriggerName = "Phase2";
    private GameObject m_activePhase2ContinuousVFX;

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
    private RectTransform barRect;


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

        if (hpBarImage != null)
        {
            barRect = hpBarImage.GetComponent<RectTransform>();
            targetHPWidth = maxWidth;
        }

        if (bossNameText != null)
        {
            bossNameText.text = bossDisplayName;
        }

        // เริ่มต้นด้วย UI จางหาย
        if (bossUIContainer != null) bossUIContainer.alpha = 0f;

        // Ensure You Win UI is hidden at start
        if (youWinCG != null)
        {
            youWinCG.alpha = 0f;
            youWinCG.blocksRaycasts = false;
            youWinCG.interactable = false;
            youWinCG.gameObject.SetActive(false); // ปิด Object ไปเลยชัวร์สุด
        }
        if (youWinAudio != null && youWinAudio.gameObject.scene.IsValid()) 
        {
            youWinAudio.Stop();
            youWinAudio.volume = 1f;
        }

        UpdateHPUI();
    }

    void Update()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // อัปเดต UI ตลอดเวลา แม้บอสจะตายหรือติดสตั้น เพื่อให้หลอดเลือดไหลลงจนสุด และให้ UI Fade ออกตอนตาย
        UpdateUIStatus(distanceToPlayer);

        // ถ้าติ๊ก isDead ใน Inspector แต่ยังไม่ตายจริง -> สั่งตายเลย
        if (isDead && !hasDied)
        {
            Die();
            return;
        }

        if (hasDied || isStunned) return; // ถ้าตายแล้วหรือมึนอยู่ ไม่ต้องทำอะไร (หยุด AI)

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

    void UpdateUIStatus(float distance)
    {
        // 1. จัดการ Fade UI ตามระยะ
        if (bossUIContainer != null)
        {
            float targetAlpha = (distance <= uiShowDistance && !hasDied) ? 1f : 0f;
            bossUIContainer.alpha = Mathf.MoveTowards(bossUIContainer.alpha, targetAlpha, Time.deltaTime * 2f);
        }

        // 2. จัดการ Lerp แถบเลือด
        if (barRect != null)
        {
            float currentWidth = barRect.sizeDelta.x;
            float newWidth = Mathf.Lerp(currentWidth, targetHPWidth, Time.deltaTime * hpLerpSpeed);
            barRect.sizeDelta = new Vector2(newWidth, barRect.sizeDelta.y);
        }
    }

    void CheckAttack(float distance)
    {
        // ไม่รับคำสั่งโจมตีซ้อนถ้ากำลังโจมตีหรือใช้สกิลอยู่
        if (isAttacking || isUsingSkill) return;

        // --- Phase Selection Logic ---
        
        // ** ท่าที่ใช้ได้เฉพาะ เฟส 2 เท่านั้น **
        if (currentPhase >= 2)
        {
            // 1. พุ่งชาร์จ (Dash Attack)
            if (distance <= dashRange && distance >= dashMinRange && Time.time >= lastDashTime + dashCooldown)
            {
                PerformDashAttack();
                return;
            }

            // 3. ฝนกระสุน (Projectile Rain)
            if (distance <= rainRange && Time.time >= lastRainTime + rainCooldown && projectilePrefab != null)
            {
                PerformProjectileRain();
                return;
            }
        }

        // ** ท่าที่ใช้ได้ตั้งแต่ เฟส 1 (และยังใช้ต่อในเฟส 2) **
        
        // 4. กระสุนติดตาม (Homing)
        if (distance <= homingRange && Time.time >= lastHomingTime + homingCooldown && projectilePrefab != null)
        {
            PerformHomingSkill();
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
        ShowChargingEffect(true);
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

        ShowChargingEffect(false);

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

    // Helper function to manage charging VFX
    void ShowChargingEffect(bool show)
    {
        if (show)
        {
            if (activeChargingEffect == null && chargingEffectPrefab != null)
            {
                activeChargingEffect = Instantiate(chargingEffectPrefab, chargingEffectSpawnPoint != null ? chargingEffectSpawnPoint : transform);
                activeChargingEffect.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            if (activeChargingEffect != null)
            {
                Destroy(activeChargingEffect);
                activeChargingEffect = null;
            }
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

        ShowChargingEffect(true);

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

        ShowChargingEffect(false);

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

        ShowChargingEffect(true);

        // รอ Animation
        float waitTimer = 0f;
        while (waitTimer < 1.0f)
        {
            if (isDead) { isUsingSkill = false; yield break; }
            FaceTarget(player.position);
            waitTimer += Time.deltaTime;
            yield return null;
        }

        ShowChargingEffect(false);

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

    // ========== ฟังก์ชันรับ Parry (เรียกจาก Script อื่น) ==========
    public void OnParried()
    {
        if (isDead) return;

        Debug.Log("Boss: PARRIED! Stunned!");
        PlaySound(hitSound);

        // 1. หยุดการทำงานทุกอย่าง
        isStunned = true;
        isUsingSkill = false;
        isAttacking = false;
        StopAllCoroutines();
        currentSkillCoroutine = null;
        ShowChargingEffect(false);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // 2. เล่น Animation Parry Hit
        if (animator != null)
        {
            animator.SetBool(animParryHitID, true);
            // บังคับเปลี่ยนท่าทันที
            animator.CrossFade("parry-hit", 0.05f); 
        }

        // 3. เริ่มการนับเวลาเพื่อหายสตั้น
        StartCoroutine(StunRoutine());
    }

    IEnumerator StunRoutine()
    {
        yield return new WaitForSeconds(stunDuration);

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
    }

    // ========== ฟังก์ชันรับดาเมจ (เรียกจาก PlayerAttack) ==========
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"<color=orange>Boss took {amount} damage! Current HP: {currentHealth}/{maxHealth}</color>");

        // เช็คการเปลี่ยนเฟส
        if (currentPhase == 1 && currentHealth <= maxHealth * phase2Threshold && !isDead)
        {
            currentPhase = 2;
            Debug.Log("<color=red>Boss AI: Entering PHASE 2!</color>");

            // --- Phase 2 Transformation Logic ---
            
            // 1. One-time transformation VFX
            if (phase2TransformationVFX != null)
            {
                Instantiate(phase2TransformationVFX, transform.position + Vector3.up, Quaternion.identity);
            }

            // 2. Continuous Phase 2 Aura
            if (phase2ContinuousVFXPrefab != null && m_activePhase2ContinuousVFX == null)
            {
                m_activePhase2ContinuousVFX = Instantiate(phase2ContinuousVFXPrefab, transform);
                m_activePhase2ContinuousVFX.transform.localPosition = Vector3.up; // Spawn slightly up from base
            }

            // 3. Animation Trigger
            if (!string.IsNullOrEmpty(phase2TriggerName))
            {
                animator.SetTrigger(phase2TriggerName);
            }

            // (Optional) เล่นเสียงหรือเอฟเฟกต์เปลี่ยนเฟสที่นี่ได้
            if (powerSound != null) PlaySound(powerSound); 
        }

        // เล่น Animation โดนตี
        animator.SetTrigger(animHitID);
        PlaySound(hitSound); // เล่นเสียงโดนตี

        if (currentHealth <= 0)
        {
            Die();
        }

        UpdateHPUI();
    }

    void UpdateHPUI()
    {
        float pct = Mathf.Clamp01(currentHealth / maxHealth);
        targetHPWidth = pct * maxWidth;
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
            animator.SetBool(animParryHitID, false);
            Debug.Log("Boss: Force playing 'Death' animation...");
        }

        // 3. แสดง Effect
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position + Vector3.up * 1.0f, Quaternion.identity);
        }

        // 4. รอเวลา
        yield return new WaitForSeconds(3.0f); // รอให้ท่าตายเล่นจบ

        // 5. แสดงเข็มทิศเมาส์ให้ทับหน้าจอ
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 6. แสดงหน้าจอ You Win แบบ Fade-in (รอจนเสร็จ)
        if (youWinCG != null)
        {
            yield return StartCoroutine(ShowYouWinRoutine());
        }

        // 7. จัดการซากหลังจาก UI ขึ้นเรียบร้อย
        if (destroyDelay > 0)
        {
             Destroy(gameObject, 1.0f); // ค่อยทำลายซากทิ้ง
        }
        else
        {
             this.enabled = false;
        }
    }

    private IEnumerator ShowYouWinRoutine()
    {
        // หน่วงเวลาให้ตายเสร็จก่อนนิดนึง ค่อยขึ้นหน้า You Win
        yield return new WaitForSeconds(1.0f);

        if (youWinCG != null)
        {
            youWinCG.gameObject.SetActive(true); // เปิด Object ให้แสดงผล
        }

        if (youWinAudio != null) 
        {
            if (youWinAudio.gameObject.scene.IsValid() && youWinAudio.gameObject.activeInHierarchy)
            {
                youWinAudio.volume = 1f;
                youWinAudio.loop = true; // เปิดให้วนลูป
                youWinAudio.Play();
            }
            else if (youWinAudio.clip != null)
            {
                GameObject tempAudioObj = new GameObject("TempYouWinAudio");
                AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
                tempSource.clip = youWinAudio.clip;
                tempSource.spatialBlend = 0f; // 2D sound
                tempSource.volume = 1f;
                tempSource.loop = true;
                tempSource.outputAudioMixerGroup = youWinAudio.outputAudioMixerGroup;
                tempSource.Play();
            }
        }

        float currentAlpha = 0f;
        while (currentAlpha < 1f)
        {
            currentAlpha += Time.deltaTime * youWinFadeSpeed;
            if (youWinCG != null) youWinCG.alpha = currentAlpha;
            yield return null;
        }

        if (youWinCG != null)
        {
            youWinCG.alpha = 1f;
            youWinCG.blocksRaycasts = true;
            youWinCG.interactable = true;
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
