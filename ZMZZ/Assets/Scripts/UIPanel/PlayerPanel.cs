using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanel : BasePanel
{

    public Button btnBack;
    public Button btnEquip;
    public Button btnDrop;

    public Text txtLev;
    public Text txtHp;
    public Text txtAtk;
    public Text txtDef;
    public Text txtEquipmentDef;
    public ItemSlotUI armorSlot1;
    public ItemSlotUI armorSlot2;
    public ItemSlotUI weaponSlot1;
    public ItemSlotUI weaponSlot2;

    private EquipmentBag equipmentBag;
    public Transform equipmentSlotsUIsGridRoot;
    public GameObject itemSlotPrefab;
    private List<ItemSlotUI> equipmentBagSlots = new List<ItemSlotUI>();
    private int selectBagSlot = -1;

    PlayerData pd;

    public override void Init()
    {
        //返回按钮
        btnBack.onClick.AddListener(() =>
        {
            UIManger.Instance.HidePanel<PlayerPanel>();
        });

        btnEquip.interactable = false;
        btnDrop.interactable = false;
        btnEquip.onClick.AddListener(OnbtnEquipClicked);
        btnDrop.onClick.AddListener(OnbtnDropClicked);

        equipmentBag = GameDataMgr.Instance.equipmentBag;

        //创建装备背包格子
        for (int i = 0; i < equipmentBag.Data.slots.Count; i++)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, equipmentSlotsUIsGridRoot, false);

            ItemSlotUI slot = slotObj.GetComponent<ItemSlotUI>();
            equipmentBagSlots.Add(slot);
        }
        equipmentBag.changed += RefreshEquipmentSlotsAndEquipmentBagSlots;
        
        RefreshEquipmentSlotsAndEquipmentBagSlots();
    }

    protected override void Update()
    {

        base.Update();
        pd = GameDataMgr.Instance.playerData;
        txtLev.text = "Lv." + pd.lev;
        txtHp.text = "" + pd.totalHp;
        txtAtk.text = "" + pd.totalAtk;
        txtDef.text = "" + pd.totalDef;

    }

    //显示装备槽和装备背包中内容的方法
    private void RefreshEquipmentSlotsAndEquipmentBagSlots()
    {
        RefreshEquipmentSlots();
        for(int i = 0;i < equipmentBagSlots.Count; i++)
        {
            equipmentBagSlots[i].SetupEquipment(i,OnEquipmentBagSlotClicked,equipmentBag.Data.slots[i]);
            equipmentBagSlots[i].SetSelected(i == selectBagSlot);
        }
    }

    //刷新装备槽的方法
    private void RefreshEquipmentSlots()
    {
        if(armorSlot1 != null)
        {
            armorSlot1.SetupEquipment(2,OnEquipmentSlotClicked, equipmentBag.Data.armorId);
        }
        if (armorSlot2 != null)
        {
            armorSlot2.SetupEquipment(3, OnEquipmentSlotClicked, equipmentBag.Data.armorId2);
        }
        if (weaponSlot1 != null)
        {
            weaponSlot1.SetupEquipment(0, OnEquipmentSlotClicked, equipmentBag.Data.weaponId);
        }
        if (weaponSlot2 != null)
        {
            weaponSlot2.SetupEquipment(1, OnEquipmentSlotClicked, equipmentBag.Data.weaponId2);
        }
    }

    /// <summary>
    /// 背包格子被点击时 要做的事情 高亮 有东西要可装备
    /// </summary>
    /// <param name="slotIndex"></param>
    private void OnEquipmentBagSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equipmentBagSlots.Count) return;
        selectBagSlot = slotIndex;
        btnEquip.interactable = !string.IsNullOrEmpty(equipmentBag.Data.slots[selectBagSlot]);
        btnDrop.interactable = !string.IsNullOrEmpty(equipmentBag.Data.slots[selectBagSlot]);
        for (int i = 0; i < equipmentBagSlots.Count; i++)
        {
            equipmentBagSlots[i].SetSelected(i == selectBagSlot);
        }
        if (selectBagSlot < 0) return;
        ItemDefinition def = ItemDataBase.Get(equipmentBag.Data.slots[selectBagSlot]);
        if(def == null)
        {
            txtEquipmentDef.text = "";
            return;
        }
        txtEquipmentDef.text = def.displayName
                + "\n攻击力:" + def.atkBonus
                + "\n防御力:" + def.defBonus
                + "\n生命值:" + def.hpBonus;
    }
    /// <summary>
    /// 装备槽被点击时 要做的事情
    /// </summary>
    /// <param name="slotIndex"></param>
    private void OnEquipmentSlotClicked(int slotIndex)
    {
        if(!equipmentBag.Unequip(slotIndex))
        {
            Debug.Log("卸下失败 装备槽为空 或背包已满");
        }
    }
    /// <summary>
    /// 装备按钮 被 点击 要做的事情
    /// </summary>
    private void OnbtnEquipClicked()
    {
        if(selectBagSlot < 0) return;
        int bagSlotIndex = selectBagSlot;
        if(!equipmentBag.Equip(bagSlotIndex))
        {
            Debug.Log("装备失败 物品无效 或对应类型的装备槽已满");
            return;
        }
        selectBagSlot = -1;
        RefreshEquipmentSlotsAndEquipmentBagSlots();
    }
    /// <summary>
    /// 丢弃按钮 被 点击 要做的事情
    /// </summary>
    private void OnbtnDropClicked()
    {
        if (selectBagSlot < 0) return;
        int bagSlotIndex = selectBagSlot;
        if(equipmentBag.Rem(bagSlotIndex))
        {
            Debug.Log("已丢弃");
            return;
        }
        selectBagSlot = -1;
        RefreshEquipmentSlotsAndEquipmentBagSlots();
    }

    private void OnDestroy()
    {
        if(equipmentBag != null)
            equipmentBag.changed -= RefreshEquipmentSlotsAndEquipmentBagSlots;
    }

}
