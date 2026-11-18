using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Dragon : MonoBehaviour
{
    public int maxHp;
    public int nowHp;

    //攻击判定位置点
    public Transform atkPoint1;
    public Transform atkPoint2;
    //攻击半径
    public float atkRadius1;
    public float atkRadius2;
    //攻击伤害
    public int atk1;
    public int atk2;

    public int def;

    private bool hasHit;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    //攻击前摇 与 攻击持续时间相关
    public float atk1Windup;
    public float atk1Duration;
    public float atk2Windup;
    public float atk2Duration;

    public float atk1Timer;
    public float nextAtkTime;

    //攻击间隔
    public float atkInterval;

    //敌人状态
    public enum State
    {
        idle,
        atk1,
        atk2,
        dead,
        move,
        idle_battle
    }

    public State state;

    public PlayerHealth playerHealth;
    public PlayerObject player;

    public Collider2D[] colliders1;
    public Collider2D[] colliders2;

    public Transform foot;

    public float distance;


    public float atkDistance;

    //从普通待机状态转向战斗待机状态的距离
    public float battleReadyDistance;


    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

    }
    // Start is called before the first frame update
    void Start()
    {
        state = State.idle;
        FindPlayer();
    }

    // Update is called once per frame
    void Update()
    {
        distance = Vector2.Distance(foot.transform.position, player.transform.position);
        switch (state)
        {
            case State.idle:
                rb.velocity = new Vector2(0, rb.velocity.y);
                if (distance <= battleReadyDistance)
                {
                    SetState(State.idle_battle);
                }
                animator.SetBool("Idle_Battle", false);

                break;
            case State.idle_battle:
                if (distance >= battleReadyDistance)
                {
                    SetState(State.idle);
                }
                animator.SetBool("Idle_Battle", true);
                rb.velocity = new Vector2(0, rb.velocity.y);
                if (distance <= atkDistance)
                    if (Time.time > nextAtkTime)
                        SetState(Random.Range(0,100)>30?State.atk1:State.atk2);
                break;

            case State.atk1:
                rb.velocity = new Vector2(0, rb.velocity.y);
                atk1Timer += Time.deltaTime;
                if (!hasHit && atk1Timer >= atk1Windup)
                {
                    hasHit = true;
                    Atk1();
                    nextAtkTime = Time.time + atkInterval;
                }
                if (atk1Timer >= atk1Duration)
                    SetState(State.idle_battle);
                break;

            case State.atk2:
                rb.velocity = new Vector2(0, rb.velocity.y);
                atk1Timer += Time.deltaTime;
                if (!hasHit && atk1Timer >= atk2Windup)
                {
                    hasHit = true;
                    Atk2();
                    nextAtkTime = Time.time + atkInterval;
                }
                if (atk1Timer >= atk2Duration)
                    SetState(State.idle_battle);
                break;

            case State.dead:
                animator.SetBool("Dead", true);
                Destroy(gameObject, 4f);
                break;

        }
    }

    //给敌人设置状态的方法
    private void SetState(State st)
    {
        if(st == State.atk1)
        {
            atk1Timer = 0;
            hasHit = false;
            animator.ResetTrigger("Atk1");
            animator.SetTrigger("Atk1");
        }
        if(st == State.atk2)
        {
            atk1Timer = 0;
            hasHit = false;
            animator.ResetTrigger("Atk2");
            animator.SetTrigger("Atk2");
        }
        state = st;
    }

    //攻击方式一
    private void Atk1()
    {
        colliders1 = Physics2D.OverlapCircleAll(atkPoint1.transform.position, atkRadius1, LayerMask.GetMask("Player"));
        if (colliders1 == null) return;
        for (int i = 0; i < colliders1.Length; i++)
        {
            playerHealth = colliders1[i].GetComponent<PlayerHealth>();

            if (playerHealth.TakeDamage(atk1))
            {
                Debug.Log("玩家受到" + atk1  + "点伤害");
            }
        }
    }
    //攻击方式二
    private void Atk2()
    {
        colliders2 = Physics2D.OverlapCircleAll(atkPoint2.transform.position, atkRadius2, LayerMask.GetMask("Player"));
        if (colliders2 == null) return;
        for (int i = 0; i < colliders2.Length; i++)
        {
            playerHealth = colliders2[i].GetComponent<PlayerHealth>();

            if (playerHealth.TakeDamage(atk2))
            {
                Debug.Log("玩家受到" + atk2 + "点伤害");
            }
        }
    }
    //使敌人能够挂载玩家对象
    private void FindPlayer()
    {
        player = FindObjectOfType<PlayerObject>();
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
    }
    //受到伤害扣血 供外部调用
    public void TakeDamage(int damage)
    {
        if (nowHp <= 0)
        {
            SetState(State.dead);
            return;
        }

        if(damage < def)
        {
            nowHp -= 1;
            DamageTextPool.Instance.ShowDamage(1, Color.red, this.transform.position + Vector3.up * 3);
        }
        else
        {
            nowHp = (int)(nowHp - damage + def * 0.8f);
            DamageTextPool.Instance.ShowDamage((int)(damage - def*0.8f), Color.red, this.transform.position + Vector3.up * 2);

        }



    }

}
