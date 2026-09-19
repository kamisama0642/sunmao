using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 全局交互状态单例管理器
/// 跨场景持久保存所有物品交互标记
/// 访问入口为静态属性 Instance：场景中已放置则复用，缺失时自动创建，任何场景单独运行均可安全访问
/// </summary>
public class GlobalInteractRecord : MonoBehaviour
{
    private static GlobalInteractRecord _instance;

    /// <summary>全局单例访问入口（懒加载自举）</summary>
    public static GlobalInteractRecord Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GlobalInteractRecord>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GlobalInteractRecord");
                    _instance = go.AddComponent<GlobalInteractRecord>();
                }
            }
            return _instance;
        }
    }

    [Header("已交互物品ID列表（序列化，用于存档读写）")]
    public List<string> interactedIdList = new List<string>();
    /// <summary>
    /// 运行时快速查询集合
    /// </summary>
    private HashSet<string> _interactedSet = new HashSet<string>();

    private void Awake()
    {
        // 单例去重
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        // 设置跨场景不销毁
        DontDestroyOnLoad(gameObject);

        // 从磁盘读档恢复已交互列表，再构建运行时查询集合
        GameSaveData data = SaveSystem.Load();
        if (data != null)
        {
            interactedIdList.Clear();
            interactedIdList.AddRange(data.interactedIds);
        }
        _interactedSet.Clear();
        foreach (string id in interactedIdList)
        {
            _interactedSet.Add(id);
        }
    }

    /// <summary>
    /// 将物品标记为已交互
    /// </summary>
    /// <param name="uniqueId">物品全局唯一ID</param>
    public void MarkInteracted(string uniqueId)
    {
        if (_interactedSet.Contains(uniqueId))
            return;
        _interactedSet.Add(uniqueId);
        interactedIdList.Add(uniqueId);

        // 状态变更立即落盘
        SaveSystem.Save();
    }

    /// <summary>
    /// 查询物品是否已经交互
    /// </summary>
    /// <param name="uniqueId">物品全局唯一ID</param>
    /// <returns>true=已交互；false=未交互</returns>
    public bool IsInteracted(string uniqueId)
    {
        return _interactedSet.Contains(uniqueId);
    }
}
