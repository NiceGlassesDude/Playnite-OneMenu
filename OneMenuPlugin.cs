using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace OneMenu
{
    public class OneMenuPlugin : GenericPlugin
    {
        public override Guid Id { get; } = Guid.Parse("24d67a51-c903-4b2d-812c-f13f556cd4e1");

        private readonly OneMenuSettings settings;
        private readonly string pluginFolder;
        private SidebarItem sidebarItem;
        private TopPanelItem tagBrowserTopPanelItem;
        private TopPanelItem tagGenreManagerTopPanelItem;
        private readonly List<SidebarItem> sideButtonItems = new List<SidebarItem>();
        private readonly List<MenuNode> sideButtonNodes = new List<MenuNode>();
        private readonly List<SideButtonVisual> sideButtonVisuals = new List<SideButtonVisual>();

        public static IPlayniteAPI Api { get; private set; }
        public static bool FollowPlayniteTheme { get; set; }
        public static string PluginDataPath { get; private set; }
        public static string DefaultIconFile { get; private set; }

        public string DefaultIconPath => Path.Combine(pluginFolder, "icon.png");

        public OneMenuPlugin(IPlayniteAPI api) : base(api)
        {
            Api = api;
            pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            PluginDataPath = GetPluginUserDataPath();
            DefaultIconFile = DefaultIconPath;
            settings = new OneMenuSettings(this);

            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            LibraryIndex.Attach(PlayniteApi);

            if (settings.TagSearchEnabled || settings.TagGenreManagerTopPanel)
            {
                LibraryIndex.Warm();
            }

            ShowWelcomeOnFirstRun();
        }

        private void ShowWelcomeOnFirstRun()
        {
            if (settings.WelcomeShown || PlayniteApi.ApplicationInfo.Mode != ApplicationMode.Desktop)
            {
                return;
            }

            settings.WelcomeShown = true;
            SavePluginSettings(settings);

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            dispatcher.BeginInvoke(new Action(() => WindowLauncher.OpenWelcome()), DispatcherPriority.ApplicationIdle);
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            sidebarItem = new SidebarItem
            {
                Title = "OneMenu",
                Icon = GetEffectiveIconPath(),
                Type = SiderbarItemType.Button,
                Activated = () =>
                {
                    var menu = MenuBuilder.BuildContextMenu(settings);
                    if (menu.Items.Count > 0)
                    {
                        menu.IsOpen = true;
                    }
                }
            };

            var items = new List<SidebarItem> { sidebarItem };

            sideButtonItems.Clear();
            sideButtonNodes.Clear();
            sideButtonVisuals.Clear();

            for (var i = 0; i < OneMenuSettings.MaxSideButtons; i++)
            {
                var slot = i;
                var visual = new SideButtonVisual();
                var sideItem = new SidebarItem
                {
                    Title = GetSideButtonTitle(slot, null),
                    Icon = visual.Root,
                    Type = SiderbarItemType.Button,
                    Visible = false,
                    Activated = () => ActivateSideButton(slot)
                };

                sideButtonItems.Add(sideItem);
                sideButtonNodes.Add(null);
                sideButtonVisuals.Add(visual);
                items.Add(sideItem);
            }

            RefreshSideButtons();
            return items;
        }

        private static string GetSideButtonTitle(int slot, string nodeTitle)
        {
            var prefix = "OneMenu " + (slot + 1).ToString("00");
            return string.IsNullOrWhiteSpace(nodeTitle) ? prefix : prefix + " - " + nodeTitle.Trim();
        }

        private void ActivateSideButton(int slot)
        {
            if (slot < 0 || slot >= sideButtonNodes.Count)
            {
                return;
            }

            MenuBuilder.ActivateNode(sideButtonNodes[slot]);
        }

        private void RefreshSideButtons()
        {
            var nodes = settings.GetSideButtonNodes();

            for (var i = 0; i < sideButtonItems.Count; i++)
            {
                var item = sideButtonItems[i];

                if (i < nodes.Count)
                {
                    var node = nodes[i];
                    sideButtonNodes[i] = node;
                    item.Title = GetSideButtonTitle(i, node.Title);
                    sideButtonVisuals[i].Show(node);
                    item.Visible = true;
                }
                else
                {
                    sideButtonNodes[i] = null;
                    item.Title = GetSideButtonTitle(i, null);
                    item.Visible = false;
                    sideButtonVisuals[i].Clear();
                }
            }
        }

        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            tagBrowserTopPanelItem = new TopPanelItem
            {
                Title = Loc.Get("LOCOneMenuTagBrowser"),
                Icon = CreateGlyph("\xE721"),
                Visible = settings.ShowTagBrowserInTopPanel,
                Activated = () => WindowLauncher.OpenTagBrowser(settings)
            };

            tagGenreManagerTopPanelItem = new TopPanelItem
            {
                Title = Loc.Get("LOCOneMenuTagGenreManager"),
                Icon = CreateGlyph("\xE8EC"),
                Visible = settings.TagGenreManagerTopPanel,
                Activated = () => WindowLauncher.OpenTagGenreManager()
            };

            return new List<TopPanelItem> { tagBrowserTopPanelItem, tagGenreManagerTopPanelItem };
        }

        private static TextBlock CreateGlyph(string glyph)
        {
            return new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        public void ApplyUiSettings()
        {
            if (sidebarItem != null)
            {
                sidebarItem.Icon = GetEffectiveIconPath();
            }

            RefreshSideButtons();

            if (tagBrowserTopPanelItem != null)
            {
                tagBrowserTopPanelItem.Visible = settings.ShowTagBrowserInTopPanel;
            }

            if (tagGenreManagerTopPanelItem != null)
            {
                tagGenreManagerTopPanelItem.Visible = settings.TagGenreManagerTopPanel;
            }

            if (settings.TagSearchEnabled || settings.TagGenreManagerTopPanel)
            {
                LibraryIndex.Warm();
            }
        }

        private string GetEffectiveIconPath()
        {
            if (!string.IsNullOrEmpty(settings.MainIconPath) && File.Exists(settings.MainIconPath))
            {
                return settings.MainIconPath;
            }

            return DefaultIconPath;
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            settings.RefreshAvailableFilterPresets();
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new OneMenuSettingsView();
        }
    }

    public class SideButtonVisual
    {
        private readonly Image image;
        private readonly TextBlock letter;

        public Grid Root { get; }

        public SideButtonVisual()
        {
            image = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            letter = new TextBlock
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            Root = new Grid();
            Root.Children.Add(image);
            Root.Children.Add(letter);
        }

        public void Show(MenuNode node)
        {
            var source = ImageHelper.LoadFromFile(node.IconPath, 0);

            if (source != null)
            {
                image.Source = source;
                image.Visibility = Visibility.Visible;
                letter.Visibility = Visibility.Collapsed;
                return;
            }

            var title = node.Title?.Trim();
            letter.Text = string.IsNullOrEmpty(title) ? "?" : title.Substring(0, 1).ToUpperInvariant();
            letter.Visibility = Visibility.Visible;
            image.Source = null;
            image.Visibility = Visibility.Collapsed;
        }

        public void Clear()
        {
            image.Source = null;
            image.Visibility = Visibility.Collapsed;
            letter.Visibility = Visibility.Collapsed;
        }
    }
}
