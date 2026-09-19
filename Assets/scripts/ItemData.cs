using UnityEngine;
using UnityEngine.Video;

// 右键Project面板可创建：Create → 背包 → 物品数据
[CreateAssetMenu(fileName = "NewItem", menuName = "背包/物品数据")]
public class ItemData : ScriptableObject
{
    [Header("基础信息")]
    public string itemTitle;        // 物品标题
    [TextArea]
    public string itemDescription;  // 物品描述
    public Sprite itemSprite;       // 物品图标

    [Header("视频配置")]
    public VideoClip itemVideo;     // 物品对应的MP4视频
    public bool autoPlay = true;    // 点击格子后是否自动播放视频
}