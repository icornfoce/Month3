using UnityEngine;

public class HealthManager : MonoBehaviour
{
    public float health = 100f;
    private bool isDead = false;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDead) return;

        // อัปเดตค่าเข้า Animator ตลอดเวลา
        anim.SetFloat("health", health);

        if (health <= 0)
        {
            Die();
        }

        // ทดสอบกด F/G
        if (Input.GetKeyDown(KeyCode.F)) health -= 10;
        if (Input.GetKeyDown(KeyCode.G)) health += 10;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // 1. เล่นแอนิเมชันตาย
        anim.SetTrigger("Is Dead");
        anim.SetFloat("health", 0); // บังคับให้เป็น 0 เพื่อความชัวร์

        // 2. หยุดแรง Rigidbody ทันที
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true; // ล็อคไม่ให้ขยับได้อีก
        }

        // 3. ปิดสคริปต์การควบคุม (ต้องพิมพ์ชื่อไฟล์สคริปต์ให้ตรงกับที่คุณตั้งชื่อไว้)
        // ถ้าคุณตั้งชื่อไฟล์ว่า PlayerController หรือ PlayerMovement ให้เปลี่ยนชื่อตามนั้นครับ
        if (GetComponent<PlayerMovement>()  != null)
            GetComponent<PlayerMovement>().enabled = false;

        if (GetComponent<PlayerAttack>() != null)
            GetComponent<PlayerAttack>().enabled = false;

        Debug.Log("Game Over: Player is Dead");
    }
}