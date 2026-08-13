using Spellbook.ViewModels;

namespace Spellbook.Tests;

public class ViewModelBaseTests
{
    private class TestVm : ViewModelBase
    {
        private string _value = "";
        public string Value { get => _value; set => SetProperty(ref _value, value); }
    }

    [Fact]
    public void SetProperty_RaisesPropertyChanged_OnChange()
    {
        var vm = new TestVm();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.Value = "new";

        Assert.Equal(new[] { "Value" }, raised);
    }

    [Fact]
    public void SetProperty_NoEvent_WhenValueUnchanged()
    {
        var vm = new TestVm { Value = "same" };
        var count = 0;
        vm.PropertyChanged += (_, _) => count++;

        vm.Value = "same";

        Assert.Equal(0, count);
    }
}
