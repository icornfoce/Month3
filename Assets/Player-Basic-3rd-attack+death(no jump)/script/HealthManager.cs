using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HealthManager : MonoBehaviour
{
    // ... [ตัวแปรอื่นๆ เหมือนเดิม] ...

    [Header("Heal Settings (Press E)")]
    public int maxHealCharges = 3;
    public int currentHealCharges;
    public float healAmount = 30f;
    public KeyCode healKey = KeyCode.E;

    [Header("Heal Audio")]
    public AudioSource healAudioSource; // ลาก AudioSource ของ Player มาใส่
    public AudioClip healSound;         // ลากไฟล์เสียงฮีลมาใส่

    // ... [ตัวแปร UI และ Stun เหมือนเดิม] ...

    [Header("Health Stats")]
    public float maxHealth = 100f;
    public float health = 100f;

    [Header("UI Reference")]
    public RawImage hpBarImage;
    public float maxWidth = 200f;
    public TextMeshProUGUI healCountText;

    private bool isDead = false;
    private Animator anim;
    private RectTransform barRect;

    [Header("Game Over Settings")]
    public CanvasGroup gameOverCG;
    public AudioSource gameOverAudio;
    public float gameOverFadeSpeed = 0.5f;

    [Header("Stun Settings")]
    public float takeDamageDuration = 1.0f;
    public bool isTakingDamage = false;
    private Coroutine hitCoroutine;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim == null) anim = GetComponent<Animator>();

        health = maxHealth;
        currentHealCharges = maxHealCharges;

        if (hpBarImage != null)
            barRect = hpBarImage.GetComponent<RectTransform>();

        // ถ้าลืมลาก AudioSource มา มันจะพยายามหาในตัวมันเองให้เองครับ
        if (healAudioSource == null) healAudioSource = GetComponent<AudioSource>();

        UpdateHPUI();
        UpdateHealUI();

        if (gameOverCG != null)
        {
            gameOverCG.alpha = 0f;
            gameOverCG.blocksRaycasts = false;
            gameOverCG.interactable = false;
        }
    }

    void Update()
    {
        if (isDead) return;

        if (anim != null) anim.SetFloat("health", health);

        if (Input.GetKeyDown(healKey))
        {
            Heal();
        }

        if (health <= 0)
        {
            health = 0;
            Die();
        }

        UpdateHPUI();
    }

    public void Heal()
    {
        // เงื่อนไข: ไม่ตาย, เลือดไม่เต็ม, และมียาเหลือ
        if (isDead || health >= maxHealth || currentHealCharges <= 0) return;

        currentHealCharges--;
        health += healAmount;

        if (health > maxHealth) health = maxHealth;

        // --- ส่วนการเล่นเสียง ---
        if (healAudioSource != null && healSound != null)
        {
            healAudioSource.PlayOneShot(healSound);
        }
        // ----------------------

        UpdateHealUI();
        Debug.Log("Healed with Sound!");
    }

    // --- ฟังก์ชันอื่นๆ (TakeDamage, Die, UpdateUI) คงเดิมเหมือนโค้ดชุดก่อนหน้า ---

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null && (movement.isImmortal || movement.IsInvincible)) return;
        health -= amount;
        PlayerAttack attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.CancelAttack();
        if (hitCoroutine != null) StopCoroutine(hitCoroutine);
        hitCoroutine = StartCoroutine(HitCoroutine());
    }

    private System.Collections.IEnumerator HitCoroutine()
    {
        isTakingDamage = true;
        if (anim != null)
        {
            anim.SetFloat("Velocity Y", 0f);
            anim.SetBool("IstakeDMG", true);
            anim.Play("Stagger", 0, 0f);
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

    void UpdateHPUI()
    {
        if (barRect != null)
        {
            float ratio = health / maxHealth;
            barRect.sizeDelta = new Vector2(maxWidth * ratio, barRect.sizeDelta.y);
        }
    }

    void UpdateHealUI()
    {
        if (healCountText != null)
            healCountText.text = "Heal: " + currentHealCharges.ToString();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        if (anim != null) anim.SetTrigger("Is Dead");
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }
        if (GetComponent<PlayerMovement>() != null) GetComponent<PlayerMovement>().enabled = false;
        if (GetComponent<PlayerAttack>() != null) GetComponent<PlayerAttack>().enabled = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (gameOverCG != null) StartCoroutine(ShowGameOverURoutine());
    }

    private System.Collections.IEnumerator ShowGameOverURoutine()
    {
        float currentAlpha = 0f;
        while (currentAlpha < 1f)
        {
            currentAlpha += Time.deltaTime * gameOverFadeSpeed;
            if (gameOverCG != null) gameOverCG.alpha = currentAlpha;
            yield return null;
        }
        if (gameOverCG != null)
        {
            gameOverCG.alpha = 1f; gameOverCG.blocksRaycasts = true; gameOverCG.interactable = true;
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}