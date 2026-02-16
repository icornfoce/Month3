using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    [Range(0.01f, 0.5f)]
    public float rotationSmoothTime = 0.12f;

    private Rigidbody rb;
    private Animator anim;
    [Header("Camera Reference")]
    public Transform cam;

    private float turnSmoothVelocity;
    private float speedSmoothVelocity;
    private float currentSpeed;
    private Vector3 movementInput;
    private bool isRunning;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        
        // ถ้าไม่ได้ลากกล้องใส่ใน Inspector ให้หา Main Camera อัตโนมัติ
        if (cam == null && Camera.main != null) 
        {
            cam = Camera.main.transform;
        }
        
        rb.freezeRotation = true;
    }

    void Update()
    {
        // เช็คว่ากำลังเล่นอนิเมชั่นโจมตีอยู่หรือไม่ (ต้องตั้ง Tag ที่ State ใน Animator ว่า Attack)
        bool isAttacking = anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack");

        if (isAttacking)
        {
            movementInput = Vector3.zero;
            // แก้เป็น Velocity Y ตามรูป Animator ของคุณ
            anim.SetFloat("Velocity Y", 0f, 0.1f, Time.deltaTime);
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        movementInput = new Vector3(horizontal, 0f, vertical).normalized;

        // ตรวจสอบกล้องเผื่อกรณีที่มีการเปลี่ยนกล้องหรือกล้องยังไม่ถูกโหลด
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // ส่งค่าไปที่ Blend Tree (0 = Idle, 1 = Walk, 2 = Run)
        float targetAnimSpeed = movementInput.magnitude > 0.1f ? (isRunning ? 2f : 1f) : 0f;

        if (anim != null)
        {
            // แก้เป็น Velocity Y ให้ตรงกับ Parameter ใน Unity ของคุณ
            anim.SetFloat("Velocity Y", targetAnimSpeed, 0.1f, Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        // ล็อคไม่ให้เลื่อนตำแหน่งขณะโจมตี
        if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        if (cam == null || movementInput.magnitude < 0.1f)
        {
            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0f, ref speedSmoothVelocity, 0.1f);
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        Vector3 camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(cam.right, new Vector3(1, 0, 1)).normalized;
        Vector3 targetMoveDir = (camForward * movementInput.z + camRight * movementInput.x).normalized;

        float targetSpeed = isRunning ? runSpeed : walkSpeed;
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, 0.1f);

        float targetAngle = Mathf.Atan2(targetMoveDir.x, targetMoveDir.z) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
        rb.MoveRotation(Quaternion.Euler(0f, smoothAngle, 0f));

        rb.linearVelocity = new Vector3(targetMoveDir.x * currentSpeed, rb.linearVelocity.y, targetMoveDir.z * currentSpeed);
    }
}