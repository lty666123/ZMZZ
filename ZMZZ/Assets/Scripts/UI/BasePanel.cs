using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//该类为所有面板的基类 主要功能是实现面板的淡入淡出
public abstract class BasePanel : MonoBehaviour
{
   
    [Tooltip("淡出时长，0 表示不做动画")]
    public float alphaSpeed = 10f;

    public bool IsShow = false;

    private UnityAction hideAction = null;

    //用于控制面板透明度的组件
    private CanvasGroup _group;
    protected virtual void Awake()
    {
        //开始获取面板上的组件 没有就添加
        _group = this.GetComponent<CanvasGroup>();
        if( _group == null ) _group = this.gameObject.AddComponent<CanvasGroup>();
    }
    protected virtual void Start()
    {
        Init();
    }

    /// <summary>
    /// 注册控件事件的方法 子面板需要去实现
    /// </summary>
    public abstract void Init();
  
    //显示自己时的逻辑
    public virtual void ShowMe()
    {
        _group.alpha = 0;
        IsShow = true;
    }

    //隐藏自己时的逻辑
    public virtual void HideMe(UnityAction unityAction)
    {
        _group.alpha = 1;
        IsShow = false;
        hideAction = unityAction;
    }

    protected virtual void Update()
    {
        if(IsShow && _group.alpha < 1f)
        {
            _group.alpha += alphaSpeed * Time.unscaledDeltaTime;
            if(_group.alpha>1f)
            {
                _group.alpha = 1;
            }
        }
        if (!IsShow && _group.alpha > 0f)
        {
            _group.alpha -= alphaSpeed * Time.unscaledDeltaTime;
            if (_group.alpha <= 0f)
            {
                _group.alpha = 0;

                //淡出结束 执行管理器交过来的收尾任务
                UnityAction callback = hideAction;
                hideAction = null;
                if (callback != null) callback(); 

            }
        }
    }

}

