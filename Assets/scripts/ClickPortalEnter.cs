using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 传送门场景切换脚本（进入触发器自动传送）
/// 玩家进入触发器范围即自动尝试切换场景
/// 前置条件：requiredItems 需已组装完成（调用 BagChecker 对比存档 finishedParts）
/// 条件不满足时用 GlobalUIRef 弹窗提示缺少的物品，E/Q 关闭
/// 挂载物体需要 Collider2D 并勾选 Is Trigger
/// </summary>
public class ClickPortalEnter : MonoBehaviour
{
    [Header("目标场景名称")]
    public string targetSceneName = "workroom";
    [Header("玩家专用出生点坐标")]
    public Vector2 playerSpawnPos = new Vector2(2.9f, -1.5f);
    [Header("前置条件（需已组装完成的物品）")]
    [Tooltip("缺任意一项时不传送，改为提示缺少的物品；留空表示无条件")]
    public ItemData[] requiredItems;

    private Collider2D portalCol;
    private bool isLoadingScene = false;
    private bool _isShowingMissingTip = false;

    void Start()
    {
        portalCol = GetComponent<Collider2D>();
        if (portalCol == null)
        {
            Debug.LogError($"传送门 {gameObject.name} 缺少Collider2D组件！");
            enabled = false;
        }
        else if (!portalCol.isTrigger)
        {
            Debug.LogWarning($"传送门 {gameObject.name} 的 Collider2D 未勾选 Is Trigger，自动传送不会生效");
        }
    }

    void Update()
    {
        // 缺少物品提示：E 或 Q 关闭
        if (_isShowingMissingTip &&
            (Input.GetKeyDown(GameKeys.DialogCancel) || Input.GetKeyDown(GameKeys.DialogConfirm)))
        {
            HideMissingTip();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoadingScene) return;

        // 只响应玩家进入
        PlayerManager pm = PlayerManager.Instance;
        GameObject player = pm != null ? PlayerManager.OnlyPlayer : null;
        if (player == null) return;
        if (other.transform != player.transform && !other.transform.IsChildOf(player.transform)) return;

        // 前置条件检查：调用背包检测脚本对比存档
        List<ItemData> missing = BagChecker.GetMissingItems(requiredItems);
        if (missing.Count > 0)
        {
            ShowMissingTip(missing);
            return;
        }

        EnterScene(player);
    }

    /// <summary>
    /// 切换场景，并把跨场景保留的玩家放到目标出生点
    /// </summary>
    void EnterScene(GameObject player)
    {
        isLoadingScene = true;

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        if (loadOp == null)
        {
            Debug.LogError($"传送门 {gameObject.name}：目标场景 {targetSceneName} 不在Build Settings中！");
            isLoadingScene = false;
            return;
        }

        // 加载期间把玩家移出屏幕，避免切换瞬间残留画面
        player.transform.position = new Vector2(-9999, -9999);

        loadOp.completed += (op) =>
        {
            // 目标场景自带同名玩家分身时销毁，避免出现两个玩家
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

    /// <summary>
    /// 前置条件不满足时，用全局弹窗显示缺少的物品
    /// </summary>
    void ShowMissingTip(List<ItemData> missing)
    {
        GlobalUIRef ui = GlobalUIRef.Instance;
        if (ui == null || ui.dialogBox == null || ui.dialogTipText == null)
        {
            Debug.LogError($"传送门 {gameObject.name}：全局弹窗UI缺失，无法提示缺少的物品");
            return;
        }

        List<string> names = new List<string>();
        foreach (ItemData item in missing)
        {
            names.Add(item != null ? item.itemTitle : "(未命名物品)");
        }

        _isShowingMissingTip = true;
        ui.dialogBox.SetActive(true);
        Canvas.ForceUpdateCanvases();
        ui.dialogTipText.text = "无法前往，还缺少：" + string.Join("、", names) + "（E关闭）";
    }

    void HideMissingTip()
    {
        _isShowingMissingTip = false;
        GlobalUIRef ui = GlobalUIRef.Instance;
        if (ui != null && ui.dialogBox != null)
        {
            ui.dialogBox.SetActive(false);
        }
    }
}
