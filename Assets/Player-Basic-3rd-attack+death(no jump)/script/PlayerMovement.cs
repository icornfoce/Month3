using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    [Range(0.01f, 0.5f)]
    public float rotationSmoothTime = 0.12f;

    [Header("Dodge Settings (Root Motion)")]
    public float dodgeCooldown = 1f;
    public float dodgeLockoutDuration = 0.5f;
    public float iframeDuration = 0.5f;
    private bool canDodge = true;
    [Header("Invincibility")]
    // ถ้าเปิดเป็น true จะเป็นอมตะ (ไม่เสียเลือด)
    public bool isImmortal = false;
    // แสดงสถานะ iframe ปัจจุบัน (อ่านได้จากสคริปต์อื่น)
    public bool IsInvincible { get; private set; } = false;
    [HideInInspector] public bool isDodgeLockingMovement = false;

    private Rigidbody rb;
    private Animator anim;
    
    [Header("Camera Reference")]
    public Transform cam;

    [Header("Stamina Reference")]
    public StaminaManager staminaManager;

    [Header("Attack Reference")]
    public PlayerAttack playerAttack;

    [Header("Health Reference")]
    public HealthManager healthManager;

    [Header("VFX Settings")]
    public GameObject runVFX;
    public GameObject dodgeVFX;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip walkSFX;
    public AudioClip runSFX;
    public AudioClip dodgeSFX;

    // ตัวแปรที่ใช้คำนวณความเร็วและการหมุน (แก้ Error CS0103)
    private float turnSmoothVelocity;
    private float speedSmoothVelocity;
    private float currentSpeed;
    private Vector3 movementInput;
    private bool isRunning;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        
        rb.freezeRotation = true;
        // ปิด Root Motion ไว้ก่อนเพื่อให้โค้ดคุมการเดินปกติ
        anim.applyRootMotion = false; 

        if (playerAttack == null) playerAttack = GetComponent<PlayerAttack>();
        if (healthManager == null) healthManager = GetComponent<HealthManager>();

        // Ensure VFX are off at start
        SetVFXState(runVFX, false);
        SetVFXState(dodgeVFX, false);
    }

    void Update()
    {
        // อัปเดตทิศทางการเคลื่อนไหวก่อนเสมอ เพื่อให้การหลบ (Dodge) รู้ทิศทางที่ต้องการแม้ขณะโจมตี
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        movementInput = new Vector3(horizontal, 0f, vertical).normalized;

        bool isAttacking = anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
        bool isDodging = anim.GetCurrentAnimatorStateInfo(0).IsTag("Dodge");

        // รับ Input หลบ (อนุญาตให้หลบขณะโจมตีได้)
        if (Input.GetKeyDown(KeyCode.Space) && canDodge && !isDodging)
        {
            // ถ้ากำลังโจมตีอยู่ ให้ยกเลิกการโจมตี
            if (isAttacking && playerAttack != null)
            {
                playerAttack.CancelAttack();
            }

            // ถ้ากำลังติดสถานะโดนดาเมจ ให้ยกเลิกสถานะนั้นเพื่อหลบได้ทันที (Dodge Cancel Stagger)
            if (healthManager != null && healthManager.isTakingDamage)
            {
                healthManager.ClearStagger();
            }

            if (staminaManager != null)
            {
                if (staminaManager.UseStamina(staminaManager.dodgeStaminaCost))
                {
                    StartCoroutine(PerformDodgeRootMotion());
                }
            }
            else
            {
                StartCoroutine(PerformDodgeRootMotion());
            }
        }

        bool isTakingDMG = healthManager != null && healthManager.isTakingDamage;

        // ใช้ isDodgeLockingMovement แทน isDodgingTag เพื่อให้คลายล็อคได้เร็วกว่าแอนิเมชันจบ
        if (isAttacking || isDodgeLockingMovement || isTakingDMG)
        {
            movementInput = Vector3.zero;
            if (anim != null) 
            {
                // ถ้าโดนดาเมจ ให้หยุดทันทีแบบไม่มี damping
                if (isTakingDMG) anim.SetFloat("Velocity Y", 0f);
                else anim.SetFloat("Velocity Y", 0f, 0.1f, Time.deltaTime);
            }
            return;
        }

        // รับ Input เดินปกติ
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // Stamina integration for running
        if (isRunning && movementInput.magnitude > 0.11f)
        {
            if (staminaManager != null)
            {
                bool hasStamina = staminaManager.UseStamina(staminaManager.runStaminaCost * Time.deltaTime);
                if (!hasStamina) isRunning = false;
            }
        }

        float targetAnimSpeed = movementInput.magnitude > 0.1f ? (isRunning ? 2f : 1f) : 0f;

        if (anim != null)
        {
            anim.SetFloat("Velocity Y", targetAnimSpeed, 0.1f, Time.deltaTime);
        }

        // Handle Run VFX
        if (runVFX != null)
        {
            bool shouldShowRunVFX = isRunning && movementInput.magnitude > 0.11f && !isDodging && !isAttacking;
            SetVFXState(runVFX, shouldShowRunVFX);
        }

        // Handle Run Audio
        if (audioSource != null)
        {
            bool isMoving = movementInput.magnitude > 0.11f && !isDodging && !isAttacking;
            
            if (isMoving)
            {
                AudioClip targetClip = isRunning ? runSFX : walkSFX;

                if (targetClip != null)
                {
                    if (!audioSource.isPlaying || audioSource.clip != targetClip)
                    {
                        audioSource.clip = targetClip;
                        audioSource.loop = true;
                        audioSource.Play();
                    }
                }
                else
                {
                    audioSource.Stop();
                }
            }
            else
            {
                // Stop audio if not moving (and not playing a one-shot like dodge which uses PlayOneShot)
                // Note: PlayOneShot doesn't change audioSource.clip, so we check if the looping clip is movement sfx
                if (audioSource.isPlaying && (audioSource.clip == runSFX || audioSource.clip == walkSFX))
                {
                    audioSource.Stop();
                }
            }
        }
    }

    IEnumerator PerformDodgeRootMotion()
    {
        canDodge = false;
        IsInvincible = true;

        // ยันหน้าไปทิศที่จะหลบก่อนเริ่มพุ่ง
        if (movementInput.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(GetMoveDirection());
        }

        // เปิด Dodge VFX
        SetVFXState(dodgeVFX, true);

        // เล่นเสียง Dodge
        if (audioSource != null && dodgeSFX != null)
        {
            audioSource.PlayOneShot(dodgeSFX);
        }

        // เปิด Root Motion ให้แอนิเมชันพาตัวละครขยับ (Not In-place)
        anim.applyRootMotion = true;
        isDodgeLockingMovement = true;
        anim.SetTrigger("isDodging");
        Debug.Log("Dodge Coroutine: Started, anim 'isDodging' triggered.");

        yield return new WaitForSeconds(iframeDuration);
        IsInvincible = false;
        Debug.Log("Dodge Coroutine: Iframe ended.");

        // รอจนพ้นระยะเวลา Lockout ที่ตั้งไว้ใน Inspector
        // คำนวณเวลาที่เหลือหลังจากผ่านช่วง iframe ไปแล้ว
        float remainingLockout = Mathf.Max(0, dodgeLockoutDuration - iframeDuration);
        if (remainingLockout > 0)
        {
            yield return new WaitForSeconds(remainingLockout); 
        }

        // ปิด Dodge VFX
        SetVFXState(dodgeVFX, false);

        // ปลดล็อคการเคลื่อนที่ทันที
        isDodgeLockingMovement = false;
        anim.applyRootMotion = false; 

        // คำนวณ Cooldown ที่เหลือจริง
        float remainingCooldown = dodgeCooldown - Mathf.Max(iframeDuration, dodgeLockoutDuration);
        if (remainingCooldown > 0)
        {
            yield return new WaitForSeconds(remainingCooldown);
        }
        canDodge = true;
    }

    private void SetVFXState(GameObject vfxObj, bool active)
    {
        if (vfxObj == null) return;

        // Ensure the GameObject itself is active/inactive
        if (vfxObj.activeSelf != active) vfxObj.SetActive(active);

        ParticleSystem ps = vfxObj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            if (active)
            {
                if (!ps.isPlaying) ps.Play();
            }
            else
            {
                if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        
        // Also check children for ParticleSystems
        ParticleSystem[] childPS = vfxObj.GetComponentsInChildren<ParticleSystem>();
        foreach (var cps in childPS)
        {
            if (active)
            {
                if (!cps.isPlaying) cps.Play();
            }
            else
            {
                if (cps.isPlaying) cps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    Vector3 GetMoveDirection()
    {
        if (cam == null) return transform.forward;
        Vector3 camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(cam.right, new Vector3(1, 0, 1)).normalized;
        return (camForward * movementInput.z + camRight * movementInput.x).normalized;
    }

    void FixedUpdate()
    {
        // ถ้ากำลังใช้ Root Motion (หลบ) ให้ข้ามการเซ็ต Velocity ในโค้ดไปเลย
        if (anim.applyRootMotion) return;

        if (movementInput.magnitude < 0.1f)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        Vector3 targetMoveDir = GetMoveDirection();
        float targetSpeed = isRunning ? runSpeed : walkSpeed;
        
        // ใช้ SmoothDamp เพื่อให้การเร่งความเร็วดูสมูท
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, 0.1f);

        float targetAngle = Mathf.Atan2(targetMoveDir.x, targetMoveDir.z) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
        rb.MoveRotation(Quaternion.Euler(0f, smoothAngle, 0f));

        rb.linearVelocity = new Vector3(targetMoveDir.x * currentSpeed, rb.linearVelocity.y, targetMoveDir.z * currentSpeed);
    }
}