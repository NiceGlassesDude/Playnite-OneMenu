using Playnite.SDK;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace OneMenu
{
    public static class WindowLauncher
    {
        private static TagSearchWindow tagBrowser;
        private static TagGenreManagerWindow tagGenreManager;

        public static Window GetActiveWindow()
        {
            var app = Application.Current;
            if (app == null)
            {
                return null;
            }

            return app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w.IsVisible) ?? app.MainWindow;
        }

        public static void OpenTagBrowser(OneMenuSettings settings)
        {
            if (tagBrowser != null)
            {
                BringToFront(tagBrowser);
                return;
            }

            GetSizeForPreset(settings.TagBrowserSize, out var width, out var height);

            tagBrowser = new TagSearchWindow(settings.TagBrowserUseOneMenuTheme)
            {
                Owner = Application.Current?.MainWindow,
                Opacity = settings.TagBrowserOpacity,
                Width = width,
                Height = height
            };
            tagBrowser.Closed += (s, e) => tagBrowser = null;
            tagBrowser.Show();
        }

        public static void OpenTagGenreManager()
        {
            if (tagGenreManager != null)
            {
                BringToFront(tagGenreManager);
                return;
            }

            tagGenreManager = new TagGenreManagerWindow
            {
                Owner = Application.Current?.MainWindow
            };
            tagGenreManager.Closed += (s, e) => tagGenreManager = null;
            tagGenreManager.Show();
        }

        public static void OpenWelcome()
        {
            try
            {
                var window = new WelcomeWindow();
                var owner = GetActiveWindow();

                if (owner != null && owner.IsVisible)
                {
                    window.Owner = owner;
                }
                else
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                LogManager.GetLogger().Error(ex, "OneMenu: couldn't show the welcome guide.");
            }
        }

        private static void BringToFront(Window window)
        {
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Activate();
        }

        private static void GetSizeForPreset(TagBrowserSizePreset preset, out double width, out double height)
        {
            switch (preset)
            {
                case TagBrowserSizePreset.Bigger:
                    width = 960;
                    height = 720;
                    break;
                case TagBrowserSizePreset.MuchBigger:
                    width = 1200;
                    height = 880;
                    break;
                default:
                    width = 760;
                    height = 560;
                    break;
            }
        }
    }

    public static class VisualHelper
    {
        public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }

                if (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                else
                {
                    current = LogicalTreeHelper.GetParent(current);
                }
            }

            return null;
        }
    }
}
