using UnityEngine;
using UnityEngine.UI; // สำหรับใช้ RawImage

public class HealthManager : MonoBehaviour
{
    [Header("Health Stats")]
    public float maxHealth = 100f;
    public float health = 100f;
    
    [Header("UI Reference (RawImage)")]
    public RawImage hpBarImage;
    public float maxWidth = 200f;

    private bool isDead = false;
    private Animator anim;
    private RectTransform barRect;

    [Header("Game Over Settings")]
    public CanvasGroup gameOverCG;
    public AudioSource gameOverAudio;
    public float gameOverFadeSpeed = 0.5f;

    [Header("Stun Settings")]
    public float takeDamageDuration = 1.0f; // ปรับเวลาชะงัก (Stun) ตรงนี้
    public bool isTakingDamage = false;
    private Coroutine hitCoroutine;


    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim == null) anim = GetComponent<Animator>();
        
        health = maxHealth;
        if (hpBarImage != null)
        {
            barRect = hpBarImage.GetComponent<RectTransform>();
        }
        UpdateHPUI();

        // Ensure Game Over UI is hidden at start
        if (gameOverCG != null)
        {
            gameOverCG.alpha = 0f;
            gameOverCG.blocksRaycasts = false;
            gameOverCG.interactable = false;
        }
        if (gameOverAudio != null) gameOverAudio.volume = 0f;
    }

    void OnEnable()
    {
        isTakingDamage = false;
    }

    void OnDisable()
    {
        isTakingDamage = false;
    }

    void Update()
    {
        if (isDead) return;

        // อัปเดตค่าเข้า Animator ตลอดเวลา
        if (anim != null) anim.SetFloat("health", health);

        if (health <= 0)
        {
            Die();
        }

        UpdateHPUI();

        // ทดสอบกด F/G
        if (Input.GetKeyDown(KeyCode.H)) TakeDamage(10);
        if (Input.GetKeyDown(KeyCode.G)) health += 10;
    }

    // ฟังก์ชันรับดาเมจจาก Boss
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        // เช็คสถานะอมตะจากการหลบ (Dodge)
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null && (movement.isImmortal || movement.IsInvincible))
        {
            Debug.Log("<color=yellow>Player is Invincible! Damage ignored.</color>");
            return;
        }

        health -= amount;
        Debug.Log($"Player HP: {health}");

        // 1. ยกเลิกการโจมตีทันที
        PlayerAttack attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.CancelAttack();

        // 2. หยุดแอนิเมชันเก่าและเล่นแอนิเมชันเจ็บทันที
        if (hitCoroutine != null) StopCoroutine(hitCoroutine);
        hitCoroutine = StartCoroutine(HitCoroutine());
    }

    private System.Collections.IEnumerator HitCoroutine()
    {
        isTakingDamage = true;
        
        if (anim != null) 
        {
            // ปิดค่า Velocity ทันทีเพื่อไม่ให้มันขัดจังหวะการเปลี่ยน State
            anim.SetFloat("Velocity Y", 0f); 
            anim.SetBool("IstakeDMG", true);
            
            // ใช้ Play แบบเจาะจง Layer 0 และเริ่มที่วินาทีที่ 0 ทันที
            anim.Play("Stagger", 0, 0f); 
            Debug.Log("HealthManager: Forced 'Stagger' animation.");
        }
        
        yield return new WaitForSeconds(takeDamageDuration);
        
        if (anim != null) anim.SetBool("IstakeDMG", false);
        isTakingDamage = false;
        hitCoroutine = null;
    }

    public void ClearStagger()
    {
        if (hitCoroutine != null) StopCoroutine(hitCoroutine);
        if (anim != null) anim.SetBool("IstakeDMG", false);
        isTakingDamage = false;
        hitCoroutine = null;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // 1. เล่นแอนิเมชันตาย
        anim.SetTrigger("Is Dead");
        anim.SetFloat("health", 0); // บังคับให้เป็น 0 เพื่อความชัวร์
        isTakingDamage = false;

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

        // 4. แสดงหน้าจอ Game Over แบบ Fade-in
        if (gameOverCG != null)
        {
            StartCoroutine(ShowGameOverURoutine());
        }
    }

    private System.Collections.IEnumerator ShowGameOverURoutine()
    {
        if (gameOverAudio != null) gameOverAudio.Play();

        float currentAlpha = 0f;
        while (currentAlpha < 1f)
        {
            currentAlpha += Time.deltaTime * gameOverFadeSpeed;
            if (gameOverCG != null) gameOverCG.alpha = currentAlpha;
            if (gameOverAudio != null) gameOverAudio.volume = currentAlpha;
            yield return null;
        }

        if (gameOverCG != null)
        {
            gameOverCG.alpha = 1f;
            gameOverCG.blocksRaycasts = true;
            gameOverCG.interactable = true;
        }
        if (gameOverAudio != null) gameOverAudio.volume = 1f;
    }

    void UpdateHPUI()
    {
        if (barRect != null)
        {
            float pct = Mathf.Clamp01(health / maxHealth);
            barRect.sizeDelta = new Vector2(pct * maxWidth, barRect.sizeDelta.y);
        }
    }
}