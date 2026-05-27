namespace GeoTutor;

using System.Windows;
using GeoTutor.Core.ViewModels;

public partial class MainWindow : Window
{
    private BaselineViewModel? _baselineVm;

    public MainWindow()
    {
        InitializeComponent();

        // Wire baseline VM if the DB was initialised successfully.
        if (App.Database is not null && App.Skills is not null)
        {
            _baselineVm = new BaselineViewModel(App.Database, App.Skills);
            BaselinePanel.DataContext = _baselineVm;
        }
    }

    // -----------------------------------------------------------------------
    // Navigation
    // -----------------------------------------------------------------------

    private void Nav_SceneViewer_Click(object sender, RoutedEventArgs e)
    {
        Phase1Panel.Visibility         = Visibility.Visible;
        BaselinePanel.Visibility       = Visibility.Collapsed;
        SceneControls.Visibility       = Visibility.Visible;
        BaselineSideControls.Visibility = Visibility.Collapsed;
        NavSceneBtn.Style    = (System.Windows.Style)FindResource("NavButtonActive");
        NavBaselineBtn.Style = (System.Windows.Style)FindResource("NavButton");
    }

    private void Nav_Baseline_Click(object sender, RoutedEventArgs e)
    {
        Phase1Panel.Visibility          = Visibility.Collapsed;
        BaselinePanel.Visibility        = Visibility.Visible;
        SceneControls.Visibility        = Visibility.Collapsed;
        BaselineSideControls.Visibility = Visibility.Visible;
        NavSceneBtn.Style    = (System.Windows.Style)FindResource("NavButton");
        NavBaselineBtn.Style = (System.Windows.Style)FindResource("NavButtonActive");
    }
}
