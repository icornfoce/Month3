using UnityEngine;
using System.Collections;

public class BossProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 14f;             // ความเร็วกระสุนตอนพุ่งไป Player
    public float damage = 25f;            // ดาเมจ
    public float hoverDuration = 5f;      // เวลาลอยอยู่กับที่ก่อนพุ่ง (วินาที)
    public float lifetimeAfterLaunch = 5f; // เวลาก่อน auto-destroy หลังพุ่งออก

    [HideInInspector]
    public Transform target;              // ตัว Player (ตั้งค่าจาก BossAI)

    private bool isLaunched = false;
    private Vector3 launchDirection;

    void Start()
    {
        // ตั้งค่า Rigidbody อัตโนมัติ
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        // ตั้งค่า Collider ให้เป็น Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // เริ่ม Coroutine: รอ hoverDuration แล้วค่อยพุ่ง
        StartCoroutine(HoverThenLaunch());
    }

    IEnumerator HoverThenLaunch()
    {
        // === ถ้า hoverDuration <= 0 → ยิงทันทีไม่ต้อง hover ===
        if (hoverDuration <= 0f)
        {
            // ยิงตรงไปตามทิศทางที่หันอยู่ (ไม่ track Player)
            if (target != null)
            {
                Vector3 targetPoint = target.position + Vector3.up * 1.0f;
                launchDirection = (targetPoint - transform.position).normalized;
            }
            else
            {
                launchDirection = transform.forward;
            }

            isLaunched = true;
            Debug.Log("BossProjectile: Instant Launch!");
            Destroy(gameObject, lifetimeAfterLaunch);
            yield break;
        }

        // === ลอยอยู่กับที่ ===
        yield return new WaitForSeconds(hoverDuration);

        // === คำนวณทิศทางไป Player (ตอนที่พุ่ง) ===
        if (target != null)
        {
            Vector3 targetPoint = target.position + Vector3.up * 1.0f;
            launchDirection = (targetPoint - transform.position).normalized;
        }
        else
        {
            launchDirection = transform.forward;
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
        // ไม่ชนตัวเอง/บอส
        if (other.CompareTag("Boss")) return;

        // ชน Player → ทำดาเมจ → ทำลายตัวเอง
        if (other.CompareTag("Player"))
        {
            HealthManager hm = other.GetComponent<HealthManager>();
            if (hm == null) hm = other.GetComponentInParent<HealthManager>();
            if (hm != null)
            {
                hm.TakeDamage(damage);
                Debug.Log($"BossProjectile: Hit Player! Dealt {damage} damage.");
            }

            Destroy(gameObject);
            return;
        }

        // ชนพื้น/กำแพง → ทำลายตัวเอง
        Destroy(gameObject);
    }

    // Fallback: ถ้า Collider ไม่ได้เป็น Trigger
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Boss")) return;

        if (collision.collider.CompareTag("Player"))
        {
            HealthManager hm = collision.collider.GetComponent<HealthManager>();
            if (hm == null) hm = collision.collider.GetComponentInParent<HealthManager>();
            if (hm != null)
            {
                hm.TakeDamage(damage);
                Debug.Log($"BossProjectile: Hit Player (Collision)! Dealt {damage} damage.");
            }
        }

        Destroy(gameObject);
    }
}
