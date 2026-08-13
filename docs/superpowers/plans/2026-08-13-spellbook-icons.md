# Spellbook v2 图标功能 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **执行说明:** 同一会话内联执行,spec 见 docs/superpowers/specs/2026-08-13-spellbook-icons-design.md。SVG 资产的具体图形在执行时创作,计划规定格式、清单与验收测试。

**Goal:** 100 个魔兽主题 SVG 图标 + 对话框图标选择器 + "所有文件"过滤 + 魔兽风右键菜单。

**Architecture:** SVG 文件为图标源(嵌入资源),SvgIconLoader 解析受限子集为 DrawingImage 缓存;IconKey 存入 SpellItem;UI 改动限于 App.xaml(菜单样式)、EditItemDialog(选择器/过滤器)、MainWindow 磁贴模板(Image)。

**Tech Stack:** 同 v1(.NET 8 WPF、xUnit),零新依赖。

## Global Constraints

- SVG 受限子集:viewBox 0 0 32 32,仅 path(d/fill/stroke/stroke-width/stroke-linecap/stroke-linejoin/opacity)。
- IconLibrary 清单与文件一一对应,Key=文件名(无扩展名),共 100 条,分 10 类(数量见 spec)。
- 空/未知 IconKey 回退 `book`。
- 每任务:红 → 绿 → commit;UI 任务以 build + 冒烟代替单测。

---

### Task 1: IconLibrary 清单 + 100 个 SVG 资产 + 预览页

**Files:**
- Create: `src/Spellbook/Services/IconLibrary.cs`(`record IconDef(string Key, string DisplayName, string Category)`;`static IReadOnlyList<IconDef> All`)
- Create: `src/Spellbook/Assets/Icons/*.svg` ×100
- Modify: `src/Spellbook/Spellbook.csproj`(`<Resource Include="Assets\Icons\*.svg"/>`)
- Create: `docs/icons-preview.html`(内联全部 SVG 按类别分组展示,深色底模拟磁贴)
- Test: `tests/Spellbook.Tests/IconAssetsTests.cs`

**Steps:**
- [ ] 测试:`Library_Has100UniqueKeys`、`EveryIconFileExistsOnDisk`(相对测试目录定位仓库路径)、`Book_IsFirstIcon`
- [ ] 红 → 写清单 + 分批创作 100 个 SVG(按类别配色)→ 绿
- [ ] 生成预览页(小脚本把 svg 内联进 html)
- [ ] Commit: `feat: 100 wow-themed svg icons with manifest`

### Task 2: SvgIconLoader

**Files:**
- Create: `src/Spellbook/Services/SvgIconLoader.cs`
- Test: `tests/Spellbook.Tests/SvgIconLoaderTests.cs`

**Interfaces (Produces):**
```csharp
public static class SvgIconLoader {
    // 从磁盘/流解析受限 SVG → 冻结的 DrawingImage;解析失败抛出(调用方容错)
    public static DrawingImage Parse(Stream svg);
    // 应用内使用:全量加载嵌入资源,失败图标跳过;结果缓存
    public static IReadOnlyDictionary<string, DrawingImage> LoadAll();
    public static DrawingImage Get(string iconKey); // 空/未知 → book
}
```

**Steps:**
- [ ] 测试(文件流解析,不依赖 WPF Application):`Parse_AllHundredIcons_Succeeds`(遍历资产目录逐个 Parse,断言 Drawing 非空)、`Parse_PathCount_Matches`(样例)、`Parse_InvalidSvg_Throws`、几何/颜色合法性(`Geometry.Parse`、fill 非法时抛)
- [ ] 红 → 实现(XDocument 解析;fill="none" → 无刷;stroke 圆角端点映射 PenLineCap/Join)→ 绿
- [ ] Commit: `feat: constrained svg parser to DrawingImage`

### Task 3: IconKey 数据链路

**Files:**
- Modify: `src/Spellbook/Models/SpellItem.cs`(+`IconKey` 默认 ""), `src/Spellbook/ViewModels/ItemViewModel.cs`(+`IconImage`,RaiseAllChanged 时刷新), `src/Spellbook/ViewModels/EditItemViewModel.cs`(+`IconKey`,回填/ToModel,新建默认 "book")
- Test: 扩展 `ItemStoreTests`(IconKey 往返)、`EditItemViewModelTests`(默认 book、编辑回填、ToModel 携带)

**Steps:**
- [ ] 红 → 实现 → 绿
- [ ] Commit: `feat: icon key persistence and viewmodel plumbing`

### Task 4: 对话框图标选择器 + 文件过滤器

**Files:**
- Modify: `src/Spellbook/Views/EditItemDialog.xaml(+.cs)`:表单加"图标"行(ListBox+WrapPanel+滚动,ItemsSource=IconLibrary.All,ItemTemplate=24×24 Image+Tooltip 中文名,SelectedValuePath=Key 绑定 IconKey,选中金框高亮);OpenFileDialog Filter 加 `所有文件 (*.*)|*.*`

**Steps:**
- [ ] 落盘 → build → Commit: `feat: icon picker grid and all-files filter`

### Task 5: 磁贴渲染 + 魔兽风上下文菜单

**Files:**
- Modify: `src/Spellbook/Views/MainWindow.xaml`(磁贴内联 Path → Image 绑定 IconImage)
- Modify: `src/Spellbook/App.xaml`(隐式样式:ContextMenu/MenuItem/Separator 魔兽化,含子菜单箭头与弹层、禁用态)

**Steps:**
- [ ] 落盘 → build + `dotnet test` 全绿
- [ ] Commit: `feat: tile icon rendering and wow-styled context menu`

### Task 6: 最终验证与交付

**Steps:**
- [ ] Release build + 全测试
- [ ] UIA 冒烟:预置样例数据启动,验证磁贴/菜单/选择器元素存在;完成后恢复数据
- [ ] 停掉旧进程,合并 main,重启新版程序
- [ ] Commit/merge per finishing-a-development-branch
