using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 全局游戏进度管理器
/// 存储所有零件组装完成状态，切换场景数据不重置
/// 访问入口为静态属性 Instance：场景中已放置则复用，缺失时自动创建，任何场景单独运行均可安全访问
/// </summary>
public class GameGlobalData : MonoBehaviour
{
    private static GameGlobalData _instance;

    /// <summary>全局进度单例访问入口（懒加载自举）</summary>
    public static GameGlobalData Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameGlobalData>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameGlobalData");
                    _instance = go.AddComponent<GameGlobalData>();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 零件组装状态字典
    /// Key：零件唯一标识partKey；Value：是否完成组装
    /// </summary>
    public Dictionary<string, bool> finishedPartDict = new Dictionary<string, bool>();

    /// <summary>
    /// 单例去重，跨场景不销毁
    /// </summary>
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 将指定零件标记为已组装完成
    /// </summary>
    /// <param name="partKey">零件唯一标识字符串</param>
    public void SetPartFinished(string partKey)
    {
        // 拦截空标识，防止字典异常
        if (string.IsNullOrEmpty(partKey))
        {
            Debug.LogError("SetPartFinished：partKey不能为空，请在检视面板填写零件唯一标识");
            return;
        }

        if (finishedPartDict.ContainsKey(partKey))
            finishedPartDict[partKey] = true;
        else
            finishedPartDict.Add(partKey, true);
    }

    /// <summary>
    /// 查询零件是否已经组装完成
    /// </summary>
    /// <param name="partKey">零件唯一标识字符串</param>
    /// <returns>true=已组装完成；false=未组装</returns>
    public bool IsPartFinished(string partKey)
    {
        // 拦截空标识，避免字典空键崩溃
        if (string.IsNullOrEmpty(partKey))
        {
            Debug.LogWarning("IsPartFinished：传入partKey为空，请检查零件物体赋值");
            return false;
        }

        bool state;
        finishedPartDict.TryGetValue(partKey, out state);
        return state;
    }

    /// <summary>
    /// 清空全部零件进度，用于游戏重置功能
    /// </summary>
    public void ClearAllProgress()
    {
        finishedPartDict.Clear();
    }
}
