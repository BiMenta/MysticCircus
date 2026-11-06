# Mystic Circus · 神秘马戏团

一款复古、梦幻、略带诡异气氛的 3D 解谜冒险游戏。目前包含 **v0.1 场景与交互灰盒原型**（基础几何体、占位表演者和 Built-in Standard 材质）和 **v0.2 可复用底层**。

- 想了解每个目录和文件的作用：看 [Docs/PROJECT_STRUCTURE.md](Docs/PROJECT_STRUCTURE.md)。
- 想用底层做新内容：看 [Docs/FOUNDATION.md](Docs/FOUNDATION.md)。
- 想了解架构和原型的边界：看 [Docs/DEVELOPMENT.md](Docs/DEVELOPMENT.md)。

## 打开与运行

- 编辑器：Unity **6000.6.8f1**。
- 克隆仓库后，通过 Unity Hub 添加该目录，或者在目录内运行下面的命令。首次打开要生成 `Library`，需要几分钟。

```sh
git clone https://github.com/BiMenta/MysticCircus.git
cd MysticCircus
unity open .
```

- 打开 `Assets/_Project/Scenes/00_Backstage.unity` 后点击 Play。
- WASD 或方向键移动，Shift 奔跑，鼠标观察，E 交互，H 提示（当前关卡尚未配置提示内容），Esc 释放鼠标，单击重新锁定鼠标。
- 按住 F1 显示调试场景导航；先按 Esc 再点击按钮可直接跳关。
- 当前界面使用英文占位文案，中文剧情与完整本地化留待后续制作。

## 场景流程

| 场景 | 内容 | 当前原型操作 |
| --- | --- | --- |
| `00_Backstage` | 后台换装与操作熟悉 | COSTUME / HAT / MASK 切换预览，再进入金色门 |
| `01_MirrorRoom` | 默剧镜子房间 | 依次 RIGHT、DOWN；按 RABBIT、STAR、MOON 点击玩具，解锁出口 |
| `02_IllusionHall` | 小丑幻象大厅 | 敲钟，SWITCH REALITY 切换棋盘，按 A1、B2、C3 激活地砖，选择 B 门 |
| `03_PuppetStage` | 木偶师舞台 | 操纵台两侧各加两次砝码，天平平衡；可点击 OPTIONAL SHOW |
| `04_ToyRoom` | 玩具房 | 音符 C、E、G、E；激活红蓝黄三条线对应的开关 |
| `05_ExteriorFinale` | 室外谢幕 | 向前走到室外道路，再转身看入口，触发集体鞠躬、淡出与标题 |

关卡已按以上顺序加入 Build Settings。任意关卡都可以单独打开测试，角色外观选择仅在本次运行期间保留。

## 项目结构

概览如下，详细说明见 [Docs/PROJECT_STRUCTURE.md](Docs/PROJECT_STRUCTURE.md)。

```text
Assets/_Project/
  Art/
    Materials/              酒红、旧金、青绿等占位材质
    Models/                 后续模型
    Shaders/                平面镜着色器
  Audio/
    Music/                  七首背景音乐，尚未绑定场景
    SFX/                    后续录制音效（目前由 Sfx 程序合成）
  Editor/
    CircusProjectBuilder.cs 场景生成与结构检查工具
  Prefabs/
    Characters/Player.prefab
    Interactables/          后续可复用道具
  Scenes/                   六个独立场景
  Scripts/
    Core/                   存档、设置、转场、音频、关卡基类、角色姿态、镜面、结尾
    Player/                 第一人称角色、镜中身体、换装预览
    Interaction/            统一交互入口、拾取、门与场景切换
    Puzzles/                纯逻辑规则与四个关卡的独立控制器
    UI/                     原型 HUD 与调试导航
  Settings/                 后续配置资产
  Tests/                    EditMode 与 PlayMode 自动化测试
Docs/
  PROJECT_STRUCTURE.md      完整的目录与文件说明
  FOUNDATION.md             可复用底层的用法与限制
  DEVELOPMENT.md            架构、边界与后续迭代
  SceneValidation.txt       场景检查工具运行后生成
  RuntimeValidation.txt     Play 模式基础流程检查记录
Packages/
ProjectSettings/
```

`Assets/Scenes/SampleScene.unity` 和 Input System 示例来自 Unity 模板，未加入游戏流程。

## 编辑器工具

- `Mystic Circus > Generate Missing Prototype Scenes`：生成缺失的场景与资源，**跳过已存在场景**，避免覆盖手工修改。
- `Mystic Circus > Validate Prototype Scenes`：检查六个场景的角色、相机、关卡控制器与缺失脚本，写入 `Docs/SceneValidation.txt`。
- 这两个工具会切换当前场景，请先保存正在编辑的场景；检查完成后回到后台。
- 生成器不会自动重建已有场景；若要重新生成，先备份目标场景，再移走对应 `.unity` 及 `.meta` 文件后运行。

通过 CLI 调用（需要项目已打开并连接 Pipeline）：

```sh
unity command script eval --project-path . -- --code 'MysticCircus.Editor.CircusProjectBuilder.Generate();'
unity command script eval --project-path . -- --code 'MysticCircus.Editor.CircusProjectBuilder.Validate();'
```

## 自动化测试

```sh
unity test --mode EditMode --output /tmp/edit.xml .
unity test --mode PlayMode --output /tmp/play.xml .
```

运行前需关闭已打开本项目的编辑器。

## 本地版本管理

远程仓库：<https://github.com/BiMenta/MysticCircus>，开发分支为 `dev_bi`。

`.gitignore` 排除 Unity Library、Temp、Logs、构建产物、用户设置和 IDE 配置；`Assets` 下的 `.meta` 文件应始终与对应资产一起提交。

现阶段不需要 Git LFS；将来加入大量模型、纹理、音频时再根据资源类型配置。
