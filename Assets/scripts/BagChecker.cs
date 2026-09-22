using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包 / 存档物品检测
/// 数据来源：存档中的 finishedParts（即 GameGlobalData.finishedPartDict 的键，值为 ItemData.partKey）
/// 背包内容本身不单独持久化，运行时背包是靠 partKey 判定后重建的，
/// 因此“玩家已拥有某物品”等价于“该物品的 partKey 已完成组装并存档”
/// </summary>
public static class BagChecker
{
    /// <summary>
    /// 返回当前玩家已写入存档的物品 key 列表（已完成组装的 partKey）
    /// </summary>
    public static List<string> GetSavedItemKeys()
    {
        return new List<string>(GameGlobalData.Instance.finishedPartDict.Keys);
    }

    /// <summary>
    /// 按 partKey 判断玩家是否已拥有该物品
    /// </summary>
    public static bool HasItemByKey(string partKey)
    {
        if (string.IsNullOrEmpty(partKey))
            return false;
        return GameGlobalData.Instance.IsPartFinished(partKey);
    }

    /// <summary>
    /// 按 ItemData 判断玩家是否已拥有该物品（使用 ItemData.partKey）
    /// </summary>
    public static bool HasItem(ItemData item)
    {
        if (item == null)
            return false;
        return HasItemByKey(item.partKey);
    }

    /// <summary>
    /// 返回 requiredItems 中玩家尚未拥有的物品（顺序与传入一致）
    /// </summary>
    public static List<ItemData> GetMissingItems(ItemData[] requiredItems)
    {
        List<ItemData> missing = new List<ItemData>();
        if (requiredItems == null)
            return missing;

        foreach (ItemData item in requiredItems)
        {
            if (item != null && !HasItem(item))
                missing.Add(item);
        }
        return missing;
    }

    /// <summary>
    /// 前置条件是否全部满足（requiredItems 为空/null 时视为满足）
    /// </summary>
    public static bool AreRequirementsMet(ItemData[] requiredItems)
    {
        return GetMissingItems(requiredItems).Count == 0;
    }
}
