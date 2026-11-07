时间：2026-11-06 Asia/Shanghai
功能归属：神秘马戏团 v0.1 项目骨架
改动：通过 Unity CLI 创建 Unity 6000.6.8f1 Built-in 3D 项目并安装本地 Pipeline；新增 _Project 目录、六场景生成器、基础材质、角色预制体、第一人称交互、换装与四关谜题占位逻辑、室外谢幕；新增项目说明和开发边界文档；初始化本地 Git 开发分支 dev_bi，配置 Unity .gitignore。公共配置影响：新增六个游戏场景的 Build Settings 顺序、产品名与窗口尺寸，启用运行时后台更新；所有新增业务代码位于 _Project，原音频素材保持原样。
现状：本地开发中；尚未提交、未配置远程、未推送。场景生成与验证结果将在完成后追加。

时间：2026-11-06 01:01 Asia/Shanghai
功能归属：神秘马戏团 v0.1 生成与本地验证
改动：已通过 Unity CLI 调用编辑器生成并保存六个场景、九种基础材质和 Player prefab；设置流程场景顺序；缩小场景标签以避免重叠，限制运行帧率为 60，使用 Input System 键鼠输入。场景结构检查六项通过，最终脚本编译 0 错误、0 警告；Play 模式确认帧数推进，CLI 验证后台换装、四关谜题状态与门切换、结尾回看触发及标题淡出。结尾测试首次传送时测试操作遗留停用的 CharacterController 导致 Move 报错，修正传送操作并重新验证后控制台 0 错误、0 警告。完整手工键鼠游玩、正式美术和安装包构建不在本次验证范围；详情见 Docs/SceneValidation.txt、Docs/RuntimeValidation.txt。
现状：本地待用户审核；Git 当前分支 dev_bi，尚无提交、无远程仓库、未推送；编辑器已退出 Play 模式并回到后台场景。

时间：2026-11-07 Asia/Shanghai
功能归属：神秘马戏团 v0.2 可复用底层
改动：新增与关卡内容无关的底层：存档与进度标记、设置与双语、输入闸门、场景淡入淡出、BGM 交叉淡入、程序合成音效、角色姿态、换装应用、平面镜反射、纯逻辑谜题规则；扩展 RoomController、Interactable、FirstPersonPlayer；脚本与编辑器代码划分为独立程序集，新增 EditMode 与 PlayMode 测试；将七首 BGM 副本放入 Audio/Music，尚未绑定场景。关卡内容、场景与美术按用户要求未改动，等待参考视频后再制作。改动前的备份位于上级目录 MysticCircus_v0.1_backup.tar.gz。详情见 Docs/DEVELOPMENT.md。
现状：底层已提交；新增 Docs/PROJECT_STRUCTURE.md 记录目录构成；配置远程仓库 https://github.com/BiMenta/MysticCircus 并推送 dev_bi 分支。EditMode 16 项、PlayMode 8 项测试通过；键鼠实际手感未人工验证。

时间：2026-11-07 Asia/Shanghai
功能归属：神秘马戏团 v0.3 互动影片框架
改动：新增 Assets/StreamingAssets/Story/story.json，游戏顺序改为由剧情文件驱动，关卡和短片都是节点，短片支持字幕、跳过、分支选项、选择时限和按标记走路线；新增 StoryGraph、StoryDirector、FilmPlayer、StoryTools，新增 Boot 启动场景；出口门改为优先跟随剧情，存档增加当前节点；各关专属脚本移到 Scripts/Levels（GUID 不变）。Build Settings 改为由剧情文件同步，公共配置影响：EditorBuildSettings 增加 Boot 为第一个场景。Films 内为 4 段占位视频，关卡内容未改动。操作指南见 Docs/CONTENT_GUIDE.md。
现状：EditMode 28 项、PlayMode 17 项通过；在 macOS 编辑器里走通 Boot、短片、关卡、分支选择流程并截图确认画面，控制台 0 错误、0 警告。真人键鼠操作、打包后的游戏和 Windows 播放未验证。未提交。

时间：2026-11-07 Asia/Shanghai
功能归属：《最后的谢幕》全片通用玩法模块
改动：按 Docs/最后的谢幕-单元内容.md 实现跨单元共用的系统，不含具体单元内容：显影（切层、调色、声音处理、镜面视频、快门图标）、碎片与二合配方表、相簿、守则、信任值与结局判定、跨周目数据、时机判定、慢半拍倒影、镜子专属物体、一掠而过的物体；短片支持叠加层、片尾黑场、看过后可跳过和关卡内播放；新增 7 种合成音效，可用录音替换。修复截图才发现的线性色彩空间问题（界面发白、暗部噪点过重）和柔焦重影；修复关闭窗口的按键穿透给玩家、场景卸载时误激活物体。公共配置影响：无。用法见 Docs/FOUNDATION.md。
新增 Docs/PROJECT_GUIDE.md，说明项目现状以及修改、新增关卡场景和模型的操作，并在 Docs/CONTENT_GUIDE.md 补充短片新字段。讨论过 2.5D，用户决定暂不改，继续第一人称。
现状：EditMode 49 项、PlayMode 36 项通过；在编辑器 Play 模式截图看过三层调色、相簿和守则。模块尚未接入关卡；相簿拖拽和按键手感未真人试玩，声音未试听。
