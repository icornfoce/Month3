using UnityEngine;

public class CameraFreeLook : MonoBehaviour
{
    public Transform target;
    public float mouseSensitivity = 200f;
    public float distanceFromTarget = 4f;
    public float heightOffset = 1.5f;

    [Header("Angle Limits")]
    public float minAngle = -20f; // ก้มได้สูงสุด (องศา)
    public float maxAngle = 45f;  // เงยได้สูงสุด (องศา)

    private float xRotation = 0f; // แกน Vertical (ก้มเงย)
    private float yRotation = 0f; // แกน Horizontal (หมุนรอบตัว)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        // ดึงค่ามุมเริ่มต้นจากกล้องปัจจุบัน
        Vector3 angles = transform.eulerAngles;
        xRotation = angles.x;
        yRotation = angles.y;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // รับค่าเมาส์
        yRotation += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        xRotation -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // --- ล็อคแกน Y (ก้ม-เงย) ตรงนี้ครับ ---
        xRotation = Mathf.Clamp(xRotation, minAngle, maxAngle);

        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0);

        // คำนวณตำแหน่งให้กล้องหมุนรอบ Target โดยมีระยะห่าง
        Vector3 position = target.position - (rotation * Vector3.forward * distanceFromTarget) + (Vector3.up * heightOffset);

        transform.rotation = rotation;
        transform.position = position;
    }
}