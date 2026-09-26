using UnityEngine;

/// <summary>
/// 室外场景的镜头跟随
/// 跟随跨场景保留的玩家（PlayerManager.OnlyPlayer），并把镜头夹在地图边界内，避免拍出地图外
/// 挂载点：室外场景的 Main Camera（地图比一屏大时才需要）
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("地图世界边界（镜头视野不越出这个范围）")]
    public Vector2 mapMin = new Vector2(-21.29f, -10.65f);
    public Vector2 mapMax = new Vector2(21.29f, 10.65f);

    [Header("跟随平滑（0 = 硬跟）")]
    public float smoothTime = 0.12f;

    private Camera cam;
    private Vector3 velocity;
    private bool snapped;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        GameObject player = PlayerManager.OnlyPlayer;
        if (player == null) return;

        Vector3 target = player.transform.position;
        target.z = transform.position.z;

        if (cam != null && cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float minX = mapMin.x + halfW, maxX = mapMax.x - halfW;
            float minY = mapMin.y + halfH, maxY = mapMax.y - halfH;

            // 视口比地图还大时退回居中，不来回抖
            target.x = minX <= maxX ? Mathf.Clamp(target.x, minX, maxX) : (mapMin.x + mapMax.x) * 0.5f;
            target.y = minY <= maxY ? Mathf.Clamp(target.y, minY, maxY) : (mapMin.y + mapMax.y) * 0.5f;
        }

        // 刚进场景（含跨场景传送）直接落位，之后才走平滑
        if (!snapped || smoothTime <= 0f)
        {
            snapped = true;
            velocity = Vector3.zero;
            transform.position = target;
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
    }
}
