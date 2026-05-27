using System.Windows;
using System.Windows.Input;
using GeoTutor.SceneEngine.Models;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace GeoTutor.SceneEngine;

/// <summary>
/// A WPF UserControl that renders a <see cref="SceneSpec"/> using SkiaSharp and provides
/// interactive drag support for points marked <see cref="ScenePoint.Draggable"/>.
///
/// Usage (code-behind or code-only host):
/// <code>
///   var ctrl = new SceneControl();
///   ctrl.Scene = mySpec;
///   ctrl.PointDragged += (id, wx, wy) => { /* update model */ };
///   ctrl.PointSelected += (id) => { /* highlight point */ };
/// </code>
/// No XAML file — the visual tree is built entirely in code.
/// </summary>
public sealed class SceneControl : System.Windows.Controls.UserControl
{
    // ── SKElement ─────────────────────────────────────────────────────────────

    private readonly SKElement _skElement;

    // ── Drag state ────────────────────────────────────────────────────────────

    private string?   _dragPointId;
    private SKPoint   _lastMouseScreen;

    // ── Hit-test radius (screen pixels) ──────────────────────────────────────

    private const float HitRadius = 12f;

    // ─────────────────────────────────────────────────────────────────────────
    // Construction
    // ─────────────────────────────────────────────────────────────────────────

    public SceneControl()
    {
        _skElement = new SKElement
        {
            Focusable   = true,
            SnapsToDevicePixels = true,
        };

        _skElement.PaintSurface += OnPaintSurface;

        // WPF mouse events are on the element, not on this UserControl, to avoid
        // double-hit-testing through the visual tree.
        _skElement.MouseDown  += OnMouseDown;
        _skElement.MouseMove  += OnMouseMove;
        _skElement.MouseUp    += OnMouseUp;
        _skElement.MouseLeave += OnMouseLeave;

        Content = _skElement;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Dependency property: Scene
    // ─────────────────────────────────────────────────────────────────────────

    public static readonly DependencyProperty SceneProperty =
        DependencyProperty.Register(
            nameof(Scene),
            typeof(SceneSpec),
            typeof(SceneControl),
            new FrameworkPropertyMetadata(
                defaultValue: null,
                flags: FrameworkPropertyMetadataOptions.AffectsRender,
                propertyChangedCallback: OnSceneChanged));

    /// <summary>
    /// Gets or sets the scene specification to render.
    /// Setting this property triggers an immediate re-render.
    /// </summary>
    public SceneSpec? Scene
    {
        get => (SceneSpec?)GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    private static void OnSceneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (SceneControl)d;
        ctrl._dragPointId = null;   // cancel any in-progress drag on scene change
        ctrl._skElement.InvalidateVisual();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired continuously while a draggable point is being moved.
    /// Arguments: (pointId, newWorldX, newWorldY).
    /// </summary>
    public event Action<string, double, double>? PointDragged;

    /// <summary>
    /// Fired on MouseDown when a draggable or non-draggable point is clicked.
    /// Argument: pointId.
    /// </summary>
    public event Action<string>? PointSelected;

    // ─────────────────────────────────────────────────────────────────────────
    // Rendering
    // ─────────────────────────────────────────────────────────────────────────

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas  = e.Surface.Canvas;
        var info    = e.Info;

        canvas.Clear(SKColors.White);

        var spec = Scene;
        if (spec == null) return;

        using var bmp = SceneRenderer.Render(spec, info.Width, info.Height);
        canvas.DrawBitmap(bmp, 0, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coordinate helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Converts a WPF Point (logical pixels) to SkiaSharp physical pixels.</summary>
    private SKPoint ToSkia(Point wpf)
    {
        double dpi   = GetDpi();
        return new SKPoint((float)(wpf.X * dpi), (float)(wpf.Y * dpi));
    }

    private double GetDpi()
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
    }

    /// <summary>Converts physical screen pixels to world coordinates.</summary>
    private (double wx, double wy) ToWorld(SKPoint screen)
    {
        var vp = Scene?.Viewport ?? new Viewport();
        double widthPx  = _skElement.ActualWidth  * GetDpi();
        double heightPx = _skElement.ActualHeight * GetDpi();

        double wx = vp.XMin + screen.X / widthPx  * (vp.XMax - vp.XMin);
        double wy = vp.YMin + (1.0 - screen.Y / heightPx) * (vp.YMax - vp.YMin);
        return (wx, wy);
    }

    /// <summary>Returns the screen position (physical px) of a world point.</summary>
    private SKPoint PointToScreen(ScenePoint pt)
    {
        var vp = Scene?.Viewport ?? new Viewport();
        double widthPx  = _skElement.ActualWidth  * GetDpi();
        double heightPx = _skElement.ActualHeight * GetDpi();

        float sx = (float)((pt.X - vp.XMin) / (vp.XMax - vp.XMin) * widthPx);
        float sy = (float)((1.0 - (pt.Y - vp.YMin) / (vp.YMax - vp.YMin)) * heightPx);
        return new SKPoint(sx, sy);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hit testing
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the first point within <see cref="HitRadius"/> of <paramref name="pos"/>,
    /// prioritising draggable points, or null if none is nearby.
    /// </summary>
    private ScenePoint? HitTest(SKPoint pos)
    {
        if (Scene == null) return null;

        ScenePoint? best       = null;
        float       bestDistSq = HitRadius * HitRadius;

        foreach (var pt in Scene.Points)
        {
            var  sc      = PointToScreen(pt);
            float dx     = sc.X - pos.X;
            float dy     = sc.Y - pos.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq <= bestDistSq)
            {
                // Prefer draggable points when distances tie.
                if (best == null || (pt.Draggable && !best.Draggable) || distSq < bestDistSq)
                {
                    best       = pt;
                    bestDistSq = distSq;
                }
            }
        }

        return best;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Mouse handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Scene == null) return;

        var skPos = ToSkia(e.GetPosition(_skElement));
        var hit   = HitTest(skPos);

        if (hit == null) return;

        // Notify selection unconditionally.
        PointSelected?.Invoke(hit.Id);

        if (!hit.Draggable) return;

        _dragPointId      = hit.Id;
        _lastMouseScreen  = skPos;

        _skElement.CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragPointId == null || Scene == null) return;
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            EndDrag();
            return;
        }

        var skPos = ToSkia(e.GetPosition(_skElement));
        _lastMouseScreen = skPos;

        var (wx, wy) = ToWorld(skPos);

        // Mutate the point directly so the next render picks up the change.
        var pt = Scene.Points.FirstOrDefault(p => p.Id == _dragPointId);
        if (pt != null)
        {
            pt.X = wx;
            pt.Y = wy;
        }

        PointDragged?.Invoke(_dragPointId, wx, wy);

        // Schedule re-render.
        _skElement.InvalidateVisual();
        e.Handled = true;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        EndDrag();
        e.Handled = true;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        EndDrag();
    }

    private void EndDrag()
    {
        _dragPointId = null;
        if (_skElement.IsMouseCaptured)
            _skElement.ReleaseMouseCapture();
    }
}
