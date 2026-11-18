using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Weapon,
    armor,
    Material,
    Consumable,
}

[CreateAssetMenu(fileName = "Item_", menuName = "ZMZZ/物品定义")]
public class ItemDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;

    public ItemType type = ItemType.Material; 
    public int maxStack = 99; //每种物品最大个数

    public int hpAdd = 30;

    [TextArea]
    public string itenDiscription;


    [Header("装备属性")]
    public int atkBonus;
    public int defBonus;
    public int hpBonus;

}
