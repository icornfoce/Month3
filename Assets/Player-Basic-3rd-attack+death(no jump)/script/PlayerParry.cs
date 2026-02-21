using UnityEngine;
using System.Collections;

public class PlayerParry : MonoBehaviour
{
    [Header("Parry Settings")]
    public float parryWindow = 0.4f; // ระยะเวลาที่กด F แล้วจะยังถือว่า Parry ได้ (วินาที)
    public bool isParryingState = false; // ตัวแปรที่บอสจะมาเช็ค

    private Animator anim;
    private HealthManager hm;

    [Header("VFX Settings")]
    public GameObject parryWindowVFX;
    public GameObject parrySuccessVFX;

    [Header("SFX Settings")]
    public AudioSource audioSource;
    public AudioClip parryWindowSFX;
    public AudioClip parrySuccessSFX;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        hm = GetComponent<HealthManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        SetVFXState(parryWindowVFX, false);
        SetVFXState(parrySuccessVFX, false);
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

        if (parryWindowVFX != null) SetVFXState(parryWindowVFX, true);
        if (audioSource != null && parryWindowSFX != null) audioSource.PlayOneShot(parryWindowSFX);

        // ระหว่างรอใน Window นี้ ถ้า BossAI เรียกฟังก์ชัน DealDamage จะถือว่า Parry สำเร็จ
        yield return new WaitForSeconds(parryWindow);

        if (parryWindowVFX != null) SetVFXState(parryWindowVFX, false);

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

        if (parryWindowVFX != null) SetVFXState(parryWindowVFX, false);
        if (parrySuccessVFX != null) StartCoroutine(ShowOneShotVFX(parrySuccessVFX));
        if (audioSource != null && parrySuccessSFX != null) audioSource.PlayOneShot(parrySuccessSFX);
        
        Debug.Log("<color=green>Player: Parry SUCCESS Animation Triggered!</color>");
    }

    IEnumerator ShowOneShotVFX(GameObject vfx)
    {
        SetVFXState(vfx, true);
        yield return new WaitForSeconds(2f); // Adjust time as needed
        SetVFXState(vfx, false);
    }

    private void SetVFXState(GameObject vfxObj, bool active)
    {
        if (vfxObj == null) return;
        if (vfxObj.activeSelf != active) vfxObj.SetActive(active);
        ParticleSystem ps = vfxObj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            if (active) { if (!ps.isPlaying) ps.Play(); }
            else { if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
        }
        foreach (var cps in vfxObj.GetComponentsInChildren<ParticleSystem>())
        {
            if (active) { if (!cps.isPlaying) cps.Play(); }
            else { if (cps.isPlaying) cps.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
        }
    }
}