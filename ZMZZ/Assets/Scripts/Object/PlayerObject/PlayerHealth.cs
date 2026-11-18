using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float invincibleTime = 0.6f;//受击后的无敌时间
    public float blinkTime = 0.08f; //受击后的闪烁时间

    public float dodgeInvincibleUntil;//无敌持续到何时 用于和当前游戏时间进行比较 没到此时间 处于无敌状态

    public bool isInvincible; //是否处于无敌状态
    private SpriteRenderer[] sr; 

    public bool IsInvincible => isInvincible || Time.time < dodgeInvincibleUntil;
    public bool isDead => GameDataMgr.Instance.playerData.nowHp <= 0;


    private void Awake()
    {
        sr = GetComponentsInChildren<SpriteRenderer>();
    }

    public bool TakeDamage(int damage)
    {
        if(isDead || IsInvincible) return false;
        GameDataMgr.Instance.TackDamage(damage);
        StartCoroutine("Blink");
        return true;
    }


    //该协程的目的是实现受击无敌加闪烁
    IEnumerator Blink()
    {
        isInvincible = true;
        float timer = 0f;
        while (timer < invincibleTime)
        {
            foreach (SpriteRenderer item in sr)
            {
                item.enabled = !item.enabled;
            }
            timer += blinkTime;
            yield return new WaitForSeconds(blinkTime) ;
            
        }
        foreach (SpriteRenderer item in sr)
        {
            item.enabled = true;
        }
        isInvincible =false;
    }

    /// <summary>
    /// 设置 无敌持续到何时
    /// </summary>
    /// <param name="time">闪避的无敌时间</param>
    public void SetDodgeInvincibleUntil(float time)
    {
        dodgeInvincibleUntil = Mathf.Max(dodgeInvincibleUntil, Time.time + time);
    }

}
