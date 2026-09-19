using System;
using System.Collections.Generic;

/// <summary>
/// 存档数据结构（JSON序列化载体）
/// </summary>
[Serializable]
public class GameSaveData
{
    /// <summary>存档版本号，便于未来格式迁移</summary>
    public int saveVersion = 1;
    /// <summary>已完成组装的零件partKey列表</summary>
    public List<string> finishedParts = new List<string>();
    /// <summary>已交互物品的uniqueId列表</summary>
    public List<string> interactedIds = new List<string>();
}
