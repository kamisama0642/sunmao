using UnityEngine;
using TMPro;
using System.Collections;
public class HintManager : MonoBehaviour
{
    [Header("提示UI配置")]
    public TMP_Text hintText; // 提示文字TMP组件
    public float showDuration = 5f; // 提示显示时长
    private Coroutine hideCoroutine;

    // 单例模式
    public static HintManager instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        // 初始隐藏提示
        hintText?.gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示提示文字
    /// </summary>
    /// <param name="tip">要显示的文字</param>
    public void ShowHint(string tip)
    {
        if (hintText == null) return;
        hintText.gameObject.SetActive(true);
        hintText.text = tip;

        // 自动隐藏提示
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideHintAfterTime(showDuration));
    }

    /// <summary>
    /// 延迟隐藏提示
    /// </summary>
    private IEnumerator HideHintAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideHint();
    }

    /// <summary>
    /// 手动隐藏提示
    /// </summary>
    public void HideHint()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }

    // 移除Tab键关闭提示的Update逻辑
}