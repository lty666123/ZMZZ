using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EquipmentBagData
{
    //版本号
    public int version = 1;
    //装备背包格子数
    public int slotCount = 16;
    //每个格子的标记方法是依靠其id
    public List<string> slots = new List<string>();

   //武器一 武器二
    public string weaponId = "";
    public string weaponId2 = "";

    //防具一 防具二 
    public string armorId = "";
    public string armorId2 = "";

}
