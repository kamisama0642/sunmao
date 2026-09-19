using UnityEngine;
using TMPro;
using System.Collections;
public class HintManager : MonoBehaviour
{
    [Header("提示UI配置")]
    public TMP_Text hintText; // 提示文字TMP组件
    public float showDuration = 5f; // 提示显示时长
    private Coroutine hideCoroutine;

    // 单例模式（UI引用在场景中绑定，不做自动创建；调用方需判空）
    public static HintManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

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
}
