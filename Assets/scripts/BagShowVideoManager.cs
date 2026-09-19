using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 唯一背包管理器
/// 挂载：场景根物体 GlobalCanvasRoot
/// 新增预览父物体统一控制，完善日志定位空白问题
/// </summary>
public class BagShowVideoManager : MonoBehaviour
{
    [Header("背包UI拖拽绑定")]
    [Tooltip("所有背包格子Image数组，按顺序拖拽")]
    public Image[] bagItemSlots;
    [Tooltip("物品预览总父物体 bag/Show")]
    public GameObject itemPreviewPanel;
    [Tooltip("物品标题TMP文本")]
    public TMP_Text itemTitleText;
    [Tooltip("物品描述TMP文本")]
    public TMP_Text itemDescText;
    [Tooltip("物品预览视频RawImage")]
    public RawImage bagVideoRawImage;

    // 视频渲染纹理缓存
    private RenderTexture _renderTexture;
    // 背包内置视频播放器
    private VideoPlayer _localVideoPlayer;
    // 当前已占用格子数量
    private int currentItemCount = 0;
    // 已获取物品全局缓存列表（存储完整ItemData）
    private List<ItemData> ownedItemCache = new List<ItemData>();

    // 全局单例
    public static BagShowVideoManager instance;

    private void Awake()
    {
        Debug.Log($"【背包Awake】挂载物体：{gameObject.name}");
        // 单例去重
        if (instance == null)
        {
            instance = this;
            Debug.Log("背包单例初始化成功");
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("重复背包管理器，已销毁");
            return;
        }

        // 初始化背包视频播放器
        _localVideoPlayer = gameObject.AddComponent<VideoPlayer>();
        _localVideoPlayer.playOnAwake = false;
        _localVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _localVideoPlayer.isLooping = true;

        // 初始化格子状态
        InitBagSlots();
    }

    private void Start()
    {
        Debug.Log("背包脚本启用，Tab监听正常运行");
    }

    private void Update()
    {
            // 按T强制读取下标0物品
            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log("=====手动调用预览，下标0====");
                OnClickBagSlot(0);
            }

        // Tab键切换背包显隐
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("===== Tab按键触发 =====");
            // 校验全局UI单例
            if (GlobalUIRef.Instance == null)
            {
                Debug.LogError("Tab失败：GlobalUIRef未初始化");
                return;
            }
            GameObject bagPanel = GlobalUIRef.Instance.bagPanel;
            if (bagPanel == null)
            {
                Debug.LogError("Tab失败：GlobalUIRef未绑定bagPanel");
                return;
            }

            bool newState = !bagPanel.activeSelf;
            bagPanel.SetActive(newState);
            Canvas.ForceUpdateCanvases();
            Debug.Log($"背包切换至：{(newState ? "打开" : "关闭")}");

            // 打开背包刷新格子，关闭清空预览
            if (newState)
                RefreshBagUIFromCache();
            else
                CloseItemPreview();
        }
    }

    /// <summary>
    /// 初始化背包格子：清空贴图、透明可点击按钮
    /// </summary>
    private void InitBagSlots()
    {
        if (bagItemSlots == null) return;
        foreach (Image slot in bagItemSlots)
        {
            if (slot != null)
            {
                slot.enabled = false;
                slot.sprite = null;
                Button btn = slot.GetComponent<Button>();
                if (btn != null)
                {
                    Image btnImg = btn.GetComponent<Image>();
                    if (btnImg != null)
                    {
                        btnImg.color = new Color(1, 1, 1, 0);
                        btnImg.raycastTarget = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 接收外部传递的完整ItemData，存入缓存并渲染格子图标
    /// </summary>
    public void AddItemToBag(ItemData item)
    {
        Debug.Log($"背包接收物品：{(item == null ? "空数据" : item.itemTitle)}");
        if (item == null)
        {
            Debug.LogError("AddItemToBag：传入ItemData为空");
            return;
        }
        if (bagItemSlots == null || bagItemSlots.Length == 0)
        {
            Debug.LogError("AddItemToBag：背包格子数组未拖拽");
            return;
        }
        if (currentItemCount >= bagItemSlots.Length)
        {
            Debug.LogWarning("AddItemToBag：背包格子已满");
            return;
        }
        if (ownedItemCache.Exists(x => x == item))
        {
            Debug.LogWarning($"物品【{item.itemTitle}】已存在，跳过");
            return;
        }

        ownedItemCache.Add(item);
        bagItemSlots[currentItemCount].sprite = item.itemSprite;
        bagItemSlots[currentItemCount].enabled = true;
        currentItemCount++;
        Debug.Log($"物品添加完成，当前总数：{currentItemCount}");
    }

    /// <summary>
    /// 打开背包时刷新格子，自动关闭预览面板
    /// </summary>
    public void RefreshBagUIFromCache()
    {
        Debug.Log($"刷新背包，缓存物品总数：{ownedItemCache.Count}");
        // 打开背包先隐藏预览
        if (itemPreviewPanel != null)
            itemPreviewPanel.SetActive(false);

        if (bagItemSlots == null) return;
        // 清空全部格子
        for (int i = 0; i < bagItemSlots.Length; i++)
        {
            bagItemSlots[i].sprite = null;
            bagItemSlots[i].enabled = false;
        }
        currentItemCount = 0;
        // 填充物品图标
        foreach (ItemData item in ownedItemCache)
        {
            if (currentItemCount >= bagItemSlots.Length) break;
            bagItemSlots[currentItemCount].sprite = item.itemSprite;
            bagItemSlots[currentItemCount].enabled = true;
            Debug.Log($"格子{currentItemCount}载入物品：{item.itemTitle}");
            currentItemCount++;
        }
        Debug.Log("背包刷新完毕");
    }

    /// <summary>
    /// 点击格子，加载物品标题/描述/预览视频
    /// 增加完整日志，快速定位空白原因
    /// </summary>
    public void OnClickBagSlot(int index)
    {
        Debug.Log($"点击格子下标：{index}");
        if (index >= ownedItemCache.Count)
        {
            Debug.LogWarning("该下标无物品数据");
            CloseItemPreview();
            return;
        }

        ItemData target = ownedItemCache[index];
        if (target == null)
        {
            Debug.LogError("缓存内该物品为空");
            CloseItemPreview();
            return;
        }
        Debug.Log($"选中物品资源：{target.name}，标题内容：{target.itemTitle}");

        // 弹出预览总面板
        if (itemPreviewPanel != null)
            itemPreviewPanel.SetActive(true);
        else
            Debug.LogError("itemPreviewPanel（Show）未拖拽！");

        // 赋值标题文本
        if (itemTitleText != null)
        {
            itemTitleText.text = target.itemTitle;
            itemTitleText.gameObject.SetActive(true);
            Debug.Log($"标题赋值完成：{target.itemTitle}");
        }
        else
        {
            Debug.LogError("itemTitleText 标题文本未拖拽赋值！");
        }

        // 赋值描述文本
        if (itemDescText != null)
        {
            itemDescText.text = target.itemDescription;
            itemDescText.gameObject.SetActive(true);
            Debug.Log($"描述赋值完成：{target.itemDescription}");
        }
        else
        {
            Debug.LogError("itemDescText 描述文本未拖拽赋值！");
        }

        // 播放预览视频
        if (target.autoPlay)
        {
            if (target.itemVideo == null)
                Debug.LogWarning("该物品无预览视频Clip");
            PlayBagItemVideo(target.itemVideo);
        }
        else
        {
            Debug.Log("物品autoPlay关闭，不自动播放视频");
        }
    }

    /// <summary>
    /// 播放物品预览视频
    /// </summary>
    private void PlayBagItemVideo(VideoClip clip)
    {
        if (bagVideoRawImage == null)
        {
            Debug.LogError("bagVideoRawImage 视频框未拖拽！");
            return;
        }
        _localVideoPlayer.Stop();
        if (clip == null)
        {
            bagVideoRawImage.gameObject.SetActive(false);
            bagVideoRawImage.texture = null;
            return;
        }
        if (_renderTexture == null || _renderTexture.width != clip.width || _renderTexture.height != clip.height)
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
            _renderTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
            _renderTexture.Create();
        }
        bagVideoRawImage.gameObject.SetActive(true);
        bagVideoRawImage.texture = _renderTexture;
        _localVideoPlayer.targetTexture = _renderTexture;
        _localVideoPlayer.clip = clip;
        _localVideoPlayer.Play();
    }

    /// <summary>
    /// 关闭预览：隐藏Show、停止视频、清空纹理
    /// </summary>
    public void CloseItemPreview()
    {
        _localVideoPlayer.Stop();
        if (bagVideoRawImage != null)
        {
            bagVideoRawImage.texture = null;
            bagVideoRawImage.gameObject.SetActive(false);
        }
        if (itemPreviewPanel != null)
            itemPreviewPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
        if (_localVideoPlayer != null)
        {
            Destroy(_localVideoPlayer);
        }
    }
}
