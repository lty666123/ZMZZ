using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

[System.Serializable]
public class ItemStack
{
    public string itemId = "";
    public int count;

    public ItemStack() { }
    public ItemStack(string itemId, int count)
    {
        this.itemId = itemId;
        this.count = count;
    }

    //判断各格子是否有物品
    public bool IsEmpty { get { return string.IsNullOrEmpty(itemId) || count <= 0; } }
}
