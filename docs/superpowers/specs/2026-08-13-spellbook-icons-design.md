# Spellbook v2 设计文档:图标库、图标选择与魔兽风格菜单

日期:2026-08-13
状态:已确认(用户全权委托)

## 概述

三项增强:

1. **100 个魔兽主题矢量图标**(武器/魔法/技能),SVG 文件形式,按系别配色,替代 v1 的单一内置书本图标。
2. **新建/编辑对话框**:仿魔兽"新建宏命令"的图标网格选择器,用户任选一个图标;文件选择器过滤下拉框增加"所有文件 (*.*)"。
3. **右键上下文菜单魔兽化**:深底、金边、羊皮纸文字,与整体主题一致。

## 图标库

### 资产形式

- 100 个独立 `.svg` 文件放在 `src/Spellbook/Assets/Icons/`,以 WPF `Resource` 方式嵌入程序集。
- 每个 SVG 为受限子集:`viewBox="0 0 32 32"`,仅含 `<path>` 元素(属性:`d`、`fill`、`stroke`、`stroke-width`、`stroke-linecap`、`stroke-linejoin`、`opacity`),无渐变/滤镜/文本,保证 WPF 侧解析简单可靠。
- `IconLibrary.cs` 持有清单:`IconDef(Key, DisplayName, Category)` 共 100 条,Key 与文件名一致(如 `sword.svg`)。清单顺序即选择器中的展示顺序(按类别分块)。

### 加载与渲染

- `SvgIconLoader`:启动时逐个读取嵌入资源,用 XDocument 解析受限子集,转成 `DrawingImage`(每个 path → `GeometryDrawing`,fill → Brush,stroke → Pen),全部 Freeze 后缓存到 `Dictionary<string, DrawingImage>`。
- 解析失败的图标记日志级容错:跳过该图标并回退默认书本图标,不崩溃。
- 磁贴与选择器均用 `Image` 控件显示同一份缓存 `DrawingImage`(矢量,任意缩放清晰)。

### 视觉风格

- 按系别配色,每图标 2~4 种颜色,统一 32×32 视口、线宽 1.5~2,整体亮度适配深色径向渐变底。
- 类别与数量(合计 100):武器 20、防具与装备 15、火系 8、冰系 7、雷电风暴 6、奥术 8、神圣 7、暗影死灵 8、自然 8、药剂与杂物 13。
- 调色板集中在各 SVG 内使用固定十六进制色(火系橙红 `#FF6A2A`、冰系 `#4FC1FF`、自然绿 `#3FBF3F`、奥术紫 `#B048F8`、神圣金 `#FFE08A`、暗影紫 `#8A5FC8`、钢铁灰蓝 `#9FB4C8`、金色点缀 `#C9A44A`/`#F0C860` 等)。图标属于"美术资产",不受 App.xaml UI 令牌约束,但金色点缀与主题金一致。

## 数据模型

- `SpellItem` 新增 `IconKey`(string,默认 `""`)。
- `""` 或未知 Key 一律回退渲染 `book`(法术书,图标库第 1 个,即 v1 默认图标的正式化)。旧 items.json 无该字段,反序列化后自然为 `""`,无需迁移。

## 对话框改动

- 图标选择器:表单新增"图标"行 —— `ListBox`(WrapPanel 布局、垂直滚动、高约 150px),展示全部 100 个图标(24×24),悬停 Tooltip 显示图标中文名,选中项金色高亮边框;默认选中当前 IconKey(新建时为 `book`)。
- 文件选择器过滤器改为:`PowerShell 脚本 (*.ps1)|*.ps1|所有文件 (*.*)|*.*`(仍运行于 powershell.exe,选什么文件是用户自由)。
- `EditItemViewModel` 新增 `IconKey` 属性,`ToModel` 带出;编辑时回填。

## 上下文菜单魔兽化

App.xaml 增加全局隐式样式(对所有 ContextMenu/MenuItem/Separator 生效):

- ContextMenu:TooltipBackgroundBrush 深底(95% 不透明)、1px 金边、圆角 4、内边距 4。
- MenuItem:羊皮纸色文字,悬停/高亮时 GoldDark 底 + GoldLight 文字;禁用项 TextDim;子菜单箭头用金色小三角 Path;子菜单弹层同 ContextMenu 底与金边。
- Separator:1px 金暗色细线。
- 动效仍 ≤120ms、无循环动画。

## 磁贴渲染改动

磁贴模板中原内联书本 Path 替换为 `Image`,Source 绑定 `ItemViewModel.IconImage`(由 IconKey 经 SvgIconLoader 缓存解析;属性在 IconKey 变化/编辑后刷新)。⚠ 失效标记、三层金边、发光/按下效果不变。

## 标题栏魔兽化(追加需求)

系统默认白色标题栏替换为自定义 chrome(`WindowChrome`,保留拖动/双击最大化/边缘缩放/Win 贴靠):

- 主窗口与对话框统一:36px(对话框 32px)标题栏,PanelBrush 深底、底部 1px 金暗线;左侧法术书小图标 + 金色加粗标题文字。
- 窗口控制按钮(最小化/最大化/关闭)为方形深底按钮,金色字形,悬停 GoldDark 底;关闭按钮悬停红底。对话框仅关闭按钮。
- 窗口外框 1px GoldDark;最大化时根容器加 7px 边距抵消无边框窗口溢出。

## 测试策略

- 图标资产:100 个文件全部可解析、Key 唯一、与 IconLibrary 清单一一对应、几何数据 `Geometry.Parse` 不抛异常、颜色为合法十六进制。
- `SpellItem.IconKey` 持久化往返;未知/空 Key 回退 book。
- `EditItemViewModel.IconKey` 回填与 ToModel 携带。
- 菜单样式、选择器 UI 以构建 + UIA/手动冒烟验证。

## 交付物

- 100 个 `.svg` 文件 + 清单;应用内选择、渲染全链路;魔兽风菜单。
- `docs/icons-preview.html`:单文件预览页(内联全部 SVG,按类别分组),便于在浏览器里审阅图标。
