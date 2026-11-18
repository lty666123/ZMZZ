using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System;

public class EnemySpawnPoint : MonoBehaviour
{
    //要生成的敌人数量
    public int enemyNum = 3;
    
    public float spawnTime = 1f;
    public int aliveEnemyNum;
    //怪物列表
    public List<Enemy> enemies = new List<Enemy>();
    
    //波次数
    public int waveNum = 2;
    
    //怪物生成时 的偏移
    public float offset = 1f;
    //战斗开始
    private bool hasStarted;
    private bool isBattling;

    //战斗时为true 通知摄像机固定 false解除固定
    public static event Action<bool> BattleStateChanged;

    //空气墙的父对象
    public GameObject invisibleWall;


    private void OnEnable()
    {
        PlayerObject.PlayerDied += OnPlayerDied;
    }

    // Start is called before the first frame update
    void Start()
    {
        //Invoke("SpawnEnemyWave", 5f);
        //11、17日再次优化出怪逻辑 并添加空气墙 和 摄像机固定 
        //StartCoroutine("SpawnEnemyWaveRoutine");

        if(invisibleWall != null)
        {
            invisibleWall.SetActive(false);
        }

    }

    // Update is called once per frame
    void Update()
    {
    }

    //战斗触发点
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(hasStarted)
        {
            return;
        }
        PlayerObject player = collision.GetComponentInParent<PlayerObject>();
        if(player == null)
        {
            return;
        }
        if(invisibleWall == null)
        {
            Debug.LogError("没有绑定空气墙");
            return;
        }
        hasStarted = true;
        isBattling = true;
        //进入战斗 开启空气墙
        invisibleWall.SetActive(true);
        //通知摄像机
        BattleStateChanged?.Invoke(true);

        StartCoroutine(SpawnEnemyWaveRoutine());

        Debug.Log("玩家进入区域 开始战斗");
    }


    public IEnumerator SpawnEnemyWaveRoutine()
    {
        yield return new WaitForSeconds(spawnTime);
        for (int i = 0; i < waveNum; ++i)
        {
            SpawnEnemy();
            yield return new WaitUntil(AllEnemyDead);
            yield return new WaitForSeconds(3f);
        }
        isBattling = false;
        invisibleWall.SetActive(false);
        BattleStateChanged?.Invoke(false);
        Debug.Log("区域战斗完成");
        //yield return new WaitForSeconds(2f);
        //GameObject obj = Instantiate(Resources.Load("Enemy/Minotaur"), transform.position, Quaternion.identity) as GameObject;

        //GameObject obj2 = Instantiate(Resources.Load("Enemy/Dragon"), transform.position, Quaternion.identity) as GameObject;

    }

    //生怪逻辑
    public void SpawnEnemy()
    {
        offset = 0;
        for (int i = 0; i < enemyNum; i++)
        {
            Vector3 v3 = new Vector3(transform.position.x + offset + 6f, transform.position.y, 0);
            GameObject obj = Instantiate(Resources.Load("Enemy/Enemy"), v3, Quaternion.identity) as GameObject;
            Enemy enemy = obj.GetComponent<Enemy>();
            ++aliveEnemyNum;
            enemies.Add(enemy);
            offset += 1f;
            Debug.Log("当前敌人数量" + enemies.Count);
        }

    }

    //这一波敌人都死亡 才会刷新下一波
    public bool AllEnemyDead()
    {
        RemoveEnemy();
        return enemies.Count == 0;
    }

    //敌人死亡 列表减去
    public void RemoveEnemy()
    {
        if (enemies == null) return;
        enemies.RemoveAll(a => a.hp == 0);
    }


    private void OnDisable()
    {
        PlayerObject.PlayerDied -= OnPlayerDied;
    }

    private void OnPlayerDied(PlayerObject player)
    {
        if (!isBattling)
            return;
        StopAllCoroutines();

        foreach (var enemy in enemies)
        {
            if(enemy == null)
            {
                continue;
            }
            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
        }
        enemies.Clear();
        aliveEnemyNum = 0;

        isBattling = false;
        hasStarted = false;
        //取消空气墙
        invisibleWall.SetActive(false);
        //解除摄像机固定
        BattleStateChanged?.Invoke(false);
        Debug.Log("挑战失败 区域已经重置");
    }

}
