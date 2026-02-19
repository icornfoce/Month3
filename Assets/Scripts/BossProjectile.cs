using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 14f;
    public float damage = 25f;
    public float hoverDuration = 5f;
    public float lifetimeAfterLaunch = 5f;

    [Header("Homing Settings (สำหรับกระสุนติดตาม)")]
    public bool isHoming = false;         // เปิดโหมดติดตามหรือไม่
    public float turnSpeed = 5.0f;        // ความไวในการเลี้ยว (ยิ่งเยอะยิ่งเลี้ยวเก่ง)

    [HideInInspector] public Transform target;

    // สถานะภายใน
    private enum State { WaitingInit, Hovering, Launched }
    private State state = State.WaitingInit;
    private float timer = 0f;
    private Vector3 launchDirection;

    void Awake()
    {
        // ตั้งค่า Rigidbody + Collider ทันทีตอน Instantiate
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    /// <summary>
    /// เรียกจาก BossAI หลังตั้งค่า speed, damage, hoverDuration, target แล้ว
    /// </summary>
    public void Initialize()
    {
        timer = 0f;

        if (hoverDuration <= 0f)
        {
            // ยิงทันที ไม่ต้อง hover
            CalculateLaunchDirection();
            state = State.Launched;
            Destroy(gameObject, lifetimeAfterLaunch);
            Debug.Log($"BossProjectile: Instant Launch! dir={launchDirection} Homing={isHoming}");
        }
        else
        {
            state = State.Hovering;
            Debug.Log($"BossProjectile: Hovering for {hoverDuration}s. target={(target != null ? target.name : "null")}");
        }
    }

    void CalculateLaunchDirection()
    {
        if (target != null)
        {
            Vector3 targetPoint = target.position + Vector3.up * 1.0f; // เล็งที่ตัว (สูงขึ้นหน่อย)
            launchDirection = (targetPoint - transform.position).normalized;
        }
        else
        {
            launchDirection = transform.forward;
        }
    }

    void Update()
    {
        switch (state)
        {
            case State.WaitingInit:
                break;

            case State.Hovering:
                timer += Time.deltaTime;
                if (timer >= hoverDuration)
                {
                    // หมดเวลา hover → พุ่งไปหา Player!
                    CalculateLaunchDirection();
                    state = State.Launched;
                    timer = 0f;
                    Destroy(gameObject, lifetimeAfterLaunch);
                    Debug.Log($"BossProjectile: LAUNCHED! dir={launchDirection} speed={speed}");
                }
                break;

            case State.Launched:
                timer += Time.deltaTime;

                // === Logic ติดตาม (Homing) ===
                if (isHoming && target != null)
                {
                    Vector3 targetPoint = target.position + Vector3.up * 1.0f;
                    Vector3 toTarget = targetPoint - transform.position;
                    float dist = toTarget.magnitude;

                    // ถ้าใกล้มาก (< 1.0 เมตร) พุ่งใส่เลย ไม่ต้องเลี้ยวแล้ว (กันวนรอบตัว)
                    if (dist < 1.0f)
                    {
                        launchDirection = toTarget.normalized;
                    }
                    else
                    {
                        Vector3 desiredDir = toTarget.normalized;
                        // ถ้าใกล้ (< 3.0 เมตร) เลี้ยวไวขึ้น 3 เท่า
                        float currentTurnSpeed = (dist < 3.0f) ? turnSpeed * 3f : turnSpeed;
                        launchDirection = Vector3.Slerp(launchDirection, desiredDir, currentTurnSpeed * Time.deltaTime);
                    }
                }

                // === Manual Check Collision (กันทะลุ) ===
                // ยิง Ray ไปข้างหน้าเท่าระยะที่จะขยับเฟรมนี้
                float moveDist = speed * Time.deltaTime;
                if (Physics.Raycast(transform.position, launchDirection, out RaycastHit hit, moveDist * 1.5f)) // *1.5f เผื่อหน่อย
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        DealDamageToPlayer(hit.collider.gameObject);
                        Destroy(gameObject);
                        return; // จบเฟรมนี้
                    }
                    else if (timer >= 0.3f && !hit.collider.CompareTag("Boss"))
                    {
                         // ชนกำแพง/พื้น
                         Destroy(gameObject);
                         return;
                    }
                }

                // สั่งเคลื่อนที่
                transform.position += launchDirection * moveDist;
                
                // หันหน้ากระสุนไปตามทิศที่พุ่งไป
                if (launchDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(launchDirection);
                }
                break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Boss")) return;
        if (state != State.Launched) return;
        
        if (other.CompareTag("Player"))
        {
            DealDamageToPlayer(other.gameObject);
            Destroy(gameObject);
            return;
        }

        if (timer < 0.3f) return;
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Boss")) return;
        if (state != State.Launched) return;

        // ถ้าชน Player → ชนได้เลยไม่ต้องรอ Grace Time
        if (collision.collider.CompareTag("Player"))
        {
            DealDamageToPlayer(collision.collider.gameObject);
            Destroy(gameObject);
            return;
        }

        // ถ้าชนอย่างอื่น (กำแพง/พื้น) → รอ 0.3 วิ
        if (timer < 0.3f) return;

        Destroy(gameObject);
    }

    void DealDamageToPlayer(GameObject playerObj)
    {
        // เช็คว่า Player กำลัง Parry อยู่หรือไม่ (เผื่อ Colliders อยู่ในลูกหลาน)
        PlayerParry pp = playerObj.GetComponent<PlayerParry>();
        if (pp == null) pp = playerObj.GetComponentInParent<PlayerParry>();

        if (pp != null && pp.isParryingState)
        {
            Debug.Log("<color=green>Projectile Parry SUCCESSFUL! Damage blocked.</color>");
            pp.SuccessfulParry(); // สั่งให้เล่นแอนิเมชันสำเร็จทันที
            return; // ไม่ทำดาเมจ
        }

        HealthManager hm = playerObj.GetComponent<HealthManager>();
        if (hm == null) hm = playerObj.GetComponentInParent<HealthManager>();
        
        if (hm != null)
        {
            // เช็คว่า Player กำลังหลบอยู่หรือไม่ (Invincible)
            PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();
            if (movement == null) movement = playerObj.GetComponentInParent<PlayerMovement>();
            
            if (movement != null && movement.IsInvincible)
            {
                Debug.Log("<color=blue>Player DODGED the projectile!</color>");
                return;
            }

            hm.TakeDamage(damage);
            Debug.Log($"<color=red>Projectile Parry FAILED! Dealt {damage} damage to Player.</color>");
        }
    }
}
