# Spellbook

魔兽世界主题的脚本/程序启动器。把散落各处的脚本、程序、文件夹和网址收进一本"法术书",以技能按钮磁贴的形式分组管理、一键运行。

提供两个版本,**共享同一份数据**(`%APPDATA%\Spellbook\items.json`),可并存交替使用:

| 版本 | 目录 | 技术栈 | 定位 |
|---|---|---|---|
| 轻量版 | `src/` | WPF / .NET 8 | 秒开、低占用的日常工具 |
| 游戏版 Spellbook Arcane | `src_unity/` | Unity 6 | 开场动画、翻页过场、粒子施法、全程音效的游戏级体验 |

## 功能

- 手动添加脚本快捷方式:路径、参数、备注、分组
- 163 个手绘风魔兽主题图标(武器/魔法/技能),新建/编辑时仿"新建宏命令"从图标网格任选
- 磁贴点击按目标类型分发:`.ps1` 在新控制台窗口运行(`powershell.exe -ExecutionPolicy Bypass -File …`),退出码异步回填状态栏;文件夹在资源管理器打开;其他程序/文档(如 `chrome.exe` + 网址参数、Excel 文件)带参数直接启动;`http(s)` 网址用默认浏览器打开
- 分组展示,组内拖拽排序;右键磁贴可编辑 / 删除 / 移动分组
- `Ctrl+K` 聚焦搜索框,按名称实时过滤
- 脚本路径失效时磁贴显示 ⚠ 标记,点击给出提示,不崩溃
- 数据持久化在 `%APPDATA%\Spellbook\items.json`,增删改立即保存

## 编译与运行

前提条件:

- Windows 系统(WPF 程序,只能在 Windows 上编译运行)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)(选 SDK,不是只装 Runtime);装完在终端跑 `dotnet --version` 确认输出 8.x

在源码根目录(有 `src` 和 `tests` 的那层)打开终端:

```powershell
# 直接编译并运行
dotnet run --project src/Spellbook

# 或者只编译(Release 版)
dotnet build src/Spellbook -c Release
# 产物在 src/Spellbook/bin/Release/net8.0-windows/Spellbook.exe

# 跑测试(可选)
dotnet test
```

第一次编译会自动从 NuGet 还原依赖,需要联网;本项目没有第三方包,基本秒过。

### 发布成可分发的版本

```powershell
dotnet publish src/Spellbook -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

产物在 `src/Spellbook/bin/Release/net8.0-windows/win-x64/publish/`,是一个自带运行时的单文件 `Spellbook.exe`,目标机器不用装 .NET 也能直接跑。若目标机器已装 .NET 8 桌面运行时,把 `--self-contained true` 改成 `false`,文件会小很多。

条目数据保存在 `%APPDATA%\Spellbook\items.json`,换机器时想保留配置就把它一起拷过去。

## 游戏版(src_unity)的编译与运行

需要 Unity 6(6000.0.81f1 LTS)。**唯一的手动步骤**:Unity 免费 Personal 许可证
必须登录 Unity 账号激活——打开 Unity Hub 登录一次(没有账号的话免费注册),
之后一切(测试/构建/运行)都可以命令行完成。

1. 打开 Unity Hub 并登录(Hub 会自动发现 `C:\Unity\6000.0.81f1` 下的编辑器;
   本仓库开发机已预装,新机器在 Hub 里装 6000.0.x LTS 即可)
2. 一键验证 + 构建:`powershell -File src_unity\verify-and-build.ps1`
   (先跑 EditMode 测试,再构建到 `src_unity/Spellbook.Unity/Builds/Windows/`)
3. 或用 Hub 打开 `src_unity/Spellbook.Unity` 在编辑器里按 Play(首次打开会自动
   导入资源、生成场景与构建设置,由 `Assets/Editor/ProjectSetup.cs` 幂等完成)

界面为全代码构建(空场景 + Bootstrap),EditMode 测试覆盖核心逻辑与两版 JSON
互通性(Test Runner 中运行)。素材授权见 `src_unity/Spellbook.Unity/CREDITS.md`。

## 结构

```
src/Spellbook/          WPF 轻量版(手写 MVVM,无第三方框架)
  Models/               数据模型
  Services/             JSON 持久化、脚本运行
  ViewModels/           业务逻辑(单元测试覆盖)
  Views/                主窗口、新建/编辑对话框
  App.xaml              魔兽主题配色令牌与全局样式
tests/Spellbook.Tests/  xUnit 测试(仅纯逻辑)
src_unity/Spellbook.Unity/  Unity 6 游戏版(code-first UI)
  Assets/Scripts/Core/      纯 C# 领域逻辑(与 WPF 版一一对应)
  Assets/Scripts/Tween/     自研轻量补间动画库
  Assets/Scripts/FX/        音频管理器、粒子工厂
  Assets/Scripts/UI/        全部界面组件(代码构建,无场景资产)
  Assets/Editor/            工程自配置、资源导入规则、一键构建
  Assets/Tests/EditMode/    核心逻辑与数据互通测试
  CREDITS.md                素材来源与授权
docs/superpowers/       设计文档与实现计划
```

## 图标素材署名

- 法术类图标:[Painterly Spell Icons](https://opengameart.org/content/painterly-spell-icons-part-1) — J. W. Bjerk (eleazzaar),[www.jwbjerk.com/art](http://www.jwbjerk.com/art),CC-BY 3.0
- 武器与物品图标:[Fantasy Icon Pack](https://opengameart.org/content/fantasy-icon-pack-by-ravenmore-0) — Ravenmore ([dycha.net](http://dycha.net)),CC-BY 3.0
