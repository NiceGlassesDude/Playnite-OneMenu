using Playnite.SDK;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

namespace OneMenu
{
    public static class MenuBuilder
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public static ContextMenu BuildContextMenu(OneMenuSettings settings)
        {
            var menu = new ContextMenu
            {
                Placement = PlacementMode.MousePoint,
                PlacementTarget = System.Windows.Application.Current?.MainWindow
            };

            foreach (var node in settings.RootNodes)
            {
                if (node.IsHidden)
                {
                    continue;
                }

                var rootItem = BuildMenuItem(node);
                if (rootItem != null)
                {
                    menu.Items.Add(rootItem);
                }
            }

            if (settings.TagSearchEnabled)
            {
                menu.Items.Add(new Separator());

                var searchItem = new MenuItem
                {
                    Header = "Tag Browser",
                    Icon = new TextBlock
                    {
                        Text = "\xE721",
                        FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
                        FontSize = 16,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center
                    }
                };
                searchItem.Click += (s, e) =>
                {
                    GetSizeForPreset(settings.TagBrowserSize, out var width, out var height);
                    var window = new TagSearchWindow
                    {
                        Owner = System.Windows.Application.Current?.MainWindow,
                        Opacity = settings.TagBrowserOpacity,
                        Width = width,
                        Height = height
                    };
                    window.Show();
                };
                menu.Items.Add(searchItem);
            }

            return menu;
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

        private static MenuItem BuildMenuItem(MenuNode node)
        {
            if (node.IsHidden)
            {
                return null;
            }

            var item = new MenuItem
            {
                Header = node.ShowText ? node.Title : string.Empty
            };

            if (node.ShowIcon && !string.IsNullOrEmpty(node.IconPath))
            {
                try
                {
                    item.Icon = new Image
                    {
                        Source = new BitmapImage(new Uri(node.IconPath)),
                        Width = 20,
                        Height = 20
                    };
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, $"Quick Launcher: couldn't load icon for '{node.Title}' from '{node.IconPath}'.");
                }
            }

            if (node.IsCategory)
            {
                var childCount = 0;
                foreach (var child in node.Children)
                {
                    if (child.IsHidden)
                    {
                        continue;
                    }

                    var childItem = BuildMenuItem(child);
                    if (childItem != null)
                    {
                        item.Items.Add(childItem);
                        childCount++;
                    }
                }

                if (childCount == 0)
                {
                    return null;
                }
            }
            else
            {
                item.Click += (s, e) => RunAction(node);
            }

            return item;
        }

        private static void RunAction(MenuNode node)
        {
            var api = OneMenuPlugin.Api;
            if (api == null)
            {
                logger.Error("Quick Launcher: Playnite API reference is null.");
                return;
            }

            if (node.ActionType == MenuActionType.OpenPath)
            {
                if (string.IsNullOrEmpty(node.TargetPath))
                {
                    logger.Warn($"Quick Launcher: '{node.Title}' has no file or folder assigned.");
                    return;
                }

                try
                {
                    Process.Start(new ProcessStartInfo(node.TargetPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Quick Launcher: couldn't open '{node.TargetPath}' for '{node.Title}'.");
                    api.Dialogs.ShowErrorMessage($"Couldn't open:\n{node.TargetPath}", "OneMenu");
                }

                return;
            }

            if (node.FilterPresetId == null)
            {
                logger.Warn($"Quick Launcher: '{node.Title}' has no filter preset assigned.");
                return;
            }

            api.MainView.ApplyFilterPreset(node.FilterPresetId.Value);
            api.MainView.SwitchToLibraryView();
        }
    }
}
