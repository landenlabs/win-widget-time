// Copyright (c) 2026 LanDen Labs - Dennis Lang
//
// Regression tests for widget drag / resize / on-screen recovery. These areas have been
// broken and re-fixed several times; every bug fixed here should get a test.
//
// The same test file is shared (namespace aside) by every win-widget-* project.

using WinWidgetTime.Services;
using Xunit;

namespace WinWidgetTime.Tests;

public class HitTestTests
{
    private const double W = 340, H = 160;

    [Theory]
    [InlineData(335, 155, ResizeZone.BottomRight)]
    [InlineData(5, 155, ResizeZone.BottomLeft)]
    [InlineData(335, 5, ResizeZone.TopRight)]
    [InlineData(5, 5, ResizeZone.TopLeft)]
    [InlineData(170, 80, ResizeZone.None)]      // centre
    [InlineData(170, 155, ResizeZone.None)]     // bottom edge, not a corner
    [InlineData(335, 80, ResizeZone.None)]      // right edge, not a corner
    public void Corners_are_detected_when_top_corners_allowed(double x, double y, ResizeZone expected)
        => Assert.Equal(expected, WidgetGeometry.HitTest(x, y, W, H, allowTopCorners: true));

    [Theory]
    [InlineData(335, 5)]
    [InlineData(5, 5)]
    public void Top_corners_are_drag_zone_when_title_shown(double x, double y)
        => Assert.Equal(ResizeZone.None, WidgetGeometry.HitTest(x, y, W, H, allowTopCorners: false));

    [Fact]
    public void Bottom_corners_still_resize_when_title_shown()
    {
        Assert.Equal(ResizeZone.BottomRight, WidgetGeometry.HitTest(335, 155, W, H, allowTopCorners: false));
        Assert.Equal(ResizeZone.BottomLeft, WidgetGeometry.HitTest(5, 155, W, H, allowTopCorners: false));
    }

    [Fact]
    public void Corner_boundary_is_inclusive()
    {
        double c = WidgetGeometry.DefaultCornerSize;
        Assert.Equal(ResizeZone.BottomRight, WidgetGeometry.HitTest(W - c, H - c, W, H, true));
        Assert.Equal(ResizeZone.None, WidgetGeometry.HitTest(W - c - 0.5, H - c, W, H, true));
    }

    [Theory]
    [InlineData(-1, 80)]
    [InlineData(341, 80)]
    [InlineData(170, -1)]
    [InlineData(170, 161)]
    public void Points_outside_the_widget_are_not_corners(double x, double y)
        => Assert.Equal(ResizeZone.None, WidgetGeometry.HitTest(x, y, W, H, true));

    [Fact]
    public void Unmeasured_widget_has_no_corners()
        => Assert.Equal(ResizeZone.None, WidgetGeometry.HitTest(0, 0, 0, 0, true));
}

public class DragTests
{
    [Fact]
    public void Window_moves_by_exactly_the_cursor_delta()
        => Assert.Equal((150, 230), WidgetGeometry.DragTo(100, 200, 500, 500, 550, 530));

    [Fact]
    public void No_cursor_movement_means_no_window_movement()
        => Assert.Equal((100, 200), WidgetGeometry.DragTo(100, 200, 777, 888, 777, 888));

    [Fact]
    public void Drag_onto_a_monitor_left_of_primary_gives_negative_coordinates()
        => Assert.Equal((-1800, 100), WidgetGeometry.DragTo(100, 100, 300, 300, -1600, 300));

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void Drag_is_independent_of_dpi(double _)
    {
        // Positions and cursor are both physical pixels, so the result must not depend on DPI.
        // (Earlier versions mixed DIPs and pixels and the widget jumped at non-100 % scaling.)
        Assert.Equal((110, 120), WidgetGeometry.DragTo(100, 100, 0, 0, 10, 20));
    }
}

public class ResizeTests
{
    private const double MinW = 160, MinH = 80;

    private static ResizeResult R(ResizeZone z, int dx, int dy, double dpi = 1.0,
                                  double w = 340, double h = 160, int x = 1000, int y = 500,
                                  double maxW = double.PositiveInfinity, double maxH = double.PositiveInfinity)
        => WidgetGeometry.Resize(z, w, h, x, y, dx, dy, dpi, dpi, MinW, MinH, maxW, maxH);

    [Fact]
    public void BottomRight_grows_size_and_keeps_origin()
    {
        var r = R(ResizeZone.BottomRight, 60, 40);
        Assert.Equal(400, r.Width);
        Assert.Equal(200, r.Height);
        Assert.Equal((1000, 500), (r.X, r.Y));
    }

    [Fact]
    public void Pixel_delta_is_converted_to_dips_at_150_percent()
    {
        var r = R(ResizeZone.BottomRight, 150, 75, dpi: 1.5);
        Assert.Equal(440, r.Width);   // 150 px / 1.5 = 100 DIP
        Assert.Equal(210, r.Height);  // 75 px / 1.5 = 50 DIP
    }

    [Fact]
    public void TopLeft_moves_origin_so_bottom_right_corner_stays_put()
    {
        var r = R(ResizeZone.TopLeft, -60, -40);
        Assert.Equal(400, r.Width);
        Assert.Equal(200, r.Height);
        Assert.Equal((940, 460), (r.X, r.Y));
    }

    [Fact]
    public void BottomLeft_moves_only_x()
    {
        var r = R(ResizeZone.BottomLeft, 20, 30);
        Assert.Equal(320, r.Width);
        Assert.Equal(190, r.Height);
        Assert.Equal((1020, 500), (r.X, r.Y));
    }

    [Fact]
    public void TopRight_moves_only_y()
    {
        var r = R(ResizeZone.TopRight, 20, 30);
        Assert.Equal(360, r.Width);
        Assert.Equal(130, r.Height);
        Assert.Equal((1000, 530), (r.X, r.Y));
    }

    [Fact]
    public void Size_is_clamped_to_minimum()
    {
        var r = R(ResizeZone.BottomRight, -1000, -1000);
        Assert.Equal(MinW, r.Width);
        Assert.Equal(MinH, r.Height);
    }

    [Fact]
    public void Shrinking_past_minimum_from_west_does_not_drift_the_widget()
    {
        // Old bug: origin kept moving with the cursor even after width hit the minimum.
        var r = R(ResizeZone.TopLeft, 5000, 5000);
        Assert.Equal(MinW, r.Width);
        Assert.Equal(MinH, r.Height);
        Assert.Equal((1000 + (340 - 160), 500 + (160 - 80)), (r.X, r.Y));
    }

    [Fact]
    public void Size_is_clamped_to_maximum()
    {
        var r = R(ResizeZone.BottomRight, 10_000, 10_000, maxW: 1920, maxH: 1080);
        Assert.Equal(1920, r.Width);
        Assert.Equal(1080, r.Height);
    }

    [Fact]
    public void None_zone_changes_nothing()
    {
        var r = R(ResizeZone.None, 50, 50);
        Assert.Equal(new ResizeResult(340, 160, 1000, 500), r);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(1.75)]
    [InlineData(2.0)]
    public void West_and_north_resizes_keep_the_opposite_edges_fixed_at_any_dpi(double dpi)
    {
        // Old bug: the west/north shift was computed in DIPs but applied to a pixel position.
        const double w = 340, h = 160;
        const int x = 1000, y = 500;
        int right  = x + (int)Math.Round(w * dpi);
        int bottom = y + (int)Math.Round(h * dpi);

        for (int d = -300; d <= 300; d += 7)
        {
            var r = R(ResizeZone.TopLeft, d, d, dpi, w, h, x, y);
            Assert.InRange(r.X + r.Width * dpi - right, -1.0, 1.0);
            Assert.InRange(r.Y + r.Height * dpi - bottom, -1.0, 1.0);
        }
    }

    [Theory]
    [InlineData(100, 340, 160, 340, 160, 100)]
    [InlineData(100, 340, 160, 680, 320, 200)]
    [InlineData(100, 340, 160, 170, 80, 50)]
    [InlineData(100, 340, 160, 5000, 5000, 200)]   // clamped to max
    [InlineData(100, 340, 160, 1, 1, 50)]          // clamped to min
    [InlineData(150, 200, 100, 300, 100, 188)]     // average of 1.5x and 1.0x = 1.25x
    public void Content_sized_widgets_scale_proportionally(int start, double sw, double sh, double nw, double nh, int expected)
        => Assert.Equal(expected, WidgetGeometry.ScalePercentFromResize(start, sw, sh, nw, nh));
}

public class OnScreenTests
{
    // Primary 2560x1440 at origin (taskbar 48 px), secondary 1920x1080 to the right.
    private static readonly ScreenInfo Primary =
        new(new PxRect(0, 0, 2560, 1440), new PxRect(0, 0, 2560, 1392), true);
    private static readonly ScreenInfo Right =
        new(new PxRect(2560, 0, 1920, 1080), new PxRect(2560, 0, 1920, 1032), false);
    private static readonly ScreenInfo LeftOfPrimary =
        new(new PxRect(-1920, 200, 1920, 1080), new PxRect(-1920, 200, 1920, 1032), false);

    private static readonly PxRect Widget = new(100, 100, 340, 160);

    [Fact]
    public void Fully_visible_widget_is_left_alone()
        => Assert.Null(WidgetGeometry.EnsureVisible(Widget, [Primary, Right]));

    [Fact]
    public void Widget_straddling_two_monitors_is_left_alone()
        => Assert.Null(WidgetGeometry.EnsureVisible(Widget with { X = 2400 }, [Primary, Right]));

    [Fact]
    public void Widget_partly_off_screen_but_grabbable_is_left_alone()
        => Assert.Null(WidgetGeometry.EnsureVisible(Widget with { X = 2560 - 60 }, [Primary]));

    [Fact]
    public void Widget_on_unplugged_monitor_moves_to_the_nearest_remaining_screen()
    {
        // Was on the right monitor; it was unplugged.
        var p = WidgetGeometry.EnsureVisible(Widget with { X = 3000, Y = 300 }, [Primary]);
        Assert.NotNull(p);
        var (x, y) = p.Value;
        Assert.Equal(2560 - 340 - 20, x);   // clamped to the right edge of primary, inset 20
        Assert.Equal(300, y);               // y was fine, keep it
    }

    [Fact]
    public void Only_a_sliver_visible_counts_as_off_screen()
    {
        var p = WidgetGeometry.EnsureVisible(Widget with { X = 2560 - 10 }, [Primary]);
        Assert.NotNull(p);
    }

    [Fact]
    public void Recovered_widget_avoids_the_taskbar()
    {
        var p = WidgetGeometry.EnsureVisible(Widget with { Y = 5000 }, [Primary]);
        Assert.NotNull(p);
        Assert.True(p.Value.Y + Widget.Height <= Primary.WorkArea.Bottom);
    }

    [Fact]
    public void Negative_coordinates_on_a_left_monitor_are_visible()
        => Assert.Null(WidgetGeometry.EnsureVisible(Widget with { X = -1000, Y = 400 }, [Primary, LeftOfPrimary]));

    [Fact]
    public void Widget_left_on_removed_left_monitor_comes_back_to_primary()
    {
        var p = WidgetGeometry.EnsureVisible(Widget with { X = -1000, Y = 400 }, [Primary]);
        Assert.NotNull(p);
        Assert.Equal(20, p.Value.X);
    }

    [Fact]
    public void Widget_larger_than_work_area_is_placed_at_its_origin()
        => Assert.Equal((0, 0), WidgetGeometry.ClampInto(new PxRect(-5000, -5000, 4000, 3000), Primary.WorkArea, 20));

    [Fact]
    public void No_screens_means_no_change()
        => Assert.Null(WidgetGeometry.EnsureVisible(Widget, []));
}

public class FingerprintTests
{
    private static readonly ScreenInfo A = new(new PxRect(0, 0, 2560, 1440), new PxRect(0, 0, 2560, 1392), true);
    private static readonly ScreenInfo B = new(new PxRect(2560, 0, 1920, 1080), new PxRect(2560, 0, 1920, 1032), false);

    [Fact]
    public void Fingerprint_is_readable_and_deterministic()
        // A literal expected value guards against regressing to string.GetHashCode(),
        // which is randomized per process and made saved positions unrecoverable.
        => Assert.Equal("MON:0,0,2560x1440,P|2560,0,1920x1080", WidgetGeometry.Fingerprint([A, B]));

    [Fact]
    public void Enumeration_order_does_not_matter()
        => Assert.Equal(WidgetGeometry.Fingerprint([A, B]), WidgetGeometry.Fingerprint([B, A]));

    [Fact]
    public void Different_layouts_have_different_fingerprints()
    {
        Assert.NotEqual(WidgetGeometry.Fingerprint([A]), WidgetGeometry.Fingerprint([A, B]));
        Assert.NotEqual(WidgetGeometry.Fingerprint([A, B]),
                        WidgetGeometry.Fingerprint([A with { IsPrimary = false }, B with { IsPrimary = true }]));
    }

    [Fact]
    public void Fingerprint_has_the_prefix_used_to_prune_stale_keys()
        => Assert.StartsWith(WidgetGeometry.FingerprintPrefix, WidgetGeometry.Fingerprint([A]));
}
