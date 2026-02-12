using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // 1. ถ้ากำลังโจมตี (Animation เล่นอยู่) จะไม่รับ Input ใหม่
        if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            return;
        }

        bool leftClick = Input.GetMouseButtonDown(0);
        bool rightClick = Input.GetMouseButtonDown(1);

        // 2. ป้องกันการกดพร้อมกัน
        if (leftClick && rightClick) return;

        // 3. สั่ง Trigger โจมตี
        if (leftClick)
        {
            anim.SetTrigger("Is attack light");
        }
        else if (rightClick)
        {
            anim.SetTrigger("Is attack heavy");
        }
    }
}