namespace UjangKasir.Desktop.ViewModels;

public class PlaceholderViewModel(string title, string description) : PageViewModelBase
{
    public override string Title { get; } = title;
    public override string Description { get; } = description;
}
