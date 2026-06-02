using CommunityToolkit.Mvvm.ComponentModel;

namespace UjangKasir.Desktop.ViewModels;

public partial class NavigationItemViewModel(string key, string title) : ObservableObject
{
    public string Key { get; } = key;
    public string Title { get; } = title;

    [ObservableProperty]
    private bool isSelected;
}
