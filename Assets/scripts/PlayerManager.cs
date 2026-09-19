using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    // 补上缺失的静态单例实例
    public static PlayerManager Instance;
    public static GameObject OnlyPlayer;
    public string playerName = "player";

    void Awake()
    {
        // 单例去重逻辑
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (OnlyPlayer == null)
        {
            OnlyPlayer = GameObject.Find(playerName);
            if (OnlyPlayer != null)
            {
                DontDestroyOnLoad(OnlyPlayer);
            }
        }
    }

    // 清理所有分身
    public void ClearAllDuplicatePlayer()
    {
        GameObject[] allObj = Object.FindObjectsOfType<GameObject>(includeInactive: true);
        foreach (GameObject obj in allObj)
        {
            if (obj.name == playerName && obj != OnlyPlayer)
            {
                Destroy(obj);
            }
        }
    }
}