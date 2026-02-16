using UnityEngine;
using System.Collections;

public class BossProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 14f;             // ความเร็วกระสุนตอนพุ่งไป Player
    public float damage = 25f;            // ดาเมจ
    public float hoverDuration = 5f;      // เวลาลอยอยู่กับที่ก่อนพุ่ง (วินาที)
    public float lifetimeAfterLaunch = 8f; // เวลาก่อน auto-destroy หลังพุ่งออก

    [HideInInspector]
    public Transform target;              // ตัว Player (ตั้งค่าจาก BossAI)

    private bool isLaunched = false;
    private Vector3 launchDirection;

    void Start()
    {
        // === ตั้งค่า Rigidbody อัตโนมัติ ===
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;                // ไม่ใช้ฟิสิกส์ (ไม่ตกลงพื้น)
        rb.useGravity = false;                // ปิดแรงโน้มถ่วง
        rb.freezeRotation = true;             // ล็อคการหมุนทุกแกน
        rb.constraints = RigidbodyConstraints.FreezeRotation; // ล็อค Rotation X, Y, Z

        // === ตั้งค่า Collider ให้เป็น Trigger อัตโนมัติ ===
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;  // ต้องเป็น Trigger เพื่อใช้ OnTriggerEnter
            col.enabled = false;   // ปิดไว้ก่อนตอน Hover
        }

        // ล็อค Rotation ไม่ให้หมุน
        transform.rotation = Quaternion.identity;

        // เริ่ม Coroutine: รอ hoverDuration แล้วค่อยพุ่ง
        StartCoroutine(HoverThenLaunch());
    }

    IEnumerator HoverThenLaunch()
    {
        // === ลอยอยู่กับที่ ===
        yield return new WaitForSeconds(hoverDuration);

        // === คำนวณทิศทางไป Player (ตอนที่พุ่ง) ===
        if (target != null)
        {
            // เล็งไปที่ตัว (บวกความสูงขึ้นมาหน่อย ไม่ให้เล็งเท้า)
            Vector3 targetPoint = target.position + Vector3.up * 1.2f;
            launchDirection = (targetPoint - transform.position).normalized;
        }
        else
        {
            launchDirection = transform.forward;
        }

        // เปิด Collider เมื่อพุ่งแล้ว
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        isLaunched = true;
        Debug.Log("BossProjectile: Launched!");

        // Auto-destroy หลังพุ่งไปสักพัก
        Destroy(gameObject, lifetimeAfterLaunch);
    }

    void Update()
    {
        if (!isLaunched) return;

        // พุ่งไปตามทิศทางที่คำนวณไว้
        transform.position += launchDirection * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isLaunched) return;

        // ★★★ ทำลายเฉพาะเมื่อชน Player เท่านั้น ★★★
        if (other.CompareTag("Player"))
        {
            HealthManager hm = other.GetComponent<HealthManager>();
            if (hm != null)
            {
                hm.TakeDamage(damage);
                Debug.Log($"BossProjectile: Hit Player! Dealt {damage} damage.");
            }

            Destroy(gameObject);
        }

        // ชนอย่างอื่น (พื้น, กำแพง, Boss) → ไม่ทำอะไร ไม่ทำลาย
    }
}
