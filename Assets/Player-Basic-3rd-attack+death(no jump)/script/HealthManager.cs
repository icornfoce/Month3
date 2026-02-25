using UnityEngine;
using UnityEngine.UI; // สำหรับใช้ RawImage
using UnityEngine.SceneManagement; // สำหรับจัดการคำสั่งเกี่ยวกับโหลดด่าน/รีสตาร์ท

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
        if (gameOverAudio != null && gameOverAudio.gameObject.scene.IsValid()) 
        {
            gameOverAudio.Stop();
            gameOverAudio.volume = 1f;
        }
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

        // 4. แสดงเข็มทิศเมาส์ให้กดปุ่ม Restart ได้
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 5. แสดงหน้าจอ Game Over แบบ Fade-in
        if (gameOverCG != null)
        {
            StartCoroutine(ShowGameOverURoutine());
        }
    }

    private System.Collections.IEnumerator ShowGameOverURoutine()
    {
        if (gameOverAudio != null) 
        {
            if (gameOverAudio.gameObject.scene.IsValid() && gameOverAudio.gameObject.activeInHierarchy)
            {
                gameOverAudio.volume = 1f;
                gameOverAudio.loop = true; // เปิดให้วนลูป
                gameOverAudio.Play();
            }
            else if (gameOverAudio.clip != null)
            {
                // Instantiate a temporary game object to play the sound properly in 2D
                GameObject tempAudioObj = new GameObject("TempGameOverAudio");
                AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
                tempSource.clip = gameOverAudio.clip;
                tempSource.spatialBlend = 0f; // 2D sound
                tempSource.volume = 1f;
                tempSource.loop = true; // เปิดให้วนลูป
                // copy from original if needed
                tempSource.outputAudioMixerGroup = gameOverAudio.outputAudioMixerGroup;
                
                tempSource.Play();
                // ลบ Destroy ออกเพื่อปล่อยให้เสียงเล่นวนลูปไปเรื่อยๆ จนกว่าจะโหลด Scene ใหม่
            }
        }

        float currentAlpha = 0f;
        while (currentAlpha < 1f)
        {
            currentAlpha += Time.deltaTime * gameOverFadeSpeed;
            if (gameOverCG != null) gameOverCG.alpha = currentAlpha;
            yield return null;
        }

        if (gameOverCG != null)
        {
            gameOverCG.alpha = 1f;
            gameOverCG.blocksRaycasts = true;
            gameOverCG.interactable = true;
        }
    }

    // ฟังก์ชันสำหรับผูกกับปุ่ม Restart ใน UI
    public void RestartGame()
    {
        // สั่งโหลด Scene ชื่อเดียวกับฉากปัจจุบันเพื่อเริ่มเล่นใหม่
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        // ทำให้เกมเดินเวลาเป็นปกติ เผื่อมีการหยุดเวลาเกิดขึ้น
        Time.timeScale = 1f;
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