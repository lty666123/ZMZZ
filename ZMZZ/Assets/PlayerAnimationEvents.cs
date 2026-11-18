using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerObject player;

    private void Awake()
    {
        player = GetComponentInParent<PlayerObject>();
    }

    public void DisableMoveAndJump()
    {
        player.EnableJumpAndMove(false);
    }
    public void EnableMoveAndJump()
    {
        player.EnableJumpAndMove(true);
    }
    public void Damage()
    {
        
        player.Atk_Damage();
    }
   
}
