using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 可交互组装零件点击脚本
/// 挂载：每个场景可点击零件物体
/// 功能：点击弹窗、播放组装视频、组装完成存档并添加物品进背包
/// 依赖：GlobalUIRef、GameGlobalData、BagShowVideoManager、HintManager
/// </summary>
public class ClickToPlayAnimation : MonoBehaviour
{
    [Header("物品绑定")]
    [Tooltip("组装完成后获得的物品ScriptableObject")]
    public ItemData itemData;
    [Tooltip("零件唯一标识，用于存档判断是否已组装")]
    public string partKey;
    [Tooltip("组装动画视频资源")]
    public VideoClip videoClip;
    [Tooltip("视频循环播放次数，默认1次")]
    public int playTimes = 1;

    [Header("前置条件（需已组装完成的物品）")]
    [Tooltip("缺任意一项时点击只提示，不进入组装流程；留空表示无前置条件")]
    public ItemData[] requiredItems;

    [Header("零件外观素材")]
    public Sprite originalSprite;
    public Sprite assembledSprite;
    public Vector3 originalScale = Vector3.one;
    public Vector3 assembledPos;
    public Vector3 assembledScale = new Vector3(0.8f, 0.8f, 1);

    [Header("弹窗提示文字")]
    public string firstClickTip = "要把这堆木料加工完成吗？(Q确认/E取消)";
    public string secondClickTip = "再次观看组装动画？(Q确认/E取消)";

    // 全局UI缓存
    private GameObject dialogBox;
    private TMP_Text dialogTipText;
    private GameObject videoPanel;
    private RawImage videoRawImage;

    private SpriteRenderer _spriteRenderer;
    private bool _isAssembled = false;
    private bool _requirementsMet = true;
    private bool _isDialogShowing = false;
    private int _currentPlayCount = 0;
    private RenderTexture _renderTexture;
    private VideoPlayer _assembleVideoPlayer;

    void Start()
    {
        // 获取精灵渲染组件，无则自动创建
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        // 读取存档，初始化零件外观
        bool finish = GameGlobalData.Instance.IsPartFinished(partKey);
        if (finish)
        {
            _spriteRenderer.sprite = assembledSprite;
            transform.position = assembledPos;
            transform.localScale = assembledScale;
            _isAssembled = true;

            // 存档恢复：已组装零件的物品重新放入背包（AddItemToBag内部按引用去重）
            if (itemData != null && BagShowVideoManager.Instance != null)
                BagShowVideoManager.Instance.AddItemToBag(itemData);
        }
        else
        {
            _spriteRenderer.sprite = originalSprite;
            transform.localScale = originalScale;
        }

        // 自动添加2D点击碰撞体
        if (GetComponent<BoxCollider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>().isTrigger = false;

        // 创建内置视频播放器
        _assembleVideoPlayer = gameObject.AddComponent<VideoPlayer>();
        _assembleVideoPlayer.playOnAwake = false;
        _assembleVideoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // 拉取全局UI单例；缺失时禁用自身（弹窗/视频不可用但不崩溃）
        if (GlobalUIRef.Instance == null)
        {
            Debug.LogError($"{gameObject.name}：全局UI单例未初始化！");
            enabled = false;
            return;
        }
        dialogBox = GlobalUIRef.Instance.dialogBox;
        dialogTipText = GlobalUIRef.Instance.dialogTipText;
        videoPanel = GlobalUIRef.Instance.videoPanel;
        videoRawImage = GlobalUIRef.Instance.videoRawImage;

        // videoPanel 在Update中被直接解引用，缺失时提前禁用自身，防止每帧NRE
        if (videoPanel == null)
        {
            Debug.LogError($"{gameObject.name}：GlobalUIRef未绑定videoPanel！");
            enabled = false;
        }
    }

    void Update()
    {
        // 鼠标点击检测，弹窗/视频打开时屏蔽点击
        if (Input.GetMouseButtonDown(0) && !_isDialogShowing && !videoPanel.activeSelf)
        {
            RayCastClick();
        }

        // 弹窗快捷键：Q确认播放 / E取消
        if (_isDialogShowing)
        {
            if (Input.GetKeyDown(GameKeys.DialogConfirm))
            {
                bool canPlay = _requirementsMet;
                CloseDialog();
                if (canPlay)
                    PlayVideoAnim();
            }
            if (Input.GetKeyDown(GameKeys.DialogCancel))
                CloseDialog();
        }

        // 视频面板关闭快捷键
        if (videoPanel.activeSelf && Input.GetKeyDown(GameKeys.ClosePanel))
        {
            CloseVideo();
        }
    }

    /// <summary>
    /// 2D点检测是否点击当前零件
    /// </summary>
    void RayCastClick()
    {
        Collider2D hit = Physics2D.OverlapPoint(InputHelper.MouseWorldPos);
        if (hit != null && hit.gameObject == gameObject)
        {
            // 点击时先检查前置条件；缺少物品只提示，不进入确认流程
            List<ItemData> missing = BagChecker.GetMissingItems(requiredItems);
            if (missing.Count > 0)
            {
                ShowMissingTip(missing);
                return;
            }
            OpenDialog();
        }
    }

    /// <summary>
    /// 打开确认弹窗
    /// </summary>
    /// <summary>
    /// 前置条件不满足时，用全局弹窗显示缺少的物品（Q/E 均可关闭，不会播放动画）
    /// </summary>
    void ShowMissingTip(List<ItemData> missing)
    {
        if (dialogBox == null || dialogTipText == null)
        {
            Debug.LogError($"{gameObject.name}：弹窗UI缺失，请检查GlobalUIRef绑定");
            return;
        }

        _requirementsMet = false;
        _isDialogShowing = true;
        dialogBox.SetActive(true);
        Canvas.ForceUpdateCanvases();

        List<string> names = new List<string>();
        foreach (ItemData item in missing)
            names.Add(item != null ? item.itemTitle : "(未命名物品)");
        dialogTipText.text = "还缺少：" + string.Join("、", names) + "（E关闭）";
    }

    void OpenDialog()
    {
        if (dialogBox == null || dialogTipText == null)
        {
            Debug.LogError($"{gameObject.name}：弹窗UI缺失，请检查GlobalUIRef绑定");
            return;
        }
        _requirementsMet = true;
        _isDialogShowing = true;
        dialogBox.SetActive(true);
        Canvas.ForceUpdateCanvases();
        dialogTipText.text = _isAssembled ? secondClickTip : firstClickTip;
    }

    /// <summary>
    /// 关闭确认弹窗
    /// </summary>
    void CloseDialog()
    {
        if (dialogBox == null) return;
        _isDialogShowing = false;
        dialogBox.SetActive(false);
    }

    /// <summary>
    /// 播放组装视频
    /// </summary>
    void PlayVideoAnim()
    {
        if (videoPanel == null || videoRawImage == null)
        {
            Debug.LogError($"{gameObject.name}：视频面板UI缺失");
            return;
        }
        if (videoClip == null)
        {
            Debug.LogError($"{gameObject.name}：未赋值组装视频");
            return;
        }

        _currentPlayCount = 0;
        videoPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();

        // 自动重建适配尺寸渲染纹理
        if (_renderTexture == null || _renderTexture.width != videoClip.width || _renderTexture.height != videoClip.height)
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
            _renderTexture = new RenderTexture((int)videoClip.width, (int)videoClip.height, 0);
            _renderTexture.Create();
        }

        videoRawImage.texture = _renderTexture;
        _assembleVideoPlayer.targetTexture = _renderTexture;
        _assembleVideoPlayer.clip = videoClip;
        _assembleVideoPlayer.isLooping = false;
        // 防止多次绑定回调
        _assembleVideoPlayer.loopPointReached -= OnVideoEnd;
        _assembleVideoPlayer.loopPointReached += OnVideoEnd;
        _assembleVideoPlayer.Play();
    }

    /// <summary>
    /// 关闭视频并释放资源
    /// </summary>
    void CloseVideo()
    {
        if (_assembleVideoPlayer != null)
        {
            _assembleVideoPlayer.Stop();
            _assembleVideoPlayer.loopPointReached -= OnVideoEnd;
        }
        if (videoRawImage != null)
            videoRawImage.texture = null;
        if (videoPanel != null)
            videoPanel.SetActive(false);
    }

    /// <summary>
    /// 视频播放完毕回调：组装完成、存档、新增物品至背包
    /// </summary>
    void OnVideoEnd(VideoPlayer vp)
    {
        _currentPlayCount++;
        // 未达到播放次数则循环播放
        if (_currentPlayCount < playTimes)
        {
            vp.Play();
            return;
        }
        CloseVideo();

        // 仅首次组装执行新增物品逻辑
        if (!_isAssembled)
        {
            if (!GameGlobalData.Instance.IsPartFinished(partKey))
            {
                GameGlobalData.Instance.SetPartFinished(partKey);

                // 传递完整ItemData给背包管理器
                if (itemData == null)
                {
                    Debug.LogError($"{gameObject.name} 未拖拽赋值 ItemData");
                }
                else if (BagShowVideoManager.Instance == null)
                {
                    Debug.LogError("BagShowVideoManager单例为空，无法存入物品");
                }
                else
                {
                    BagShowVideoManager.Instance.AddItemToBag(itemData);
                    if (HintManager.Instance != null)
                    {
                        HintManager.Instance.ShowHint("已解锁物品，按Tab打开背包查看");
                    }
                }
            }

            // 更新零件外观为组装完成样式
            _isAssembled = true;
            _spriteRenderer.sprite = assembledSprite;
            transform.position = assembledPos;
            transform.localScale = assembledScale;
        }
        InteractExclamationTip tipComp = GetComponent<InteractExclamationTip>();
        if (tipComp != null)
        {
            tipComp.CompleteInteract();
        }
    }

    void OnDestroy()
    {
        // 释放渲染纹理防止内存泄漏
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
        if (_assembleVideoPlayer != null)
        {
            Destroy(_assembleVideoPlayer);
        }
    }
}
