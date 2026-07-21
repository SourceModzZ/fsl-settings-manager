using System.Windows;
using FSLSettingsManager.App.ViewModels;

namespace FSLSettingsManager.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.RefreshProfiles();
    }
}
