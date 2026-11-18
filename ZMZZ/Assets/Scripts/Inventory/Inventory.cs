using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//背包逻辑类 用于管理InventoryData
public class Inventory 
{
    public event Action changed;

    public InventoryData Data {  get; private set; }

    public Inventory(InventoryData data)
    {
        Data = data;
        //补齐各子 避免null的出现
        while(Data.slots.Count<data.slotCount)
        {
            Data.slots.Add(new ItemStack());
        }
    }

    //增加物品的方法
    public int Add(string itemId,int count)
    {
        //根据查找表 查堆叠上限
        ItemDefinition def = ItemDataBase.Get(itemId);
        if (def == null) return count;
        int maxStack = def.maxStack;
        if(maxStack < 1) maxStack = 1;

        //进行堆叠 第一轮
        for (int i = 0; i < Data.slots.Count && count > 0; i++)
        {
            if (Data.slots[i].itemId != itemId) continue;
            int space =maxStack - Data.slots[i].count - count; 
            //空间放不放的下 尽可能的放满
            if(space < 0)
            {
                count = count - (maxStack - Data.slots[i].count);
                Data.slots[i].count = maxStack; 
            }
            else
            {
                Data.slots[i].count += count;
                count = 0;
            }
        }
        //还有没放下的 进行第二轮创建
        for(int i = 0;i < Data.slots.Count&&count > 0;i++)
        {
            if (!Data.slots[i].IsEmpty) continue; //找空格 不是空的就跳过
            Data.slots[i].itemId = itemId;
            int put = Mathf.Min(maxStack, count);
            Data.slots[i].count += put;
            count -= put;
        }
        Raise();
        return count;
    }

    //查找物品数量的方法
    public int CountFind(string itemId)
    {
        int total = 0;
        for (int i = 0; i < Data.slots.Count; i++)
        {
            if (Data.slots[i].itemId == itemId) total += Data.slots[i].count;
        }
        return total;
    }

    //拿走物品的方法
    public bool Remove(string itemId, int count)
    {
        if (count <= 0) return true;
        if (CountFind(itemId) < count) return false; //如果要拿走的数量大于总的数量 则不能动
        int getCount = 0;
        for (int i = Data.slots.Count - 1; i >= 0; i--)
        {
            if (Data.slots[i].itemId != itemId) continue;
            getCount = Mathf.Min(Data.slots[i].count, count);
            Data.slots[i].count -= getCount;
            count -= getCount;
            if (Data.slots[i].count<=0) //一个格子里的东西被拿完了 就将其变为空格子
            {
                Data.slots[i] = new ItemStack();
            }
        }
        Raise();
        return true;
    }

    //使用消耗品的方法
    public bool UseAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Data.slots.Count) return false;
        ItemStack slot = Data.slots[slotIndex];
        if (slot.IsEmpty) return false;
        ItemDefinition def = ItemDataBase.Get(slot.itemId);
        if(def == null) return false;
        if(def.type != ItemType.Consumable ) return false;
        
        //进行测试
        if(def.hpAdd<=0 || GameDataMgr.Instance.playerData.nowHp>=GameDataMgr.Instance.playerData.totalHp) return false;
        
        Debug.Log("使用了" + def.name);


        //这里是一个测试
        GameDataMgr.Instance.RestoreHp(def.hpAdd);
        Debug.Log("玩家血量增加" + def.hpAdd + "当前血量为" + GameDataMgr.Instance.playerData.nowHp);
        Remove(slot.itemId, 1);
        return true;
    }

    //用于通知订阅者
    public void Raise()
    {
        changed?.Invoke();
    }

}
