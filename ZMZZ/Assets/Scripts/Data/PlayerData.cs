using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData 
{
    public int equipAtk;
    public int equipDef;
    public int equipHp;

    public int lev = 1;
    public int exp = 0;
    public int atk = 10;
    public int maxHp = 500;
    public int def = 30;

    public int nowHp = 500;

    public int totalAtk => equipAtk + atk;
    public int totalDef => equipDef + def;
    public int totalHp => equipHp + maxHp;


    //装备栏 目前 后面还会调整
    public List<string> equipmentIds = new List<string>();

    
}
