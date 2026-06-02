using CommunityToolkit.Mvvm.ComponentModel;

namespace UjangKasir.Desktop.ViewModels;

public abstract class PageViewModelBase : ObservableObject
{
    public abstract string Title { get; }
    public abstract string Description { get; }
}
