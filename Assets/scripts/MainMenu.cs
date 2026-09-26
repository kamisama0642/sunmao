using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 主菜单
/// 主菜单面板：标题 + 开始新游戏 / 继续游戏 / 操作方法 / 退出游戏（自上而下）
/// 操作方法面板：与主菜单同场景，左上角返回键；4 条操作说明，每行旁可配一个动画
/// 未导入的美术（主菜单样式、标题、返回键）在 Inspector 留 Image 字段，导入后拖入
/// 未导入的动画在 Inspector 留 howToClips（VideoClip），留空的行不播放
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("继续游戏 / 开场结束后进入的场景")]
    public string gameSceneName = "playScenes";
    [Header("开始新游戏进入的开场场景")]
    public string newGameSceneName = "opening";

    [Header("面板")]
    public GameObject mainMenuPanel;
    public GameObject howToPanel;

    [Header("未导入的美术（留空，导入 png 后拖入）")]
    public Image menuStyleImage;
    public Image titleImage;
    public Image backButtonImage;

    [Header("按钮")]
    public Button newGameButton;
    public Button continueButton;
    public Button howToButton;
    public Button quitButton;
    public Button backButton;

    [Header("操作方法行的动画（留空）")]
    [Tooltip("4 个动画显示区（RawImage），与下面 howToClips 一一对应")]
    public RawImage[] howToVideoAreas;
    [Tooltip("4 个操作方法动画（VideoClip），留空则不播放")]
    public VideoClip[] howToClips;

    private readonly List<VideoPlayer> _howToPlayers = new List<VideoPlayer>();
    private readonly List<RenderTexture> _howToTextures = new List<RenderTexture>();

    void Start()
    {
        // 清掉从游戏场景带回主菜单的常驻对象（游戏 UI、玩家等）
        CleanupPersistentObjects();

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (howToPanel != null) howToPanel.SetActive(false);

        if (newGameButton != null) newGameButton.onClick.AddListener(StartNewGame);
        if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
        if (howToButton != null) howToButton.onClick.AddListener(ShowHowTo);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (backButton != null) backButton.onClick.AddListener(ShowMainMenu);

        // 没有存档时"继续游戏"置灰不可点
        if (continueButton != null)
            continueButton.interactable = HasSave();
    }

    /// <summary>
    /// 开始新游戏：清空存档后进入游戏主场景
    /// </summary>
    public void StartNewGame()
    {
        SaveSystem.DeleteSave();
        LoadScene(newGameSceneName);
    }

    /// <summary>
    /// 继续游戏：直接进入主场景，进度由存档在场景初始化时恢复
    /// </summary>
    public void ContinueGame()
    {
        LoadGameScene();
    }

    void LoadGameScene()
    {
        LoadScene(gameSceneName);
    }

    void LoadScene(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (op == null)
            Debug.LogError($"MainMenu：场景 {sceneName} 不在Build Settings中！");
    }

    public void ShowHowTo()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (howToPanel != null) howToPanel.SetActive(true);
        PlayHowToClips();
    }

    public void ShowMainMenu()
    {
        StopHowToClips();
        if (howToPanel != null) howToPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 清掉从游戏场景带回的常驻对象（GlobalCanvasRoot、玩家等 DontDestroyOnLoad 内容），
    /// 避免游戏 UI 与玩家残留在主菜单上，也避免下次进入游戏时出现两个玩家
    /// </summary>
    void CleanupPersistentObjects()
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in all)
        {
            if (go == null) continue;
            if (go.transform.parent != null) continue;                // 只处理根对象
            if (!go.scene.IsValid()) continue;                        // 跳过工程资产
            if (go.scene.name != "DontDestroyOnLoad") continue;       // 只处理跨场景常驻对象
            Destroy(go);
        }
    }

    bool HasSave()
    {
        return SaveSystem.Load() != null;
    }

    /// <summary>
    /// 为配了 VideoClip 的操作方法行创建播放器并循环播放（留空的行跳过）
    /// </summary>
    void PlayHowToClips()
    {
        StopHowToClips();
        if (howToClips == null || howToVideoAreas == null) return;

        for (int i = 0; i < howToClips.Length; i++)
        {
            if (howToClips[i] == null) continue;
            if (i >= howToVideoAreas.Length) break;
            RawImage area = howToVideoAreas[i];
            if (area == null) continue;

            VideoPlayer vp = area.gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = true;
            vp.renderMode = VideoRenderMode.RenderTexture;

            RenderTexture rt = new RenderTexture((int)howToClips[i].width, (int)howToClips[i].height, 0);
            rt.Create();
            vp.targetTexture = rt;
            area.texture = rt;
            vp.clip = howToClips[i];
            vp.Play();

            _howToPlayers.Add(vp);
            _howToTextures.Add(rt);
        }
    }

    void StopHowToClips()
    {
        foreach (VideoPlayer vp in _howToPlayers)
        {
            if (vp == null) continue;
            vp.Stop();
            Destroy(vp);
        }
        _howToPlayers.Clear();

        foreach (RenderTexture rt in _howToTextures)
        {
            if (rt == null) continue;
            rt.Release();
            Destroy(rt);
        }
        _howToTextures.Clear();
    }

    void OnDestroy()
    {
        StopHowToClips();
    }
}
