using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootPickUp : MonoBehaviour
{
    public string itemId = "";
    public int count = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.GetComponent<PlayerObject>() == null) return;

        ItemDefinition def = ItemDataBase.Get(itemId);
        if(def == null) return;

        if(def.type == ItemType.Weapon || def.type == ItemType.armor)
        {
            bool getLoot = GameDataMgr.Instance.equipmentBag.Add(itemId);
            if(getLoot)
            {
                Debug.Log("成功拾取" + def.name);
                Destroy(gameObject);

            }
            else
            {
                Debug.Log("拾取失败 装备背包已满");
            }
        }
        else {
            int left = GameDataMgr.Instance.inventory.Add(itemId, count);
            if (left > 0)
            {
                Debug.Log("背包已满");
            }
            else
            {
                Debug.Log("已拾取" + itemId);
                GameDataMgr.Instance.SaveInventoryData();
                Destroy(gameObject);
            }
        }
        

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
