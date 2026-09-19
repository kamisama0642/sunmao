using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 点击传送门切换场景
/// 玩家判断基于 PlayerManager.OnlyPlayer 引用（含子物体），不再使用物体名字符串比对
/// </summary>
public class ClickPortalEnter : MonoBehaviour
{
    [Header("目标场景名称")]
    public string targetSceneName = "workroom";
    [Header("本门专属出生坐标")]
    public Vector2 playerSpawnPos = new Vector2(2.9f, -1.5f);

    private Collider2D portalCol;
    private bool isLoadingScene = false;

    void Start()
    {
        portalCol = GetComponent<Collider2D>();
        if (portalCol == null)
        {
            Debug.LogError($"传送门 {gameObject.name} 缺少Collider2D触发器！");
            enabled = false;
        }
    }

    void OnMouseDown()
    {
        if (portalCol == null || isLoadingScene) return;

        // 玩家由 PlayerManager 统一管理；无玩家时（如单独运行本场景）直接不响应
        PlayerManager pm = PlayerManager.Instance;
        GameObject player = pm != null ? PlayerManager.OnlyPlayer : null;
        if (player == null) return;

        // 判断玩家（或其子物体上的碰撞体）是否站在门内
        Collider2D[] hits = new Collider2D[20];
        int hitCount = Physics2D.OverlapCollider(portalCol, new ContactFilter2D(), hits);
        bool playerInside = false;
        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i] != null && hits[i].transform.IsChildOf(player.transform))
            {
                playerInside = true;
                break;
            }
        }
        if (!playerInside) return;

        isLoadingScene = true;
        // 先启动加载：目标场景不在Build Settings中时loadOp为null，恢复状态避免卡死
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        if (loadOp == null)
        {
            Debug.LogError($"传送门 {gameObject.name}：目标场景 {targetSceneName} 不在Build Settings中！");
            isLoadingScene = false;
            return;
        }
        // 传送前把玩家移出屏幕，消除残影
        player.transform.position = new Vector2(-9999, -9999);

        loadOp.completed += (op) =>
        {
            // 加载完成销毁所有分身（目标场景若内置同 prefab 实例，则保留持久化的原玩家）
            GameObject[] allPlayers = Object.FindObjectsOfType<GameObject>(true);
            foreach (GameObject obj in allPlayers)
            {
                if (obj.name == player.name && obj != player)
                {
                    Destroy(obj);
                }
            }
            player.transform.position = playerSpawnPos;
            isLoadingScene = false;
        };
    }
}
