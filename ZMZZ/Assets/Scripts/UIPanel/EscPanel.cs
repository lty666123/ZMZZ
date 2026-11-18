using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EscPanel : BasePanel
{
    public Button btnSetting;
    public Button btndaiding;
    public Button btnBack;
    public Button btnBackOut;

    public override void Init()
    {
        //游戏设置
        btnSetting.onClick.AddListener(() =>
        {

            UIManger.Instance.ShowPanel<SettingPanel>();

        });
        //没确定
        btndaiding.onClick.AddListener(() =>
        {

        });
        //返回游戏
        btnBack.onClick.AddListener(() =>
        {
            UIManger.Instance.HidePanel<EscPanel>();

        });
        //返回游戏主界面
        btnBackOut.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("GamePanelCeShi");
            //UIManger.Instance.ShowPanel<BeginPanel>();
            UIManger.Instance.HidePanel<EscPanel>();
        });
    }

   
}
