using UnityEngine;

/// <summary>
/// 头顶感叹号提示组件
/// 未交互动态生成感叹号预制体；交互完成永久移除提示
/// 支持每个物体独立调节感叹号尺寸，不受父物体缩放影响
/// </summary>
public class InteractExclamationTip : MonoBehaviour
{
    [Header("物品全局唯一ID，项目内不能重复")]
    public string itemUniqueId;
    [Header("感叹号预制体（资源内预制体，无需提前放入场景）")]
    public GameObject exclamationPrefab;
    [Header("感叹号相对于物体向上偏移距离")]
    public float tipOffsetY = 0.6f;
    [Header("【单个物体独立调节】感叹号缩放大小")]
    public float tipScale = 1f;
    [Header("感叹号上下浮动动画速度")]
    public float floatSpeed = 2f;
    [Header("感叹号上下浮动幅度")]
    public float floatRange = 0.25f;

    /// <summary>
    /// 运行时实例化生成的感叹号物体
    /// </summary>
    private GameObject _runtimeTip;
    /// <summary>
    /// 感叹号基础本地坐标
    /// </summary>
    private Vector3 _baseLocalPos;

    private void Start()
    {
        // Instance 为懒加载自举，任何场景单独运行都可安全获取
        RefreshTipDisplay(GlobalInteractRecord.Instance);
    }

    private void Update()
    {
        // 浮动动画
        if (_runtimeTip != null && _runtimeTip.activeSelf)
        {
            float verticalShift = Mathf.Sin(Time.time * floatSpeed) * floatRange;
            _runtimeTip.transform.localPosition = _baseLocalPos + Vector3.up * verticalShift;
        }
    }

    /// <summary>
    /// 根据交互记录刷新感叹号生成/销毁状态
    /// </summary>
    void RefreshTipDisplay(GlobalInteractRecord record)
    {
        bool isInteracted = record.IsInteracted(itemUniqueId);

        if (!isInteracted)
        {
            if (_runtimeTip == null && exclamationPrefab != null)
            {
                _runtimeTip = Instantiate(exclamationPrefab);
                // false：不继承父物体缩放旋转
                _runtimeTip.transform.SetParent(transform, false);
                // Z轴向前偏移，防止被物体遮挡
                _baseLocalPos = new Vector3(0, tipOffsetY, -0.2f);
                _runtimeTip.transform.localPosition = _baseLocalPos;
                // 应用当前物体独立缩放设置
                _runtimeTip.transform.localScale = Vector3.one * tipScale;
                // 强制提高渲染层级，避免被瓦片/家具遮挡
                SpriteRenderer tipRenderer = _runtimeTip.GetComponent<SpriteRenderer>();
                if (tipRenderer != null)
                    tipRenderer.sortingOrder = 10;
            }
        }
        else
        {
            if (_runtimeTip != null)
            {
                Destroy(_runtimeTip);
                _runtimeTip = null;
            }
        }
    }

    /// <summary>
    /// 外部调用接口：物品交互完成，永久标记并移除感叹号
    /// 在物品交互成功逻辑末尾调用
    /// </summary>
    public void CompleteInteract()
    {
        GlobalInteractRecord.Instance.MarkInteracted(itemUniqueId);
        if (_runtimeTip != null)
        {
            Destroy(_runtimeTip);
            _runtimeTip = null;
        }
    }
}
