using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Controls;

namespace OneMenu
{
    public class OneMenuPlugin : GenericPlugin
    {
        public override Guid Id { get; } = Guid.Parse("24d67a51-c903-4b2d-812c-f13f556cd4e1");

        private readonly OneMenuSettings settings;
        private readonly string pluginFolder;
        private SidebarItem sidebarItem;

        public static IPlayniteAPI Api { get; private set; }

        public string DefaultIconPath => Path.Combine(pluginFolder, "icon.png");

        public OneMenuPlugin(IPlayniteAPI api) : base(api)
        {
            Api = api;
            pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            settings = new OneMenuSettings(this);

            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
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
                    var menu = MenuBuilder.BuildContextMenu(settings.RootNodes);
                    menu.IsOpen = true;
                }
            };

            yield return sidebarItem;
        }

        public void RefreshSidebarIcon()
        {
            if (sidebarItem != null)
            {
                sidebarItem.Icon = GetEffectiveIconPath();
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
}
