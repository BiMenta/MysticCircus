# 项目目录说明

这份文档说明仓库里每个目录和文件的作用，帮助第一次接触项目的人快速找到要改的地方。

- 工程：Unity **6000.6.8f1**，Built-in 渲染管线，第一人称 3D 解谜。
- 分层思路和 API：见 [FOUNDATION.md](FOUNDATION.md)。

## 1. 顶层

```text
MysticCircus/
├── Assets/                 所有游戏资源和代码
│   ├── StreamingAssets/    剧情文件 Story/story.json 和短片视频 Films/
│   ├── _Project/           本项目的代码、场景和美术资源
│   ├── Scenes/             Unity 模板自带的 SampleScene，未加入游戏流程
│   └── InputSystem_Actions.inputactions   模板自带的输入配置（玩家代码目前直接读键鼠，未使用）
├── Docs/                   项目文档与校验记录
├── Packages/               依赖声明：manifest.json、packages-lock.json
├── ProjectSettings/        Unity 工程设置，含 Build 场景顺序
├── DEVLOG.md               开发日志，按时间记录每次改动
├── README.md               项目入口：如何打开、运行、测试
└── .gitignore
```

## 2. `Assets/_Project`

```text
Assets/_Project/
├── Art/
│   ├── Materials/          9 个占位材质：酒红、旧金、青绿、象牙、深夜蓝、三种玩具色、镜面占位
│   ├── Models/             （空）正式模型放这里
│   └── Shaders/
│       └── PlanarMirror.shader            平面镜着色器，配合 PlanarMirror.cs
├── Audio/
│   ├── Music/              7 首 BGM，尚未绑定到场景
│   └── SFX/                （空）录制音效放这里；目前音效由 Sfx.cs 程序合成
├── Editor/
│   ├── CircusProjectBuilder.cs            用代码生成场景和材质，并校验场景
│   ├── StoryTools.cs                      检查剧情文件，同步 Build Settings
│   └── MysticCircus.Editor.asmdef
├── Prefabs/
│   ├── Characters/Player.prefab           玩家预制体
│   └── Interactables/      （空）可复用道具放这里
├── Scenes/                 Boot 启动场景和 6 个关卡场景
├── Scripts/                运行时代码，详见第 3 节
├── Settings/               （空）配置资产放这里
└── Tests/
    ├── EditMode/           纯逻辑测试：PuzzleLogicTests.cs、StoryGraphTests.cs
    └── PlayMode/           引擎内测试：FoundationTests.cs、StoryFlowTests.cs，Fixtures/ 放测试视频
```

标为“空”的目录里只有一个 `.gitkeep`，用来让 Git 保留目录。

### 场景（按游戏流程）

| 场景 | 内容 | 关卡控制器 |
| --- | --- | --- |
| `Boot` | 黑屏启动场景，加载后立即开始剧情 | 无 |
| `00_Backstage` | 后台换装与操作熟悉 | `WardrobeController` |
| `01_MirrorRoom` | 默剧镜子房间 | `MirrorRoomController` |
| `02_IllusionHall` | 小丑幻象大厅 | `IllusionRoomController` |
| `03_PuppetStage` | 木偶师舞台 | `PuppetStageController` |
| `04_ToyRoom` | 玩具房 | `ToyRoomController` |
| `05_ExteriorFinale` | 室外谢幕 | `EndingSequence` |

六个关卡目前是 v0.1 灰盒内容。游戏顺序由 `story.json` 决定，Build Settings 只决定哪些场景会被打包，可用菜单同步。

### 音乐

| 文件 | 对应场景或用途 |
| --- | --- |
| `bgm_title.mp3` | 标题页（尚未制作） |
| `bgm_backstage.mp3` | 后台 |
| `bgm_mirror_room.mp3` | 镜子房间 |
| `bgm_hall_of_illusions.mp3` | 幻象大厅 |
| `bgm_puppet_stage.mp3` | 木偶舞台 |
| `bgm_toy_room.mp3` | 玩具房 |
| `bgm_outside.mp3` | 室外 |

## 3. `Assets/_Project/Scripts`

`Levels/` 是各关专属脚本，会随关卡替换；其他目录都是可复用底层。

### `Core/`

| 文件 | 类别 | 作用 |
| --- | --- | --- |
| `GameSession.cs` | 底层 | 启动时创建持久服务；存档、读档、进度标记、游玩时长 |
| `GameSettings.cs` | 底层 | 设置项；同文件含 `L`（双语文本）、`CircusLayers`（层编号）、`InputGate`（禁用操作） |
| `ScreenFader.cs` | 底层 | 带淡入淡出的场景切换 |
| `MusicDirector.cs` | 底层 | 跨场景的 BGM 交叉淡入 |
| `SceneAmbience.cs` | 底层 | 声明当前场景要播放的 BGM |
| `Sfx.cs` | 底层 | 程序合成音效和音符 |
| `RoomController.cs` | 底层 | 所有关卡规则的基类 |
| `PuppetRig.cs` | 底层 | 关节角色的姿态驱动 |
| `ActorLook.cs` | 底层 | 把换装选择应用到角色身上 |
| `PlanarMirror.cs` | 底层 | 实时平面反射 |

### 其他目录

| 文件 | 类别 | 作用 |
| --- | --- | --- |
| `Interaction/Interactable.cs` | 底层 | 统一交互入口：拾取、出口、关卡动作 |
| `Player/FirstPersonPlayer.cs` | 底层 | 移动、视角、准星交互、传送 |
| `Player/MirrorBody.cs` | 底层 | 只在镜中可见、跟随玩家的身体 |
| `Puzzles/PuzzleLogic.cs` | 底层 | 不依赖引擎的谜题规则，含 `SequenceMatcher` |
| `Story/StoryGraph.cs` | 底层 | 剧情文件的数据结构、路线判断和结构检查 |
| `Story/StoryDirector.cs` | 底层 | 按剧情推进，决定下一个关卡或短片 |
| `Story/FilmPlayer.cs` | 底层 | 播放短片、字幕、跳过和分支选项 |
| `Levels/WardrobeController.cs` | 内容 | 后台换装关卡 |
| `Levels/MirrorRoomController.cs` | 内容 | 镜子房间规则 |
| `Levels/IllusionRoomController.cs` | 内容 | 幻象大厅规则 |
| `Levels/PuppetStageController.cs` | 内容 | 木偶舞台规则 |
| `Levels/ToyRoomController.cs` | 内容 | 玩具房规则 |
| `Levels/EndingSequence.cs` | 内容 | 室外谢幕流程 |
| `UI/PrototypeHUD.cs` | 内容 | 原型 HUD 与 F1 调试跳关，正式 UI 会替换它 |
| `MysticCircus.Runtime.asmdef` | 底层 | 把以上代码编译为一个程序集 |

## 4. 程序集依赖

```text
MysticCircus.Runtime  ──依赖──▶  Unity.InputSystem
   ▲        ▲       ▲
   │        │       └── MysticCircus.Tests.PlayMode
   │        └────────── MysticCircus.Tests.EditMode（仅编辑器）
   └─────────────────── MysticCircus.Editor（仅编辑器）
```

运行时代码不能引用编辑器和测试代码。新脚本放进 `Scripts/` 下任意子目录，会自动归入 `MysticCircus.Runtime`。

## 5. `Docs/`

| 文件 | 内容 |
| --- | --- |
| `PROJECT_STRUCTURE.md` | 本文档 |
| `CONTENT_GUIDE.md` | 替换关卡和短片的操作指南 |
| `FOUNDATION.md` | 底层的用法、约定、测试和已知限制 |
| `DEVELOPMENT.md` | 设计方向、v0.1 原型的边界和迭代顺序 |
| `SceneValidation.txt` | 场景校验工具的输出，运行时会被覆盖 |
| `RuntimeValidation.txt` | v0.1 的流程验证记录，仅作历史参考 |

## 6. 我要改某样东西，该去哪里？

| 想做的事 | 位置 |
| --- | --- |
| 替换或新增短片、调整剧情顺序 | 见 [CONTENT_GUIDE.md](CONTENT_GUIDE.md) |
| 新增一关的规则 | 在 `Scripts/Levels/` 新建 `RoomController` 子类 |
| 新增可复用的谜题规则 | `Scripts/Puzzles/PuzzleLogic.cs`，并在 `Tests/EditMode/` 加测试 |
| 新增或替换音效 | `Scripts/Core/Sfx.cs`，录制文件放 `Audio/SFX/` |
| 给场景配音乐 | 在场景放 `SceneAmbience`，绑定 `Audio/Music/` 的曲目 |
| 替换模型、贴图 | `Art/Models/`、`Art/Materials/` |
| 调整关卡顺序 | 修改 `StreamingAssets/Story/story.json` |
| 调整移动手感 | `Scripts/Player/FirstPersonPlayer.cs` |
| 修改生成的灰盒场景 | `Editor/CircusProjectBuilder.cs`（已存在的场景不会被覆盖） |

## 7. 不在仓库里的内容

下列内容由 `.gitignore` 排除。克隆后首次用 Unity 打开，`Library` 等目录会自动生成，耗时几分钟。

| 路径 | 说明 |
| --- | --- |
| `Library/`、`Temp/`、`obj/`、`Logs/` | Unity 缓存和日志 |
| `UserSettings/` | 个人编辑器设置 |
| `*.csproj`、`*.slnx` | Unity 自动生成的 IDE 工程 |
| `.vscode/` | 个人 IDE 配置 |
| `Build/`、`Builds/` | 构建产物 |
| `Screenshots/` | 本地截图 |

## 8. 协作规则

- **`.meta` 文件必须和对应资源一起提交**。场景通过其中的 GUID 引用资源，丢失后引用会断。
- 目前没有启用 Git LFS，音频和占位视频以普通文件提交；加入正式短片前需要配置，见 [CONTENT_GUIDE.md](CONTENT_GUIDE.md)。
- 场景文件是 YAML 文本，但多人同时改同一场景时很难合并，开工前先沟通。
