using UnityEngine;
using UnityEngine.Playables;
using System.Collections;
using System.Collections.Generic; // เพิ่มเพื่อให้ใช้ List ได้

public class Cutscene : MonoBehaviour
{
    [Header("--- Timeline Settings ---")]
    public PlayableDirector mainTimeline;

    [Header("--- Audio Settings ---")]
    public AudioSource audioSource; // ลาก AudioSource มาใส่
    public AudioClip cutsceneSound; // ลากไฟล์เสียงมาใส่

    [Header("--- Objects to ACTIVATE (เปิดเมื่อจบ) ---")]
    public List<GameObject> objectsToActivate; // กดเพิ่มจำนวนใน Inspector ได้เลย

    [Header("--- Objects to DEACTIVATE (เล่น Timeline แล้วปิด) ---")]
    public List<GameObject> objectsToDeactivate; // กดเพิ่มจำนวนใน Inspector ได้เลย

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

        // 1. เล่นเสียง (ถ้ามีการใส่เสียงไว้)
        if (audioSource != null && cutsceneSound != null)
        {
            audioSource.PlayOneShot(cutsceneSound);
        }

        // 2. เริ่มเล่น Main Timeline
        if (mainTimeline != null) mainTimeline.Play();

        // 3. จัดการกลุ่มวัตถุที่จะ "ปิด" (สั่งเล่น Timeline ของแต่ละตัวก่อนถ้ามี)
        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null)
            {
                PlayableDirector outTimeline = obj.GetComponent<PlayableDirector>();
                if (outTimeline != null)
                {
                    outTimeline.Play();
                }
            }
        }

        // รอจนกว่า Main Timeline จะเล่นจบ 
        // (หรือจะรอตามความยาวของไอเทมที่ปิดนานที่สุดก็ได้ แต่ในที่นี้จะอิงตาม Main Timeline)
        if (mainTimeline != null)
        {
            yield return new WaitForSeconds((float)mainTimeline.duration);
        }

        // 4. สั่ง "ปิด" วัตถุใน List ทั้งหมดจริงๆ หลังจากรอแล้ว
        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null) obj.SetActive(false);
        }

        // 5. สั่ง "เปิด" วัตถุใน List ทั้งหมด (ทำงานหลังจบ Timeline)
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null) obj.SetActive(true);
        }

        // ปิดการทำงานของ Trigger
        GetComponent<Collider>().enabled = false;
    }
}