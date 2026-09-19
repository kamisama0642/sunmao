using UnityEngine;

/// <summary>
/// 全局键位定义：所有交互按键统一在此维护，避免各脚本硬编码导致冲突
/// E=交互确认 / Q=取消 / Tab=背包 / Esc=关闭弹层
/// </summary>
public static class GameKeys
{
    /// <summary>交互确认：打开告示牌、弹窗确认</summary>
    public const KeyCode Interact = KeyCode.E;
    /// <summary>取消：弹窗取消</summary>
    public const KeyCode Cancel = KeyCode.Q;
    /// <summary>开关背包</summary>
    public const KeyCode Bag = KeyCode.Tab;
    /// <summary>关闭视频面板等弹层</summary>
    public const KeyCode ClosePanel = KeyCode.Escape;
}
