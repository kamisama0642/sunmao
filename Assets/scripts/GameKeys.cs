using UnityEngine;

/// <summary>
/// 全局键位定义：所有交互按键统一在此维护，避免各脚本硬编码导致冲突
/// E=世界交互(告示牌)/弹窗取消 / Q=弹窗确认 / Tab=背包 / Esc=关闭视频面板
/// </summary>
public static class GameKeys
{
    /// <summary>世界交互：打开告示牌；弹窗打开时兼作取消键</summary>
    public const KeyCode Interact = KeyCode.E;
    /// <summary>弹窗确认：开始组装/播放视频</summary>
    public const KeyCode DialogConfirm = KeyCode.Q;
    /// <summary>弹窗取消（与交互键同为E，弹窗打开时优先处理弹窗）</summary>
    public const KeyCode DialogCancel = KeyCode.E;
    /// <summary>开关背包</summary>
    public const KeyCode Bag = KeyCode.Tab;
    /// <summary>关闭视频面板等弹层</summary>
    public const KeyCode ClosePanel = KeyCode.Escape;
}
