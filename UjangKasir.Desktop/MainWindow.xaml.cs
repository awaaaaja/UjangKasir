using System.Windows;
using UjangKasir.Desktop.ViewModels;

namespace UjangKasir.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
