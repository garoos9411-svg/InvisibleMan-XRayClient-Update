using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace InvisibleManXRay.Utilities
{
    internal static class TrayIconFactory
    {
        private const int Size = 32;
        private const string ShieldPath =
            "M 16 2 C 20.5 5.8 24.5 6.8 28 7 C 28 17.5 24 26 16 30 C 8 26 4 17.5 4 7 C 7.5 6.8 11.5 5.8 16 2 Z";
        private const string CheckPath = "M 10.5 16.5 L 14.5 20.5 L 22 12.5";

        public static ImageSource CreateDisconnectedIcon()
        {
            Color outline = ResolveBrushColor("TextFillColorTertiaryBrush");
            return Render(
                fill: WithAlpha(outline, 0x40),
                stroke: WithAlpha(outline, 0xB0),
                check: null
            );
        }

        public static ImageSource CreateConnectedIcon()
        {
            return Render(
                fill: ResolveColor("Accent.Success.Color"),
                stroke: ResolveColor("Accent.Success.Bright.Color"),
                check: ResolveColor("Text.On.Accent.Color")
            );
        }

        public static ImageSource[] CreateConnectingFrames(int frameCount)
        {
            Color accent = ResolveColor("Accent.Success.Color");
            ImageSource[] frames = new ImageSource[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                double phase = (double)i / frameCount;
                double pulse = 0.5 - 0.5 * Math.Cos(2 * Math.PI * phase);
                frames[i] = Render(
                    fill: WithAlpha(accent, (byte)(50 + 185 * pulse)),
                    stroke: WithAlpha(accent, (byte)(120 + 135 * pulse)),
                    check: null
                );
            }

            return frames;
        }

        private static ImageSource Render(Color fill, Color stroke, Color? check)
        {
            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext context = visual.RenderOpen())
            {
                Geometry shield = Geometry.Parse(ShieldPath);
                context.DrawGeometry(CreateBrush(fill), null, shield);
                context.DrawGeometry(null, CreatePen(stroke, 1.6), shield);

                if (check.HasValue)
                    context.DrawGeometry(null, CreatePen(check.Value, 2.8), Geometry.Parse(CheckPath));
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(Size, Size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }

        private static Brush CreateBrush(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static Pen CreatePen(Color color, double thickness)
        {
            Pen pen = new Pen(CreateBrush(color), thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            pen.Freeze();
            return pen;
        }

        private static Color ResolveColor(string key)
        {
            if (Application.Current?.TryFindResource(key) is System.Windows.Media.Color color)
                return color;

            return Colors.Gray;
        }

        private static Color ResolveBrushColor(string key)
        {
            if (Application.Current?.TryFindResource(key) is SolidColorBrush brush)
                return brush.Color;

            return Colors.Gray;
        }

        private static Color WithAlpha(Color color, byte alpha)
            => Color.FromArgb(alpha, color.R, color.G, color.B);
    }
}
