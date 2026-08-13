using System.Windows;
using Microsoft.Win32;
using Spellbook.ViewModels;

namespace Spellbook.Views;

/// <summary>新建/编辑条目对话框(逻辑在 EditItemViewModel)。</summary>
public partial class EditItemDialog : Window
{
    public EditItemViewModel ViewModel { get; }

    public EditItemDialog(EditItemViewModel viewModel, string title)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
        Title = title;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "所有文件 (*.*)|*.*|PowerShell 脚本 (*.ps1)|*.ps1|程序 (*.exe)|*.exe",
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(this) == true)
        {
            ViewModel.SetScriptPath(dialog.FileName);
        }
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog(this) == true)
        {
            ViewModel.SetScriptPath(dialog.FolderName);
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    /// <summary>标题栏关闭按钮 = 取消。</summary>
    private void TitleClose_Click(object sender, RoutedEventArgs e) => Close();
}
