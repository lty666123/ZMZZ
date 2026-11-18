using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BagPanel : BasePanel
{
    public Button btnBack;//返回按钮
    public Transform gridRoot;//子物体的自动排列
    public GameObject slotPrefab; //格子预设体 作为基础格子使用 
    public Button btnUse; //使用按钮
    public Button btnDrop; //丢弃按钮

    public Button btnAll;
    public Button btnConsumable;
    public Button btnMaterials;

    private List<int> viewIndex = new List<int>();
    private ItemType? filterType = null;
    
    private Inventory inv;

    public Text txtDescription;// 物品详情 加载到这里
    public int selectIndex = -1;

    public List<ItemSlotUI> slots = new List<ItemSlotUI>();
    public override void Init()
    {

        inv = GameDataMgr.Instance.inventory;

        for(int i = 0;i<inv.Data.slotCount;i++)
        {
            GameObject obj = GameObject.Instantiate(slotPrefab, gridRoot, false);
            slots.Add(obj.GetComponent<ItemSlotUI>());
        }

        btnAll.onClick.AddListener(() => SetFilter(null));
        btnConsumable.onClick.AddListener(() => SetFilter(ItemType.Consumable));
        btnMaterials.onClick.AddListener(() => SetFilter(ItemType.Material));



        inv.changed += Refresh;
        Refresh();

        btnBack.onClick.AddListener(() =>
        {
            UIManger.Instance.HidePanel<BagPanel>();
        });
      
        btnUse.interactable = false;
        btnDrop.interactable = false;
        btnUse.onClick.AddListener(OnUseClicked);
        btnDrop.onClick.AddListener(OnDropClicked);


    }

    /// <summary>
    /// 更新每一个格子界面即信息的方法
    /// setup 为每个格子设置了编号 增加了每个格子的点击事件 
    /// </summary>
    private void Refresh()
    {
        viewIndex.Clear();
        //先是遍历进行记录 符合filtertype的记录下角标
        for(int i =0;i<inv.Data.slots.Count;i++)
        {
            if (filterType.HasValue)
            {
                
                 ItemStack stack = inv.Data.slots[i];
                if (stack.IsEmpty) continue;
                ItemDefinition def = ItemDataBase.Get(stack.itemId);
                if(def == null||def.type != filterType.Value) 
                    continue;
            }
           viewIndex.Add(i);
        }
        //进行创建图标
        for (int ui = 0; ui < slots.Count; ui++)
        {
            if (ui < viewIndex.Count)
            {
                int dataIndex = viewIndex[ui];
                slots[ui].Setup(ui, OnSlotClicked, inv.Data.slots[dataIndex]);
            }
            else
            {
                slots[ui].Setup(ui, OnSlotClicked, null);
            }
            slots[ui].SetSelected(ui < viewIndex.Count && viewIndex[ui] == selectIndex);

        }

    }

    private void OnSlotClicked(int uiIndex)
    {
        if (uiIndex < 0 || uiIndex > viewIndex.Count) return;
        selectIndex = viewIndex[uiIndex];
        showDescription(selectIndex);
        for(int ui= 0; ui<slots.Count; ui++)
        {
            slots[ui].SetSelected(ui < viewIndex.Count && viewIndex[ui] == selectIndex);
        }
        //showDescription(index);
        //selectIndex = index;
        //for (int i = 0; i < slots.Count; i++)
        //{
        //    slots[i].SetSelected(i == selectIndex);
        //}
        //if (inv.UseAt(index))
        //{
        //    GameDataMgr.Instance.SaveInventoryData();
        //}
    }

    /// <summary>
    /// 点击使用 使用消耗品
    /// </summary>
    private void OnUseClicked()
    {
        if(selectIndex < 0 ) return;
        if(inv.UseAt(selectIndex))
        {
            GameDataMgr.Instance.SaveInventoryData();
            showDescription(selectIndex);
        }
    }
    
    /// <summary>
    /// 点击丢弃
    /// </summary>
    private void OnDropClicked()
    {
        if (selectIndex < 0) return;
        ItemStack stack = inv.Data.slots[selectIndex];
        if(inv.Remove(stack.itemId, stack.count))
            GameDataMgr.Instance.SaveInventoryData();
        showDescription(selectIndex);
    }
   
    private void OnDestroy()
    {
        if (inv != null)
        {
            inv.changed -= Refresh;
        }
    }


    /// <summary>
    /// 此方法是 用来显示物品详情的 顺便 控制 使用和丢弃按钮
    /// 选中才亮
    /// </summary>
    /// <param name="index"></param>
    public void showDescription(int index)
    {
        if (index < 0 || index >= slots.Count)
        {
            btnUse.interactable = false;
            btnDrop.interactable = false;
            txtDescription.text = "";
            return;
        }
        ItemStack itemStack = inv.Data.slots[index];
        if (itemStack.IsEmpty){
            btnUse.interactable = false;
            btnDrop.interactable = false;
            txtDescription.text = "";
            return; }
        ItemDefinition def = ItemDataBase.Get(itemStack.itemId);
        if (def == null) {
            btnUse.interactable = false;
            btnDrop.interactable = false;
            txtDescription.text = "";
            return; }
        txtDescription.text = "这是一个" + def.displayName + def.itenDiscription;
        btnUse.interactable = def.type == ItemType.Consumable;
        btnDrop.interactable = true;
    }


    private void SetFilter(ItemType? type)
    {
        filterType = type;
        showDescription(-1);
        selectIndex = -1;
        Refresh();
    }
}
