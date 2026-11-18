using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using System;

//该脚本是玩家相关逻辑 移动 跳跃 攻击 闪避........
public class PlayerObject : MonoBehaviour
{
    public float moveSpeed = 3f;//移动速度
    Rigidbody2D rb;
    int jumpCount = 0;//跳跃次数

    public Collider2D[] colliders;
    public List<Collider2D> exampleList;

    private float atkRadius = 1;//攻击半径
    public Transform atkPoint;//攻击点

    public Transform warrior;//
    public Animator animator;

    private bool isGround = true;
    public float groundCheckDistance = 1f;

    private bool canMove = true;
    private bool canJump = true;

    private bool isDash = false;
    private bool isBlock = false;
    private float dashSpeed = 10f;
    private float facing = 1f;
    private float dashDuration = 0.2f;//闪避的持续时间
    private float dashInvincibleTime = 0.25f; //闪避的无敌时间
    private float dashTime = 0.5f;//下一次可以使用闪避的时间间隔
    private float nextDashTime;//下一次可以闪避的时间

    //当前所在平台的高度 为了控制摄像机的上下移动
    public float goundy;

    public int layerMask;

    public PlayerData playerData;
    public EquipmentBag equipmentBag;

    private static readonly ProfilerMarker AttackQueryMarker =
    new ProfilerMarker("ZMZZ.Player.AttackQuery");

    //是否死亡
    private bool isDead;
    //通知其它系统 玩家死亡 主要是要告知enemy那里要重新布置
    public static event Action<PlayerObject> PlayerDied;

    private void Awake()
    {
        layerMask = LayerMask.GetMask("Ground");

        rb = GetComponent<Rigidbody2D>();
        warrior = this.transform.Find("warrior");
        animator = warrior.GetComponent<Animator>();
        playerData = GameDataMgr.Instance.playerData;
        equipmentBag = GameDataMgr.Instance.equipmentBag;
        equipmentBag.changed += AddAllEquipmentBonuses;
        AddAllEquipmentBonuses();
    }
    void Start()
    {
        goundy = this.transform.position.y;
        
    }

    // Update is called once per frame
    void Update()
    {
        Die();
        if(isDead)
        {
            return;
        }

        IsGround();
        Block();
        Moving();
        Jump();
        animator.SetFloat("yVelocity", rb.velocity.y);
        animator.SetFloat("xVelocity", rb.velocity.x);
        animator.SetBool("Jump", !isGround);
        Atk();
        Dash();
       
        
        
    }

    /// <summary>
    /// 给敌人造成伤害的方法
    /// </summary>
    private void DamageEnemies(int damage)
    {
        using (AttackQueryMarker.Auto())
        {
            colliders = Physics2D.OverlapCircleAll(atkPoint.transform.position, atkRadius, LayerMask.GetMask("Enemy"));

        }
        foreach (Collider2D collider in colliders) 
        {
            Enemy enemy =  collider.GetComponent<Enemy>();
            if(enemy != null)
                enemy.TakeDamage(damage);

            Enemy_Dragon enemy_Dragon = collider.GetComponent<Enemy_Dragon>();
            if (enemy_Dragon != null)
                enemy_Dragon.TakeDamage(damage);
        }
    }

    public void EnableJumpAndMove(bool enable)
    {
        canJump = enable;
        canMove = enable;
    }
    public void EnableJumpAndMove()
    {
        canJump = true;
        canMove = true;
    }
    //玩家移动
    public void Moving()
    {
        if (isDash) return;// 使在冲刺时 move不参与速度运算
        if (UIManger.Instance.HasPanel||(canMove == false))
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }
        float h = Input.GetAxisRaw("Horizontal");
        FlipMove(h);
        rb.velocity = new Vector2(h * moveSpeed, rb.velocity.y);
        
        
    }

    //玩家跳跃
    public void Jump()
    {
        float jumpSpeed = 8f;
        if (Input.GetKeyDown(KeyCode.K) && jumpCount < 1 &&canJump)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
            jumpCount++;
        }
            

    }

    //玩家普通攻击 冲刺攻击的逻辑在DashRoutine里面
    public void Atk()
    {
        if(isDash) return; 
        if(Input.GetKeyDown(KeyCode.J)&&isGround)
        {
            animator.ResetTrigger("DashAtk");
            animator.SetTrigger("Atk");
           
        }
    }

    public void Atk_Damage()
    {
        if (isDead) return;
        AudioManager.Instance.PlaySfx("Music/Gun");
        DamageEnemies((int) UnityEngine.Random.Range(playerData.totalAtk,(int) playerData.totalAtk*1.2f));
    }

    //实现 最多进行二连跳 即碰到地面时 重置jumpCount为0
    void OnCollisionEnter2D(Collision2D col)
    {
        for (int i = 0; i < col.contactCount; i++)
        {
            if (col.GetContact(i).normal.y > 0.5f)
            {
                jumpCount = 0;
                if(Mathf.Abs(goundy - transform.position.y)>1f)
                    goundy = transform.position.y;
                return;
            }
        }
    }

    //移动 进行翻转
    private void FlipMove(float h)
    {
        if (h != 0)
        {

            if (h < 0)
            {
                this.transform.rotation = Quaternion.Euler(0, 180, 0);
                facing = -1;
            }
            else if (h > 0)
            {
                this.transform.rotation = Quaternion.Euler(0, 0, 0);
                facing = 1;
            }
            animator.SetBool("Move", true);
        }
        else
        {
            animator.SetBool("Move", false);

        }
    }

    //
    public void IsGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(this.transform.position, Vector2.down, groundCheckDistance,  layerMask);
        isGround = hit.collider != null;
    }

    //玩家冲刺
    public void Dash()
    {
        if(Input.GetKeyDown(KeyCode.LeftShift)&&isGround 
            &&!isDash
            &&!isBlock
            &&Time.time>=nextDashTime)
        {
            StartCoroutine(DashRoutine());
        }
    }
    /// <summary>
    /// 用于闪避的协程
    /// </summary>
    /// <returns></returns>
    private  IEnumerator DashRoutine()
    {
        isDash = true;
        nextDashTime = Time.time + dashTime;
        animator.SetTrigger("Dash");

        animator.ResetTrigger("Atk");
        animator.ResetTrigger("DashAtk");

        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        { 
            playerHealth.SetDodgeInvincibleUntil(dashInvincibleTime);
        }
        float timer = 0;
        while(timer<dashDuration)
        {
            rb.velocity = new Vector2(facing * dashSpeed, rb.velocity.y);

            if(Input.GetKeyDown(KeyCode.J))
            {
                animator.SetTrigger("DashAtk");
                DamageEnemies((int)(playerData.totalAtk * 1.2f));

            }

            timer += Time.deltaTime;
            yield return null;
        }
        isDash = false;
    }

    private void Die()
    {
        if(playerData.nowHp>0||isDead)
        {
            return;
        }

        isDead = true;

        //以下几行来做状态清零
        //停止在执行的冲刺 协程
        StopAllCoroutines();
        CancelInvoke();
        isDash = false;
        //清除防御时加的防御力
        if(isBlock)
        {
            playerData.def -= 15;
            isBlock = false;
        }
        rb.velocity = Vector2.zero;
        animator.SetBool("Move", false);
        animator.SetBool("Block", false);
        animator.SetBool("Die", true);
        //enemy区域重置
        PlayerDied?.Invoke(this);

        StartCoroutine(RespwanAfterDelay());
    }

    private IEnumerator RespwanAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        Respwan();
    }

    //死亡后复活 这里待完善 应该复活至最近的复活点 还没做
    private void Respwan()
    {
        if (!isDead)
            return;
        this.transform.position = new Vector3(0, 0, 0);
        rb.velocity = Vector2.zero;
        goundy = transform.position.y;
        jumpCount = 0;
        playerData.nowHp = playerData.totalHp;

        canMove = true;
        canJump = true;

        animator.ResetTrigger("Atk");
        animator.ResetTrigger("Dash");
        animator.ResetTrigger("DashAtk");
        animator.SetBool("Die", false);
        isDead =false;
 }

    //玩家格挡 减伤 这里没有什么好的想法 只想到了 直接加防御 按住格挡键格挡加防御 松开减回去
    public void Block()
    {

        if(Input.GetKeyDown(KeyCode.H)&&isGround&&!isDash)
        {
            CancelInvoke("EnableJumpAndMove");
            rb.velocity = new Vector2(0, rb.velocity.y);
            EnableJumpAndMove(false);
            animator.SetBool("Move", false);
            isBlock = true;
            GameDataMgr.Instance.playerData.def += 15;
            animator.SetBool("Block", true);
        }
        if(Input.GetKeyUp(KeyCode.H))
        {
            GameDataMgr.Instance.playerData.def -= 15;
            animator.SetBool("Block", false);
            rb.velocity = new Vector2(0, rb.velocity.y);

            Invoke("EnableJumpAndMove", 1.1f);
            isBlock = false;
        }
    }

    //所有装备的加成
    private void AddAllEquipmentBonuses()
    {
        GameDataMgr.Instance.playerData.equipAtk = 0;
        GameDataMgr.Instance.playerData.equipDef = 0;
        GameDataMgr.Instance.playerData.equipHp = 0;

        AddEquipmentBonus(equipmentBag.Data.weaponId);
        AddEquipmentBonus(equipmentBag.Data.weaponId2);
        AddEquipmentBonus(equipmentBag.Data.armorId);
        AddEquipmentBonus(equipmentBag.Data.armorId2);

        if (GameDataMgr.Instance.playerData.nowHp > GameDataMgr.Instance.playerData.totalHp)
        {
            GameDataMgr.Instance.playerData.nowHp = GameDataMgr.Instance.playerData.totalHp;
        }
        GameDataMgr.Instance.SavePlayerData();
    }

    //根据装备为player添加属性
    private void AddEquipmentBonus(string equipmentId)
    {
        if (string.IsNullOrEmpty(equipmentId)) return;
        ItemDefinition def = ItemDataBase.Get(equipmentId);
        if (def == null) return;
        GameDataMgr.Instance.playerData.equipAtk += def.atkBonus;
        GameDataMgr.Instance.playerData.equipDef += def.defBonus;
        GameDataMgr.Instance.playerData.equipHp += def.hpBonus;

    }

    private void OnDestroy()
    {
        if (equipmentBag != null)
        {
            equipmentBag.changed -= AddAllEquipmentBonuses;
        }
    }

}
