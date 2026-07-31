using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

namespace OneMenu
{
    public static class MenuBuilder
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public static ContextMenu BuildContextMenu(IEnumerable<MenuNode> rootNodes, bool tagSearchEnabled)
        {
            var menu = new ContextMenu
            {
                Placement = PlacementMode.MousePoint,
                PlacementTarget = System.Windows.Application.Current?.MainWindow
            };

            foreach (var node in rootNodes)
            {
                menu.Items.Add(BuildMenuItem(node));
            }

            if (tagSearchEnabled)
            {
                menu.Items.Add(new Separator());

                var searchItem = new MenuItem
                {
                    Header = "Tag Search",
                    Icon = new TextBlock
                    {
                        Text = "🔍",
                        FontSize = 16,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center
                    }
                };
                searchItem.Click += (s, e) =>
                {
                    var window = new TagSearchWindow
                    {
                        Owner = System.Windows.Application.Current?.MainWindow
                    };
                    window.Show();
                };
                menu.Items.Add(searchItem);
            }

            return menu;
        }

        private static MenuItem BuildMenuItem(MenuNode node)
        {
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
                foreach (var child in node.Children)
                {
                    item.Items.Add(BuildMenuItem(child));
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
