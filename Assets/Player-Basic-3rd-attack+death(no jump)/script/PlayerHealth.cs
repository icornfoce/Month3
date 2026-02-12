using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings (ค่าพลังชีวิต)")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Status (สถานะ)")]
    public bool isDead = false;

    // เก็บ Reference ไปยัง Component อื่นๆ เพื่อปิดการใช้งานเมื่อตาย
    private PlayerMovement movement;
    private PlayerAttack attack;
    private Animator anim;

    void Start()
    {
        currentHealth = maxHealth;
        
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        anim = GetComponent<Animator>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"Player HP: {currentHealth}/{maxHealth}");

        // ถ้ามี Animation เจ็บ (Hurt/Hit) ให้ใส่ตรงนี้
        // if (anim != null) anim.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player Died!");

        // เล่น Animation ตาย
        if (anim != null) 
        {
            anim.SetTrigger("Death"); // ต้องมี Parameter "Death" ใน Animator
            // หรือถ้าใช้แบบเดิมของโมเดลนี้อาจจะเป็น bool isDead
            // anim.SetBool("IsDead", true); 
        }

        // ปิดการควบคุม
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;
        
        // ปิด Physics หรือ Collider ถ้าจำเป็น
        // GetComponent<CapsuleCollider>().enabled = false;
        // GetComponent<Rigidbody>().isKinematic = true;
    }
}
