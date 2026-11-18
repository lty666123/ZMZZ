using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//这里要实现的是在游戏进行时 打开背包页面 或 打开设置页面等操作的
public class maingame : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
         GameObject player = Resources.Load<GameObject>("Role/Player");
        //创建玩家对象 并对其进行玩家数据初始化
        //绑定摄像机
        if(player != null)
            Instantiate(player, new Vector3(0,0,0),Quaternion.identity);

        //加载游戏场景panel

        //数据初始化



       
    }

    // Update is called once per frame
    void Update()
    {
        //按B键打开背包
        if (Input.GetKeyDown(KeyCode.B))
        {
            UIManger.Instance.ShowPanel<BagPanel>();
        }
        //按Esc键 打开设置
        if (Input.GetKeyDown(KeyCode.Escape))
        { 
            UIManger.Instance.ShowPanel<EscPanel>();
        }
        //按M键打开地图
        if (Input.GetKeyDown(KeyCode.M))
        { 
            
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            GameDataMgr.Instance.inventory.Add("Item_Coin", 1);
            int left = GameDataMgr.Instance.inventory.Add("Item_Box", 5);
            Debug.Log(left == 0 ? "加了 5 瓶红药水" : "背包满了，还剩 " + left + " 瓶");
            GameDataMgr.Instance.SaveInventoryData();
        }

        //给自己扣血 方便测试
        if (Input.GetKeyDown(KeyCode.RightShift))
        {
            if (GameDataMgr.Instance.playerData.nowHp - 100 < 0) return;
            GameDataMgr.Instance.playerData.nowHp -= 100;

            GameDataMgr.Instance.SavePlayerData();
            Debug.Log("扣了100hp当前剩余hp "+ GameDataMgr.Instance.playerData.nowHp);

        }
    }
}
