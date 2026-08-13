# Spellbook

魔兽世界主题的 PowerShell 脚本启动器(WPF / .NET 8)。把散落各处的 `.ps1` 脚本收进一本"法术书",以技能按钮磁贴的形式分组管理、一键运行。

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

## 结构

```
src/Spellbook/          WPF 应用(手写 MVVM,无第三方框架)
  Models/               数据模型
  Services/             JSON 持久化、脚本运行
  ViewModels/           业务逻辑(单元测试覆盖)
  Views/                主窗口、新建/编辑对话框
  App.xaml              魔兽主题配色令牌与全局样式
tests/Spellbook.Tests/  xUnit 测试(仅纯逻辑)
docs/superpowers/       设计文档与实现计划
```

## 图标素材署名

- 法术类图标:[Painterly Spell Icons](https://opengameart.org/content/painterly-spell-icons-part-1) — J. W. Bjerk (eleazzaar),[www.jwbjerk.com/art](http://www.jwbjerk.com/art),CC-BY 3.0
- 武器与物品图标:[Fantasy Icon Pack](https://opengameart.org/content/fantasy-icon-pack-by-ravenmore-0) — Ravenmore ([dycha.net](http://dycha.net)),CC-BY 3.0
