using UnityEditor;
using UnityEngine;

/// <summary>
/// 室外场景的编辑视图辅助（只改 Scene 视图的取景，不动场景里任何数据，放心按）
/// 菜单：Tools/室外场景/…
/// </summary>
public static class OutdoorSceneViewTools
{
    [MenuItem("Tools/室外场景/Scene 视图：框住整个院子")]
    static void FrameYard()
    {
        if (!Apply(new Vector3(0f, 0f, 0f), 15f)) return;
        Debug.Log("Scene 视图已框到整个院子（地图 42.59×21.29，中心在原点）");
    }

    [MenuItem("Tools/室外场景/Scene 视图：贴到主屋门口")]
    static void FrameDoor()
    {
        if (!Apply(new Vector3(0.15f, 0.825f, 0f), 6f)) return;
        Debug.Log("Scene 视图已贴到主屋房门（传送口就在这儿，尺寸 1.8×3.15）");
    }

    [MenuItem("Tools/室外场景/Scene 视图：看院子南侧（出生点）")]
    static void FrameSpawn()
    {
        if (!Apply(new Vector3(0.15f, -3.4f, 0f), 8f)) return;
        Debug.Log("Scene 视图已框到进院子的落点 (0.15, -3.4)");
    }

    /// <summary>把 Scene 视图切到 2D 正交并取景；不动任何场景对象</summary>
    static bool Apply(Vector3 pivot, float halfHeight)
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null)
        {
            Debug.LogWarning("没有打开的 Scene 视图：先点一下 Scene 标签页再执行菜单");
            return false;
        }
        sv.in2DMode = true;
        sv.orthographic = true;
        sv.pivot = pivot;
        sv.size = halfHeight;
        sv.Repaint();
        return true;
    }
}
