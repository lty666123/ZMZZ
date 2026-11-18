using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class GameDataMgr
{
    private static GameDataMgr instance = new GameDataMgr();
    public static GameDataMgr Instance => instance;

    public SettingData settingData;

    public PlayerData playerData;

    public List<EnemyData> enemyDatas;

    public InventoryData inventoryData;
    public Inventory inventory {  get; private set; }

    public EquipmentBagData equipmentBagData;
    public EquipmentBag equipmentBag { get; private set; }

    public WorldData worldData;

    public event Action GameSaveChanged;

    private GameDataMgr()
    {
        //设置相关的数据的初始化
        settingData = JsonMgr.Instance.LoadData<SettingData>("SettingData");
        //玩家相关数据初始化
        playerData = JsonMgr.Instance.LoadData<PlayerData>("PlayerData");

        enemyDatas = JsonMgr.Instance.LoadData<List<EnemyData>>("EnemyData");

        inventoryData = JsonMgr.Instance.LoadData<InventoryData>("Inventory");
        inventory = new Inventory(inventoryData);

        worldData = JsonMgr.Instance.LoadData<WorldData>("WorldData");

        equipmentBagData = JsonMgr.Instance.LoadData<EquipmentBagData>("EquipmentBag");
        equipmentBag = new EquipmentBag(equipmentBagData);
        equipmentBag.changed += SaveEquipmentBagData;

    }

    //收到伤害 扣除血量
    public void TackDamage(int damage)
    {
        if(playerData.totalDef >= damage)
        {
            playerData.nowHp = Mathf.Max(0, playerData.nowHp - 1);
        }
        else
        {
            playerData.nowHp = Mathf.Max(0, playerData.nowHp - (damage - playerData.totalDef));
        }
        SavePlayerData();
    }
    //使用药剂 增加血量
    public void RestoreHp(int hp)
    {
        playerData.nowHp = Mathf.Min(playerData.totalHp,playerData.nowHp +  hp);
        SavePlayerData();
    }


    //对设置相关数据的存储方法 
    public void SaveSettingData()
    {
        JsonMgr.Instance.SaveData(settingData, "SettingData",JsonType.LitJson);
    }

    //对玩家数据相关的存储方法
    public void SavePlayerData()
    {
        JsonMgr.Instance.SaveData(playerData, "PlayerData");
        GameSaveChanged?.Invoke();
    }

    //对敌人相关数据存储的方法
    public void SaveEnemyData()
    {
        JsonMgr.Instance.SaveData(enemyDatas, "enemyData");
    }
    
    //对背包相关数据进行存储的方法
    public void SaveInventoryData()
    {
        JsonMgr.Instance.SaveData(inventoryData, "Inventory");
        GameSaveChanged?.Invoke();

    }

    //对装备背包数据进行存储的方法
    public void SaveEquipmentBagData()
    {
        JsonMgr.Instance.SaveData(equipmentBagData,"EquipmentBag");
        GameSaveChanged?.Invoke();

    }

    //对世界数据进行存储的方法
    public void SaveWorldData()
    {
        JsonMgr.Instance.SaveData(worldData, "WorldData");
        GameSaveChanged?.Invoke();

    }

    public GameSaveData GetAllNeedSaveData()
    {
        GameSaveData save = new GameSaveData
        {
            playerData = this.playerData,
            inventoryData = this.inventoryData,
            equipmentBagData = this.equipmentBagData,
            worldData = this.worldData
        };
        return save;
    }
    public bool ApplyCloudSave(GameSaveData save)
    {
        if(save == null
            || save.version!=1
            ||save.playerData == null
            ||save.inventoryData == null
            ||save.equipmentBagData == null
            ||save.worldData == null)
        {
            Debug.LogError("存档版本不支持 或缺少必要数据");
            return false;
        }
        //背包 装备里的构造方法会使用这些列表
        if(save.inventoryData.slots == null
            ||save.equipmentBagData.slots == null
            ||save.worldData.anchorDatas == null)
        {
            Debug.LogError("存档中的列表数据不完整");
            return false;
        }

        Inventory newInventory = new Inventory(save.inventoryData);
        EquipmentBag newEquipmentBag = new EquipmentBag(save.equipmentBagData);

        if (equipmentBag != null)
        {
            equipmentBag.changed -= SaveEquipmentBagData;
        }

        playerData = save.playerData;
        inventoryData = save.inventoryData;
        inventory = newInventory;
        equipmentBagData = save.equipmentBagData;
        equipmentBag = newEquipmentBag;
        worldData = save.worldData;

        equipmentBag.changed += SaveEquipmentBagData;

        return true;

    }
}
