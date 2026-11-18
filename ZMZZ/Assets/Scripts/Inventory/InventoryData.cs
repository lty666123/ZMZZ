using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//背包数据
[System.Serializable]
public class InventoryData 
{
    public int version = 1;                                  // 存档版本号
    public int slotCount = 20;                               // 一共几格
    public List<ItemStack> slots = new List<ItemStack>();     // 格子的内容

}
