# 扑克牌《叫主》Unity 实现

一个四人单机扑克牌练习项目：一名玩家对战三名 AI。每轮经过准备、发牌、叫分、确定庄家、选择主花色与弃牌、12 墩出牌、结算，然后进入下一轮。

## 玩法展示 Demo

- [观看 / 下载玩法演示（不含控制台输出）](https://github.com/Alpha-3C/Poker-DZ/releases/download/demo-2026-10-19/gameplay-demo.mp4)
- [观看 / 下载玩法演示（含控制台输出）](https://github.com/Alpha-3C/Poker-DZ/releases/download/demo-2026-10-19/gameplay-demo-with-console.mp4)：Game 视图左上角实时显示轮次、叫分和胜负等日志。

两段视频均为 Unity 编辑器 Game 视图的 Play 模式录制，也可在 [玩法展示 Release 页面](https://github.com/Alpha-3C/Poker-DZ/releases/tag/demo-2026-10-19)找到。

## 运行方式

1. 使用 **Unity 2022.3.62f1c1** 打开本目录。
2. 打开 `Assets/Scenes/SampleScene.unity` 并运行。

项目使用 Unity UI、TextMesh Pro 和 Universal Render Pipeline 等包，依赖记录在 `Packages/manifest.json`。

## 项目文档

- [项目学习笔记](docs/项目学习笔记.md)：管理器组织、单例、状态与协程、事件、牌力规则、AI 接口，以及当前实现边界。
- [项目结构整理](docs/项目结构整理.md)：目录和场景对象、类职责、数据归属、运行时序与扩展入口。
- [《叫主》玩法规则](docs/扑克-叫主.md)：完整玩法与牌力比较规则。

## 在编辑器中录制演示

项目使用 Unity Recorder 4.0.1 录制 Game 视图，无需打包游戏。在 `SampleScene` 的 Hierarchy 中选中 `GameManager`，在 Inspector 的 `Play Mode Recording Toggle` 组件中勾选 `Record On Play`，然后点击 Play。退出 Play 时录制自动结束，MP4 文件保存在项目根目录的 `Record Videos` 文件夹。录制分辨率为 1920×1080、帧率为 30 fps；每次录制使用带时间戳的新文件名。录制文件已被 Git 忽略。

要关闭自动录制，在开始 Play 前取消勾选该选项并保存场景。

运行时，Game 视图左上角会显示可自动滚动的日志面板，实时展示最近 80 条 Unity 控制台消息，包括轮次、叫分与胜负信息。该面板不会拦截游戏点击；可在 `GameManager` 的 `Game Console Overlay` 组件中调整面板大小与保留条数。

## 玩法概览

- 使用含大小王的 54 张牌，四名玩家各发 12 张，剩余 6 张为底牌。
- 叫分范围为 60～100 分，最高叫分者成为庄家，获得底牌后指定主花色并弃掉 6 张。
- 每轮进行 12 墩。后手有首出花色时必须跟牌；2 和 Joker 按主牌处理。
- 闲家所得分牌为 5、10、K，分别计 5、10、10 分。庄家叫分与闲家得分之和超过 100，或闲家赢得最后一墩时，闲家获胜。

完整规则见 [玩法文档](docs/扑克-叫主.md)。

## 实现要点

- `GameFlowController` 以协程串联一轮游戏的各个阶段；`GameStateMachine` 广播状态变化。
- 叫分、底牌选择、出牌、结算由独立的阶段管理脚本负责。
- `CardComparer` 分别实现手牌排序和墩内比较；出牌阶段执行跟牌检查与胜者判定。
- `IAIPlayer` 定义统一决策接口，项目中有基础和进阶两套 AI 策略，当前按座位分配。

## 当前状态

基本玩法、状态机、游戏流程、牌力比较和排序流程已由作者手动验证。项目仍是个人练习作品；界面展示、AI 策略及自动化测试可继续完善。

素材许可与来源在公开仓库前仍需核对。
