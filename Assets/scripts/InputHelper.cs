using UnityEngine;

/// <summary>
/// 输入工具类：统一鼠标世界坐标换算，缓存主相机避免重复查找
/// </summary>
public static class InputHelper
{
    private static Camera _mainCamera;

    /// <summary>鼠标所在位置的世界坐标（2D平面）</summary>
    public static Vector2 MouseWorldPos
    {
        get
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
            if (_mainCamera == null)
                return Vector2.zero;
            return _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }
    }
}
