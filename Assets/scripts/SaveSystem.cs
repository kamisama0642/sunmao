using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 存档系统：将全局进度序列化为JSON写入磁盘
/// 存档路径：Application.persistentDataPath/save.json
/// 所有IO异常只记录日志，不允许中断游戏流程
/// </summary>
public static class SaveSystem
{
    /// <summary>存档文件完整路径</summary>
    private static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, "save.json"); }
    }

    /// <summary>
    /// 从两个全局管理器收集数据并写入磁盘
    /// </summary>
    public static void Save()
    {
        try
        {
            GameSaveData data = new GameSaveData();
            data.finishedParts.AddRange(GameGlobalData.Instance.finishedPartDict.Keys);
            data.interactedIds.AddRange(GlobalInteractRecord.Instance.interactedIdList);
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem.Save 写入存档失败：{e.Message}");
        }
    }

    /// <summary>
    /// 读取存档；文件不存在或读取失败返回null
    /// </summary>
    public static GameSaveData Load()
    {
        try
        {
            if (!File.Exists(SavePath))
                return null;
            return JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem.Load 读取存档失败：{e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 删除存档文件，用于游戏重置；文件不存在时静默忽略
    /// </summary>
    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem.DeleteSave 删除存档失败：{e.Message}");
        }
    }
}
