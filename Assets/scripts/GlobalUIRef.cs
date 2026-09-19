using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 全局持久UI引用管理器
/// 挂载位置：GlobalCanvasRoot（场景根物体，唯一允许DontDestroyOnLoad）
/// 所有UI面板统一在此拖拽绑定，全项目共用
/// </summary>
public class GlobalUIRef : MonoBehaviour
{
    // 全局静态单例访问入口
    public static GlobalUIRef Instance;

    [Header("弹窗确认UI")]
    public GameObject dialogBox;
    public TMP_Text dialogTipText;

    [Header("组装视频播放面板")]
    public GameObject videoPanel;
    public RawImage videoRawImage;

    [Header("背包主面板")]
    public GameObject bagPanel;

    private void Awake()
    {
        // 单例去重逻辑
        if (Instance == null)
        {
            Instance = this;
            // 根物体允许持久化，子UI禁止调用此方法
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 重复创建直接销毁
            Destroy(gameObject);
            return;
        }

        // 游戏启动强制隐藏所有弹窗/视频/背包面板
        if (dialogBox != null) dialogBox.SetActive(false);
        if (videoPanel != null) videoPanel.SetActive(false);
        if (bagPanel != null) bagPanel.SetActive(false);
    }
}
