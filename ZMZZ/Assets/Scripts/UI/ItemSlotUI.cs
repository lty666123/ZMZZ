using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    public Image icon;
    public Text txt;
    public Button btn;

    private int slotIndex;
    private Action<int> onClick;

    public Color normal = new Color(0.24f, 0.24f, 0.27f);
    public Color selected = new Color(0.85f, 0.7f, 0.25f);

    private Image bk;

    private void Awake()
    {
        bk = GetComponent<Image>();
        btn.onClick.AddListener(HandleClick);
    }

    // BagPanel 每次刷新时调用 用于创建背包格子的图标等信息
    public void Setup(int index,Action<int> action,ItemStack stack)
    {
        slotIndex = index; //为格子进行编号
        onClick = action;//为每个格子增加点击事件
        if (stack == null || stack.IsEmpty)
        {
            icon.sprite = null;
            icon.enabled = false;
            txt.text = "";
            return;
        }
        ItemDefinition def = ItemDataBase.Get(stack.itemId);
        icon.sprite = def!=null ? def.icon : null;
        icon.enabled = icon.sprite != null;
        txt.text = stack.count > 1 ? stack.count.ToString() : "";
    }

    public void SetupEquipment(int index,Action<int> action,string itemId)
    {
        slotIndex = index;
        onClick = action;
        if(string.IsNullOrEmpty(itemId))
        {
            icon.sprite=null;
            icon.enabled = false;
            txt.text = "";
            return;
        }
        ItemDefinition def = ItemDataBase.Get(itemId);
        icon.sprite = def!=null ? def.icon : null;
        icon.enabled = icon.sprite != null;
        txt.text = "";
    }

    public void SetSelected(bool isSelected)
    {

        if (bk == null) return;
        bk.color = isSelected ? selected : normal;

        
    }

    private void HandleClick()
    {
        onClick?.Invoke(slotIndex);
    }

}
