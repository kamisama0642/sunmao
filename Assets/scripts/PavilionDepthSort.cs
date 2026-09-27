using UnityEngine;

/// <summary>
/// 亭子前景的深度排序（挂在 PavilionFront 上）
/// 每帧比较玩家脚点与亭子脚点：玩家在亭子前面（脚点 y 更小）→ 前景排到玩家之后，玩家盖住亭子；
/// 玩家在亭子后面 → 前景排到玩家之前，亭子盖住玩家。
/// 用它就不必改项目级的 Transparency Sort Mode（那个要重启 Unity 才生效）
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PavilionDepthSort : MonoBehaviour
{
    [Header("亭子脚点的世界 y（图的底边位置）")]
    public float pivotY = -6.5882f;

    [Header("玩家在亭子前面时，前景的 sortingOrder（应低于玩家的 order=1）")]
    public int orderWhenPlayerFront = 0;

    [Header("玩家在亭子后面时，前景的 sortingOrder（应高于玩家的 order=1）")]
    public int orderWhenPlayerBehind = 2;

    private SpriteRenderer sr;
    private int lastOrder = int.MinValue;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        GameObject player = PlayerManager.OnlyPlayer;
        if (player == null || sr == null) return;

        int order = player.transform.position.y < pivotY ? orderWhenPlayerFront : orderWhenPlayerBehind;
        if (order != lastOrder)          // 只在需要时写，避免每帧无谓赋值
        {
            sr.sortingOrder = order;
            lastOrder = order;
        }
    }
}
