using UnityEngine;
using System.Collections;

public class PlayerParry : MonoBehaviour
{
    [Header("Parry Settings")]
    public float parryWindow = 0.4f; // ระยะเวลาที่กด F แล้วจะยังถือว่า Parry ได้ (วินาที)
    public bool isParryingState = false; // ตัวแปรที่บอสจะมาเช็ค

    private Animator anim;
    private PlayerHealth ph;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        ph = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (ph.isDead) return;

        // กด F เพื่อทำการ Parry
        if (Input.GetKeyDown(KeyCode.F) && !isParryingState && !ph.isTakingDamage)
        {
            StartCoroutine(PerformParry());
        }
    }

    IEnumerator PerformParry()
    {
        isParryingState = true;
        Debug.Log("<color=cyan>Player: Parry Window STARTED</color>");

        // สั่งเล่นแอนิเมชัน (อย่าลืมตั้งชื่อใน Animator ให้ตรง)
        if (anim != null) anim.SetTrigger("isParrying");

        // ระหว่างรอใน Window นี้ ถ้า BossAI เรียกฟังก์ชัน DealDamage จะถือว่า Parry สำเร็จ
        yield return new WaitForSeconds(parryWindow);

        isParryingState = false;
        Debug.Log("<color=gray>Player: Parry Window ENDED</color>");
    }
}