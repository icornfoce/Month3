using UnityEngine;
using System.Collections.Generic;

public class PlayerWeaponTrigger : MonoBehaviour
{
    private float currentDamage;
    private List<GameObject> hitTargets = new List<GameObject>();
    private Collider weaponCollider;

    [Header("Hit Feedback")]
    public GameObject hitVFXPrefab;
    public AudioClip hitSFX;
    public AudioSource audioSource;

    void Awake()
    {
        weaponCollider = GetComponent<Collider>();
        if (weaponCollider != null)
        {
            weaponCollider.isTrigger = true;
            weaponCollider.enabled = false;
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();
    }

    public void EnableTrigger(float damage)
    {
        currentDamage = damage;
        hitTargets.Clear();
        if (weaponCollider != null) weaponCollider.enabled = true;
        Debug.Log($"Weapon Trigger: ENABLED with {damage} damage");
    }

    public void DisableTrigger()
    {
        if (weaponCollider != null) weaponCollider.enabled = false;
        Debug.Log("Weapon Trigger: DISABLED");
    }

    private void OnTriggerEnter(Collider other)
    {
        // ป้องกันการตีโดนตัวเอง
        if (other.CompareTag("Player")) return;

        // ป้องกันการตีโดนซ้ำในครั้งเดียว
        if (hitTargets.Contains(other.gameObject)) return;

        BossAI boss = other.GetComponent<BossAI>();
        if (boss == null) boss = other.GetComponentInParent<BossAI>();

        if (boss != null)
        {
            hitTargets.Add(other.gameObject);
            Debug.Log($"<color=orange>Weapon Hit: {other.name}, Dealt: {currentDamage}</color>");
            boss.TakeDamage(currentDamage);

            // Trigger Hit Feedback
            if (hitVFXPrefab != null)
            {
                // Instantiate at the point of impact (approximate with other collider's closest point or center)
                Vector3 hitPoint = other.ClosestPointOnBounds(transform.position);
                GameObject vfx = Instantiate(hitVFXPrefab, hitPoint, Quaternion.identity);
                Destroy(vfx, 2f); // Auto destroy after 2 seconds
            }

            if (audioSource != null && hitSFX != null)
            {
                audioSource.PlayOneShot(hitSFX);
            }
        }
    }
}
