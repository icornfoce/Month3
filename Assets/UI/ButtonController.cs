using UnityEngine;
using UnityEngine.SceneManagement; // สำคัญมากสำหรับการย้าย Scene

public class ButtonController : MonoBehaviour
{
    public AudioSource mySound;    // ลาก AudioSource มาใส่ในช่องนี้ที่ Inspector
    public string sceneName;      // พิมพ์ชื่อ Scene ที่ต้องการจะไปลงในช่องนี้

    public void PlayAndChangeScene()
    {
        // 1. สั่งให้เล่นเสียง
        if (mySound != null)
        {
            mySound.Play();
            
            // ถ้าอยากให้รอจนเสียงจบก่อนค่อยย้าย ให้ใช้ Invoke
            // แต่ถ้าจะย้ายทันที ให้ใช้คำสั่งบรรทัดล่างนี้เลยครับ
            Invoke("NextScene", mySound.clip.length); 
        }
        else
        {
            // ถ้าลืมใส่เสียง ให้ย้ายฉากทันที
            NextScene();
        }
    }

    void NextScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}