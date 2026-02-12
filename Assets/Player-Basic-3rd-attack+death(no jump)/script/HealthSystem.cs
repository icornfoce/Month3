using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public float health = 100f;
    private bool isDead = false;

    private Animator anim;
    private Rigidbody rb;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // ตรวจสอบตลอดเวลาว่าตายหรือยัง และเลือดเหลือ 0 ไหม
        if (!isDead && health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        // 1. สั่งเล่น Animation ตาย (ตามที่ตั้งชื่อ Trigger ไว้)
        anim.SetTrigger("Is Dead");

        // 2. หยุดการเคลื่อนที่ทั้งหมด
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true; // ล็อคตัวละครไม่ให้โดนผลัก
        }

        // 3. ปิด Script การควบคุมอื่นๆ (ถ้ามีแยกไฟล์)
        // ยกตัวอย่างเช่น ปิดไฟล์ PlayerController เพื่อไม่ให้เดินได้อีก
        if (GetComponent<PlayerMovement>() != null)
        {
            GetComponent<PlayerMovement>().enabled = false;
        }

        Debug.Log("Player is Dead");
    }

    // ฟังก์ชันสำหรับรับดาเมจ (เอาไว้เรียกใช้จาก Script อื่น เช่น ศัตรูมาตี)
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        health -= damage;
    }
}