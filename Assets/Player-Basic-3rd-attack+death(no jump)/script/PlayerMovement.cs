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
    private bool isInvincible = false;
    [HideInInspector] public bool isDodgeLockingMovement = false;

    private Rigidbody rb;
    private Animator anim;
    
    [Header("Camera Reference")]
    public Transform cam;

    [Header("Stamina Reference")]
    public StaminaManager staminaManager;

    [Header("Attack Reference")]
    public PlayerAttack playerAttack;

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

        // ใช้ isDodgeLockingMovement แทน isDodgingTag เพื่อให้คลายล็อคได้เร็วกว่าแอนิเมชันจบ
        if (isAttacking || isDodgeLockingMovement)
        {
            movementInput = Vector3.zero;
            anim.SetFloat("Velocity Y", 0f, 0.1f, Time.deltaTime);
            return;
        }

        // รับ Input เดินปกติ
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // Stamina integration for running
        if (isRunning && movementInput.magnitude > 0.11f)
        {
            if (staminaManager != null)
            {
                // We consume per frame, so we multiply by delta time
                bool hasStamina = staminaManager.UseStamina(staminaManager.runStaminaCost * Time.deltaTime);
                if (!hasStamina)
                {
                    isRunning = false;
                }
            }
        }

        float targetAnimSpeed = movementInput.magnitude > 0.1f ? (isRunning ? 2f : 1f) : 0f;

        if (anim != null)
        {
            anim.SetFloat("Velocity Y", targetAnimSpeed, 0.1f, Time.deltaTime);
        }
    }

    IEnumerator PerformDodgeRootMotion()
    {
        canDodge = false;
        isInvincible = true;

        // หันหน้าไปทิศที่จะหลบก่อนเริ่มพุ่ง
        if (movementInput.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(GetMoveDirection());
        }

        // เปิด Root Motion ให้แอนิเมชันพาตัวละครขยับ (Not In-place)
        anim.applyRootMotion = true;
        isDodgeLockingMovement = true;
        anim.SetTrigger("isDodging");

        yield return new WaitForSeconds(iframeDuration);
        isInvincible = false;

        // รอจนพ้นระยะเวลา Lockout ที่ตั้งไว้ใน Inspector
        // คำนวณเวลาที่เหลือหลังจากผ่านช่วง iframe ไปแล้ว
        float remainingLockout = Mathf.Max(0, dodgeLockoutDuration - iframeDuration);
        if (remainingLockout > 0)
        {
            yield return new WaitForSeconds(remainingLockout); 
        }

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