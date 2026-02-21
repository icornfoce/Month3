using UnityEngine;
using System.Collections; // เพิ่มบรรทัดนี้เพื่อแก้ Error CS0103

public class PlayerAttack : MonoBehaviour
{
    [Header("Damage Settings")]
    public float lightDamage = 15f;
    public float heavyDamage = 30f;

    [Header("Attack Detection")]
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public LayerMask enemyLayer;

    [Header("Attack Timing")]
    public float attackHitDelay = 0.2f;
    public float lightAttackVFXDelay = 0f;
    public float heavyAttackVFXDelay = 0.15f;
    public float lightAttackVFXDuration = 0.5f;
    public float heavyAttackVFXDuration = 0.7f;

    [Header("Stamina Reference")]
    public StaminaManager staminaManager;

    [Header("Movement Reference")]
    public PlayerMovement playerMovement;

    [Header("Health Reference")]
    public HealthManager healthManager;

    [Header("Weapon Trigger")]
    public PlayerWeaponTrigger weaponTrigger;

    [Header("VFX Settings")]
    public GameObject lightAttackVFX;
    public GameObject heavyAttackVFX;

    [Header("SFX Settings")]
    public AudioSource audioSource;
    public AudioClip lightAttackSFX;
    public AudioClip heavyAttackSFX;

    private Animator anim;
    private bool isAttackingFlag = false;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim == null) anim = GetComponent<Animator>();

        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (healthManager == null) healthManager = GetComponent<HealthManager>();

        // พยายามหา WeaponTrigger ถ้ายังไม่ได้ลากใส่
        if (weaponTrigger == null) weaponTrigger = GetComponentInChildren<PlayerWeaponTrigger>();
        
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // Ensure VFX are off at start
        SetVFXState(lightAttackVFX, false);
        SetVFXState(heavyAttackVFX, false);

        Debug.Log($"PlayerAttack: Start complete. Anim: {anim != null}, WeaponTrigger: {weaponTrigger != null}");
    }

    void Update()
    {
        if (anim == null || isAttackingFlag) return;

        bool isDodgeLocked = playerMovement != null && playerMovement.isDodgeLockingMovement;
        bool isTakingDMG = healthManager != null && healthManager.isTakingDamage;

        if (isDodgeLocked || isTakingDMG || anim.IsInTransition(0)) return;

        if (Input.GetMouseButtonDown(0)) StartCoroutine(PerformAttack(true));
        else if (Input.GetMouseButtonDown(1)) StartCoroutine(PerformAttack(false));
    }

    IEnumerator PerformAttack(bool isLight)
    {
        isAttackingFlag = true;

        if (staminaManager != null)
        {
            float cost = isLight ? staminaManager.lightAttackStaminaCost : staminaManager.heavyAttackStaminaCost;
            if (!staminaManager.UseStamina(cost))
            {
                isAttackingFlag = false;
                yield break;
            }
        }

        anim.SetTrigger(isLight ? "Is attack light" : "Is attack heavy");

        // Trigger VFX and SFX (with delay)
        StartCoroutine(TriggerAttackEffects(isLight));

        // 1. เปิด Trigger (รอจังหวะที่ดาบเหวี่ยง)
        yield return new WaitForSeconds(attackHitDelay);
        if (weaponTrigger != null) 
            weaponTrigger.EnableTrigger(isLight ? lightDamage : heavyDamage);

        // 2. ปิด Trigger (รอจนกว่าจะจบวงสวิง)
        yield return new WaitForSeconds(0.4f); 
        if (weaponTrigger != null) weaponTrigger.DisableTrigger();
        
        isAttackingFlag = false;
    }

    IEnumerator TriggerAttackEffects(bool isLight)
    {
        float delay = isLight ? lightAttackVFXDelay : heavyAttackVFXDelay;
        if (delay > 0) yield return new WaitForSeconds(delay);

        GameObject vfx = isLight ? lightAttackVFX : heavyAttackVFX;
        AudioClip sfx = isLight ? lightAttackSFX : heavyAttackSFX;

        if (vfx != null) SetVFXState(vfx, true);
        if (audioSource != null && sfx != null) audioSource.PlayOneShot(sfx);

        float duration = isLight ? lightAttackVFXDuration : heavyAttackVFXDuration;
        yield return new WaitForSeconds(duration);

        if (vfx != null) SetVFXState(vfx, false);
    }

    public void CancelAttack()
    {
        StopAllCoroutines();
        if (weaponTrigger != null) weaponTrigger.DisableTrigger();
        SetVFXState(lightAttackVFX, false);
        SetVFXState(heavyAttackVFX, false);
        isAttackingFlag = false;
        anim.ResetTrigger("Is attack light");
        anim.ResetTrigger("Is attack heavy");
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
