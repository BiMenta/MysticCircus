# 底层框架说明（v0.2）

这份文档说明 Mystic Circus 的可复用底层：它提供什么、怎么用、后续做新内容时还要补什么。底层不包含任何具体关卡的内容，后续无论按参考视频做什么关卡，都可以直接调用。

## 1. 底层写好后，是不是只要换模型和编排场景？

**大部分是，但不全是。** 通用系统不用重写，每一关独有的规则和表现仍然要做。

| 工作 | 是否需要 | 说明 |
| --- | --- | --- |
| 存档、设置、双语、转场、音乐、音效、移动、交互、短片播放 | 不用重写 | 直接调用底层 |
| 调整顺序、替换短片 | 不用写代码 | 改 `story.json` 或替换视频，见 [CONTENT_GUIDE.md](CONTENT_GUIDE.md) |
| 替换模型、贴图、材质 | 需要 | 美术资源到位后替换 |
| 摆放场景、灯光、镜头 | 需要 | 可在编辑器里手工摆放，也可用代码生成 |
| **每关的谜题规则** | **需要写少量代码** | 继承 `RoomController`，大多只需几十行 |
| **角色动画和过场演出** | **需要制作** | 底层只有简单姿态，正式动画要另做 |
| **UI** | **需要制作** | 标题页、暂停菜单、设置页还没有，底层只提供接口 |
| **底层没覆盖的新玩法** | **需要扩展** | 例如对话树、背包、怪物 AI |

判断方法：如果一项玩法只是换了外观，那只需要换资源；如果规则变了，就要写这一关的控制器。

## 2. 结构

```text
内容层（随关卡替换）    场景、模型、贴图、各关 RoomController 子类、UI
─────────────────────────────────────────────────────────────
剧情（story.json）      关卡和短片的顺序、分支
─────────────────────────────────────────────────────────────
底层（本文档）          剧情 StoryDirector、FilmPlayer · 交互 Interactable · 玩家 FirstPersonPlayer · 关卡基类 RoomController
                        角色 PuppetRig、ActorLook · 镜面 PlanarMirror、MirrorBody
                        服务 GameSession、ScreenFader、MusicDirector、Sfx、GameSettings、InputGate
─────────────────────────────────────────────────────────────
纯逻辑（不依赖引擎）    SequenceMatcher、PuzzleLogic、StoryGraph 的路线与检查
```

`GameSession`、`ScreenFader`、`MusicDirector`、`Sfx`、`FilmPlayer`、`StoryDirector` 在游戏启动时自动创建，并跨场景保留，**不需要放进场景**。

运行时代码属于程序集 `MysticCircus.Runtime`（`Assets/_Project/Scripts`）。编辑器工具和测试各有独立程序集。

## 3. 添加一个新关卡

**第一步：写规则。** 下面的例子是“按对顺序就开门”：

```csharp
using MysticCircus;

public sealed class BellRoomController : RoomController
{
    readonly SequenceMatcher order = new SequenceMatcher(new[] { 2, 0, 1 });

    public override string[] Hints => new[]
    {
        L.T("墙上的画有提示。", "The painting on the wall is a clue."),
        L.T("从最小的钟开始。", "Start with the smallest bell."),
    };

    public override void Interact(string action, int value)
    {
        if (IsSolved || action != "bell") return;
        Sfx.Play(SfxKind.Bell);
        if (!order.Feed(value)) Say("顺序不对。", "Wrong order.");
        if (order.Complete) Solve("门开了。", "The door opens.");
    }
}
```

**第二步：搭场景。**

1. 放入带 `CharacterController` 和 `FirstPersonPlayer` 的玩家，并绑定 `View` 相机。
2. 新建物体，挂上上面的控制器。
3. 给每个可互动物体加碰撞体和 `Interactable`，填写 `Prompt`、`Room`、`Action`、`Value`。
4. 给出口填写 `NextScene`，并勾选 `RequiresSolved`。去向由剧情决定，`NextScene` 只在该场景不属于剧情时生效，但必须填写，门才会被当作出口。
5. 放一个 `SceneAmbience` 并绑定音乐。
6. 放入 HUD（目前是 `PrototypeHUD`），绑定 `Room` 和 `Player`。
7. **把关卡写进 `story.json`**，再运行 `Mystic Circus > Sync Build Settings From Story`，否则无法按名称加载。

## 4. 模块说明

### 进度与设置

| API | 用途 |
| --- | --- |
| `GameSession.Instance.NewGame(scene)` | 清除存档，开始新游戏；默认从剧情开头开始 |
| `GameSession.Instance.Continue()`、`GameSession.HasSave` | 读档继续；存档损坏或场景不存在时不会卡死 |
| `SetFlag(id)`、`HasFlag(id)` | 保存任意进度，如已拾取物品、已看过的剧情 |
| `Data` | 存档内容：当前剧情节点、当前场景、换装选择、提示次数、游玩时长、进度标记 |
| `GameSession.NonGameplayScenes` | 标题页等场景加入后，不记检查点也不计时 |
| `L.T("中文", "English")` | 按当前语言返回文本 |
| `GameSettings` | 语言、音乐和音效音量、灵敏度；修改后调用 `Save()` |
| `InputGate.Hold()`、`Release()` | 打开菜单或对话时禁用操作，需成对调用 |

存档是一个 JSON 文件，位于 `Application.persistentDataPath/save.json`。进入场景时自动保存，退出时也会保存。**存的是到达过哪个剧情节点，不存房间内的谜题进度**，读档后这一关或这段短片要从头开始。分支选择记录在进度标记里，读档后仍然有效。

### 剧情 `StoryDirector`、`FilmPlayer`

剧情文件的写法见 [CONTENT_GUIDE.md](CONTENT_GUIDE.md)。

| API | 用途 |
| --- | --- |
| `StoryDirector.Begin()` | 从剧情开头开始 |
| `StoryDirector.Go(id)` | 跳到某个节点；转场进行中返回 false |
| `StoryDirector.Advance()` | 当前关卡结束，进入下一个节点；不在剧情中或已是最后一个节点时返回 false |
| `StoryDirector.Current` | 当前节点 |
| `FilmPlayer.Skip()`、`Choose(index)` | 跳过短片、选择分支 |

普通出口门会自动调用 `Advance()`。没有门的关卡，在完成时自己调用它，`EndingSequence` 就是这样做的。

### 场景与声音

| API | 用途 |
| --- | --- |
| `ScreenFader.Load(name)` | 淡出、加载、淡入；期间玩家不可操作 |
| `ScreenFader.Cover(routine)` | 淡出后在黑屏下执行任意工作，再淡入 |
| `MusicDirector.Play(clip, volume)` | 交叉淡入切歌；同一首不会重头播放 |
| `MusicDirector.Stop()` | 淡出音乐；进入短片时自动调用 |
| `SceneAmbience` | 场景进入时自动播放绑定的音乐 |
| `Sfx.Play(SfxKind.X, volume)` | 程序合成音效：Click、Good、Bad、Door、Bell、Laugh、Pickup、Twang、Fanfare、Step、Page、Crank |
| `Sfx.Note(0~4)` | 音乐盒音符 C、D、E、F、G |

`Audio/Music` 里已有七首曲目（后台、镜子房、幻象大厅、木偶舞台、玩具房、室外、标题），**还没有绑定到任何场景**。

### 关卡基类 `RoomController`

| 成员 | 用途 |
| --- | --- |
| `Interact(action, value)` | 必须实现，接收所有互动 |
| `Solve(...)` | 标记完成：设置消息、点亮 `ExitLamp`、播放音效、触发 `Solved` 事件；重复调用无效 |
| `Say(zh, en)` | 设置 HUD 底部的反馈文字 |
| `Objective` | 当前目标；可重写，让目标随阶段变化 |
| `Hints` | 当前阶段的提示，由轻到重；返回新列表时重新计数 |
| `NextHint()` | 取下一条提示并计入 `HintsUsed`；HUD 的 H 键调用它 |

### 交互 `Interactable`

`Use()` 按以下优先级执行，只走其中一条：

1. `CollectibleId` 不为空：作为拾取物，记入存档并隐藏，重新进入房间也不会再出现。
2. `NextScene` 不为空：作为出口；若勾选了 `RequiresSolved`，未解谜时门打不开。打开后由剧情决定去向，场景不在剧情中时才去 `NextScene`。
3. 否则调用 `Room.Interact(Action, Value)`。

成功使用后触发 `Used` 事件，可以在 Inspector 里直接连接简单效果。准星对准时物体会自动变亮。`SetGlow(color)` 用于长期显示状态，例如琴键亮起。两者都不会改动共享材质。

### 玩家 `FirstPersonPlayer`

操作：WASD 或方向键移动，Shift 奔跑，鼠标看，E 互动，Esc 释放鼠标。

| API | 用途 |
| --- | --- |
| `Frozen` | 过场时锁定操作 |
| `Focus` | 当前准星对准的 `Interactable` |
| `Hand` | 手持物挂点，把物体设为其子节点即可 |
| `Warp(pos, rot)`、`WarpToSpawn()` | 传送，已处理 `CharacterController` 的启停 |
| `Tumble(seconds)` | 视角翻滚一周，中途回到出生点，可用于惩罚或转场 |

### 角色 `PuppetRig`、`ActorLook`

`PuppetRig` 用于由简单几何体组成的关节角色：`Pose(lean, bow, armLeft, armRight)` 平滑过渡到目标姿态，`SwayAmount` 增加摆动。需要把 `Torso`、`ArmLeft`、`ArmRight` 绑定到对应的轴心。手臂角度：0 下垂，90 前伸，180 上举。

**使用骨骼动画的正式模型应改用 Animator，不需要 `PuppetRig`。**

`ActorLook` 把存档里的换装选择应用到任意身体上（服装材质、帽子、面具）。

### 镜面 `PlanarMirror`、`MirrorBody`

这是实时的平面反射，并且镜子能显示房间里实际看不到的东西。

1. 新建 Quad，让可见面朝向玩家，材质使用 `MysticCircus/PlanarMirror` 着色器，再挂上 `PlanarMirror`。
2. 想让物体只出现在镜子里，就把它放到第 8 层（`CircusLayers.MirrorOnly`）。玩家相机会自动排除这一层。
3. 想在镜子里看到“玩家自己”，给一个第 8 层的身体挂上 `MirrorBody`，它会跟随玩家。

第 8 层的物体如果带碰撞体，玩家会撞上一堵看不见的墙。不需要时要删掉碰撞体。

### 纯逻辑 `SequenceMatcher`、`PuzzleLogic`

| API | 用途 |
| --- | --- |
| `SequenceMatcher` | 按顺序输入。`Feed(v)` 返回是否前进；输错时保留仍然成立的部分，如 C-E-G-E 输成 C C E G E 也算通过 |
| `Opposites(seq)` | 配对取反（0↔1、2↔3），适合镜像动作 |
| `Shuffled(n, rng)`、`NoRepeatSequence(len, n, rng)` | 随机排列、相邻不重复的随机序列 |
| `Torque(...)`、`IsBalanced(...)` | 天平：左减右的力矩；所有砝码上秤且两边相等才算平衡 |
| `SameLayout(a, b)` | 比较摆放是否一致 |
| `Decoys(correct, n, rng)` | 所有错误选项，顺序随机 |

### 全片通用玩法（《最后的谢幕》）

规则见 [最后的谢幕-单元内容.md](最后的谢幕-单元内容.md)。下面的模块不含任何单元内容，单元只负责摆放和配置。`Album`、`RuleSheetView`、`TimingQte` 随 `GameSession` 自动创建；进度都存成进度标记，不另存档。

| 模块 | 用法 |
| --- | --- |
| `RevealSystem` | 显影。场景放一个即可，它会自动给相机加调色、加快门图标。`Unlock()` 交给玩家，`Q` 或点图标切换梦界 → 暖 → 冷；`Hold()`/`Release()` 暂停切换，`Set()` 由剧情直接切层，`Changed` 事件通知其他物体 |
| `RevealLayerObject` | 勾选物体在哪几层出现，隐藏时碰撞和互动一起关闭。物体在场景里要保持激活 |
| `RevealGrade` | 全屏调色，四套外观 `Dream/Warm/Cold/White`；`Override(look)` 强制外观，传 null 交还给当前层 |
| `RevealAudio` | 一条循环音乐按层切换处理（隔壁房间、旧录音带），`PickUpTheBeat()` 让音乐清晰地重新起拍 |
| `RevealVideoSurface` | 表面在指定层循环播放视频，如镜子变成回忆画面。视频路径相对 `StreamingAssets/Films`，缺文件时显示纯色 |
| `MemoryCatalog` | 全部碎片和二合配方的数据表，五组都已录入；改内容改这里 |
| `Album` | 相簿，`Tab` 打开，可拖拽二合。`Collect`、`Observe`、`TryCombine`、`Capture`；`Combined` 在二合短片播完后触发。碎片配图放 `Resources/Fragments/<id>`，没有图时显示色块 |
| `FragmentPickup`、`Inspectable`、`RuleBoard` | 挂在带 `Interactable` 的物体上：拾取碎片、推镜头看细节并收进观察页、打开守则 |
| `RuleSheet`、`RuleSheetView` | 守则文字和全屏阅读，痕迹由 `combo.*` 标记推出，每个单元的守则都读同一份进度 |
| `Trust`、`Endings` | 信任值和结局判定。信任值 = `trust.` 标记数，同一时刻不会重复计分；`Trust.Award(id)` 记一次 |
| `MetaProgress` | 跨周目数据，独立存 `meta.json`，新游戏不清除：场次（从 47 开始）、看过的短片、达成的结局 |
| `TimingQte` | 无文字的时机判定：外圈收拢到内圈时按键。失败只回报 false，由调用方决定重置什么 |
| `FilmPlayer.PlayClip(path)` | 关卡内全屏播放短片，播完回到原位；缺文件时跳过并警告 |
| 短片节点新字段 | `overlays`（`typewriter`/`title`/`doubled`/`caption`）、`tailSeconds`（片尾黑场）、`hideChrome`（选项不带遮罩和编号）、`skippableOnceSeen`（看过后可跳过） |
| `PlanarMirror.OnlyHere` | 列表里的物体只在这面镜子里出现，运行时可改，比如演员换镜子 |
| `DelayedMirrorBody` | 镜中身体慢半拍，`Stretch()` 让所有倒影同时明显变慢 |
| `Apparition` | 隔一段时间一掠而过的物体，如菱格袖口 |
| `PuzzleLogic.MirrorSlot` | 按面对玩家的表演者自身左右算平台位置 |
| `PrototypeHUD.Minimal` | 只留准星的无文字模式，`F1` 调试导航仍可用 |
| `Sfx` | 新增 `Shutter`、`Heartbeat`、`Glass`、`Tap`、`Slide`、`Mechanism`、`Ripple`。`Resources/Sfx/<名字>` 放录音后会替换合成音 |

`InputGate.Release()` 之后，当帧仍算禁用，关闭窗口的那次按键不会再传给玩家。用 `GuiKit` 画界面时直接写想要显示的颜色，它会处理线性色彩空间。

## 5. 约定

- 模型正面朝局部 **-Z**，`PuppetRig` 的鞠躬方向依赖这一点。
- 需要跨语言的文本用 `L.T`。
- 新增 MonoBehaviour 时每个类一个文件，且文件名与类名相同，否则无法挂到场景上。
- 纯规则放进 `PuzzleLogic` 并写测试，控制器只负责表现。

## 6. 测试

```sh
unity test --mode EditMode --output /tmp/edit.xml .
unity test --mode PlayMode --output /tmp/play.xml .
```

| 范围 | 用例数 | 覆盖内容 |
| --- | --- | --- |
| EditMode | 49 | 序列匹配、配对取反、洗牌、随机序列、天平、布局比对、错误选项、剧情解析与路线、内容错误检查、实际剧情文件引用的场景和视频是否齐全；显影循环、碎片与配方表、守则痕迹、信任值、结局判定、叠加层时间、时机判定、倒影延迟曲线 |
| PlayMode | 36 | 服务启动、各关卡加载运行无报错、转场与检查点、拾取持久化、出口上锁、提示计数、鞠躬方向、镜面只显示第 8 层、短片播放与跳过、画面颜色和铺满、门跟随剧情、空短片位跳过、分支选择与超时、标记路线、Boot 启动、存档节点有效性检查；显影切层与卸载、碎片拾取、二合只计一次、叠加层短片、看过后可跳过、时机判定、倒影延迟、镜子专属物体、调色实际画面、关卡内短片、镜面视频、推镜头细看、一掠而过的物体 |

`Mystic Circus > Validate Prototype Scenes` 可检查六个场景是否有丢失的脚本引用，结果写入 `SceneValidation.txt`。

## 7. 现状与限制

**没有验证的部分**（自动化测试未覆盖，也没有人工试玩）：

- 键鼠实际手感：移动、奔跑、脚步声、准星高亮。
- `Tumble`、`MirrorBody`、`ActorLook`、音乐交叉淡入的实际效果。
- 音效听起来如何：只验证了能生成，没有人耳试听。
- 中文显示：`PrototypeHUD` 用的是默认字体，门锁提示现在默认为中文，能否正常显示还没看过。短片字幕和选项的中文在 macOS 编辑器里已看过，显示正常，但依赖系统字体回退。正式 UI 需要配置中文字体。
- 短片的实际鼠标点击和键盘选择：选择逻辑已测试，点击区域的真人操作没有验证。
- `GameSession.Continue()` 从存档节点恢复：测试为避免碰真实存档，只检查了节点有效性，没有走完整读档。
- 短片在打包后的游戏和 Windows 上的播放：目前只在 macOS 编辑器中验证过。

- 通用玩法模块：相簿和守则画面看过截图，但相簿的鼠标拖拽、`Q` 切层的真人手感都没有试玩；`RevealAudio` 只验证了运行不报错，没有人耳试听。
- 通用玩法模块依赖第一人称：`FirstPersonPlayer` 被 `RevealSystem`、`Album`、`Inspectable`、`DelayedMirrorBody`、`MirrorBody`、`PrototypeHUD` 直接引用。以后换视角要先解开这些引用。

**还没有做的部分**：

- 单元一场景、演员示范、摆放判定尚未制作，通用模块还没有接到任何关卡；`Endings`、`MetaProgress.NextShow` 只有规则，没有终章流程调用。
- 照相亭的「拍照多一个人」和放大查看照片（单元三）尚未制作，相簿观察页目前只有缩略图。

- 标题页、暂停菜单、设置页：`GameSettings`、`NewGame`、`Continue` 都已就绪，但没有界面调用，玩家目前无法切换语言或读档。
- 启动时总是从头开始，并覆盖旧存档：`Boot` 目前直接调用 `Begin()`。标题页应在这里决定“新游戏”或“继续”。
- 短片内暂停、音量单独设置：短片音量暂时跟随音乐音量。
- 房间内谜题进度的存档。
- 手柄支持。
- 第 8 层未在 Tag Manager 中命名：按编号可以正常工作，只是在编辑器里显示为空名。

**旧内容的已知问题**（v0.1 的关卡内容，这次没有改动，读代码发现，准备被新内容替换）：

- `EndingSequence`：角色鞠躬时向后仰。可改用 `PuppetRig`。
- `PuppetStageController`：天平倾斜方向反了，右边重时左边下沉。
- `IllusionRoomController`：小丑“指向” C 门时朝向不对。
- `05_ExteriorFinale`：地面没有边界，玩家能走到边缘掉下去。
- 待确认：世界文字可能会穿透物体显示。这是根据默认字体材质的常见行为推断的，尚未实际验证。
- 各关的 HUD 目标文字直接写出了答案。
