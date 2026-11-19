using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    EnemyData enemyData;

    //敌人当前的血量
    public int hp;
    public int maxHp = 50;

    private SpriteRenderer sr;

    //敌人死亡掉落的物品
    public GameObject lootPrefab;
    public GameObject lootPrefab2;

    //下一次造成伤害的时间
    public float nextDamageTime;
    //时间间隔
    public float nextAddTime = 1f;

    //攻击节奏：先起手，再判定伤害，最后等待后摇结束
    public float atkWindup = 0.22f;
    public float atkDuration = 0.7f;
    private float atkTimer;
    private bool hasHit;

    public int damage = 10;

    public Animator animator;

    public Transform atkPoint;
    public float atkRadius = 1;

    public int atk;

    public Collider2D[] colliders;


    public enum State
    {
        idle,
        atk,
        move,
        die,
    }

    public State state;
    public PlayerObject player;
    public PlayerHealth playerHealth;
    public float distance;
    public float atkDistance = 2f;

    //public float minFindDistance = 3f;
    public float maxFindDistance = 10f;

    public Rigidbody2D rb;
    public float moveSpeed = 3f;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        rb = GetComponentInChildren<Rigidbody2D>();
    }
    private void Start()
    {

        state = State.idle;
        FindPlayer();
    }


    public void Init(EnemyData enemyData)
    {
        hp = enemyData.hp;

        atk = enemyData.atk;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }
        if (player == null) return;

        distance = Vector3.Distance(player.transform.position, this.transform.position);
        switch(state)
        {
            case State.idle:
                rb.velocity = new Vector2(0, rb.velocity.y);
                animator.SetBool("Move", false);

                if (distance <= atkDistance)
                {
                    if (Time.time >= nextDamageTime)
                        SetState(State.atk);
                }
                else if (distance < maxFindDistance)
                {                    SetState(State.move);
                }
                break;

            case State.atk:
                rb.velocity = new Vector2(0, rb.velocity.y);
                animator.SetBool("Move", false);
                atkTimer += Time.deltaTime;

                if (!hasHit && atkTimer >= atkWindup)
                {
                    hasHit = true;
                    EnemyAtk();
                    nextDamageTime = Time.time + nextAddTime;
                }

                if (atkTimer >= atkDuration)
                    SetState(State.idle);
                break;

            case State.move:
                if (distance <= atkDistance)
                {
                    rb.velocity = new Vector2(0, rb.velocity.y);

                    if (Time.time >= nextDamageTime)
                        SetState(State.atk);
                    else
                        SetState(State.idle);
                }
                else if (distance > maxFindDistance)
                {
                    rb.velocity = new Vector2(0, rb.velocity.y);
                    SetState(State.idle);
                }
                else
                {
                    MoveToPlayer();
                }
                break;

            case State.die:
                rb.velocity = Vector2.zero;
                break;
        }


    }

    //收到伤害的表现
    public void TakeDamage(int damage)
    {
        if (hp <= 0) return;
        Debug.Log("收到伤害" + damage);
        hp -= damage;
        if (hp>0)
        {
            sr.color = Color.red;
            Invoke("TurnWrite", 0.6f);
            Debug.Log("当前生命值" + hp);
            DamageTextPool.Instance.ShowDamage(damage,Color.yellow ,this.transform.position);
        }
        else if(hp<=0)
        {
            hp = 0;
            Debug.Log("死亡");
            Die();
            
            Destroy(gameObject, 1f);
            PlayerLevSystem.AddExp(100);
        }
       
    }

   
    public void Die()
    {
        animator.SetBool("Dead",true);

        if (lootPrefab != null)
        {
            Vector2 pos = this.transform.position + Vector3.up * 0.5f;
            Instantiate(lootPrefab,pos,Quaternion.identity);
            Instantiate(lootPrefab2, pos, Quaternion.identity);

        }

        //Destroy(gameObject);
    }

    public void TurnWrite()
    {
        sr.color = Color.white;
    }

    public void EnemyAtk()
    {
        colliders = Physics2D.OverlapCircleAll(atkPoint.transform.position, atkRadius ,LayerMask.GetMask("Player"));
        if (colliders == null) return;
        foreach(Collider2D collider in colliders)
        {
            
            PlayerHealth playerHealth = collider.GetComponent<PlayerHealth>();
            if (playerHealth == null) return;
            if(playerHealth.TakeDamage(atk))
            {
                Debug.Log("玩家受到 " + damage + "点伤害" + "当前剩余生命值 " + GameDataMgr.Instance.playerData.nowHp);
            }
        }
    }

    //public void OnCollisionStay2D(Collision2D collision)
    //{
    //    if (Time.time < nextDamageTime) return;

    //    PlayerHealth player = collision.gameObject.GetComponentInParent<PlayerHealth>();
    //    if(player == null) return;
    //    if(player.TakeDamage(enemyData.atk))
    //    {
    //        Debug.Log("玩家受到 " + damage + "点伤害" + "当前剩余生命值 " + GameDataMgr.Instance.playerData.nowHp);
    //    }
    //    nextDamageTime = Time.time + nextAddTime;

    //}

    public void SetState(State s)
    {
        if (s == State.atk && state != State.atk)
        {
            atkTimer = 0;
            hasHit = false;

            rb.velocity = new Vector2(0, rb.velocity.y);
            animator.SetBool("Move", false);
            FacePlayer();

            animator.ResetTrigger("Atk");
            animator.SetTrigger("Atk");
        }

        state = s;
    }
    //寻找玩家 并获取到玩家身上的血条组件 可以进行伤害
    public void FindPlayer()
    {
        player = FindObjectOfType<PlayerObject>();
        if(player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    //移动向玩家
    public void MoveToPlayer()
    {
        float dir = Mathf.Sign( player.transform.position.x - this.transform.position.x);
        FacePlayer();

        rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);
        animator.SetBool("Move", true);
    }
    //是朝向 向玩家
    private void FacePlayer()
    {
        float dir = Mathf.Sign(player.transform.position.x - this.transform.position.x);
        if (Mathf.Approximately(dir, 0)) return;

        sr.flipX = dir < 0;

        Vector3 point = atkPoint.localPosition;
        point.x = Mathf.Abs(point.x) * dir;
        atkPoint.localPosition = point;
    }

}
