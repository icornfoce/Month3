using UnityEngine;

public class PlayerMovement : MonoBehaviour // ตรวจสอบให้แน่ใจว่าชื่อ Class ตรงกับชื่อไฟล์ .cs
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    [Range(0.01f, 0.5f)]
    public float rotationSmoothTime = 0.12f;

    private Rigidbody rb;
    private Animator anim;
    private Transform cam;

    [Header("Jump Settings (ตั้งค่ากระโดด)")]
    public float jumpForce = 5f;
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;
    
    private bool isGrounded;

    private float turnSmoothVelocity;
    private Vector3 movementInput;
    private bool isRunning;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        // หา Main Camera
        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }
        else
        {
            // ถ้าไม่มี Tag MainCamera ให้ลองหา Camera ตัวแรกใน Scene
            Camera foundCam = FindFirstObjectByType<Camera>();
            if (foundCam != null)
            {
                cam = foundCam.transform;
                Debug.LogWarning("Warning: No camera tagged 'MainCamera'. Using " + foundCam.name + " instead.");
            }
            else
            {
                Debug.LogError("Error: No Camera found in the scene! Player cannot move relative to camera.");
            }
        }

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // สร้าง GroundCheck ให้อัตโนมัติถ้าไม่ได้ลากใส่
        if (groundCheck == null)
        {
            GameObject checkObj = new GameObject("GroundCheck");
            checkObj.transform.parent = transform;
            checkObj.transform.localPosition = new Vector3(0, 0.1f, 0); // ยกขึ้นนิดนึง
            groundCheck = checkObj.transform;
        }
    }

    void Update()
    {
        // 1. รับค่า Input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        movementInput = new Vector3(horizontal, 0f, vertical).normalized;

        isRunning = Input.GetKey(KeyCode.LeftShift);

        // --- Ground Check & Jump ---
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            // ถ้ามี Animation กระโดด ให้ใส่ตรงนี้
            // anim.SetTrigger("Jump"); 
        }
        // ---------------------------

        // 2. จัดการ Animation ใน Update (เพื่อให้ค่าสมูท)
        if (movementInput.magnitude >= 0.1f)
        {
            float speedMultiplier = isRunning ? 2f : 1f;
            // ส่งค่าไปที่ Parameter ของ Blend Tree (ต้องสะกดให้ตรงกับใน Animator)
            anim.SetFloat("valocity Y", horizontal * speedMultiplier, 0.1f, Time.deltaTime);
            anim.SetFloat("valocity Z", vertical * speedMultiplier, 0.1f, Time.deltaTime);
        }
        else
        {
            // ค่อยๆ ปรับกลับเป็น Idle (0,0)
            anim.SetFloat("valocity Y", 0f, 0.1f, Time.deltaTime);
            anim.SetFloat("valocity Z", 0f, 0.1f, Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        // 3. จัดการฟิสิกส์ใน FixedUpdate
        if (movementInput.magnitude >= 0.1f)
        {
            float targetAngle = transform.eulerAngles.y;
            if (cam != null)
            {
                targetAngle = Mathf.Atan2(movementInput.x, movementInput.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            }
            else
            {
                // ถ้าหากล้องไม่เจอ ให้เดินตามทิศทาง World Space ไปเลย (แก้ขัด)
                targetAngle = Mathf.Atan2(movementInput.x, movementInput.z) * Mathf.Rad2Deg;
            }

            // หมุนตัวละคร
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
            rb.MoveRotation(Quaternion.Euler(0f, angle, 0f));

            // เคลื่อนที่
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, rb.linearVelocity.y, moveDir.z * currentSpeed);
        }
        else
        {
            // หยุดนิ่งแต่ยังคงแรงโน้มถ่วง
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }

        // เช็คว่ากำลังเล่นท่าโจมตีอยู่หรือไม่ (Base Layer index 0)
        if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            // ถ้าโจมตีอยู่ ให้หยุดเดินทันที
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }
    }

   
}