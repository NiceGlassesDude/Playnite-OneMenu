using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

namespace OneMenu
{
    public static class MenuBuilder
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public static ContextMenu BuildContextMenu(IEnumerable<MenuNode> rootNodes)
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
                item.Click += (s, e) =>
                {
                    if (node.FilterPresetId == null)
                    {
                        logger.Warn($"Quick Launcher: '{node.Title}' has no filter preset assigned.");
                        return;
                    }

                    var api = OneMenuPlugin.Api;
                    if (api == null)
                    {
                        logger.Error("Quick Launcher: Playnite API reference is null.");
                        return;
                    }

                    api.MainView.ApplyFilterPreset(node.FilterPresetId.Value);
                    api.MainView.SwitchToLibraryView();
                };
            }

            return item;
        }
    }
}
