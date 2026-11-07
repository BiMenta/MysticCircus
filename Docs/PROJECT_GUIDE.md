# 项目说明与制作指南

这份文档回答两件事：项目现在做到哪一步，以及要改关卡、加场景、换模型时具体怎么操作。第一次接手项目，从这里读起。

- API 细节见 [FOUNDATION.md](FOUNDATION.md)。
- `story.json` 的字段和短片规格见 [CONTENT_GUIDE.md](CONTENT_GUIDE.md)。
- 每个文件的作用见 [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md)。
- 剧情和玩法设计见 [最后的谢幕-单元内容.md](最后的谢幕-单元内容.md)、[最后的谢幕-序章与单元一方案.md](最后的谢幕-序章与单元一方案.md)。

## 1. 项目现状

### 1.1 一句话概括

《最后的谢幕》的**通用玩法系统已经做好，但还没有接进任何关卡**。现在运行游戏，看到的仍是 v0.1 的灰盒原型和占位短片，不是《最后的谢幕》的内容。

- 引擎：Unity 6000.6.8f1，Built-in 渲染管线，线性色彩空间。
- 视角：第一人称。曾讨论过 2.5D，目前决定不改。
- 界面：全部用 IMGUI 代码绘制，没有 Canvas 和 Prefab 形式的 UI。

### 1.2 现在运行会看到什么

`story.json` 目前的流程：

```text
film_opening → 00_Backstage → 01_MirrorRoom → film_choice ─┬→ film_branch_a ─┬→ 02_IllusionHall
                                                           └→ film_branch_b ─┘
→ 03_PuppetStage → 04_ToyRoom → 05_ExteriorFinale
```

- 六个关卡都是基础几何体搭的灰盒，四段短片都是占位视频。
- 按 `Tab` 能打开相簿，但里面是空的，因为还没有关卡放碎片。
- 按 `Q` 没有反应，因为还没有场景放 `RevealSystem`。
- 序章、单元一到四、终章的场景都还不存在。

### 1.3 进度

| 部分 | 状态 | 说明 |
| --- | --- | --- |
| 底层：存档、转场、交互、第一人称、平面镜、音乐音效 | 完成 | 后续关卡直接使用 |
| 剧情流程：`story.json`、分支、短片 | 完成 | 改顺序不用动代码 |
| 短片扩展：叠加文字、片尾黑场、看过后可跳过、关卡内短片 | 完成 | 序章要用，序章本身还没配置 |
| 显影：切层、调色、声音处理、镜面视频、快门图标 | 完成，未接关卡 | |
| 碎片、相簿、二合、守则 | 完成，未接关卡 | 五组二合的数据已录入 |
| 信任值、结局判定、跨周目数据 | 只有规则 | 终章流程还没有调用它们 |
| 单元一场景、演员示范、摆放判定 | 未开始 | 规则函数 `PuzzleLogic.MirrorSlot` 已有 |
| 序章配置、单元二到四、终章、结局 | 未开始 | |
| 标题页、暂停菜单、设置页 | 未开始 | `Boot` 目前总是从头开始并覆盖旧存档 |
| 单元三的「拍照多一个人」、放大查看照片 | 未开始 | 相簿观察页目前只有缩略图 |
| 正式模型、动画、配音、录制音效 | 未开始 | 音效是程序合成的占位，七首音乐还没绑定场景 |

### 1.4 验证到什么程度

- EditMode 49 项、PlayMode 36 项测试全部通过。
- 在编辑器 Play 模式下截图看过三层调色、相簿和守则。
- 在临时场景里走过这些步骤，控制台没有错误：放玩家预制体、导入 FBX、摆只在暖层出现的碎片、切层、拾取、开守则、开相簿。这一轮用代码调用完成，只确认流程能跑通。

**还没有验证的：**

- 真人键鼠手感，包括相簿拖拽、`Q` 切层、准星交互。
- 所有声音的实际听感。
- 打包后的游戏和 Windows 平台。
- 新建一关的完整流程：门、`story.json`、同步 Build Settings 这一段还没有用新场景走过。
- 录音替换音效（`Resources/Sfx/`）和碎片配图（`Resources/Fragments/`）：代码已写，还没有用实际文件试过。

## 2. 运行和调试

打开项目后，打开 `Assets/_Project/Scenes/Boot.unity` 点 Play，从剧情开头运行。也可以直接打开任意关卡运行，用来单独测试这一关。

| 按键 | 作用 |
| --- | --- |
| WASD 或方向键、Shift、鼠标 | 移动、奔跑、观察 |
| E | 互动 |
| Q | 切换显影层，需要场景里有 `RevealSystem` 且已解锁 |
| Tab | 打开或关闭相簿 |
| H | 提示，关卡配置了 `Hints` 才有内容 |
| Esc | 释放鼠标，单击重新锁定 |
| 按住 F1 | 列出所有剧情节点；先按 Esc 释放鼠标，再点击跳转 |
| 空格、回车 | 跳过短片 |

存档在 `Application.persistentDataPath` 下：`save.json` 是当前进度，`meta.json` 是跨周目数据，`album/` 是观察页缩略图。想彻底重来就删掉它们。

## 3. 场景和关卡

### 3.1 先判断要改什么

| 想做的事 | 改哪里 | 要写代码吗 |
| --- | --- | --- |
| 调整关卡顺序、加分支 | `story.json` | 不用 |
| 挪物件、改灯光、换模型 | 打开场景直接改 | 不用 |
| 改已有关卡的规则 | `Scripts/Levels/` 里对应的控制器 | 要 |
| 新增一关 | 新场景 + `story.json`，规则不同就新写控制器 | 通常要 |
| 新增一种所有关卡都能用的玩法 | `Scripts/` 下的底层目录，并补测试 | 要 |

### 3.2 修改已有场景

直接在编辑器里改，然后保存。需要注意：

- **不要再用生成器重建改过的场景。** `Generate Missing Prototype Scenes` 会跳过已存在的场景，不会覆盖手工修改；但如果把场景文件移走再生成，手工改动就全没了。
- **改 `Player.prefab` 不影响现有场景。** 六个旧场景里的玩家是生成器直接创建的普通物体，不是预制体实例。要改旧场景里的玩家，就在各场景里分别改。
- 场景文件很难合并。多人同时改同一个场景前先沟通。

### 3.3 新增一个关卡

以新建 `U1_MirrorRoom` 为例。

**第一步：建场景。** `File > New Scene` 选 Empty，保存到 `Assets/_Project/Scenes/U1_MirrorRoom.unity`。放入地面、墙和灯光。空场景默认没有灯光，也没有雾。

**第二步：放玩家。** 把 `Assets/_Project/Prefabs/Characters/Player.prefab` 拖进场景，放在出生点。它已经带好 `CharacterController`、`FirstPersonPlayer`、相机和 `AudioListener`，相机也绑定好了。玩家的根节点在脚底，所以 Y 取地面高度再加一点（旧场景用 `0.12`）。

**第三步：写规则。** 在 `Assets/_Project/Scripts/Levels/` 新建脚本。一个文件只放一个 `MonoBehaviour`，文件名和类名必须相同，否则挂不到场景上，打包后还可能启动失败。

```csharp
namespace MysticCircus
{
    public sealed class BellRoomController : RoomController
    {
        readonly SequenceMatcher order = new SequenceMatcher(new[] { 2, 0, 1 });

        public override void Interact(string action, int value)
        {
            if (IsSolved || action != "bell") return;
            Sfx.Play(SfxKind.Bell);
            if (order.Feed(value) && order.Complete) Solve("门开了。", "The door opens.");
        }
    }
}
```

纯规则放进 `Scripts/Puzzles/PuzzleLogic.cs` 并写 EditMode 测试，控制器只管表现。

**第四步：挂控制器和互动物件。** 新建空物体，挂上控制器。每个可互动物件需要：

1. 一个碰撞体。准星靠射线检测，没有碰撞体就点不到。
2. 一个 `Interactable`，填 `Prompt`、`Room`（拖入控制器）、`Action`、`Value`。按 E 后会调用 `Room.Interact(Action, Value)`。

玩家的互动距离是 4 米（`FirstPersonPlayer.Reach`）。

**第五步：放出口。** 门也是 `Interactable`：

- `NextScene` 必须填写，否则不会被当作出口。实际去向由 `story.json` 决定，这里填的值只在场景不属于剧情时生效。
- 需要解谜才能开，就勾选 `RequiresSolved` 并填 `Room`。
- 可选：放一盏默认关闭的灯，拖到控制器的 `ExitLamp`，解谜后会自动点亮。

没有门的关卡，在结束时调用 `StoryDirector.Advance()`。

**第六步：放 HUD 和音乐。**

- 新建物体挂 `PrototypeHUD`，绑定 `Room` 和 `Player`。不想显示文字（单元一到四都不要）就勾选 `Minimal`，只留准星。
- 新建物体挂 `SceneAmbience`，绑定 `Audio/Music/` 里的曲目。

**第七步：写进剧情。** 在 `story.json` 加节点，并把上一个节点的 `next` 指过来：

```json
{ "id": "level_u1_mirror", "type": "level", "scene": "U1_MirrorRoom", "next": "level_illusion" }
```

**第八步：同步和检查。** 依次运行 `Mystic Circus > Sync Build Settings From Story` 和 `Mystic Circus > Validate Story`。没写进剧情的场景不会进 Build Settings，无法按名称加载。

### 3.4 删除或替换旧关卡

只想让玩家不再经过旧关卡，改 `story.json` 的 `next` 跳过它就行，**不用删场景**。

确实要删除时，要同步修改：

- `StoryFlowTests`、`NarrativeFlowTests`、`FoundationTests` 写死了 `01_MirrorRoom` 到 `05_ExteriorFinale` 等场景名。
- `CircusProjectBuilder.SceneNames` 列出了六个旧场景，`Validate Prototype Scenes` 只检查这六个，不会检查新场景。

另外，旧场景从 `story.json` 移除后再同步 Build Settings，这些场景就不在 Build Settings 里了，上述测试同样会失败。替换旧关卡时要一起调整测试。

## 4. 模型

### 4.1 放在哪里

- 模型和贴图放 `Assets/_Project/Art/Models/<名字>/`，同一组放一个文件夹。
- 用 FBX 格式。提交时 `.meta` 必须一起提交。
- 单个文件超过 100 MB 时 GitHub 会拒收，这种文件要先配置 Git LFS，见第 8 节。

### 4.2 导出约定

| 项目 | 约定 |
| --- | --- |
| 单位 | 1 单位 = 1 米。玩家身高 1.8 米，视线高 1.6 米，门高约 3 米 |
| 正面 | 模型正面朝局部 **-Z**，`PuppetRig` 的鞠躬方向依赖这一点 |
| 轴心 | 地面物件放在底部中心；会旋转的部件放在旋转中心 |
| 变换 | 导出前应用缩放和旋转 |

Blender 可以用这组 FBX 导出参数，这样导出的模型在 Unity 里 `Scale Factor` 为 1，大小正确：

```python
bpy.ops.export_scene.fbx(filepath=path, object_types={"MESH"}, axis_forward="-Z", axis_up="Y",
                         apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True,
                         mesh_smooth_type="FACE", use_mesh_modifiers=False, add_leaf_bones=False)
```

### 4.3 导入后要检查的设置

选中 FBX，在 Inspector 里看：

1. **大小。** 拖进场景和玩家比一下。不对就改 `Model` 页的 `Scale Factor`，不要在场景里逐个缩放。
2. **碰撞。** 导入的 FBX **默认没有碰撞体**，玩家会直接穿过去，准星也点不到。
   - 房间这类静态大模型：勾选 `Model` 页的 `Generate Colliders`，或者摆几个隐藏的 Box Collider。后者更省，也不容易卡住玩家。
   - 道具：在物体上手动加 Box Collider。
3. **材质。** 默认使用 `Standard` 着色器，可以直接用。要修改材质时，在 `Materials` 页点 `Extract Materials` 导出成可编辑的文件。
4. **角色。** 有骨骼动画时在 `Rig` 页设置，用 `Animator` 驱动，不需要 `PuppetRig`。

### 4.4 用模型替换灰盒物件

灰盒物件身上通常有 `Interactable` 和碰撞体，保留它们，只换外观：

1. 把模型拖到灰盒物件下面，作为子物体，调好位置。
2. 关掉灰盒物件自己的 `Mesh Renderer`。不要删物体，也不要关碰撞体。
3. 运行一下，准星对准时应该能变亮。

准星高亮是通过材质的 `_Color` 属性实现的。`Standard` 没问题；自定义着色器没有 `_Color` 时能正常互动，只是不会变亮。

`Interactable` 挂在父物体、碰撞体在子物体上也可以，射线命中后会向上查找。

### 4.5 只在镜子里出现的物体

演员、镜中玩具、玩家的镜中身体都这样做：

1. 把物体**连同所有子物体**放到第 8 层（`CircusLayers.MirrorOnly`）。编辑器询问时选 `Yes, change children`。这一层在 Tag Manager 里没有名字，显示为空，按编号选即可。
2. **删掉碰撞体**，否则玩家会撞上一堵看不见的墙。
3. 只想让它出现在某一面镜子里，就把它加进那面镜子 `PlanarMirror` 的 `OnlyHere` 列表，运行时也能改。

做一面镜子：新建 Quad，让正面朝向玩家（Quad 的可见面是局部 -Z 方向），材质用 `MysticCircus/PlanarMirror` 着色器，再挂上 `PlanarMirror`。

玩家的镜中身体：第 8 层放一个身体，挂 `DelayedMirrorBody` 并绑定 `Player`，它会慢半拍跟随。不需要延迟时用 `MirrorBody`。

## 5. 把通用玩法接进关卡

下面这些都只需要摆放和配置，不用写代码。

### 5.1 显影

1. 新建空物体，挂 `RevealSystem`。调色和快门图标会自动加上。
2. 让玩家能用：勾选 `UnlockedOnEnter`，或者在控制器里到时机时调用 `RevealSystem.Unlock()`。**两者都没有的话，按 Q 没有反应。** 解锁状态存在进度里，后面的单元会一直保留。
3. 给只在某一层出现的物体挂 `RevealLayerObject`，勾选对应的层。物体在场景里**必须保持激活**，初始就关闭的物体不会响应切层。

| 物件 | 配置 |
| --- | --- |
| 只在暖层出现的彩灯光斑 | 灯光物体 + `RevealLayerObject`，只勾 `InWarm` |
| 镜面在暖层和冷层播视频 | 镜面 Quad 挂 `RevealVideoSurface`，填 `WarmVideo`、`ColdVideo`（如 `u1/1-3.mp4`），把 `PlanarMirror` 拖进 `PauseWhilePlaying` |
| 手风琴按层换处理方式 | 物体上放 `AudioSource` 并绑定曲目，挂 `RevealAudio`。它会让公共音乐淡出，所以不要再放 `SceneAmbience` |
| 门缝一掠而过的袖口 | 物体挂 `Apparition`，`Figure` 绑定它的**子物体**（绑自己会把脚本一起关掉），`From`、`To` 填子物体的局部坐标，再给父物体加 `RevealLayerObject` 只勾 `InCold` |

### 5.2 碎片和二合

1. 物件上放碰撞体和 `Interactable`，`Room`、`Action`、`NextScene`、`CollectibleId` 都留空。
2. 挂 `FragmentPickup`，`FragmentId` 填 `MemoryCatalog` 里的 ID（如 `u1_ticket`）。
3. 挂 `RevealLayerObject`，只勾碎片所在的层。

二合不用在关卡里做任何事，玩家在相簿里拖拽，成功后自动播短片、记标记、加信任值、守则多一道痕迹。关卡想做反应，就监听 `Album.Combined`。

**新增碎片或配方：** 改 `Scripts/Memory/MemoryCatalog.cs`。EditMode 测试会检查配方是否成对、是否引用了不存在的碎片。

**碎片配图：** 放到任意 `Resources/Fragments/<碎片ID>`，没有图时显示色块。

**二合短片：** 放到配方里写的路径，如 `Assets/StreamingAssets/Films/u1/1-5.mp4`。文件不存在时会跳过并写一条警告，其他效果照常。`Validate Story` **不会**检查这些路径，也不会检查 `RevealVideoSurface` 的视频，要自己确认。

### 5.3 其他

| 功能 | 配置 |
| --- | --- |
| 墙上的守则 | 碰撞体 + `Interactable` + `RuleBoard`，每个单元都可以放一张 |
| 可以细看的细节 | 碰撞体 + `Interactable` + `Inspectable`，`Id` 全游戏唯一（如 `u1_frame_scratch`）。可以和关卡互动共存 |
| 信任值 | 控制器里调用 `Trust.Award("u1_follow")`，同一个 ID 只算一次 |
| 时机判定 | 协程里 `yield return TimingQte.Run(1.2f, 0.15f, ok => { ... })` |
| 关卡内短片 | 协程里 `yield return FilmPlayer.PlayClip("u1/xxx.mp4")` |
| 所有倒影变慢 | `DelayedMirrorBody.Stretch()` |

## 6. 短片、音乐和音效

**短片：** 换同名文件就立即生效。格式、转码命令和 `story.json` 字段见 [CONTENT_GUIDE.md](CONTENT_GUIDE.md)。

**音乐：** `Audio/Music/` 有七首，都还没绑定场景。用 `SceneAmbience` 或 `RevealAudio` 绑定。

**音效：** 目前全部是程序合成的占位。要换成录音，把文件放到任意 `Resources/Sfx/` 目录，文件名和音效名相同（如 `Resources/Sfx/Shutter.wav`），重新运行就会替换，不用改代码。音效名见 `Scripts/Core/Sfx.cs` 的 `SfxKind`。方案建议优先替换 `Shutter` 和 `Heartbeat`。

新增音效种类时，要加在 `SfxKind` 的**最后**。

## 7. 改完之后怎么检查

| 改了什么 | 检查 |
| --- | --- |
| `story.json` | `Mystic Circus > Validate Story` |
| 新增场景 | `Sync Build Settings From Story`，然后直接打开场景运行一遍 |
| 代码 | 运行测试 |
| 画面和界面 | 进 Play 看一眼。这个项目有过测试全过、但界面颜色发白的情况 |

**运行测试：** 要先关闭打开这个项目的编辑器。

```sh
unity close .
unity test --mode EditMode --output edit.xml .
unity test --mode PlayMode --output play.xml .
```

`EveryBuildScene_LoadsAndRunsCleanly` 会加载 Build Settings 里的每个场景，运行时报错就算失败，所以新场景会自动被测到。结果文件不要提交到仓库。

**编辑器开着时可以用 CLI：**

```sh
unity recompile                                         # 编译并报告错误
unity command script eval -- --code 'return 1 + 1;'     # 在编辑器里执行代码
unity command editor.playmode start                     # 进入 Play
unity command capture game_view --output-path shot.png  # 截图
unity command console get --level error                 # 查看报错
```

刚编译完就进 Play 可能失败，因为编辑器还在重新加载脚本。先用 `unity status --until-ready` 等它就绪，再确认 `Application.isPlaying` 为真。

## 8. Git

- 开发分支是 `dev_bi`，远程仓库是 <https://github.com/BiMenta/MysticCircus>。
- `Assets` 下的 `.meta` 必须和资源一起提交。场景靠其中的 GUID 引用资源，丢了引用就断了。
- 现在没有启用 Git LFS。**加入正式短片或大模型前要先配置**：

```sh
git lfs install
git lfs track "Assets/StreamingAssets/Films/**/*.mp4" "*.fbx"
git add .gitattributes
```

## 9. 常见问题

| 现象 | 原因 | 处理 |
| --- | --- | --- |
| 准星对着物体没反应 | 没有碰撞体，或超过 4 米 | 加碰撞体；导入的 FBX 默认没有 |
| 玩家穿过模型 | 同上 | 加碰撞体 |
| 走着走着撞上看不见的东西 | 第 8 层物体带了碰撞体 | 删掉碰撞体 |
| 门打不开 | 勾了 `RequiresSolved` 但没绑 `Room`，或谜题没解 | 绑定 `Room`；检查是否调用了 `Solve` |
| 门被当成普通物件，开门没有转场 | `NextScene` 是空的 | 填写 `NextScene` |
| 过关后没进入预期的下一关 | 去向由 `story.json` 决定 | 改 `story.json`，不是改门 |
| 按 Q 没反应 | 场景没有 `RevealSystem`，或还没解锁，或相簿、守则正开着 | 见第 5.1 节 |
| 切层时物体不响应 | 物体在场景里初始是关闭的 | 保持激活，由 `RevealLayerObject` 控制 |
| 物体在玩家眼里能看到，本该只在镜中 | 子物体没一起改到第 8 层 | 连同子物体一起改 |
| 三面镜子都看到了演员 | 没加进 `OnlyHere` | 加进要出现的那面镜子 |
| 按名称加载场景报错 | 场景不在 Build Settings | 写进 `story.json` 后同步 |
| `ShippedStory_HasEveryLevelAndFilmItNames` 失败 | 剧情引用的视频或场景缺失 | 这是提醒，游戏仍可运行；补文件或改剧情 |
| 脚本挂不到物体上 | 文件名和类名不同，或一个文件放了多个类 | 一个文件一个 `MonoBehaviour` |
| 新写的界面颜色发白 | 线性色彩空间下 IMGUI 会多编码一次 | 用 `GuiKit` 绘制 |
| 中文显示不正常 | 还没配置中文字体，依赖系统字体回退，打包后可能不同 | 正式 UI 要配置字体 |
