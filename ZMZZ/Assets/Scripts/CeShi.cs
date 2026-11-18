using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CeShi : MonoBehaviour
{
    Inventory inv;
    public Transform pos;
    // Start is called before the first frame update
    void Start()
    {
        //先进行一些测试
        InventoryData inventory = new InventoryData();
        inventory.slots.Add(new ItemStack("血瓶", 6));
        Debug.Log("数量" + inventory.slots[0].count);
        Debug.Log("是否为空格子" + inventory.slots[0].IsEmpty);

        var def = ItemDataBase.Get("Item_Box");
        Debug.Log(def != null ? "找到这个Box了" : "没有这个Box");
        var def2 = ItemDataBase.Get("232");

        inv = new Inventory(new InventoryData());
        Debug.Log("背包的格子数" + inv.Data.slotCount);


        bool swordSaved = GameDataMgr.Instance.equipmentBag.Data.slots.Contains("Item_TestSword");
        Debug.Log(swordSaved ? "找到测试铁剑" : "目前存档还没有测试铁剑");
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.L))
        {
            int left = GameDataMgr.Instance.inventory.Add("Item_Box", 50);
            GameDataMgr.Instance.inventory.Add("Item_Gun", 50);

            Debug.Log(left>0 ? "背包满了" : "加了50个Item_Box,当前一共有" + GameDataMgr.Instance.inventory.CountFind("Item_Box"));
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            bool use = GameDataMgr.Instance.inventory.UseAt(1);
            
            Debug.Log( use ? "使用了1个Item_Box,当前一共有" + GameDataMgr.Instance.inventory.CountFind("Item_Box") : "使用失败");


        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            GameDataMgr.Instance.SaveInventoryData();
        }
        if(Input.GetKeyDown(KeyCode.P))
        {
            bool added = GameDataMgr.Instance.equipmentBag.Add("Item_TestSword");
            Debug.Log(added ? "添加成功" : "添加失败");

        }
        if( Input.GetKeyDown(KeyCode.F6))
        {
            int index = GameDataMgr.Instance.equipmentBag.Data.slots.FindIndex(id => id=="Item_TestSword");

            bool success = index >= 0 && GameDataMgr.Instance.equipmentBag.Equip(index);

            Debug.Log(success ? "装备成功" : "装备失败 背包没有铁剑 或武器槽已满");
            Debug.Log("武器槽1：" + GameDataMgr.Instance.equipmentBag.Data.weaponId);
            Debug.Log("武器槽2：" + GameDataMgr.Instance.equipmentBag.Data.weaponId2);
        }
        if(Input.GetKeyDown(KeyCode.F7))
        {
            bool success = GameDataMgr.Instance.equipmentBag.Unequip(0);
            Debug.Log(success ? "已卸下武器槽1" : "卸下失败 槽为空 或背包已满");
        }
        if(Input.GetKeyDown(KeyCode.F8))
        {
            bool success = GameDataMgr.Instance.equipmentBag.Unequip(1);
            Debug.Log(success ? "已卸下武器槽2" : "卸下失败 槽为空 或背包已满");
        }
        if(Input.GetKeyDown (KeyCode.W))
        {
            GameObject obj = Instantiate(Resources.Load<GameObject>("Enemy/Dragon"), pos.transform.position, quaternion.identity);
        }
    }
}
