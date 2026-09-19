using UnityEngine;

/// <summary>
/// 玩家全局管理器：玩家跨场景持久化的唯一归属
/// 只有本类允许对玩家物体调用 DontDestroyOnLoad，其余脚本一律通过 OnlyPlayer 静态引用访问玩家
/// </summary>
public class PlayerManager : MonoBehaviour
{
    private static PlayerManager _instance;

    /// <summary>
    /// 全局单例访问入口
    /// 不做自动创建：玩家物体只存在于 playScenes，管理器缺失时由调用方降级处理
    /// </summary>
    public static PlayerManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<PlayerManager>();
            return _instance;
        }
    }

    /// <summary>全局唯一玩家物体（由本类负责 DontDestroyOnLoad）</summary>
    public static GameObject OnlyPlayer;

    [Header("玩家物体名称")]
    public string playerName = "player";

    private void Awake()
    {
        // 单例去重
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (OnlyPlayer == null)
        {
            OnlyPlayer = GameObject.Find(playerName);
            if (OnlyPlayer != null)
            {
                DontDestroyOnLoad(OnlyPlayer);
            }
        }
    }
}
