using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//锚点数据雷
public class AnchorData
{
    public string id;
    public string displayName;
    public string sceneName;

    public float posX;
    public float posY;

}


public class WorldData
{
    public List<AnchorData> anchorDatas = new List<AnchorData>();
   
    public void EnsureNoNull()
    {
        if(anchorDatas ==  null)
        {
            anchorDatas = new List<AnchorData>();
        }
    }
    //用于判断是否是已经存储过的激活锚点
    public bool IsAnchorActive(string id)
    {
        EnsureNoNull();
        return anchorDatas.Exists(a => a.id == id);
    }
    public bool ActivateAnchor(string id, string displayName, string sceneName , float posX,float posY)
    {
        //如果是已经激活并存储的锚点就跳过
        if(IsAnchorActive(id))
        {
            return false;
        }
        anchorDatas.Add(new AnchorData { id = id, displayName = displayName, sceneName = sceneName , posX = posX,posY = posY });
        return true;
    }

}
