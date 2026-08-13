using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Spellbook.Services;
using Spellbook.ViewModels;

namespace Spellbook.Views;

/// <summary>主窗口:仅做事件转发与对话框编排,业务逻辑在 MainViewModel。</summary>
public partial class MainWindow : Window
{
    // 拖拽起点与拖拽对象(超过系统阈值才启动拖拽,避免误触点击)
    private Point _dragStart;
    private ItemViewModel? _dragCandidate;

    public MainWindow()
    {
        InitializeComponent();
        TitleIcon.Source = IconLoader.Get("book");
        // 最大化/还原时切换按钮字形(Segoe MDL2:E922 最大化,E923 还原)
        StateChanged += (_, _) =>
            MaximizeButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
    }

    private MainViewModel Vm => (MainViewModel)DataContext;

    // ---------- 标题栏窗口控制 ----------

    private void Minimize_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
        else SystemCommands.MaximizeWindow(this);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);

    // ---------- 快捷键 ----------

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
    }

    // ---------- 新建 / 编辑 / 删除 / 移动 ----------

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new EditItemDialog(new EditItemViewModel(Vm.ExistingGroupNames), "添加脚本")
        {
            Owner = this,
        };
        if (dialog.ShowDialog() == true)
        {
            Vm.AddItem(dialog.ViewModel.ToModel(0)); // SortOrder 由 AddItem 重排为组尾
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromMenu(sender) is not { } item) return;

        var dialog = new EditItemDialog(
            new EditItemViewModel(Vm.ExistingGroupNames, item.Model), "编辑脚本")
        {
            Owner = this,
        };
        if (dialog.ShowDialog() == true)
        {
            // 把对话框字段拷回原模型,再由 VM 处理换组/保存/刷新
            var edited = dialog.ViewModel.ToModel(item.Model.SortOrder);
            item.Model.Name = edited.Name;
            item.Model.ScriptPath = edited.ScriptPath;
            item.Model.Arguments = edited.Arguments;
            item.Model.Notes = edited.Notes;
            item.Model.GroupName = edited.GroupName;
            Vm.ApplyEdit(item);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (ItemFromMenu(sender) is not { } item) return;

        var answer = MessageBox.Show(
            $"确定删除“{item.Name}”吗?", "Spellbook",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Yes) Vm.DeleteItem(item);
    }

    /// <summary>“移动”子菜单打开时动态生成分组列表(未分组 + 全部已有分组)。</summary>
    private void Move_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menu || ItemFromMenu(sender) is not { } item) return;

        menu.Items.Clear();
        var targets = new List<(string Display, string Key)> { ("未分组", "") };
        targets.AddRange(Vm.ExistingGroupNames.Select(name => (name, name)));

        foreach (var (display, key) in targets)
        {
            var child = new MenuItem { Header = display, IsEnabled = key != item.GroupName };
            child.Click += (_, _) => Vm.MoveItemToGroup(item, key);
            menu.Items.Add(child);
        }
    }

    // ---------- 运行 ----------

    private async void Tile_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ItemViewModel item) return;

        var started = await Vm.RunItemAsync(item);
        if (!started)
        {
            MessageBox.Show(
                $"脚本文件不存在:\n{item.ScriptPath}", "Spellbook",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ---------- 分组排序(拖拽标题 + 右键上移/下移) ----------

    private Point _groupDragStart;
    private GroupViewModel? _groupDragCandidate;

    private static GroupViewModel? GroupFromSender(object sender) =>
        (sender as FrameworkElement)?.DataContext as GroupViewModel;

    private void GroupHeader_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _groupDragStart = e.GetPosition(this);
        var group = GroupFromSender(sender);
        // “未分组”固定置顶,不可拖动
        _groupDragCandidate = group is { Key.Length: > 0 } ? group : null;
    }

    private void GroupHeader_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_groupDragCandidate is null || e.LeftButton != MouseButtonState.Pressed) return;

        var offset = e.GetPosition(this) - _groupDragStart;
        if (Math.Abs(offset.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(offset.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var group = _groupDragCandidate;
        _groupDragCandidate = null;
        DragDrop.DoDragDrop((DependencyObject)sender,
            new DataObject(typeof(GroupViewModel), group), DragDropEffects.Move);
    }

    private static GroupViewModel? DraggedGroup(DragEventArgs e) =>
        e.Data.GetData(typeof(GroupViewModel)) as GroupViewModel;

    private void GroupHeader_DragOver(object sender, DragEventArgs e)
    {
        var source = DraggedGroup(e);
        var target = GroupFromSender(sender);
        e.Effects = source is not null && target is not null &&
                    source != target && target.Key.Length > 0
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void GroupHeader_Drop(object sender, DragEventArgs e)
    {
        var source = DraggedGroup(e);
        var target = GroupFromSender(sender);
        if (source is not null && target is not null) Vm.ReorderGroupBefore(source, target);
        e.Handled = true;
    }

    /// <summary>打开分组菜单时按边界启用/禁用上移下移。</summary>
    private void GroupMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu || menu.DataContext is not GroupViewModel group) return;
        ((MenuItem)menu.Items[0]).IsEnabled = Vm.CanMoveGroup(group, -1);
        ((MenuItem)menu.Items[1]).IsEnabled = Vm.CanMoveGroup(group, 1);
    }

    private void GroupMoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (GroupFromSender(sender) is { } group) Vm.MoveGroup(group, -1);
    }

    private void GroupMoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (GroupFromSender(sender) is { } group) Vm.MoveGroup(group, 1);
    }

    // ---------- 同组拖拽排序 ----------

    private void Tile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _dragCandidate = (sender as FrameworkElement)?.DataContext as ItemViewModel;
    }

    private void Tile_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragCandidate is null || e.LeftButton != MouseButtonState.Pressed) return;

        var offset = e.GetPosition(this) - _dragStart;
        if (Math.Abs(offset.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(offset.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var item = _dragCandidate;
        _dragCandidate = null;
        DragDrop.DoDragDrop((DependencyObject)sender,
            new DataObject(typeof(ItemViewModel), item), DragDropEffects.Move);
    }

    private static ItemViewModel? DraggedItem(DragEventArgs e) =>
        e.Data.GetData(typeof(ItemViewModel)) as ItemViewModel;

    private void Tile_DragOver(object sender, DragEventArgs e)
    {
        var source = DraggedItem(e);
        var target = (sender as FrameworkElement)?.DataContext as ItemViewModel;
        // 仅允许同分组内排序
        e.Effects = source is not null && target is not null &&
                    source != target && source.GroupName == target.GroupName
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Tile_Drop(object sender, DragEventArgs e)
    {
        var source = DraggedItem(e);
        var target = (sender as FrameworkElement)?.DataContext as ItemViewModel;
        if (source is not null && target is not null) Vm.ReorderBefore(source, target);
        e.Handled = true;
    }

    private void Group_DragOver(object sender, DragEventArgs e)
    {
        var source = DraggedItem(e);
        var group = (sender as FrameworkElement)?.DataContext as GroupViewModel;
        e.Effects = source is not null && group is not null && source.GroupName == group.Key
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    /// <summary>拖到组内空白处 = 移到该组末尾(仅同组)。</summary>
    private void Group_Drop(object sender, DragEventArgs e)
    {
        var source = DraggedItem(e);
        var group = (sender as FrameworkElement)?.DataContext as GroupViewModel;
        if (source is not null && group is not null && source.GroupName == group.Key)
        {
            Vm.MoveItemToGroup(source, group.Key);
        }
        e.Handled = true;
    }

    // ---------- 工具 ----------

    /// <summary>从上下文菜单项取出其磁贴的 ItemViewModel。</summary>
    private static ItemViewModel? ItemFromMenu(object sender) =>
        (sender as FrameworkElement)?.DataContext as ItemViewModel;
}
