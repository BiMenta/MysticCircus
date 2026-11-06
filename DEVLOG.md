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
