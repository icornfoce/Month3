using UnityEngine;
using UnityEngine.UI; // สำหรับใช้ RawImage

public class StaminaManager : MonoBehaviour
{
    [Header("Stamina Stats")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float regenRate = 15f;
    public float regenDelay = 1f;

    [Header("Stamina Costs")]
    public float runStaminaCost = 20f; // per second
    public float dodgeStaminaCost = 15f;
    public float lightAttackStaminaCost = 10f;
    public float heavyAttackStaminaCost = 25f;

    [Header("UI Reference (RawImage)")]
    public RawImage staminaBarImage; // เปลี่ยนจาก Slider เป็น RawImage
    public float maxWidth = 200f;    // ความกว้างสูงสุดของหลอดใน UI (เช่น 200 pixel)

    private float regenTimer;
    private RectTransform barRect;

    void Start()
    {
        currentStamina = maxStamina;
        if (staminaBarImage != null)
        {
            // ดึง RectTransform มาเพื่อคุมความกว้าง
            barRect = staminaBarImage.GetComponent<RectTransform>();
        }
    }

    void Update()
    {
        // ระบบฟื้นฟู Stamina
        if (regenTimer > 0)
        {
            regenTimer -= Time.deltaTime;
        }
        else if (currentStamina < maxStamina)
        {
            currentStamina += regenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }

        // อัปเดตการแสดงผลหลอด (UI)
        UpdateStaminaUI();
    }

    void UpdateStaminaUI()
    {
        if (barRect != null)
        {
            // คำนวณเปอร์เซ็นต์ (0 ถึง 1)
            float pct = currentStamina / maxStamina;
            
            // ปรับความกว้างตามเปอร์เซ็นต์ (ถ้า Pivot X เป็น 0 มันจะลดจากขวามาซ้าย)
            barRect.sizeDelta = new Vector2(pct * maxWidth, barRect.sizeDelta.y);
        }
    }

    public bool UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            regenTimer = regenDelay;
            return true;
        }
        return false;
    }
}