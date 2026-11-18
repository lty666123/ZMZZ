using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    public int version = 1;

    public PlayerData playerData;
    public InventoryData inventoryData;
    public EquipmentBagData equipmentBagData;
    public WorldData worldData;
}
