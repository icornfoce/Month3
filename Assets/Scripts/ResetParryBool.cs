using UnityEngine;

public class ResetParryBool : StateMachineBehaviour
{
    // จะทำงานเมื่อเล่น Animation จบ หรือออกจาก State นี้
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("isParryHit", false);

        // สั่งให้บอสหายมึนใน Logic ของ BossAI ด้วย
        BossAI boss = animator.GetComponent<BossAI>();
        if (boss != null)
        {
            boss.isStunned = false;
            if (boss.GetComponent<UnityEngine.AI.NavMeshAgent>().isOnNavMesh)
                boss.GetComponent<UnityEngine.AI.NavMeshAgent>().isStopped = false;
        }
    }
}