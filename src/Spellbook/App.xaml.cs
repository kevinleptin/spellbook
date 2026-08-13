using System.Windows;
using Spellbook.Services;
using Spellbook.ViewModels;

namespace Spellbook;

/// <summary>应用入口:手动装配存储、视图模型与主窗口。</summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var store = new ItemStore();
        var viewModel = new MainViewModel(store);

        if (viewModel.StoreLoadFailed)
        {
            // 数据文件损坏:提示后以空列表启动,不覆盖原文件(直到用户做出修改)
            MessageBox.Show(
                $"数据文件已损坏,本次以空列表启动:\n{ItemStore.DefaultPath}",
                "Spellbook", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        new MainWindow { DataContext = viewModel }.Show();
    }
}
