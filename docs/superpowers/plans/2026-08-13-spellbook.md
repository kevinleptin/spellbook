# Spellbook Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **执行说明:** 本计划由同一会话内联执行(executing-plans),执行者已持有完整 spec 上下文(docs/superpowers/specs/2026-08-13-spellbook-design.md)。视图 XAML 的完整标记在执行时依据 spec 视觉规范落盘,计划内给出结构与关键模板片段。

**Goal:** 交付可编译运行的 Spellbook —— WoW 主题的 PowerShell 脚本启动器(WPF / .NET 8)。

**Architecture:** 手写 MVVM;`ItemStore` 负责 JSON 持久化,`MainViewModel` 持有分组/条目集合与全部业务逻辑(可脱离 UI 测试),Views 仅做绑定与拖拽/菜单事件转发。

**Tech Stack:** .NET 8 WPF、xUnit、System.Text.Json。无第三方 MVVM 框架。

## Global Constraints

- TargetFramework: `net8.0-windows`,`UseWPF=true`,Nullable enable。
- 不引入任何第三方运行时依赖;测试仅用 xUnit 模板自带包。
- 所有颜色经 App.xaml 令牌(Brush/Color 资源)引用,控件不写死色值(令牌表见 spec)。
- 关键代码带中文注释;动效 ≤120ms,无空闲动画。
- 数据文件:`%APPDATA%\Spellbook\items.json`;测试中 ItemStore 一律注入临时路径。
- 每个任务:先写失败测试 → 实现 → 测试通过 → commit。UI 任务以 `dotnet build` + 手动冒烟代替单测。

---

### Task 1: 脚手架

**Files:**
- Create: `Spellbook.sln`, `src/Spellbook/Spellbook.csproj`(WPF 模板), `tests/Spellbook.Tests/Spellbook.Tests.csproj`(xunit 模板), `.gitignore`

**Steps:**
- [ ] `dotnet --list-sdks` 确认可构建 net8.0-windows
- [ ] `dotnet new sln -n Spellbook`;`dotnet new wpf -o src/Spellbook -n Spellbook`;`dotnet new xunit -o tests/Spellbook.Tests -n Spellbook.Tests`
- [ ] 两个 csproj 的 TFM 均改为 `net8.0-windows`(测试项目需加 `<UseWPF>true</UseWPF>` 以引用 WPF 程序集);测试项目 `dotnet add reference` 主项目;`dotnet sln add` 两项目
- [ ] `dotnet new gitignore`
- [ ] 验证: `dotnet build` 成功、`dotnet test` 通过(模板空测试)
- [ ] Commit: `chore: scaffold solution`

### Task 2: SpellItem + ItemStore

**Files:**
- Create: `src/Spellbook/Models/SpellItem.cs`, `src/Spellbook/Services/ItemStore.cs`
- Test: `tests/Spellbook.Tests/ItemStoreTests.cs`

**Interfaces (Produces):**
```csharp
public class SpellItem { public string Name, ScriptPath, Arguments, Notes, GroupName; public int SortOrder; } // 均 string 默认 ""
public class ItemStore {
    public ItemStore(string? filePath = null);      // null → %APPDATA%\Spellbook\items.json
    public bool LoadFailed { get; }                  // JSON 损坏时 true
    public List<SpellItem> Load();                   // 文件不存在 → 创建空文件并返回空表;损坏 → 空表+LoadFailed
    public void Save(List<SpellItem> items);         // 自动建目录,缩进 UTF-8 JSON
}
```

**Steps:**
- [ ] 写测试(临时目录注入路径): `SaveThenLoad_Roundtrips`(全字段往返)、`Load_MissingFile_ReturnsEmptyAndCreatesFile`、`Load_CorruptedJson_ReturnsEmptyAndSetsLoadFailed`(写入 `"{not json"` 后 Load)、`Save_CreatesDirectory`
- [ ] `dotnet test` 确认编译失败/测试失败
- [ ] 实现两类;`dotnet test` 全绿
- [ ] Commit: `feat: data model and json persistence`

### Task 3: ViewModelBase + RelayCommand

**Files:**
- Create: `src/Spellbook/ViewModels/ViewModelBase.cs`, `src/Spellbook/ViewModels/RelayCommand.cs`
- Test: `tests/Spellbook.Tests/ViewModelBaseTests.cs`

**Interfaces (Produces):**
```csharp
public abstract class ViewModelBase : INotifyPropertyChanged {
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null); // 值变才通知,返回是否变化
}
public class RelayCommand : ICommand { public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null); }
```

**Steps:**
- [ ] 测试: `SetProperty_RaisesPropertyChanged_OnChange`、`SetProperty_NoEvent_WhenValueUnchanged`
- [ ] 红 → 实现 → 绿
- [ ] Commit: `feat: mvvm primitives`

### Task 4: ItemViewModel

**Files:**
- Create: `src/Spellbook/ViewModels/ItemViewModel.cs`
- Test: `tests/Spellbook.Tests/ItemViewModelTests.cs`

**Interfaces (Produces):**
```csharp
public class ItemViewModel : ViewModelBase {
    public ItemViewModel(SpellItem model); public SpellItem Model { get; }
    public string Name/ScriptPath/Arguments/Notes/GroupName { get; }   // 读模型
    public string DisplayName { get; }        // >4 字符截断为前 4 字符 + "…"
    public string TooltipPathLine { get; }    // 路径 + (参数非空 ? " " + 参数 : "")
    public bool HasNotes { get; }
    public bool IsVisible { get; set; }       // 搜索过滤用
    public bool PathMissing { get; set; }     // 路径失效标记
    public void RefreshPathMissing();          // File.Exists 检查
    public void RaiseAllChanged();             // 编辑回填后整体刷新
}
```

**Steps:**
- [ ] 测试: `DisplayName_FourCharsOrLess_Unchanged`("清理" / "abcd")、`DisplayName_OverFourChars_Truncated`("清理临时文件"→"清理临时…"、"cleanup"→"clea…")、`TooltipPathLine_WithArgs_Appended`、`TooltipPathLine_EmptyArgs_PathOnly`、`HasNotes_FalseWhenEmpty`
- [ ] 红 → 实现 → 绿
- [ ] Commit: `feat: item viewmodel with display truncation`

### Task 5: GroupViewModel + MainViewModel 核心逻辑

**Files:**
- Create: `src/Spellbook/ViewModels/GroupViewModel.cs`, `src/Spellbook/ViewModels/MainViewModel.cs`
- Test: `tests/Spellbook.Tests/MainViewModelTests.cs`

**Interfaces (Produces):**
```csharp
public class GroupViewModel : ViewModelBase {
    public string Key { get; }            // 原始分组名,"" = 未分组
    public string DisplayName { get; }    // Key 为空 → "未分组"
    public ObservableCollection<ItemViewModel> Items { get; }
    public bool IsVisible { get; set; }
}
public class MainViewModel : ViewModelBase {
    public MainViewModel(ItemStore store);                 // 构造即 Load + 建组
    public ObservableCollection<GroupViewModel> Groups { get; }
    public string SearchText { get; set; }                 // setter 实时过滤(完整名称,忽略大小写;空组隐藏)
    public IReadOnlyList<GroupViewModel> AllGroups { get; }// 供“移动”菜单/编辑对话框
    public IEnumerable<string> ExistingGroupNames { get; } // 非空分组名去重
    public string StatusText { get; set; } public bool IsStatusError { get; set; }
    public bool StoreLoadFailed { get; }
    public void AddItem(SpellItem item);                   // 排对应组末尾(SortOrder=组内max+1),保存
    public void ApplyEdit(ItemViewModel item);             // 编辑后调用:若换组则排新组末尾;保存+刷新
    public void DeleteItem(ItemViewModel item);
    public void MoveItemToGroup(ItemViewModel item, string groupKey); // 排目标组末尾
    public void ReorderBefore(ItemViewModel source, ItemViewModel target); // 仅同组;插到 target 前,组内 SortOrder 重排为 0..n-1
    public async Task<bool> RunItemAsync(ItemViewModel item); // false=路径不存在(置 PathMissing);否则跑脚本、异步回填状态栏
}
```
分组顺序规则:未分组(Key="")恒最上且仅在非空时显示;其余按首次出现顺序(载入时按 json 中首次出现;新组追加尾部;组清空即消失,重建按新出现算)。所有增删改后调用 `Save`(保存归一化后的主列表:按组块+SortOrder 排列)。

**Steps:**
- [ ] 测试(ItemStore 注入临时路径): `AddItem_AppendsToGroupEnd_AndPersists`、`Groups_UngroupedFirst_ThenFirstAppearance`、`Ungrouped_HiddenWhenEmpty`、`DeleteItem_RemovesAndPersists`、`MoveItem_AppendsToTargetGroupEnd`、`ReorderBefore_InsertsAndRenumbers`、`Filter_CaseInsensitive_MatchesFullName`(搜 "clea" 命中 "Cleanup",搜显示名 "clea…" 的 "…" 不参与)、`Filter_HidesGroupWithNoMatch`、`Filter_Empty_ShowsAll`
- [ ] 红 → 实现(RunItemAsync 留到 Task 6,先占位抛 NotImplemented 不测) → 绿
- [ ] Commit: `feat: main viewmodel grouping, ordering, filtering`

### Task 6: ScriptRunner + RunItemAsync

**Files:**
- Create: `src/Spellbook/Services/ScriptRunner.cs`
- Modify: `src/Spellbook/ViewModels/MainViewModel.cs`(实现 RunItemAsync)
- Test: `tests/Spellbook.Tests/ScriptRunnerTests.cs`

**Interfaces (Produces):**
```csharp
public static class ScriptRunner {
    public static string BuildArguments(string scriptPath, string args); // -ExecutionPolicy Bypass -File "<路径>"[ <参数原样>]
    public static async Task<int> RunAsync(string scriptPath, string args); // UseShellExecute=true 新控制台;返回退出码
}
```
RunItemAsync:`File.Exists` 失败 → `PathMissing=true`、返回 false;成功 → 状态置 `"<名> 运行中…"`,`await RunAsync` 后置 `"<名> 退出码 <code>"`、`IsStatusError = code != 0`(异常捕获 → 红色错误文本)。

**Steps:**
- [ ] 测试: `BuildArguments_QuotesPath`、`BuildArguments_AppendsArgsVerbatim`("-a 1 --flag" 原样拼接)、`BuildArguments_EmptyArgs_NoTrailingSpace`
- [ ] 红 → 实现 → 绿
- [ ] Commit: `feat: script runner with async exit code`

### Task 7: App.xaml 主题资源

**Files:**
- Modify: `src/Spellbook/App.xaml`(全部令牌 + 共享样式), `src/Spellbook/App.xaml.cs`(OnStartup 手动建 store/vm/窗口,LoadFailed 弹提示;移除 StartupUri)

**内容要点(完整标记执行时落盘):**
- Color + SolidColorBrush 双份令牌(Color 供 DropShadowEffect/渐变引用),名称与 spec 令牌表一致;TooltipBackgroundBrush Opacity 0.95;红按钮渐变 `RedButtonBackground`/`RedButtonHoverBackground`(基于 #8B1A1A)
- `GameToolTipStyle`(ToolTip 重模板:金边 1px、圆角 4、深蓝黑底)
- `RedButtonStyle`(金边、暗红渐变、羊皮纸字,hover 提亮、pressed 变暗)
- `DarkTextBoxStyle`(面板底、金暗边、羊皮纸字)供搜索框/对话框用
- `BooleanToVisibilityConverter` 资源

**Steps:**
- [ ] 落盘 → `dotnet build` 通过 → Commit: `feat: wow theme tokens and shared styles`

### Task 8: EditItemViewModel + EditItemDialog

**Files:**
- Create: `src/Spellbook/ViewModels/EditItemViewModel.cs`, `src/Spellbook/Views/EditItemDialog.xaml(+.cs)`
- Test: `tests/Spellbook.Tests/EditItemViewModelTests.cs`

**Interfaces (Produces):**
```csharp
public class EditItemViewModel : ViewModelBase {
    public EditItemViewModel(IEnumerable<string> existingGroups, SpellItem? editing = null); // editing 非空 → 回填
    public string Name/ScriptPath/Arguments/Notes/GroupName { get; set; }
    public List<string> ExistingGroups { get; }
    public void SetScriptPath(string path); // Name 为空或仍等于上次自动名 → 自动填文件名(无扩展名)
    public bool CanConfirm { get; }         // Name 与 ScriptPath 非空
    public SpellItem ToModel(int sortOrder);
}
```
对话框:文件选择 `OpenFileDialog`(过滤器 `PowerShell 脚本 (*.ps1)|*.ps1`)、名称/参数单行、备注多行(AcceptsReturn)、分组 `ComboBox IsEditable`;确定/取消;`ShowDialog()`。

**Steps:**
- [ ] 测试: `SetScriptPath_FillsNameFromFileName`、`SetScriptPath_KeepsUserEditedName`(先手改 Name 再换文件)、`SetScriptPath_RefillsWhenNameStillAuto`(连续换两次文件名跟随)、`CanConfirm_RequiresNameAndPath`
- [ ] 红 → 实现 VM → 绿 → 落盘 XAML → build
- [ ] Commit: `feat: add/edit item dialog`

### Task 9: MainWindow

**Files:**
- Create: `src/Spellbook/Views/MainWindow.xaml(+.cs)`(模板生成的根目录 MainWindow 移到 Views 并改命名空间)

**布局(DockPanel,窗口 560×640 可缩放):**
- Top:搜索框(DarkTextBoxStyle,空时显示提示"搜索 (Ctrl+K)")+ 右侧"＋ 新建"(RedButtonStyle)
- Bottom:状态栏 Border(PanelBrush、上金暗边、Consolas):`[Spellbook]` 前缀 TextBlock(SystemYellow,IsStatusError→ErrorRed)+ 内容 TextBlock(TextBrush,IsStatusError→ErrorRed)
- Center:ScrollViewer → ItemsControl(Groups):组模板 = 任务日志标题(◆─金线─◆ 文字 ◆─金线─◆,金线 GoldDark 1px、菱形 4×4 旋 45°)+ 内层 ItemsControl(WrapPanel)磁贴;组容器 AllowDrop(同组拖到空白 → 移组尾)

**磁贴模板(核心片段):**
```xml
<Grid x:Name="IconBlock" Width="46" Height="46">
  <Border BorderBrush="{StaticResource GoldLightBrush}" BorderThickness="1" CornerRadius="7">
    <Border BorderBrush="{StaticResource GoldBrush}" BorderThickness="2" CornerRadius="6">
      <Border BorderBrush="{StaticResource GoldDarkBrush}" BorderThickness="1" CornerRadius="5">
        <Border.Background><RadialGradientBrush>中心微亮深底(令牌色)</RadialGradientBrush></Border.Background>
        <Grid><Path 内置书本图标 Fill=GoldBrush/><TextBlock "⚠" 右上角 Visibility=PathMissing/></Grid>
      </Border></Border></Border>
</Grid>
<!-- 下方 DisplayName;Triggers: IsMouseOver→IconBlock.Effect=DropShadowEffect(GoldLightColor,Blur12,Depth0);
     IsPressed→IconBlock.RenderTransform=TranslateTransform Y=1 + Opacity 0.75 -->
```
Tooltip 用 GameToolTipStyle:名称(金粗)/路径+参数(TextDim 小字)/备注(UncommonBrush 斜体,HasNotes 才显示)。

**code-behind 职责(全部转发到 VM):**
- `PreviewKeyDown`:Ctrl+K → SearchBox.Focus()
- 磁贴 Click → `RunItemAsync`,返回 false → MessageBox "脚本文件不存在"
- 右键菜单:编辑(开对话框回填,确定 → ApplyEdit)、删除(MessageBox 确认 → DeleteItem)、移动(SubmenuOpened 动态生成 AllGroups 菜单项 → MoveItemToGroup)
- 拖拽:磁贴 PreviewMouseLeftButtonDown 记按点,PreviewMouseMove 超阈值 DoDragDrop(ItemViewModel);目标磁贴 DragOver 校验同组否则 Effects=None,Drop → ReorderBefore;组容器 Drop → 同组移末尾
- "＋ 新建" Click → 对话框 → AddItem

**Steps:**
- [ ] 落盘 XAML + code-behind → `dotnet build`
- [ ] `dotnet test` 全绿
- [ ] 冒烟: 启动程序,依次验证 新建/运行/退出码/搜索/Ctrl+K/右键三项/拖拽/Tooltip/路径失效标记
- [ ] Commit: `feat: main window with tiles, search, drag reorder`

### Task 10: 最终验证与交付

**Steps:**
- [ ] `dotnet build -c Release` + `dotnet test` 全绿
- [ ] 冒烟通过后在 README.md 写:项目简介 + `dotnet run --project src/Spellbook` 运行步骤与 `dotnet test`
- [ ] Commit: `docs: readme with run steps`
- [ ] 启动程序交付用户(`dotnet run --project src/Spellbook` 后台运行)
