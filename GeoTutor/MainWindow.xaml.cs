namespace GeoTutor;

using System.Windows;
using System.Windows.Controls;
using GeoTutor.Core.ViewModels;

public partial class MainWindow : Window
{
    private BaselineViewModel? _baselineVm;
    private LessonViewModel?   _lessonVm;

    public MainWindow()
    {
        InitializeComponent();

        if (App.Database is not null && App.Skills is not null)
        {
            _baselineVm = new BaselineViewModel(App.Database, App.Skills);
            BaselinePanel.DataContext = _baselineVm;
        }

        if (App.LessonEngine   is not null &&
            App.DialogueEngine is not null &&
            App.SessionLogger  is not null &&
            App.Skills         is not null &&
            App.Database       is not null)
        {
            _lessonVm = new LessonViewModel(
                App.LessonEngine,
                App.DialogueEngine,
                App.SessionLogger,
                App.Skills,
                App.Database,
                App.AudioPlayer);
            LessonPanel.DataContext = _lessonVm;
        }
    }

    // -----------------------------------------------------------------------
    // Navigation helpers
    // -----------------------------------------------------------------------

    private void HideAllPanels()
    {
        Phase1Panel.Visibility         = Visibility.Collapsed;
        BaselinePanel.Visibility       = Visibility.Collapsed;
        LessonPanel.Visibility         = Visibility.Collapsed;
        SceneControls.Visibility       = Visibility.Collapsed;
        BaselineSideControls.Visibility = Visibility.Collapsed;
        Unit1SideControls.Visibility   = Visibility.Collapsed;
        Unit2SideControls.Visibility   = Visibility.Collapsed;
        Unit3SideControls.Visibility   = Visibility.Collapsed;
    }

    private void ClearNavStyles()
    {
        var normal = (System.Windows.Style)FindResource("NavButton");
        NavSceneBtn.Style    = normal;
        NavBaselineBtn.Style = normal;
        NavUnit1Btn.Style    = normal;
        NavUnit2Btn.Style    = normal;
        NavUnit3Btn.Style    = normal;
    }

    // -----------------------------------------------------------------------
    // Navigation handlers
    // -----------------------------------------------------------------------

    private void Nav_SceneViewer_Click(object sender, RoutedEventArgs e)
    {
        HideAllPanels();
        ClearNavStyles();
        Phase1Panel.Visibility   = Visibility.Visible;
        SceneControls.Visibility = Visibility.Visible;
        NavSceneBtn.Style = (System.Windows.Style)FindResource("NavButtonActive");
    }

    private void Nav_Baseline_Click(object sender, RoutedEventArgs e)
    {
        HideAllPanels();
        ClearNavStyles();
        BaselinePanel.Visibility        = Visibility.Visible;
        BaselineSideControls.Visibility = Visibility.Visible;
        NavBaselineBtn.Style = (System.Windows.Style)FindResource("NavButtonActive");
    }

    private void Nav_Unit1_Click(object sender, RoutedEventArgs e)
    {
        HideAllPanels();
        ClearNavStyles();
        LessonPanel.Visibility       = Visibility.Visible;
        Unit1SideControls.Visibility = Visibility.Visible;
        NavUnit1Btn.Style = (System.Windows.Style)FindResource("NavButtonActive");
    }

    private void Nav_Unit2_Click(object sender, RoutedEventArgs e)
    {
        HideAllPanels();
        ClearNavStyles();
        LessonPanel.Visibility       = Visibility.Visible;
        Unit2SideControls.Visibility = Visibility.Visible;
        NavUnit2Btn.Style = (System.Windows.Style)FindResource("NavButtonActive");
    }

    private void Nav_Unit3_Click(object sender, RoutedEventArgs e)
    {
        HideAllPanels();
        ClearNavStyles();
        LessonPanel.Visibility       = Visibility.Visible;
        Unit3SideControls.Visibility = Visibility.Visible;
        NavUnit3Btn.Style = (System.Windows.Style)FindResource("NavButtonActive");
    }

    private async void StartLesson_Click(object sender, RoutedEventArgs e)
    {
        if (_lessonVm is null) return;
        if (sender is not Button btn) return;

        string skillId = btn.Tag as string ?? "geo-u1-point-line-plane";

        HideAllPanels();
        ClearNavStyles();
        LessonPanel.Visibility = Visibility.Visible;

        if (skillId.StartsWith("geo-u3-", StringComparison.Ordinal))
        {
            Unit3SideControls.Visibility = Visibility.Visible;
            NavUnit3Btn.Style = (System.Windows.Style)FindResource("NavButtonActive");
        }
        else if (skillId.StartsWith("geo-u2-", StringComparison.Ordinal))
        {
            Unit2SideControls.Visibility = Visibility.Visible;
            NavUnit2Btn.Style = (System.Windows.Style)FindResource("NavButtonActive");
        }
        else
        {
            Unit1SideControls.Visibility = Visibility.Visible;
            NavUnit1Btn.Style = (System.Windows.Style)FindResource("NavButtonActive");
        }

        await _lessonVm.LoadLessonCommand.ExecuteAsync(skillId);
    }
}
