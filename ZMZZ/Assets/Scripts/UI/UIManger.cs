using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManger
{
   private static UIManger instance = new UIManger();
    public static UIManger Instance => instance;


    //用于存储显示着的面板 每显示一个面板 就会存入字典中
    //隐藏面板时 直接获取字典中的对应面板 进行隐藏
    private Dictionary<string,BasePanel> panelDic = new Dictionary<string,BasePanel>();

    //用于设置为面板的父对象 方便显示与隐藏
    private Transform canvasTrans;

    public bool HasPanel => panelDic.Count > 0;

    private UIManger()
    {
        GameObject canvas = GameObject.Instantiate(Resources.Load<GameObject>("UI/Canvas"));
        canvasTrans = canvas.transform;
        GameObject.DontDestroyOnLoad(canvas);
    }

    //显示面板
    public T ShowPanel<T>() where T : BasePanel
    {
        //规则是 该泛型T的类型和面板预设体名字一致
        string panelName = typeof(T).Name;

        //判断字典中有没有
        if(panelDic.ContainsKey(panelName))
        {
            BasePanel cached = panelDic[panelName];
            if (cached != null)
            { 
                cached.ShowMe();
                return cached as T;
            }

            panelDic.Remove(panelName);
        }
        //没有就创建 然后存在字典中
        GameObject panelObj = GameObject.Instantiate(Resources.Load<GameObject>("UI/" + panelName));
        panelObj.transform.SetParent(canvasTrans, false);

        T panel = panelObj.GetComponent<T>();
        panelDic.Add(panelName, panel);
        //调用其显示逻辑
        panel.ShowMe();

        return panel;
    }

    //隐藏面板
    public void HidePanel<T>(bool isFade = true) where T : BasePanel
    {
        string panelName = typeof(T).Name;
        if (panelDic.ContainsKey(panelName))
        {
            //先淡出 再删除
            if (isFade)
            {
                panelDic[panelName].HideMe(() =>
                {
                    GameObject.Destroy(panelDic[panelName].gameObject);
                    panelDic.Remove(panelName);
                });
            }
            else
            {
                GameObject.Destroy(panelDic[panelName].gameObject);
                panelDic.Remove(panelName);
            }
        }
    }

    //得到面板
    public T GetPanel<T>()where T:BasePanel
    {
        string panelName = typeof(T).Name;
        if (panelDic.ContainsKey(panelName))
            return panelDic[panelName] as T;
        return null;
    }
}
