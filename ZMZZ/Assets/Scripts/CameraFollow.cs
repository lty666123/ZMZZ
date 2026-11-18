using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public PlayerObject targetPlayer;                    
    public Vector2 offset = new Vector2(0f, 1.5f);
    public float moveSpeed = 6f;

    
    public Vector3 lastplayerPos;
    public float teleportDistance = 10f;

    Vector3 velocity;
    //是否被锁定
    private bool isLocked;

    private void Awake()
    {
        
    }

    private void OnEnable()
    {
        //订阅事件 
        EnemySpawnPoint.BattleStateChanged += OnBattleStateChanged;
    }

    void LateUpdate()
    {
        if (isLocked) return;

        if (targetPlayer == null)
        {
            targetPlayer = FindObjectOfType<PlayerObject>();
            if (targetPlayer == null) return;
        }// 还没生成就继续找
            
        Vector3 targetPos = new Vector3(
            targetPlayer.transform.position.x + offset.x,
            targetPlayer.goundy + offset.y,
            transform.position.z);    
        //玩家上一帧的位置与这一帧的位置比较 距离过远时直接改变摄像机 近距离再缓慢跟随
        if(Vector3.Distance(lastplayerPos,targetPlayer.transform.position) > teleportDistance)
        {
            SnapToTarget(targetPos);
        }
     
        this.transform.position = Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime);

        lastplayerPos = targetPlayer.transform.position;
    }

    private void OnDisable()
    {
        EnemySpawnPoint.BattleStateChanged -= OnBattleStateChanged;
    }

    public void SnapToTarget(Vector3 v)
    {
        if (targetPlayer == null) return;

        transform.position = v;
    }


    private void OnBattleStateChanged(bool isInBattle)
    {
        isLocked = isInBattle;
        if(!isLocked&&targetPlayer != null)
        {
            lastplayerPos = targetPlayer.transform.position;
        }
    }

}
