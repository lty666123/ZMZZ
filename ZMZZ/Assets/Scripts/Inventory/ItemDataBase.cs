using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//该类主要作为查表工具 对item 进行配置
public static class ItemDataBase
{
    static Dictionary<string, ItemDefinition> map;
     
    //加载的方法 主要用于读取 并 缓存
    static void EnsureLoaded()
    {
        if (map != null) return;
        map = new Dictionary<string, ItemDefinition>();
        foreach(var def in Resources.LoadAll<ItemDefinition>("Items"))
        {
            if(string.IsNullOrEmpty(def.id))
            {
                Debug.Log("这个物品没有id 已跳过");
                continue;
            }
            map[def.id] = def;
        }
        Debug.Log("[物品表] 加载了 " + map.Count + " 个物品");
    }

    //根据id 找物品说明表 找到返回 找不到返回null
    public static ItemDefinition Get(string id)
    {
        EnsureLoaded();
        if(string.IsNullOrEmpty (id)) return null;
        ItemDefinition def;
        if (map.TryGetValue(id, out def)) return def;
        Debug.Log("没有找到要找的物品");
        return null;
    }
    
}
