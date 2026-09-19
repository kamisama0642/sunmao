using UnityEngine;
using UnityEngine.SceneManagement;

public class ClickPortalEnter : MonoBehaviour
{
    [Header("玩家物体名称")]
    public string playerObjName = "player";
    [Header("目标场景名称")]
    public string targetSceneName = "workroom";
    [Header("本门专属出生坐标")]
    public Vector2 playerSpawnPos = new Vector2(2.8f, -2f);

    private Collider2D portalCol;
    private bool isLoadingScene = false;
    private static GameObject globalPlayer;

    void Start()
    {
        portalCol = GetComponent<Collider2D>();
        if (portalCol == null)
        {
            Debug.LogError($"传送门 {gameObject.name} 缺少Collider2D触发器！");
            enabled = false;
            return;
        }
        // 仅第一次初始化全局玩家
        if (globalPlayer == null)
        {
            globalPlayer = GameObject.Find(playerObjName);
            if (globalPlayer != null)
                DontDestroyOnLoad(globalPlayer);
        }
    }

    void OnMouseDown()
    {
        if (portalCol == null || isLoadingScene || globalPlayer == null) return;

        // 判断玩家是否站在门内（原版名称匹配逻辑）
        Collider2D[] hits = new Collider2D[20];
        int hitCount = Physics2D.OverlapCollider(portalCol, new ContactFilter2D(), hits);
        bool playerInside = false;
        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].gameObject.name == playerObjName)
            {
                playerInside = true;
                break;
            }
        }
        if (!playerInside) return;

        isLoadingScene = true;
        // 传送前把玩家移出屏幕，消除残影
        globalPlayer.transform.position = new Vector2(-9999, -9999);
        // 直接加载
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);

        loadOp.completed += (op) =>
        {
            // 加载完成销毁所有分身
            GameObject[] allPlayers = Object.FindObjectsOfType<GameObject>(true);
            foreach (GameObject obj in allPlayers)
            {
                if (obj.name == playerObjName && obj != globalPlayer)
                {
                    Destroy(obj);
                }
            }
            globalPlayer.transform.position = playerSpawnPos;
            isLoadingScene = false;
        };
    }
}