using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // Singleton เพื่อให้เรียกจากที่ไหนก็ได้ CameraShake.Instance.Shake(...)
    public static CameraShake Instance { get; private set; }

    [Header("Default Shake Settings (ค่าเริ่มต้น)")]
    public float defaultDuration = 0.15f;
    public float defaultMagnitude = 0.1f;

    private bool isShaking = false;

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // เก็บค่า Offset ปัจจุบันเพื่อให้ CameraFreeLook เอาไปใช้
    public Vector3 CurrentShakeOffset { get; private set; }

    /// <summary>
    /// สั่นกล้อง — เรียก CameraShake.Instance.Shake() ได้เลย
    /// </summary>
    public void Shake(float duration = -1f, float magnitude = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        if (magnitude < 0) magnitude = defaultMagnitude;

        // ถ้าสั่นอยู่แล้ว ให้หยุดอันเก่าก่อน (หรือจะ blend ก็ได้ แต่ง่ายสุดคือเริ่มใหม่)
        StopAllCoroutines();
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // สร้าง offset สุ่ม
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            CurrentShakeOffset = new Vector3(x, y, 0f);

            // ค่อยๆ ลด magnitude ลงเรื่อยๆ ให้จางหายไป
            float progress = elapsed / duration;
            float currentMagnitude = magnitude * (1f - progress);
            magnitude = currentMagnitude;

            elapsed += Time.deltaTime;
            yield return null;
        }

        CurrentShakeOffset = Vector3.zero; // รีเซ็ตเมื่อจบ
        isShaking = false;
    }
}
