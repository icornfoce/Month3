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
        if (Input.GetKeyDown(KeyCode.H)) TakeDamage(10);
        if (Input.GetKeyDown(KeyCode.G)) health += 10;
    }

    // ฟังก์ชันรับดาเมจจาก Boss
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;
        Debug.Log($"Player HP: {health}");



        // ถ้ามี Animation เจ็บ (Hit) ก็ใส่ตรงนี้ได้
        // anim.SetTrigger("Hit"); 
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

        // 3. ปิดสคริปต์การควบคุม
        if (GetComponent<PlayerMovement>()  != null)
            GetComponent<PlayerMovement>().enabled = false;

        if (GetComponent<PlayerAttack>() != null)
            GetComponent<PlayerAttack>().enabled = false;

        Debug.Log("Game Over: Player is Dead");
    }
}