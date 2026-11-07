# 替换关卡和短片

游戏的顺序全部写在 `Assets/StreamingAssets/Story/story.json`。关卡和短片都是其中的**节点**，一个节点结束后进入下一个。换内容时不需要改场景里的门，也不需要改代码。

## 常见操作

| 想做的事 | 操作 |
| --- | --- |
| 换一段短片 | 用同名文件覆盖 `Assets/StreamingAssets/Films/` 里的视频 |
| 新增一段短片 | 把视频放进 `Films/`，在 `story.json` 加一个 `film` 节点，再把上一个节点的 `next` 指向它 |
| 调整顺序、删掉一段 | 只改 `story.json` 里的 `next` |
| 换一个关卡 | 新建场景，把节点的 `scene` 改成新场景名，再同步 Build Settings（见下文） |
| 短片还没做好 | 节点先写好，文件不放。没有选项的短片会自动跳过；有选项的会在黑屏上显示选项 |

每次改完，在 Unity 里运行 `Mystic Circus > Validate Story`，它会一次列出所有问题。`ShippedStory_HasEveryLevelAndFilmItNames` 测试做同样的检查，所以短片缺文件时这个测试会失败，这是提醒，不影响游戏运行。

## 剧情文件

```json
{
  "start": "film_opening",
  "nodes": [
    {
      "id": "film_opening",
      "type": "film",
      "video": "film_opening.mp4",
      "subtitles": [
        { "from": 0.5, "to": 3.5, "zh": "中文字幕", "en": "English subtitle" }
      ],
      "next": "level_backstage"
    },
    { "id": "level_backstage", "type": "level", "scene": "00_Backstage", "next": "film_choice" },
    {
      "id": "film_choice",
      "type": "film",
      "video": "film_choice.mp4",
      "promptZh": "你要怎么做？",
      "promptEn": "What do you do?",
      "choices": [
        { "zh": "留下", "en": "Stay", "next": "film_stay", "setFlag": "stayed" },
        { "zh": "离开", "en": "Leave", "next": "film_leave" }
      ],
      "choiceSeconds": 10,
      "defaultChoice": 0
    }
  ]
}
```

| 字段 | 适用 | 说明 |
| --- | --- | --- |
| `id` | 全部 | 节点名，不能重复。存档记录的就是它，已发布后不要改名 |
| `type` | 全部 | `level` 或 `film` |
| `next` | 全部 | 下一个节点。关卡留空表示停在这里；短片留空表示故事结束，清除存档后从头开始 |
| `routes` | 全部 | 按之前的选择走不同路线，如 `[{ "ifFlag": "stayed", "next": "ending_a" }]`。第一条匹配的生效，都不匹配时走 `next` |
| `scene` | 关卡 | 场景名，场景文件放在 `Assets/_Project/Scenes/` |
| `video` | 短片 | `Films/` 里的文件名 |
| `subtitles` | 短片 | 字幕，单位是秒。可不填 |
| `unskippable` | 短片 | 设为 `true` 后不能跳过，默认可以跳过 |
| `choices` | 短片 | 分支选项，短片播完后显示，最后一帧停在画面上。选项不能跳过，跳过短片会直接进入选择。键盘只支持 1 到 4 |
| `setFlag` | 选项 | 记录这次选择，后面的节点可以用 `routes` 判断。也可以在关卡代码里用 `GameSession.Instance.SetFlag` 记录 |
| `promptZh`、`promptEn` | 短片 | 选项上方的问题 |
| `choiceSeconds` | 短片 | 选择时限（秒），到时自动选 `defaultChoice`。0 表示一直等待 |
| `defaultChoice` | 短片 | 超时的默认选项，从 0 开始 |

## 短片规格

- **推荐格式：** MP4，H.264 视频（yuv420p，标注 BT.709），AAC 音频，1920×1080，16:9。其他比例会自动加黑边。
- AI 工具生成的视频，先用下面的命令转码，可以避免颜色偏差和解码问题：

```sh
ffmpeg -i 原始.mp4 -vf "scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2,scale=out_color_matrix=bt709,format=yuv420p" \
  -c:v libx264 -crf 20 -preset slow -x264-params colorprim=bt709:transfer=bt709:colormatrix=bt709 \
  -c:a aac -b:a 192k -movflags +faststart film_xxx.mp4
```

- 视频放在 `StreamingAssets`，不经过 Unity 导入，替换后立刻生效，打包后的游戏也可以直接替换文件。
- 这种读取方式适用于桌面平台，不支持 WebGL 和 Android。

## Git 与大文件

`Films/` 里现在是 4 段很小的占位视频。**加入正式短片前要先配置 Git LFS**，因为 GitHub 拒收超过 100 MB 的单个文件，普通提交的视频也会让仓库一直变大：

```sh
git lfs install
git lfs track "Assets/StreamingAssets/Films/*.mp4"
git add .gitattributes
```

## 换关卡

1. 新建场景，保存到 `Assets/_Project/Scenes/`，按 [FOUNDATION.md](FOUNDATION.md) 第 3 节搭好。
2. 关卡规则脚本放进 `Assets/_Project/Scripts/Levels/`。
3. 在 `story.json` 里修改或新增 `level` 节点。
4. 运行 `Mystic Circus > Sync Build Settings From Story`，再运行 `Mystic Circus > Validate Story`。

**关卡如何结束：** 有出口门的关卡，门打开时会自动进入下一节点。门的 `NextScene` 必须填写，否则不会被当作出口，但实际去向以剧情文件为准。没有门的关卡，在结束时调用 `StoryDirector.Advance()`。

**让关卡影响后面的短片：** 在关卡代码里调用 `GameSession.Instance.SetFlag("id")`，再在节点上写 `routes`。

## 调试

- 打开 `Boot` 场景运行，从头开始。
- 直接打开任意关卡场景运行，剧情会从这一关接上。
- 运行中按住 F1 可跳到任意节点，包括短片。
- 短片中按空格或回车跳过。
