# Spellbook

魔兽世界主题的 PowerShell 脚本启动器(WPF / .NET 8)。把散落各处的 `.ps1` 脚本收进一本"法术书",以技能按钮磁贴的形式分组管理、一键运行。

## 功能

- 手动添加脚本快捷方式:路径、参数、备注、分组
- 100 个魔兽主题矢量图标(武器/魔法/技能,按系别配色),新建/编辑时仿"新建宏命令"从图标网格任选
- 磁贴点击即在新控制台窗口运行(`powershell.exe -ExecutionPolicy Bypass -File …`),退出码异步回填状态栏
- 分组展示,组内拖拽排序;右键磁贴可编辑 / 删除 / 移动分组
- `Ctrl+K` 聚焦搜索框,按名称实时过滤
- 脚本路径失效时磁贴显示 ⚠ 标记,点击给出提示,不崩溃
- 数据持久化在 `%APPDATA%\Spellbook\items.json`,增删改立即保存

## 运行

需要 .NET 8(Windows Desktop Runtime)。

```powershell
# 运行
dotnet run --project src/Spellbook

# 测试
dotnet test
```

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
