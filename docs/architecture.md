# sunmao 运行时架构现状

面向"继续开发 / 扩内容"的现状描述。只记录当前代码怎么运作、新增内容需要触及哪些位置，不含评价与改造建议。

- 引擎：Unity 2022.3.62f2c1（中国版），内置渲染管线，旧版 Input Manager（`activeInputHandler = 0`）
- 产品名：`ONE OF TEN DEMO`，默认分辨率 1920x1080
- 构建场景：`Assets/Scenes/playScenes.unity`(0)、`Assets/Scenes/workroom.unity`(1)、`Assets/Scenes/outground.unity`(2)、`Assets/Scenes/attic.unity`(3)
- 自定义层 `inroom`；排序层 `Default` / `inroom` / `ontable`
- 脚本全部位于 `Assets/scripts/`，17 个 `.cs`，无 asmdef、无单元测试、无第三方运行时依赖
- 包依赖只用到 `com.unity.feature.2d`、`textmeshpro`、`ugui`、`timeline`、`ide.*` 等官方包

## 1. 单例与生命周期

全部单例都是"静态 `Instance` 惰性查找 + 可选 `DontDestroyOnLoad`"模式，没有注册表或依赖注入。

| 类 | 挂载点 | 持久化 | 职责 |
|---|---|---|---|
| `GameGlobalData` | `playScenes` 场景对象 `GameGlobalData` | `DontDestroyOnLoad` | 已完成组装的 `partKey` 字典 |
| `GlobalInteractRecord` | `playScenes` 场景对象 `GlobalCanvasRoot` | 同物体随 `GlobalUIRef` 持久化 | 已交互物品 `uniqueId` 列表 + 查询用 `HashSet` |
| `GlobalUIRef` | 同上 | `DontDestroyOnLoad` | 跨场景 UI 引用容器：`dialogBox`/`dialogTipText`/`videoPanel`/`videoRawImage`/`bagPanel` |
| `BagShowVideoManager` | 同上 | 同物体持久化 | 背包格子、物品预览、预览视频播放 |
| `HintManager` | `playScenes` 场景对象 `GameManager` | 无 | 单条提示文字，定时自动隐藏 |
| `PlayerManager` | `playScenes` 场景对象 `playerManager` | `DontDestroyOnLoad` | 维护 `OnlyPlayer` 静态引用（真正持久化的是 player 物体本身） |

生命周期细节：

- `GameGlobalData.Awake` 先做去重，再 `LoadProgress()` 从存档回填（`GameGlobalData.cs:40`）
- `GlobalInteractRecord.Awake` 直接调 `SaveSystem.Load()` 回填列表并重建 `HashSet`（`GlobalInteractRecord.cs:38`）
- `GlobalUIRef.Awake` 强制隐藏 `dialogBox`/`videoPanel`/`bagPanel`（`GlobalUIRef.cs:26`）
- `HintManager` 所在物体不持久化，切换场景后实例随场景销毁；所有调用点都带 null 检查（`ClickToPlayAnimation.cs:259`）
- `PlayerManager` 在 `Awake` 里 `GameObject.Find(playerName)` 找到玩家后对其单独调用 `DontDestroyOnLoad`（`PlayerManager.cs:42`）
- 三个"惰性自动创建"的单例（`GameGlobalData`/`GlobalInteractRecord`/`PlayerManager`）在找不到实例时会 `new GameObject` 现场建，因此任意场景直接调用不会 NRE

## 2. 脚本分层

**数据层（无 MonoBehaviour）**

- `GameSaveData.cs:8` — `[Serializable]` 存档结构：`saveVersion` / `finishedParts` / `interactedIds`
- `ItemData.cs:6` — `ScriptableObject`，菜单 `资产/物品数据`：`itemTitle` / `itemDescription` / `itemSprite` / `itemVideo` / `autoPlay`

**基础设施（静态类）**

- `SaveSystem.cs:13` — JSON 读写 `Application.persistentDataPath/save.json`；`Save()` 从两个全局管理器收集数据（`SaveSystem.cs:21`），`Load()` 失败返回 null（`SaveSystem.cs:39`），`DeleteSave()` 忽略不存在的文件（`SaveSystem.cs:57`）。所有异常只 `Debug.LogError`，不中断流程
- `GameKeys.cs:10` — 全部键位常量：E=世界交互/弹窗取消、Q=弹窗确认、Tab=背包、Esc=关闭弹层
- `InputHelper.cs:11` — `MouseWorldPos`，缓存 `Camera.main` 做 `ScreenToWorldPoint`

**玩家**

- `PlayerMove.cs:25` — `Start` 取 `Rigidbody2D`/子物体 `Animator`，`gravityScale = 0`、`freezeRotation = true`
- `PlayerMove.cs:37` — `Update` 分三步：采集输入 → 更新动画 → 处理翻转；`FixedUpdate` 统一写 `rb.velocity`
- `PlayerMove.cs:67` — 动画状态优先级：idle → 水平 → 上(back) → 下(forward)；驱动 6 个 Bool，其中 `backidle`/`forwardidle` 与 `idle` 同值
- 平移靠 `horizontalInput`/`verticalInput`，无寻路、无 Tilemap 碰撞特判

**交互**

- `ClickToPlayAnimation.cs` — 单构件核心脚本。`Start` 按存档恢复形态并补发背包物品（`ClickToPlayAnimation.cs:48`）；`Update` 处理鼠标点选 + Q/E 弹窗 + Esc 关视频（`ClickToPlayAnimation.cs:103`）；`RayCastClick` 用 `Physics2D.OverlapPoint` 判定点中自身（`ClickToPlayAnimation.cs:133`）
- `ClickPortalEnter.cs:28` — `OnMouseDown` 切场景：先判断玩家碰撞体是否在传送门内，再 `LoadSceneAsync(Single)`，加载完成销毁新场景同名玩家分身并把真身放到 `playerSpawnPos`
- `Sign.cs:18` — Trigger 期间按 E 开关告示牌弹窗；进入 Trigger 直接置 `signEnabled = true`，离开时置 false 并关闭弹窗
- `InteractExclamationTip.cs:32` — 未交互时实例化头顶感叹号并按 sin 上下浮动；`CompleteInteract()`（`InteractExclamationTip.cs:87`）标记已交互并销毁提示

**UI**

- `BagShowVideoManager.cs:61` — Tab 开关 `bagPanel`；`AddItemToBag` 追加图标；`RefreshBagUIFromCache` 重刷格子；`OnClickBagSlot` 填标题/描述并循环播放预览视频
- `HintManager.cs:31` — `ShowHint` 显示文字并按 `showDuration` 协程自动隐藏（重复调用会重置计时）
- `SaveGameMenu.cs:23` — Esc 打开菜单（视频面板/背包打开时让位）；`OnGUI`（`SaveGameMenu.cs:60`）用 IMGUI 画窗口，尺寸与字号按 `Screen.height / 1080` 缩放；`RestartGame`（`SaveGameMenu.cs:125`）清进度、清背包、回 `firstSceneName`

## 3. 关键数据流

**组装一个构件（主流程）**

```
鼠标左键 Update
  -> Physics2D.OverlapPoint 命中自身
  -> OpenDialog：显示 firstClickTip / secondClickTip
  -> Q 确认 PlayVideoAnim：按 clip 尺寸新建 RenderTexture 并播放
  -> VideoPlayer.loopPointReached -> OnVideoEnd
       - 未达 playTimes：重播
       - 达到：CloseVideo
       - 首次完成：GameGlobalData.SetPartFinished(partKey) -> SaveSystem.Save()
                    BagShowVideoManager.AddItemToBag(itemData)
                    HintManager.ShowHint(...)
       - 换 sprite / position / localScale 为已组装形态
       - InteractExclamationTip.CompleteInteract() -> GlobalInteractRecord.MarkInteracted -> SaveSystem.Save()
```

两个独立进度键：`partKey`（组装完成，决定构件形态）与 `itemUniqueId`（是否交互过，决定感叹号显隐）。

**存档写入时机**

只在两个方法里触发：`GameGlobalData.SetPartFinished`（`GameGlobalData.cs:84`）与 `GlobalInteractRecord.MarkInteracted`（`GlobalInteractRecord.cs:76`）。写入是"全量重写"，数据源为两个管理器的内存状态。

**存档读取时机**

`GameGlobalData.Awake`、`GlobalInteractRecord.Awake` 各读一次；`ClickToPlayAnimation.Start` 通过 `GameGlobalData.Instance.IsPartFinished` 决定初始形态。因此读取顺序依赖脚本 `Awake`/`Start` 的 Unity 调度，没有显式初始化阶段。

**场景切换**

`playScenes` ⇄ `workroom` 都靠 `Trigger_nextroom` 上的 `ClickPortalEnter`。玩家物体跨场景保留，新场景自带的同名 player 被销毁。返回 `playScenes` 时同样处理，`SaveGameMenu.RestartGame` 走的是同一套"借用分身出生点后销毁"的逻辑（`SaveGameMenu.cs:141`）。

## 4. 场景与预制体挂载

`Assets/Scenes/playScenes.unity`

| 物体 | 挂载脚本 |
|---|---|
| `GameGlobalData` | GameGlobalData |
| `GlobalCanvasRoot` | GlobalUIRef, BagShowVideoManager, GlobalInteractRecord |
| `GameManager` | HintManager |
| `chair` | ClickToPlayAnimation, InteractExclamationTip |
| `wood-1` | Sign |
| `Trigger_nextroom` | ClickPortalEnter |
| `Trigger_attic` | ClickPortalEnter（Click 触发 + 需 Q 确认，目标 `attic`，玩家落点 `(-6.5, -4.29)`；碰撞盒由手调，覆盖神龛那幅画并向下延伸到玩家站位） |
| `SaveGameMenu` | SaveGameMenu |
| `playerManager` | PlayerManager |
| 其余 | Canvas/EventSystem/Grid/Tilemap/Button/RawImage/TMP 文本等内置组件 |

`Assets/Scenes/workroom.unity`

| 物体 | 挂载脚本 |
|---|---|
| `pencup` | ClickToPlayAnimation, InteractExclamationTip |
| `chair_two` | ClickToPlayAnimation, InteractExclamationTip |
| `Trigger_nextroom` | ClickPortalEnter |
| 其余 | Grid/Tilemap/Main Camera 等 |

`Assets/Scenes/attic.unity`

| 物体 | 挂载脚本 / 内容 |
|---|---|
| `Grid` → `Tilemap` | 整张阁楼地图当作一个 Tile 铺底（`Assets/Scenes/TileMaps/attic.asset` → `attic.png`，385×190、PPU 100、Point 过滤；Tilemap 缩放 5.3，地图中心在世界原点，相机正对） |
| `Walls` | 4 个实体 BoxCollider2D 围出房间：左右内壁 `x=±9.4`、前壁内沿 `y=-4.5`、后壁内沿 `y=-0.1`（贴地板后沿，玩家能在地板上自由走动） |
| `Furniture` | **17 个实体 BoxCollider2D，逐件对应画面里的家具**（柜子、椅子、斜靠的木板、凳子、长桌、工作台、箱子、麻袋、木框等）。每件是 `Grid/Furniture` 下的一个同名子物体（`furn_cabinet_big`…），碰撞盒下沿 = 该家具在图里的落地线、上沿到 `y=0.3`；要微调直接改对应子物体的 Position/Size 即可 |
| `Trigger_back` | ClickPortalEnter（Auto：走到左下角活板门上自动回 `playScenes`，落点 `(0.24, -1.8)`） |
| `Main Camera` | ortho 6，位置 `(0, 0, -10)` |

家具碰撞箱的坐标来源：把 `attic.png`（有家具）与无家具的旧版逐件目测量出「落地线 + 左右边界」，再换算成世界坐标（1 图像像素 = 0.053 世界单位 = `PPU 100 × Tilemap 缩放 5.3`）。玩家碰撞体是「整个身子」（0.70×1.79，位于脚点上方 0.104~1.889），正好等于俯视视角里"站在家具前方"应有的进深——身体顶端停在家具落地线处，看起来就是紧贴着家具站着，因此家具碰撞箱不需要再往下延伸。

`Assets/Prefabs/`

- `player.prefab` → PlayerMove（子物体 Animator 用 `all-walk.controller`）
- `exclamation.prefab` → 无脚本，纯图片
- `Image.prefab` → 仅 UI Image

## 5. 数据资产

- `Assets/Inventory/` 三个 `ItemData`：`item_tenon_straight`（直榫出头）、`item_tenon_full_shoulder`（直肩二面出头，资源名 `全肩二面出头`）、`item_tenon_dovetail`（全透燕尾榫），三者 `autoPlay = 1`
- 组装视频：`Assets/BasicStructure/*.mp4`（`0001-0060`、`0001-0100`、`0001-0150`、`bitong`、`deng`、`jiansun`）
- 渲染纹理：`Assets/BasicStructure/room_video_rt.renderTexture`、`Assets/UI/video_rt.renderTexture`；运行时实际使用的是代码按 clip 尺寸动态创建的 RenderTexture，这两个资源是场景内挂载遗留
- 中文 TMP：`Assets/TextMesh Pro/Fonts/youmo.ttf`（40 MB）、`Resources/Fonts & Materials/STXINWEI SDF.asset`；`youmo SDF.asset` 被 `.gitignore` 排除，克隆后需本机重新生成

## 6. 扩内容时按现状需要触及的位置

**新增一种榫卯（第 4 个构件）**

1. 新建 `ItemData` 资产（`Assets/Inventory/`），填标题、描述、图标、`itemVideo`
2. 准备 mp4 放进 `Assets/BasicStructure/`
3. 场景里新建物体并挂 `ClickToPlayAnimation` + `InteractExclamationTip`
4. 在 Inspector 填：`itemData`、`partKey`（新键值，全局唯一）、`videoClip`、`playTimes`、`originalSprite`/`assembledSprite`、`originalScale`/`assembledPos`/`assembledScale`、两条提示文案
5. `InteractExclamationTip.itemUniqueId` 填新唯一 ID，`exclamationPrefab` 指向 `Assets/Prefabs/exclamation.prefab`
6. 确认新增的 `partKey` / `uniqueId` 不与已有值重复（无集中登记处，靠人工保证）

无需改动任何 C# 文件。存档结构、背包、提示、感叹号都是按数据驱动的。

**新增一个场景**

1. 场景放进 `Assets/Scenes/`，加入 Build Settings（`ClickPortalEnter` 与 `SaveGameMenu` 都依赖这里存在对应场景，否则 `LoadSceneAsync` 返回 null 并报错）
2. 场景内放置 `Trigger_nextroom` 并挂 `ClickPortalEnter`，填 `targetSceneName`、`playerSpawnPos`
3. 场景内需要弹窗/背包/视频/提示的物体直接引用 `GlobalUIRef.Instance`，不必重复摆 UI；玩家物体由 `PlayerManager` 维护
4. 注意 `HintManager` 只在 `playScenes` 有实例，新场景中 `HintManager.Instance` 为 null

**新增一个存档字段**

1. `GameSaveData.cs` 加字段（考虑 `saveVersion` 迁移）
2. 在 `SaveSystem.Save()`（`SaveSystem.cs:21`）里补写入来源
3. 在 `SaveSystem.Load()` 之后补回填位置（当前回填分散在 `GameGlobalData.Awake` 与 `GlobalInteractRecord.Awake`）

**新增一个 UI 面板**

1. 在 `GlobalCanvasRoot` 下建面板
2. 若需要跨场景访问，在 `GlobalUIRef` 加字段并在 Inspector 绑定；`Awake` 里按现有写法显式隐藏
3. 若开关要用新按键，加进 `GameKeys`；注意 Esc 已被 `SaveGameMenu` 与视频面板共用，代码用 `otherPanelOpen` 判断让位

**接入新逻辑时的既有约定**

- 全局状态改动后立刻调 `SaveSystem.Save()`（全量重写），没有脏标记或批量提交
- 跨场景引用一律走单例，引用缺失时用 `Debug.LogError` + `enabled = false` 兜底（见 `ClickToPlayAnimation.cs:84`、`BagShowVideoManager.cs:67`）
- 2D 点选靠 `Physics2D.OverlapPoint` + `BoxCollider2D`；`ClickToPlayAnimation.Start` 会在缺失时自动补 `BoxCollider2D`
- 场景内"重载后多出来的同名玩家"靠 GameObject 名字匹配查找并销毁（`ClickPortalEnter.cs:66`、`SaveGameMenu.cs:147`）

## 7. 与代码相关的现存事实

- 17 个 `.cs` 中 10 个是 GBK 编码（`GameGlobalData`、`GlobalInteractRecord`、`GlobalUIRef`、`PlayerManager`、`PlayerMove`、`ClickToPlayAnimation`、`ClickPortalEnter`、`InteractExclamationTip`、`HintManager`、`ItemData`），7 个是 UTF-8（`BagShowVideoManager` 带 BOM）；`.gitattributes` 只统一行尾（`*.cs text eol=crlf`），不涉及编码
- `HintManager` 挂在 `GameManager` 上且不持久化，切场景即失效
- `SaveGameMenu` 是 IMGUI 实现，文件头注释标注为"雏形"
- `Sign.OnTriggerEnter2D` 不校验进入者身份
- `PlayerMove` 用 6 个 Bool 驱动 Animator，代码内 TODO 标注尚未改为单一状态量
- 仓库无 README、LICENSE、测试与 CI
