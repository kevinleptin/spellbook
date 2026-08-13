# Spellbook 设计文档

日期：2026-08-13
状态：已确认

## 概述

Spellbook 是一个 Windows 桌面小工具（WPF / .NET 8），用于管理和启动散落在电脑各处的 PowerShell 脚本。用户手动添加脚本快捷方式，以魔兽世界风格的"技能按钮"磁贴形式展示，点击即在新控制台窗口运行。

## 数据模型与存储

每个条目（`SpellItem`）字段：

| 字段 | 类型 | 说明 |
|------|------|------|
| Name | string | 名称，必填 |
| ScriptPath | string | ps1 文件完整路径，必填 |
| Arguments | string | 命令行参数，可为空 |
| Notes | string | 备注，可为空，多行 |
| GroupName | string | 分组名，可为空（空 = 未分组） |
| SortOrder | int | 组内排序序号 |

- 持久化位置：`%APPDATA%\Spellbook\items.json`（UTF-8，缩进 JSON）。
- 程序启动时加载；任何增删改（含拖拽排序、移动分组）后立即整体保存。
- 文件或目录不存在时自动创建；JSON 损坏时不崩溃，提示用户并以空列表启动（不覆盖原文件，直到用户做出修改）。

## 架构

单解决方案，主项目 + 测试项目，手写 MVVM（不引第三方 MVVM 框架）：

```
Spellbook.sln
├─ src/Spellbook/
│  ├─ App.xaml                 # 全局配色令牌 + 控件样式
│  ├─ Models/SpellItem.cs
│  ├─ Services/
│  │  ├─ ItemStore.cs          # items.json 读写
│  │  └─ ScriptRunner.cs       # 进程启动 + 异步退出码
│  ├─ ViewModels/
│  │  ├─ ViewModelBase.cs      # INotifyPropertyChanged 基类
│  │  ├─ RelayCommand.cs
│  │  ├─ MainViewModel.cs      # 分组集合、搜索过滤、状态栏
│  │  ├─ GroupViewModel.cs
│  │  ├─ ItemViewModel.cs
│  │  └─ EditItemViewModel.cs  # 新建/编辑对话框共用
│  └─ Views/
│     ├─ MainWindow.xaml
│     └─ EditItemDialog.xaml
└─ tests/Spellbook.Tests/      # xUnit，仅测纯逻辑
```

关键实现决策：

- **分组渲染**：外层 `ItemsControl`（分组列表，垂直）嵌套内层 `ItemsControl`（磁贴，`WrapPanel` 自动换行），不用 CollectionViewSource 分组——便于控制分组标题装饰与拖拽。
- **分组顺序**："未分组"恒在最上方；其余分组按首次创建的先后排列（即 items.json 中首次出现顺序）。
- **拖拽排序**：磁贴按下并移动超过系统阈值后调用 `DragDrop.DoDragDrop`；仅接受同组磁贴为落点，落到某磁贴上则插入其前；松开后重排该组 `SortOrder` 并保存。跨组拖拽不响应（跨组移动走右键菜单）。
- **搜索过滤**：ViewModel 层做，`ItemViewModel.IsVisible` / `GroupViewModel.IsVisible` 绑定控件 `Visibility`；按完整名称不区分大小写子串匹配；组内无匹配项时整组隐藏。
- **路径失效**：启动加载时与每次点击运行时各检查一次 `File.Exists`；失效则磁贴显示警告标记，点击运行时弹提示框，不崩溃。

## 功能需求

### 条目管理

1. 窗口右上角"＋ 新建"按钮弹出对话框：
   - 文件选择器选 `.ps1`（过滤器 `PowerShell 脚本 (*.ps1)`）
   - 名称输入框（默认填所选文件名去扩展名，可改）
   - 参数输入框
   - 备注输入框（多行）
   - 分组下拉框（`ComboBox IsEditable`，列出已有分组，可输入新分组名）
   - 确定后新磁贴出现在对应分组末尾（SortOrder = 组内最大值 + 1）
2. 右键磁贴上下文菜单：
   - **编辑**：打开同一对话框回填内容
   - **删除**：确认框后删除
   - **移动**：子菜单列出所有分组（含"未分组"），点击后移到该分组末尾
3. 同组内拖拽调整顺序，顺序持久化。

### 运行

4. 点击磁贴执行：`powershell.exe -ExecutionPolicy Bypass -File <路径> <参数>`，`UseShellExecute = true` 新控制台窗口运行，不阻塞 UI；参数原样拼接。
5. 底部状态栏显示最近一次运行的脚本名和进程退出码：进程启动后异步等待退出（`Process.WaitForExitAsync`），回填到 UI 线程；退出码非 0 整行红色。

### 显示

6. 磁贴标题最多 4 个字符，超出截断加"…"（按 `string` 字符计，中英文同算 1 个）；Tooltip 显示完整信息：完整名称 / 完整路径 + 参数 / 备注（备注为空则不显示该行）。
7. 顶部搜索框，`Ctrl+K` 聚焦（窗口级 `KeyBinding`），实时按完整名称过滤，不区分大小写。

## 视觉规范（魔兽世界主题）

### 配色令牌

全部在 `App.xaml` 定义为全局资源（`SolidColorBrush` / `Color`），所有控件引用令牌，不写死颜色：

| 令牌 | 值 | 用途 |
|------|-----|------|
| BackgroundBrush | `#0E0B08` | 窗口背景（近黑暖褐） |
| PanelBrush | `#1A140C` | 面板底 |
| GoldLightBrush | `#F0C860` | 金框亮部 |
| GoldBrush | `#C9A44A` | 金框主色 |
| GoldDarkBrush | `#6B4413` | 金框暗部 |
| TextBrush | `#E8D8B0` | 正文字（羊皮纸色） |
| TextDimBrush | `#8A7A5A` | 次要字 |
| EpicBrush | `#A335EE` | 史诗紫（点缀） |
| RareBrush | `#0070DD` | 稀有蓝（点缀） |
| UncommonBrush | `#1EFF00` | 优秀绿（Tooltip 备注） |
| LegendaryBrush | `#FF8000` | 传说橙（点缀） |
| SystemYellowBrush | `#FFC125` | 状态栏 `[Spellbook]` 前缀 |
| ErrorRedBrush | `#FF4040` | 状态栏错误行 |
| TooltipBackgroundBrush | `#0A0A1E`（95% 不透明） | Tooltip 底 |

### 磁贴（技能按钮）

- 图标 46×46 圆角方块；三层嵌套 `Border` 模拟金属浮雕：外 1px 金亮部、中 2px 金主色、内 1px 金暗部。
- 图标底为深色径向渐变（中心微亮）；本版本统一使用一个内置默认图标（XAML 矢量绘制，不引外部图片）。
- 悬停：整块磁贴淡金色外发光（`DropShadowEffect`，金色、BlurRadius 12、ShadowDepth 0）。
- 按下：图标下沉 1px 并变暗。
- 路径失效时磁贴角落显示警告标记。

### Tooltip（物品提示框）

深蓝黑底 95% 不透明、1px 金边、圆角 4px；第一行名称金色加粗，第二行路径+参数灰色小字，备注优秀绿斜体。

### 分组标题（任务日志）

羊皮纸色文字，两侧细金线收边，金线两端各一个 4×4 旋转 45° 的菱形装饰（`Path`/`Rectangle`）。

### 状态栏（聊天框）

等宽字体（Consolas）；前缀 `[Spellbook]` 系统黄；退出码非 0 整行红色。

### "＋ 新建"按钮（游戏红按钮）

暗红底 `#8B1A1A` 渐变、金色边框、羊皮纸色文字，悬停微微提亮。

### 窗口与动效

- 默认 560×640，可调整大小，磁贴网格自动换行。
- 所有动效 ≤ 120ms；无空闲循环动画（空闲 CPU 为零）。

## 错误处理

- 脚本路径不存在：磁贴警告标记 + 运行时提示框，不崩溃。
- items.json 损坏：提示 + 空列表启动，不覆盖原文件。
- 进程启动失败（如 powershell.exe 不可用等异常）：捕获异常，状态栏红色显示错误。

## 测试策略

- xUnit 测试项目，仅覆盖纯逻辑（不测 UI）：
  - `ItemStore`：读写往返、文件不存在自动创建、损坏 JSON 不抛异常。
  - 排序逻辑：新增条目排组尾、拖拽重排后 SortOrder 连续、移动分组排目标组尾。
  - 搜索过滤：不区分大小写、按完整名称匹配、空关键字显示全部。
  - 标题截断：≤4 字符原样、>4 字符截断加"…"。
- UI 与视觉效果手动验证。

## 工程要求

- .NET 8 WPF，单解决方案；手写 `INotifyPropertyChanged`，不引第三方 MVVM 框架。
- 关键部分带中文注释。
- 交付完整可编译的所有文件与 `dotnet` 创建/运行步骤。
