using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapPanel : BasePanel
{
    Vector3 pos;

    public Button btnBack;

    public Button btnAnchor01;

    public Button btnAnchor02;

    public override void Init()
    {


        btnBack.onClick.AddListener(() =>
        {
            UIManger.Instance.HidePanel<MapPanel>();
        });


        btnAnchor01.onClick.AddListener(() =>
        {
            Teleport(btnAnchor01.name);
            UIManger.Instance.HidePanel<MapPanel>();

        });

        btnAnchor02.onClick.AddListener(() =>
        {
            Teleport(btnAnchor02.name);
            UIManger.Instance.HidePanel<MapPanel>();
        });
    }

    
     public void Teleport(string name)
    {
        foreach (AnchorData data in GameDataMgr.Instance.worldData.anchorDatas)
        {
            if ("btn" + data.id == name)
            {
                pos = new Vector3(data.posX, data.posY, 0);
                PlayerObject player = FindObjectOfType<PlayerObject>();
                player.transform.position = pos;

            }
        }
    }
  
}
