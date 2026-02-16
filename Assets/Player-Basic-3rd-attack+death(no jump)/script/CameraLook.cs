using UnityEngine;

public class CameraFreeLook : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // ลากตัวละครมาใส่ตรงนี้
    public float distanceFromTarget = 4f;
    public float heightOffset = 1.5f;

    [Header("Sensitivity & Limits")]
    public float mouseSensitivity = 200f;
    public float minAngle = -20f; // ก้ม
    public float maxAngle = 45f;  // เงย

    private float xRotation = 0f; // สำหรับก้ม-เงย (Vertical)
    private float yRotation = 0f; // สำหรับหมุนรอบตัว (Horizontal)

    void Start()
    {
        // ล็อคเมาส์ไว้กลางจอ
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ดึงค่ามุมปัจจุบันของกล้องมาตั้งต้น เพื่อไม่ให้กล้องดีดตอนเริ่ม
        Vector3 angles = transform.eulerAngles;
        xRotation = angles.x;
        yRotation = angles.y;
    }

    // ใช้ LateUpdate เพื่อให้กล้องขยับหลังจากตัวละครขยับเสร็จแล้ว (ลดอาการภาพสั่น)
    void LateUpdate()
    {
        if (target == null) return;

        // 1. รับค่า Input จากเมาส์
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;

        // 2. ล็อคมุมก้ม-เงย ไม่ให้กล้องหมุนตีลังกา
        xRotation = Mathf.Clamp(xRotation, minAngle, maxAngle);

        // 3. สร้าง Quaternion การหมุน
        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0);

        // 4. คำนวณตำแหน่งกล้อง:
        // เริ่มจากตำแหน่ง Target + ความสูง -> ถอยหลังไปตามทิศทาง Rotation * ระยะห่าง
        Vector3 targetPosition = target.position + Vector3.up * heightOffset;
        Vector3 position = targetPosition - (rotation * Vector3.forward * distanceFromTarget);

        // 5. อัปเดตค่าให้กล้อง
        transform.rotation = rotation;
        transform.position = position;
    }
}