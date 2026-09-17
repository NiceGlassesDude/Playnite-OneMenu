using Playnite.SDK;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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
                if (node.IsHidden || node.ShowInSidebar)
                {
                    continue;
                }

                var rootItem = BuildMenuItem(node);
                if (rootItem != null)
                {
                    menu.Items.Add(rootItem);
                }
            }

            if (settings.ShowTagBrowserInFlyout)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new Separator());
                }

                var searchItem = new MenuItem
                {
                    Header = Loc.Get("LOCOneMenuTagBrowser"),
                    Icon = new TextBlock
                    {
                        Text = "\xE721",
                        FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
                        FontSize = 16,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center
                    }
                };
                searchItem.Click += (s, e) => WindowLauncher.OpenTagBrowser(settings);
                menu.Items.Add(searchItem);
            }

            return menu;
        }

        public static void ActivateNode(MenuNode node)
        {
            if (node == null)
            {
                return;
            }

            if (!node.IsCategory)
            {
                RunAction(node);
                return;
            }

            var menu = new ContextMenu
            {
                Placement = PlacementMode.MousePoint,
                PlacementTarget = System.Windows.Application.Current?.MainWindow
            };

            foreach (var child in node.Children)
            {
                var childItem = BuildMenuItem(child);
                if (childItem != null)
                {
                    menu.Items.Add(childItem);
                }
            }

            if (menu.Items.Count > 0)
            {
                menu.IsOpen = true;
            }
        }

        private static MenuItem BuildMenuItem(MenuNode node)
        {
            if (node.IsHidden || node.ShowInSidebar)
            {
                return null;
            }

            var item = new MenuItem
            {
                Header = node.ShowText ? node.Title : string.Empty
            };

            if (node.ShowIcon && !string.IsNullOrEmpty(node.IconPath))
            {
                var source = ImageHelper.LoadFromFile(node.IconPath, 48);
                if (source != null)
                {
                    item.Icon = new Image
                    {
                        Source = source,
                        Width = 20,
                        Height = 20
                    };
                }
                else
                {
                    logger.Warn($"OneMenu: couldn't load icon for '{node.Title}' from '{node.IconPath}'.");
                }
            }

            if (node.IsCategory)
            {
                var childCount = 0;
                foreach (var child in node.Children)
                {
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
                logger.Error("OneMenu: Playnite API reference is null.");
                return;
            }

            if (node.ActionType == MenuActionType.OpenPath)
            {
                if (string.IsNullOrEmpty(node.TargetPath))
                {
                    logger.Warn($"OneMenu: '{node.Title}' has no file or folder assigned.");
                    return;
                }

                try
                {
                    Process.Start(new ProcessStartInfo(node.TargetPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"OneMenu: couldn't open '{node.TargetPath}' for '{node.Title}'.");
                    api.Dialogs.ShowErrorMessage(Loc.Format("LOCOneMenuCouldNotOpen", node.TargetPath), "OneMenu");
                }

                return;
            }

            if (node.FilterPresetId == null)
            {
                logger.Warn($"OneMenu: '{node.Title}' has no filter preset assigned.");
                return;
            }

            api.MainView.ApplyFilterPreset(node.FilterPresetId.Value);
            api.MainView.SwitchToLibraryView();
        }
    }
}
