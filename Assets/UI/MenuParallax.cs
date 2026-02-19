using UnityEngine;

public sealed class MenuParallax : MonoBehaviour
{
    [Header("Settings")]
    public float moveAmount = 0.5f;     // ความแรงในการขยับ (Position)
    public float rotationAmount = 2.0f; // ความแรงในการหมุน (Rotation)
    public float smoothTime = 5.0f;     // ความลื่นไหล (Lerp Speed)

    private Vector3 startPos;
    private Quaternion startRot;

    void Start()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;
    }

    void Update()
    {
        // 1. หาตำแหน่งเมาส์ในรูปแบบ -1 ถึง 1 (Center is 0,0)
        float mouseX = (Input.mousePosition.x / Screen.width) - 0.5f;
        float mouseY = (Input.mousePosition.y / Screen.height) - 0.5f;

        // 2. คำนวณตำแหน่งเป้าหมาย
        Vector3 targetPos = new Vector3(
            startPos.x + (mouseX * moveAmount),
            startPos.y + (mouseY * moveAmount),
            startPos.z
        );

        // 3. คำนวณการหมุนเป้าหมาย (สลับแกน X, Y เพื่อให้หันตามเมาส์)
        Quaternion targetRot = startRot * Quaternion.Euler(-mouseY * rotationAmount, mouseX * rotationAmount, 0);

        // 4. สั่งให้ขยับแบบลื่นๆ (Smoothing)
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smoothTime);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smoothTime);
    }
}