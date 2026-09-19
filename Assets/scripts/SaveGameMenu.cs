using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 存档菜单（雏形）：挂在场景空物体上
/// Esc 打开/关闭菜单窗口，显示当前进度，提供"重新开始"（清空进度+删除存档+回到起始场景）
/// 界面暂用IMGUI实现，窗口尺寸与字号随分辨率缩放；后续可替换为uGUI正式面板
/// </summary>
public class SaveGameMenu : MonoBehaviour
{
    [Header("重新开始后回到的场景")]
    public string firstSceneName = "playScenes";

    private bool _menuOpen = false;
    private Rect _windowRect;
    // 缓存样式，避免OnGUI每帧新建
    private GUIStyle _windowStyle;
    private GUIStyle _infoLabel;
    private GUIStyle _tipLabel;
    private GUIStyle _bigButton;
    private int _builtAtScale = -1;

    private void Update()
    {
        if (!Input.GetKeyDown(GameKeys.ClosePanel))
            return;

        // 视频面板/背包打开时，Esc优先归它们处理，不弹菜单
        GlobalUIRef ui = GlobalUIRef.Instance;
        bool otherPanelOpen = ui != null
            && ((ui.videoPanel != null && ui.videoPanel.activeSelf)
                || (ui.bagPanel != null && ui.bagPanel.activeSelf));

        if (!_menuOpen && !otherPanelOpen)
            OpenMenu();
        else if (_menuOpen)
            CloseMenu();
    }

    /// <summary>以1080p为基准的界面缩放倍数，高分屏自动放大</summary>
    private float UiScale
    {
        get { return Mathf.Clamp(Screen.height / 1080f, 1f, 3f); }
    }

    private void OpenMenu()
    {
        _menuOpen = true;
        float scale = UiScale;
        float width = 480f * scale;
        float height = 340f * scale;
        _windowRect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
    }

    private void CloseMenu()
    {
        _menuOpen = false;
    }

    private void OnGUI()
    {
        if (!_menuOpen)
            return;
        EnsureStyles();
        _windowRect = GUILayout.Window(0, _windowRect, DrawWindow, "存档", _windowStyle);
    }

    /// <summary>
    /// 按当前缩放倍数构建样式；分辨率变化时自动重建
    /// </summary>
    private void EnsureStyles()
    {
        int scale = Mathf.RoundToInt(UiScale);
        if (_windowStyle != null && scale == _builtAtScale)
            return;
        _builtAtScale = scale;

        _windowStyle = new GUIStyle(GUI.skin.window)
        {
            fontSize = Mathf.RoundToInt(18 * scale)
        };
        _infoLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(20 * scale)
        };
        _tipLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(14 * scale)
        };
        _bigButton = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(18 * scale)
        };
    }

    private void DrawWindow(int id)
    {
        float scale = UiScale;
        GameGlobalData data = GameGlobalData.Instance;
        GlobalInteractRecord record = GlobalInteractRecord.Instance;

        GUILayout.Space(10 * scale);
        GUILayout.Label($"已组装零件：{data.finishedPartDict.Count}", _infoLabel);
        GUILayout.Label($"已交互物品：{record.interactedIdList.Count}", _infoLabel);
        GUILayout.Space(4 * scale);
        GUILayout.Label("进度在每次关键操作后自动保存", _tipLabel);
        GUILayout.Space(14 * scale);

        if (GUILayout.Button("重新开始（清空进度并回到游戏开头）", _bigButton, GUILayout.Height(48 * scale)))
        {
            RestartGame();
            return;
        }
        GUILayout.Space(8 * scale);
        if (GUILayout.Button("继续游戏", _bigButton, GUILayout.Height(40 * scale)))
            CloseMenu();

        // 标题栏可拖动
        GUI.DragWindow(new Rect(0, 0, 10000f, 30f * scale));
    }

    /// <summary>
    /// 重新开始：清空全部进度、删除存档、清空背包，并回到起始场景
    /// </summary>
    public void RestartGame()
    {
        GameGlobalData.Instance.ClearAllProgress();
        if (BagShowVideoManager.Instance != null)
            BagShowVideoManager.Instance.ClearBag();
        CloseMenu();

        // 持久化玩家在场景重载后需要重新安放：先记录引用，加载完成后处理
        GameObject player = PlayerManager.Instance != null ? PlayerManager.OnlyPlayer : null;

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(firstSceneName, LoadSceneMode.Single);
        if (loadOp == null)
        {
            Debug.LogError($"SaveGameMenu：起始场景 {firstSceneName} 不在Build Settings中！");
            return;
        }
        loadOp.completed += (op) =>
        {
            if (player == null)
                return;
            // 找到重载场景自带的玩家分身，借用其出生点后销毁，避免出现两个玩家
            GameObject fresh = null;
            foreach (GameObject obj in Object.FindObjectsOfType<GameObject>(true))
            {
                if (obj.name == player.name && obj != player)
                {
                    fresh = obj;
                    break;
                }
            }
            if (fresh != null)
            {
                player.transform.position = fresh.transform.position;
                Destroy(fresh);
            }
        };
    }
}
