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

    private PlayerMovement movement;
    private PlayerAttack attack;
    private Animator anim;

    void Start()
    {
        currentHealth = maxHealth;
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        anim = GetComponentInChildren<Animator>();

        // ตั้งค่าเริ่มต้นใน Animator
        if (anim != null)
        {
            if (HasParameter("health")) anim.SetFloat("health", currentHealth);
            if (HasParameter("IstakeDMG")) anim.SetBool("IstakeDMG", false);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        // Respect player invincibility / immortality
        if (movement != null)
        {
            if (movement.isImmortal || movement.IsInvincible)
            {
                Debug.Log("Damage ignored: player is invincible/immortal.");
                return;
            }
        }

        currentHealth -= amount;
        Debug.Log($"Player HP: {currentHealth}/{maxHealth}");

        // 1. ส่งค่าเลือดไปที่ Animator (ชื่อตัวแปร health ตัวพิมพ์เล็ก)
        if (anim != null && HasParameter("health"))
        {
            anim.SetFloat("health", currentHealth);
        }

        // 2. เช็คเงื่อนไขตาย (ถ้าน้อยกว่า 0.1 ให้เล่นแอนิเมชัน Death)
        if (currentHealth < 0.1f)
        {
            Die();
            return;
        }

        // 3. เล่นแอนิเมชันเจ็บ (ใช้ชื่อ IstakeDMG ตามที่คุณแจ้ง)
        if (anim != null && !isDead)
        {
            if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
            staggerCoroutine = StartCoroutine(TakeDamageAnimation());
        }
    }

    private IEnumerator TakeDamageAnimation()
    {
        isTakingDamage = true;

        // เริ่มเล่นท่าเจ็บ
        if (HasParameter("IstakeDMG")) anim.SetBool("IstakeDMG", true);

        yield return new WaitForSeconds(takeDamageDuration);

        // จบจังหวะชะงัก
        if (HasParameter("IstakeDMG")) anim.SetBool("IstakeDMG", false);
        isTakingDamage = false;
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
        if (anim != null && HasParameter("IstakeDMG")) anim.SetBool("IstakeDMG", false);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player Died! Locking everything.");

        if (anim != null)
        {
            // 1. ส่งค่าเลือดเป็น 0 เพื่อให้เข้าเงื่อนไขท่าตาย
            anim.SetFloat("health", 0f);

            // 2. ปิดการใช้ Root Motion (ถ้ามี) เพื่อไม่ให้ตัวละครขยับตามแอนิเมชันตาย
            anim.applyRootMotion = false;

            // 3. ปรับความเร็ว Animator เป็น 1 (เผื่อติดค่าหน่วงจากท่าอื่น) 
            // หรือถ้าอยากให้หยุดกึกที่ท่าตายเฟรมสุดท้าย สามารถเขียนเพิ่มได้ในภายหลัง
        }

        // 4. ปิด Script การควบคุมทั้งหมด
        if (movement != null)
        {
            movement.enabled = false;
        }
        if (attack != null)
        {
            attack.enabled = false;
        }

        // 5. จัดการ Rigidbody (หยุดแรงเฉื่อยและปิดฟิสิกส์ไม่ให้ไถล)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // หยุดความเร็วสะสม
            rb.isKinematic = true;           // ปิดแรงภายนอก ไม่ให้บอสเดินชนแล้วเรากระเด็น
            rb.useGravity = false;           // ปิดแรงโน้มถ่วง
        }

        // 6. ปิด Collider เพื่อไม่ให้ศัตรูโจมตีซ้ำได้ หรือเดินติดศพ
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // หยุด Coroutine ทั้งหมดที่อาจจะค้างอยู่ (เช่น ท่าเจ็บ)
        StopAllCoroutines();
    }

    private bool HasParameter(string paramName)
    {
        if (anim == null) return false;
        foreach (var param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}