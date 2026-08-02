using System.Windows;
using System.Windows.Media;

namespace OneMenu
{
    public static class OneMenuTheme
    {
        public static readonly SolidColorBrush NormalBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x32));
        public static readonly SolidColorBrush NormalBrushDark = new SolidColorBrush(Color.FromRgb(0x1C, 0x1C, 0x22));
        public static readonly SolidColorBrush TextBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xE6, 0xEC));
        public static readonly SolidColorBrush GlyphBrush = new SolidColorBrush(Color.FromRgb(0x6E, 0x4F, 0x91));

        public static void Apply(FrameworkElement root, bool useCustom)
        {
            if (useCustom)
            {
                root.Resources["NormalBrush"] = NormalBrush;
                root.Resources["NormalBrushDark"] = NormalBrushDark;
                root.Resources["TextBrush"] = TextBrush;
                root.Resources["GlyphBrush"] = GlyphBrush;
            }
            else
            {
                root.Resources.Remove("NormalBrush");
                root.Resources.Remove("NormalBrushDark");
                root.Resources.Remove("TextBrush");
                root.Resources.Remove("GlyphBrush");
            }
        }
    }
}
