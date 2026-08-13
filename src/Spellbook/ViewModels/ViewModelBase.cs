using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Spellbook.ViewModels;

/// <summary>手写 INotifyPropertyChanged 基类(不引第三方 MVVM 框架)。</summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>值变化时赋值并通知,返回是否发生变化。</summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
