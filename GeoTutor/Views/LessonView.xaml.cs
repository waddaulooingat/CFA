namespace GeoTutor.Views;

using System;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using GeoTutor.Core.Models;
using GeoTutor.Core.ViewModels;
using GeoTutor.SceneEngine.Models;

public partial class LessonView : UserControl
{
    private LessonViewModel? _vm;

    public LessonView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
        {
            _vm.CanvasActionRequested -= OnCanvasAction;
            Canvas.PointDragged       -= OnCanvasPointDragged;
        }

        _vm = e.NewValue as LessonViewModel;

        if (_vm is not null)
        {
            _vm.CanvasActionRequested += OnCanvasAction;
            Canvas.PointDragged       += OnCanvasPointDragged;
        }
    }

    private void OnCanvasPointDragged(string pointId, double wx, double wy)
        => _vm?.OnPointDragged(pointId, wx, wy);

    // -----------------------------------------------------------------------
    // Canvas action execution
    // -----------------------------------------------------------------------

    private void OnCanvasAction(CanvasAction action)
    {
        switch (action.Type)
        {
            case "highlight":
                if (action.Target is not null)
                    HighlightElement(action.Target);
                break;

            case "loadScene":
                if (action.Target is not null)
                    LoadScene(action.Target);
                break;

            case "annotate":
                if (action.Target is not null && action.Label is not null)
                    AddTemporaryLabel(action.Target, action.Label);
                break;

            case "morph":
                // morph is handled by the VM swapping CurrentScene;
                // flash the canvas to signal the change
                FlashCanvas();
                break;
        }
    }

    private void HighlightElement(string elementId)
    {
        if (Canvas.Scene is null) return;

        // Set Highlighted = true on matching point or segment, clear others
        foreach (var pt in Canvas.Scene.Points)
            pt.Highlighted = pt.Id == elementId;
        foreach (var seg in Canvas.Scene.Segments)
            seg.Highlighted = seg.Id == elementId;

        Canvas.InvalidateVisual();

        // Auto-clear highlight after 2.5 seconds
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2.5)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (Canvas.Scene is null) return;
            foreach (var pt  in Canvas.Scene.Points)   pt.Highlighted  = false;
            foreach (var seg in Canvas.Scene.Segments) seg.Highlighted = false;
            Canvas.InvalidateVisual();
        };
        timer.Start();
    }

    private void AddTemporaryLabel(string anchorPointId, string labelText)
    {
        if (Canvas.Scene is null) return;

        var label = new SceneLabel
        {
            Id          = $"tmp_{Guid.NewGuid():N}",
            Text        = labelText,
            AnchorPoint = anchorPointId,
            OffsetX     = 0.4,
            OffsetY     = 0.4,
            IsTemporary = true,
        };
        Canvas.Scene.Labels.Add(label);
        Canvas.InvalidateVisual();

        // Remove after 3 seconds
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Canvas.Scene?.Labels.Remove(label);
            Canvas.InvalidateVisual();
        };
        timer.Start();
    }

    private void LoadScene(string sceneSpecId)
    {
        // The VM's CurrentScene binding handles the swap;
        // this is a visual flash to signal it.
        FlashCanvas();
    }

    private void FlashCanvas()
    {
        // Brief opacity pulse to signal a scene change
        var anim = new DoubleAnimation(0.4, 1.0, TimeSpan.FromMilliseconds(350));
        Canvas.BeginAnimation(OpacityProperty, anim);
    }
}
