using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GeoTutor.SceneEngine.Models;
using SkiaSharp;

namespace GeoTutor.SceneEngine;

/// <summary>
/// A WPF UserControl that renders a <see cref="SceneSpec"/> using SkiaSharp into a
/// <see cref="WriteableBitmap"/> and provides interactive drag support for points
/// marked <see cref="ScenePoint.Draggable"/>.
/// </summary>
public sealed class SceneControl : UserControl
{
    // ── Rendering ─────────────────────────────────────────────────────────────

    private readonly Image _image;
    private WriteableBitmap? _bitmap;

    // ── Drag state ────────────────────────────────────────────────────────────

    private string?  _dragPointId;
    private SKPoint  _lastMouseScreen;

    // ── Hit-test radius (screen pixels) ──────────────────────────────────────

    private const float HitRadius = 12f;

    // ─────────────────────────────────────────────────────────────────────────
    // Construction
    // ─────────────────────────────────────────────────────────────────────────

    public SceneControl()
    {
        _image = new Image
        {
            Stretch             = Stretch.Fill,
            Focusable           = true,
            SnapsToDevicePixels = true,
        };

        _image.MouseDown  += OnMouseDown;
        _image.MouseMove  += OnMouseMove;
        _image.MouseUp    += OnMouseUp;
        _image.MouseLeave += OnMouseLeave;

        Content = _image;

        SizeChanged += (_, _) => Render();
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
        ctrl._dragPointId = null;
        ctrl.Render();
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
    /// Fired once when the mouse is released after dragging a point.
    /// Use this for success-condition evaluation (single check on drop).
    /// Arguments: (pointId, finalWorldX, finalWorldY).
    /// </summary>
    public event Action<string, double, double>? PointDropped;

    /// <summary>
    /// Fired on MouseDown when a draggable or non-draggable point is clicked.
    /// Argument: pointId.
    /// </summary>
    public event Action<string>? PointSelected;

    // ─────────────────────────────────────────────────────────────────────────
    // Rendering
    // ─────────────────────────────────────────────────────────────────────────

    private void Render()
    {
        double dpi     = GetDpi();
        int    width   = (int)(ActualWidth  * dpi);
        int    height  = (int)(ActualHeight * dpi);
        if (width <= 0 || height <= 0) return;

        if (_bitmap == null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
        {
            double screenDpi = dpi * 96.0;
            _bitmap      = new WriteableBitmap(width, height, screenDpi, screenDpi, PixelFormats.Bgra32, null);
            _image.Source = _bitmap;
        }

        _bitmap.Lock();
        try
        {
            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info, _bitmap.BackBuffer, _bitmap.BackBufferStride);
            var canvas = surface.Canvas;

            canvas.Clear(SKColors.White);

            var spec = Scene;
            if (spec != null)
            {
                using var bmp = SceneRenderer.Render(spec, width, height);
                canvas.DrawBitmap(bmp, 0, 0);
            }

            canvas.Flush();
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            _bitmap.Unlock();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Coordinate helpers
    // ─────────────────────────────────────────────────────────────────────────

    private SKPoint ToSkia(Point wpf)
    {
        double dpi = GetDpi();
        return new SKPoint((float)(wpf.X * dpi), (float)(wpf.Y * dpi));
    }

    private double GetDpi()
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
    }

    private (double wx, double wy) ToWorld(SKPoint screen)
    {
        var    vp      = Scene?.Viewport ?? new Viewport();
        double dpi     = GetDpi();
        double widthPx  = ActualWidth  * dpi;
        double heightPx = ActualHeight * dpi;

        double wx = vp.XMin + screen.X / widthPx  * (vp.XMax - vp.XMin);
        double wy = vp.YMin + (1.0 - screen.Y / heightPx) * (vp.YMax - vp.YMin);
        return (wx, wy);
    }

    private SKPoint PointToScreen(ScenePoint pt)
    {
        var    vp      = Scene?.Viewport ?? new Viewport();
        double dpi     = GetDpi();
        double widthPx  = ActualWidth  * dpi;
        double heightPx = ActualHeight * dpi;

        float sx = (float)((pt.X - vp.XMin) / (vp.XMax - vp.XMin) * widthPx);
        float sy = (float)((1.0 - (pt.Y - vp.YMin) / (vp.YMax - vp.YMin)) * heightPx);
        return new SKPoint(sx, sy);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hit testing
    // ─────────────────────────────────────────────────────────────────────────

    private ScenePoint? HitTest(SKPoint pos)
    {
        if (Scene == null) return null;

        ScenePoint? best       = null;
        float       bestDistSq = HitRadius * HitRadius;

        foreach (var pt in Scene.Points)
        {
            var   sc     = PointToScreen(pt);
            float dx     = sc.X - pos.X;
            float dy     = sc.Y - pos.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq <= bestDistSq)
            {
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

        var skPos = ToSkia(e.GetPosition(_image));
        var hit   = HitTest(skPos);

        if (hit == null) return;

        PointSelected?.Invoke(hit.Id);

        if (!hit.Draggable) return;

        _dragPointId     = hit.Id;
        _lastMouseScreen = skPos;

        _image.CaptureMouse();
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

        var skPos = ToSkia(e.GetPosition(_image));
        _lastMouseScreen = skPos;

        var (wx, wy) = ToWorld(skPos);

        var pt = Scene.Points.FirstOrDefault(p => p.Id == _dragPointId);
        if (pt != null)
        {
            pt.X = wx;
            pt.Y = wy;
        }

        PointDragged?.Invoke(_dragPointId, wx, wy);

        Render();
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
        if (_dragPointId != null && Scene != null)
        {
            var (wx, wy) = ToWorld(_lastMouseScreen);
            PointDropped?.Invoke(_dragPointId, wx, wy);
        }

        _dragPointId = null;
        if (_image.IsMouseCaptured)
            _image.ReleaseMouseCapture();
    }
}
