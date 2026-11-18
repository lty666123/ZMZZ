using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerLevSystem 
{


    public const int maxLevel = 20;

    public static int NeedExp(int level)
    {
        return 100 + (level - 1) * 50;
    }

    public static void AddExp(int exp)
    {
        PlayerData pd = GameDataMgr.Instance.playerData;
        if(pd.lev <= 0)
        {
            pd.lev = 1;
        }
        if(pd.lev>=maxLevel)
        {
            pd.exp = 0;
            return;
        }
        pd.exp += exp;
        while(pd.lev<maxLevel&&pd.exp>NeedExp(pd.lev))
        {
            pd.exp -= NeedExp(pd.lev);
            LevelUp(pd);
            Debug.Log("当前等级" + pd.lev + "当前攻击力" + pd.atk);
        }
        GameDataMgr.Instance.SavePlayerData();
    }
    public static void LevelUp(PlayerData pd)
    {
        pd.lev += 1;
        pd.atk += 5;

    }
    
}
