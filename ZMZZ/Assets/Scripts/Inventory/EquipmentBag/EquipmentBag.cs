using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EquipmentBag
{
    public EquipmentBagData Data {  get; private set; }

    public event Action changed;

    public EquipmentBag(EquipmentBagData data)
    {
        Data = data;
        while(Data.slots.Count<Data.slotCount)
        {
            Data.slots.Add("");
        }
    }

    public bool Add(string itemId)
    {
        ItemDefinition def = ItemDataBase.Get(itemId);

        if (def == null) return false;
        if (def.type != ItemType.armor && def.type != ItemType.Weapon) return false;

        int index = Data.slots.FindIndex(string.IsNullOrEmpty);
        if (index < 0) return false;
        Data.slots[index] = itemId;
        Raise();
        return true;

    }
    public bool Rem(int index)
    {
        Data.slots[index] = "";
        Raise();
        return true;
    }
    /// <summary>
    /// 用于装备
    /// </summary>
    /// <param name="slotIndex">传入要装备的排序号</param>
    /// <returns></returns>
    public bool Equip(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Data.slots.Count) return false;

        string id = Data.slots[slotIndex];
        if(string.IsNullOrEmpty(id)) return false;
        ItemDefinition def = ItemDataBase.Get(id);
        if (def == null) return false;

        if(def.type == ItemType.Weapon)
        {
            if(string.IsNullOrEmpty(Data.weaponId))
            {
                Data.weaponId = id;
            }
            else if(string.IsNullOrEmpty(Data.weaponId2))
            {
                Data.weaponId2 = id;
            }
            else
            {
                return false;
            }
        }
        else if (def.type == ItemType.armor)
        {
            if( string.IsNullOrEmpty(Data.armorId))
            {
                Data.armorId = id;
            }
            else if( string.IsNullOrEmpty(Data.armorId2))
            {
                Data.armorId2 = id;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
        Data.slots[slotIndex] = "";
        Raise();
        return true;
    }
    /// <summary>
    /// 卸下装备
    /// </summary>
    /// <param name="equipmentSlotIndex"></param>
    /// <returns></returns>
    public bool Unequip(int equipmentSlotIndex)
    {
        string id;
        switch (equipmentSlotIndex)
        {
            case 0: id = Data.weaponId; break;
            case 1: id = Data.weaponId2; break;
            case 2: id = Data.armorId; break;
            case 3: id = Data.armorId2; break;
                default: return false;
        }
        if(string.IsNullOrEmpty(id)) return false;
        //到装备背包里面找空格
        int bagIndex = Data.slots.FindIndex(string.IsNullOrEmpty);
        if(bagIndex < 0) return false;

        Data.slots[bagIndex] = id;

        switch(equipmentSlotIndex)
        {
            case 0: Data.weaponId = ""; break;
            case 1: Data.weaponId2 = ""; break;
            case 2: Data.armorId = ""; break;
            case 3: Data.armorId2 = ""; break;
        }

        Raise();
        return true;

    }
    public void Raise()
    {
        changed?.Invoke();
    }
}
