using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings (ค่าพลังชีวิต)")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Status (สถานะ)")]
    public bool isDead = false;
    public bool isTakingDamage = false;

    [Header("Damage Stagger Settings")]
    public float takeDamageDuration = 0.5f;
    private Coroutine staggerCoroutine;


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

        // รีเซ็ตค่าเพื่อป้องกันการเล่นอัตโนมัติตอนเริ่มเกม
        if (anim != null) anim.SetBool("IstakeDMG", false);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"Player HP: {currentHealth}/{maxHealth}");

        // เล่น Animation เจ็บ และตั้งสถานะล็อคการโจมตี
        if (anim != null) 
        {
            if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
            staggerCoroutine = StartCoroutine(TakeDamageAnimation());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator TakeDamageAnimation()
    {
        isTakingDamage = true;
        anim.SetBool("IstakeDMG", true);

        yield return new WaitForSeconds(takeDamageDuration);

        isTakingDamage = false;
        anim.SetBool("IstakeDMG", false);
        staggerCoroutine = null;
    }

    public void ClearStagger()
    {
        if (staggerCoroutine != null)
        {
            StopCoroutine(staggerCoroutine);
            staggerCoroutine = null;
        }

        isTakingDamage = false;
        if (anim != null) anim.SetBool("IstakeDMG", false);
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player Died!");

        // เล่น Animation ตาย
        if (anim != null) 
        {
            anim.SetTrigger("Death");
        }

        // ปิดการควบคุม
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;
    }
}
