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
        // Disabled Camera Shake as requested
        // if (duration < 0) duration = defaultDuration;
        // if (magnitude < 0) magnitude = defaultMagnitude;

        // // ถ้าสั่นอยู่แล้ว ให้หยุดอันเก่าก่อน (หรือจะ blend ก็ได้ แต่ง่ายสุดคือเริ่มใหม่)
        // StopAllCoroutines();
        // StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        yield break; // Do nothing
    }
}
