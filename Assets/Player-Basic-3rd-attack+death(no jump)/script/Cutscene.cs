using UnityEngine;
using UnityEngine.Playables;
using System.Collections;
using System.Collections.Generic;

// เปลี่ยนชื่อจาก Cutscene เป็น CutsceneManager เพื่อไม่ให้ซ้ำกับของเก่า
public class CutsceneManager : MonoBehaviour
{
    [Header("--- Timeline Settings ---")]
    public PlayableDirector mainTimeline;

    [Header("--- Audio Settings ---")]
    public AudioSource audioSource;
    public AudioClip cutsceneSound;

    [Header("--- Objects to ACTIVATE (เปิดเมื่อจบ) ---")]
    public List<GameObject> objectsToActivate;

    [Header("--- Objects to DEACTIVATE (ปิดเมื่อจบ) ---")]
    public List<GameObject> objectsToDeactivate;

    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasPlayed)
        {
            StartCoroutine(PlayCutsceneRoutine());
        }
    }

    IEnumerator PlayCutsceneRoutine()
    {
        hasPlayed = true;

        // 1. เล่นเสียงทันทีที่ชน
        if (audioSource != null && cutsceneSound != null)
        {
            audioSource.PlayOneShot(cutsceneSound);
        }

        // 2. เริ่มเล่น Main Timeline
        if (mainTimeline != null)
        {
            mainTimeline.Play();

            // --- รอจนกว่า Timeline หลักจะเล่นจบ ---
            yield return new WaitForSeconds((float)mainTimeline.duration);
        }

        // --- 3. [ทำงานเมื่อ Timeline จบแล้วเท่านั้น] ---

        // ติ๊กออก (Deactivate) วัตถุใน List
        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null) obj.SetActive(false);
        }

        // ติ๊กถูก (Activate) วัตถุใน List
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null) obj.SetActive(true);
        }

        // 4. ปิดการทำงานของ Trigger
        GetComponent<Collider>().enabled = false;
    }
}