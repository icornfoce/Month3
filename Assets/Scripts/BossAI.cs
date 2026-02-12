using UnityEngine;
using UnityEngine.AI;

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
    
    [Header("Jump Settings (ตั้งค่ากระโดด)")]
    public float jumpForce = 5.0f;          // แรงกระโดด (ถ้าใช้ Rigidbody - แต่ใน NavMesh อาจใช้แค่ Animation)
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
    
    // Animation Parameter Hash (เพื่อประสิทธิภาพที่ดีกว่าการใช้ string ตรงๆ)
    private int animSpeedID;
    private int animAttackID;
    private int animPowerID;
    private int animIsGroundedID;
    private int animIsJumpingID;
    private int animHitID;

    void Start()
    {
        // 1. รับ Component
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 2. ตั้งค่า NavMeshAgent
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange - 0.5f; // หยุดก่อนถึงระยะโจมตีนิดหน่อย

        // 3. หาตัวผู้เล่น
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("BossAI: ไม่พบผู้เล่น! กรุณาตั้ง Tag 'Player' ให้ตัวละครผู้เล่นด้วยครับ");
        }

        // 4. Cache Animation IDs
        animSpeedID = Animator.StringToHash("Speed");
        animAttackID = Animator.StringToHash("Attack");
        animPowerID = Animator.StringToHash("UsePower");
        animIsGroundedID = Animator.StringToHash("IsGrounded");
        animIsJumpingID = Animator.StringToHash("IsJumping");
        animHitID = Animator.StringToHash("Hit");
    }

    void Update()
    {
        if (player == null) return;

        // --- Logic การเคลื่อนที่และการโจมตี ---
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // เช็คว่ากำลังเล่น Animation โจมตีอยู่ไหม
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        isAttacking = stateInfo.IsName("attack") || stateInfo.IsName("Use power"); 

        if (isAttacking)
        {
            // ถ้ากำลังโจมตี ให้หยุดเดินและหันหน้าหาผู้เล่น
            agent.isStopped = true;
            FaceTarget(player.position);
            agent.velocity = Vector3.zero; // บังคับหยุดเดินจริงๆ
        }
        else
        {
            // ถ้าไม่ได้โจมตี ให้เดินหาผู้เล่น
            agent.isStopped = false;
            agent.SetDestination(player.position);

            // เช็คเงื่อนไขการโจมตี
            CheckAttack(distanceToPlayer);
        }

        // --- Update Animator ---
        UpdateAnimator();
    }

    void CheckAttack(float distance)
    {
        // 1. ท่าพิเศษ (Use Power) - เท่ๆ ใช้ได้เมื่ออยู่ในระยะกลาง และ Cooldown พร้อม
        if (distance <= powerRange && distance > attackRange && Time.time >= lastPowerTime + powerCooldown)
        {
             PerformUsePower();
             return;
        }

        // 2. ท่าโจมตีปกติ (Attack) - ระยะประชิด
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
        
        // สุ่มเสียงโจมตี หรือ Effect ตรงนี้ได้
        Debug.Log("Boss: Attack!");
    }

    void PerformUsePower()
    {
        animator.SetTrigger(animPowerID);
        lastPowerTime = Time.time;
        
        Debug.Log("Boss: Use Power! (So Cool)");
    }

    void UpdateAnimator()
    {
        // ส่งความเร็วไปให้ Animator (Speed)
        // ใช้ velocity.magnitude เพื่อดูความเร็วรวม
        float speed = agent.velocity.magnitude;
        animator.SetFloat(animSpeedID, speed);

        // จัดการเรื่อง Jump / Ground
        // เนื่องจาก NavMeshAgent เดินบนพื้นตลอด (ยกเว้น OffMeshLink) เราจะสมมติว่าอยู่บนพื้นตลอดไปก่อน
        // ถ้าอยากให้กระโดดข้ามสิ่งกีดขวางจริงๆ ต้องใช้ OffMeshLink แต่ในที่นี้จะ Set ให้ True ตลอด
        animator.SetBool(animIsGroundedID, true);

        // (Option) ถ้าอยากทำ Animation กระโดดหลอกๆ (เช่น กดปุ่มเทส)
        // if (Input.GetKeyDown(KeyCode.Space)) animator.SetTrigger(animIsJumpingID);
    }

    // ฟังก์ชันช่วยหมุนตัวหาเป้าหมาย (ให้ดูเป็นธรรมชาติ)
    void FaceTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // ไม่ต้องเงยหน้าหรือก้ม
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    // ฟังก์ชันนี้ไว้เรียกจาก Animation Event (ตอนดาบฟันโดน)
    public void DealDamage()
    {
        // เช็คระยะอีกทีก็ได้ หรือใช้ Collider ที่ดาบ (ระยะ + นิดหน่อยเผื่ออนิเมชั่นพุ่งไปข้างหน้า)
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange + 1.0f)
        {
            // โค้ดลดเลือดผู้เล่น
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(10);
            }
            
            Debug.Log("Boss: Deal Damage to Player!");
        }
    }
    
    // ฟังก์ชันรับดาเมจ (Parry/Hit)
    public void TakeDamage()
    {
        animator.SetTrigger(animHitID);
        // ลดเลือดบอส...
    }
}
