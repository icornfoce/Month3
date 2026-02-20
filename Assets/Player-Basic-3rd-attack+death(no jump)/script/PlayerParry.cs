using UnityEngine;
using System.Collections;

public class PlayerParry : MonoBehaviour
{
    [Header("Parry Settings")]
    public float parryWindow = 0.4f; // ระยะเวลาที่กด F แล้วจะยังถือว่า Parry ได้ (วินาที)
    public bool isParryingState = false; // ตัวแปรที่บอสจะมาเช็ค

    private Animator anim;
    private HealthManager hm;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        hm = GetComponent<HealthManager>();
    }

    void OnEnable()
    {
        isParryingState = false;
    }

    void OnDisable()
    {
        isParryingState = false;
    }

    void Update()
    {
        if (hm != null && (hm.health <= 0)) return;

        // กด F เพื่อทำการ Parry
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isParryingState)
            {
                Debug.Log("<color=yellow>Parry BLOCKED: Already in parrying state.</color>");
                return;
            }

            if (hm != null && hm.isTakingDamage)
            {
                Debug.Log("<color=red>Parry BLOCKED: Player is taking damage (Stunned).</color>");
                return;
            }

            StartCoroutine(PerformParry());
        }
    }

    IEnumerator PerformParry()
    {
        isParryingState = true;
        Debug.Log("<color=cyan>Player: Parry Window STARTED</color>");

        // สั่งเล่นแอนิเมชัน (อย่าลืมตั้งชื่อใน Animator ให้ตรง)
        if (anim != null) 
        {
            anim.ResetTrigger("isParrying");
            anim.SetTrigger("isParrying");
        }

        // ระหว่างรอใน Window นี้ ถ้า BossAI เรียกฟังก์ชัน DealDamage จะถือว่า Parry สำเร็จ
        yield return new WaitForSeconds(parryWindow);

        isParryingState = false;
        Debug.Log("<color=gray>Player: Parry Window ENDED</color>");
    }

    // ฟังก์ชันนี้จะถูกเรียกจากบอสเมื่อ Parry สำเร็จ
    public void SuccessfulParry()
    {
        StopAllCoroutines();
        isParryingState = false;
        
        if (anim != null)
        {
            // เคลียร์ trigger เก่าที่อาจจะค้างอยู่จากการกด F
            anim.ResetTrigger("isParrying"); 
            
            // เปลี่ยนแอนิเมชันทันที
            anim.SetTrigger("ParrySuccess"); 
            anim.Play("ParrySuccess", 0, 0f); // บังคับเล่นข้ามทุกอย่าง
        }
        
        Debug.Log("<color=green>Player: Parry SUCCESS Animation Triggered!</color>");
    }
}