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
    private Transform cam;

    private float turnSmoothVelocity;
    private float speedSmoothVelocity;
    private float currentSpeed;
    private Vector3 movementInput;
    private bool isRunning;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        // ถ้าหา Animator ไม่เจอที่ Root ให้ลองหาใน Child
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }

        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }
        else
        {
            Debug.LogError("PlayerMovement: ไม่พบ Camera.main! ตรวจสอบว่ากล้องมี Tag 'MainCamera'");
        }

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        // รับ Input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        movementInput = new Vector3(horizontal, 0f, vertical).normalized;
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // ส่งค่าเข้า Blend Tree
        float targetAnimSpeed = movementInput.magnitude > 0.1f ? (isRunning ? 2f : 1f) : 0f;

        if (anim != null)
        {
            anim.SetFloat("Velocity Z", targetAnimSpeed, 0.1f, Time.deltaTime);
            anim.SetFloat("Velocity Y", 0f, 0.1f, Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        // ตรวจสอบว่า anim และ cam ไม่เป็น null
        if (anim != null && anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        if (cam == null) return; // ถ้าไม่มีกล้องจะเดินไม่ได้

        if (movementInput.magnitude >= 0.1f)
        {
            // === ส่วนสำคัญของ Relative Movement ===

            // เอาทิศกล้องมา แต่ตัดแกน Y ออก
            Vector3 camForward = cam.forward;
            Vector3 camRight = cam.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            // คำนวณทิศทางที่ต้องเดินตามกล้อง
            Vector3 targetMoveDir = (camForward * movementInput.z + camRight * movementInput.x).normalized;

            float targetSpeed = isRunning ? runSpeed : walkSpeed;
            
            // Smooth Speed (ค่อยๆ เร่ง/ผ่อน)
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, 0.1f);

            // หมุนตัวละครแบบ Smooth (ค่อยๆ หันหน้า)
            float targetAngle = Mathf.Atan2(targetMoveDir.x, targetMoveDir.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref turnSmoothVelocity,
                rotationSmoothTime
            );

            rb.MoveRotation(Quaternion.Euler(0f, smoothAngle, 0f));

            // เคลื่อนที่ตามทิศทางที่หมุนไป (Relative to Character Forward which is now target dir-ish)
            // หรือใช้ targetMoveDir โดยตรงเพื่อให้แม่นยำตามกล้อง
            rb.linearVelocity = new Vector3(
                targetMoveDir.x * currentSpeed,
                rb.linearVelocity.y,
                targetMoveDir.z * currentSpeed
            );
        }
        else
        {
            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0f, ref speedSmoothVelocity, 0.1f);
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }
}
