using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AboutGamePanel : BasePanel
{
    public Button btnBack;
    public override void Init()
    {
        btnBack.onClick.AddListener(() =>
        {
            UIManger.Instance.HidePanel<AboutGamePanel>();
            UIManger.Instance.ShowPanel<BeginPanel>();
            
        });
    }
}
