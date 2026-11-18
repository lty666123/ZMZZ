namespace ZMZZ.UI
{
    /// <summary>
    /// UI 层级。数值越大越靠上，同一层面板按打开顺序叠放。
    /// 加新界面时先想清楚它属于哪一层，比后面互相遮挡再返工省事。
    /// </summary>
    public enum UILayer
    {
        /// <summary>背景装饰、过场</summary>
        Background = 0,
        /// <summary>主菜单、HUD 这类常驻界面</summary>
        Normal = 1,
        /// <summary>设置、暂停、背包等弹窗</summary>
        Popup = 2,
        /// <summary>加载遮罩、飘字提示</summary>
        Toast = 3,
    }
}
