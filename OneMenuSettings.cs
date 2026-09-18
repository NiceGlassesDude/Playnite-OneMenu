using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace OneMenu
{
    public class OptionItem
    {
        public object Value { get; set; }
        public string DisplayText { get; set; }
    }

    public class OneMenuSettings : LocalObservableObject, ISettings
    {
        public const int MaxSideButtons = 15;

        private readonly OneMenuPlugin plugin;

        private List<MenuNode> editingSnapshot;
        private string editingMainIconPath;
        private bool editingTagSearchEnabled;
        private bool editingFollowPlayniteTheme;
        private double editingTagBrowserOpacity;
        private TagBrowserSizePreset editingTagBrowserSize;
        private TagBrowserButtonPlacement editingTagBrowserPlacement;
        private bool editingTagGenreManagerTopPanel;
        private bool editingTagBrowserUseOneMenuTheme;
        private bool normalizingSideButtons;

        private ObservableCollection<MenuNode> rootNodes = new ObservableCollection<MenuNode>();
        public ObservableCollection<MenuNode> RootNodes
        {
            get => rootNodes;
            set
            {
                SetValue(ref rootNodes, value);
                NormalizeSideButtonOrder();
            }
        }

        private string mainIconPath;
        public string MainIconPath
        {
            get => mainIconPath;
            set
            {
                SetValue(ref mainIconPath, value);
                OnPropertyChanged(nameof(MainIconPreviewPath));
            }
        }

        [DontSerialize]
        public string MainIconPreviewPath => !string.IsNullOrEmpty(MainIconPath) && File.Exists(MainIconPath) ? MainIconPath : plugin?.DefaultIconPath;

        private MenuNode selectedNode;
        [DontSerialize]
        public MenuNode SelectedNode
        {
            get => selectedNode;
            set
            {
                if (selectedNode != null)
                {
                    selectedNode.PropertyChanged -= SelectedNode_PropertyChanged;
                }

                SetValue(ref selectedNode, value);

                if (selectedNode != null)
                {
                    selectedNode.PropertyChanged += SelectedNode_PropertyChanged;
                }

                OnPropertyChanged(nameof(SideButtonPositionText));
            }
        }

        [DontSerialize]
        public string SideButtonPositionText
        {
            get
            {
                if (SelectedNode == null || !SelectedNode.ShowInSidebar)
                {
                    return string.Empty;
                }

                return Loc.Format("LOCOneMenuSideButtonPosition", SelectedNode.SidebarPosition, GetAllSideButtonNodes().Count);
            }
        }

        private bool showMainIconEditor;
        [DontSerialize]
        public bool ShowMainIconEditor { get => showMainIconEditor; set => SetValue(ref showMainIconEditor, value); }

        private bool tagSearchEnabled;
        public bool TagSearchEnabled { get => tagSearchEnabled; set => SetValue(ref tagSearchEnabled, value); }

        private bool followPlayniteTheme;
        public bool FollowPlayniteTheme
        {
            get => followPlayniteTheme;
            set
            {
                SetValue(ref followPlayniteTheme, value);
                OneMenuPlugin.FollowPlayniteTheme = value;
            }
        }

        private double tagBrowserOpacity = 1.0;
        public double TagBrowserOpacity
        {
            get => tagBrowserOpacity;
            set
            {
                SetValue(ref tagBrowserOpacity, value);
                OnPropertyChanged(nameof(TagBrowserTransparencyPercent));
                OnPropertyChanged(nameof(TagBrowserTransparencyText));
            }
        }

        [DontSerialize]
        public double TagBrowserTransparencyPercent
        {
            get => (1.0 - TagBrowserOpacity) * 100.0;
            set => TagBrowserOpacity = 1.0 - (value / 100.0);
        }

        [DontSerialize]
        public string TagBrowserTransparencyText => Loc.Format("LOCOneMenuAdvTransparencyValue", Math.Round(TagBrowserTransparencyPercent));

        private TagBrowserSizePreset tagBrowserSize = TagBrowserSizePreset.Default;
        public TagBrowserSizePreset TagBrowserSize { get => tagBrowserSize; set => SetValue(ref tagBrowserSize, value); }

        private TagBrowserButtonPlacement tagBrowserPlacement = TagBrowserButtonPlacement.FlyoutMenu;
        public TagBrowserButtonPlacement TagBrowserPlacement { get => tagBrowserPlacement; set => SetValue(ref tagBrowserPlacement, value); }

        private bool tagGenreManagerTopPanel;
        public bool TagGenreManagerTopPanel { get => tagGenreManagerTopPanel; set => SetValue(ref tagGenreManagerTopPanel, value); }

        private bool tagBrowserUseOneMenuTheme;
        public bool TagBrowserUseOneMenuTheme { get => tagBrowserUseOneMenuTheme; set => SetValue(ref tagBrowserUseOneMenuTheme, value); }

        private bool welcomeShown;
        public bool WelcomeShown { get => welcomeShown; set => SetValue(ref welcomeShown, value); }

        [DontSerialize]
        public bool ShowTagBrowserInFlyout => TagSearchEnabled && TagBrowserPlacement != TagBrowserButtonPlacement.TopPanel;

        [DontSerialize]
        public bool ShowTagBrowserInTopPanel => TagSearchEnabled && TagBrowserPlacement != TagBrowserButtonPlacement.FlyoutMenu;

        private List<OptionItem> tagBrowserSizeOptions;
        [DontSerialize]
        public List<OptionItem> TagBrowserSizeOptions => tagBrowserSizeOptions ?? (tagBrowserSizeOptions = new List<OptionItem>
        {
            new OptionItem { Value = TagBrowserSizePreset.Default, DisplayText = Loc.Format("LOCOneMenuAdvSizeDefault", "760x560") },
            new OptionItem { Value = TagBrowserSizePreset.Bigger, DisplayText = "960x720" },
            new OptionItem { Value = TagBrowserSizePreset.MuchBigger, DisplayText = "1200x880" }
        });

        private List<OptionItem> tagBrowserPlacementOptions;
        [DontSerialize]
        public List<OptionItem> TagBrowserPlacementOptions => tagBrowserPlacementOptions ?? (tagBrowserPlacementOptions = new List<OptionItem>
        {
            new OptionItem { Value = TagBrowserButtonPlacement.FlyoutMenu, DisplayText = Loc.Get("LOCOneMenuPlacementFlyout") },
            new OptionItem { Value = TagBrowserButtonPlacement.TopPanel, DisplayText = Loc.Get("LOCOneMenuPlacementTopPanel") },
            new OptionItem { Value = TagBrowserButtonPlacement.Both, DisplayText = Loc.Get("LOCOneMenuPlacementBoth") }
        });

        private List<OptionItem> displayModeOptions;
        [DontSerialize]
        public List<OptionItem> DisplayModeOptions => displayModeOptions ?? (displayModeOptions = new List<OptionItem>
        {
            new OptionItem { Value = MenuItemDisplayMode.IconOnly, DisplayText = Loc.Get("LOCOneMenuDisplayIconOnly") },
            new OptionItem { Value = MenuItemDisplayMode.TextOnly, DisplayText = Loc.Get("LOCOneMenuDisplayTextOnly") },
            new OptionItem { Value = MenuItemDisplayMode.IconAndText, DisplayText = Loc.Get("LOCOneMenuDisplayIconAndText") }
        });

        private List<OptionItem> actionTypeOptions;
        [DontSerialize]
        public List<OptionItem> ActionTypeOptions => actionTypeOptions ?? (actionTypeOptions = new List<OptionItem>
        {
            new OptionItem { Value = MenuActionType.FilterPreset, DisplayText = Loc.Get("LOCOneMenuActionFilterPreset") },
            new OptionItem { Value = MenuActionType.OpenPath, DisplayText = Loc.Get("LOCOneMenuActionOpenPath") }
        });

        [DontSerialize]
        public List<FilterPreset> AvailableFilterPresets { get; private set; } = new List<FilterPreset>();

        [DontSerialize]
        public RelayCommand AddRootNodeCommand { get; }

        [DontSerialize]
        public RelayCommand AddChildNodeCommand { get; }

        [DontSerialize]
        public RelayCommand RemoveSelectedNodeCommand { get; }

        [DontSerialize]
        public RelayCommand MoveSelectedUpCommand { get; }

        [DontSerialize]
        public RelayCommand MoveSelectedDownCommand { get; }

        [DontSerialize]
        public RelayCommand MoveSideButtonUpCommand { get; }

        [DontSerialize]
        public RelayCommand MoveSideButtonDownCommand { get; }

        [DontSerialize]
        public RelayCommand BrowseIconCommand { get; }

        [DontSerialize]
        public RelayCommand PickIconFromLibraryCommand { get; }

        [DontSerialize]
        public RelayCommand ClearIconCommand { get; }

        [DontSerialize]
        public RelayCommand BrowseTargetFileCommand { get; }

        [DontSerialize]
        public RelayCommand BrowseTargetFolderCommand { get; }

        [DontSerialize]
        public RelayCommand ToggleMainIconEditorCommand { get; }

        [DontSerialize]
        public RelayCommand BrowseMainIconCommand { get; }

        [DontSerialize]
        public RelayCommand PickMainIconFromLibraryCommand { get; }

        [DontSerialize]
        public RelayCommand ResetMainIconCommand { get; }

        [DontSerialize]
        public RelayCommand OpenAdvancedSettingsCommand { get; }

        [DontSerialize]
        public RelayCommand ShowWelcomeGuideCommand { get; }

        public OneMenuSettings()
        {
            AddRootNodeCommand = new RelayCommand(() =>
            {
                var node = new MenuNode { Title = Loc.Get("LOCOneMenuNewCategory") };
                RootNodes.Add(node);
                SelectedNode = node;
            });

            AddChildNodeCommand = new RelayCommand(() =>
            {
                if (SelectedNode == null)
                {
                    return;
                }

                var child = new MenuNode { Title = Loc.Get("LOCOneMenuNewItem") };
                SelectedNode.Children.Add(child);
                SelectedNode.IsExpanded = true;
                SelectedNode = child;
            });

            RemoveSelectedNodeCommand = new RelayCommand(() =>
            {
                if (SelectedNode == null)
                {
                    return;
                }

                RemoveNode(RootNodes, SelectedNode);
                SelectedNode = null;
                NormalizeSideButtonOrder();
            });

            MoveSideButtonUpCommand = new RelayCommand(() => MoveSideButton(-1));
            MoveSideButtonDownCommand = new RelayCommand(() => MoveSideButton(1));

            MoveSelectedUpCommand = new RelayCommand(() => MoveSelected(-1));
            MoveSelectedDownCommand = new RelayCommand(() => MoveSelected(1));

            BrowseIconCommand = new RelayCommand(() =>
            {
                if (SelectedNode == null)
                {
                    return;
                }

                var path = OneMenuPlugin.Api?.Dialogs?.SelectFile(IconLibrary.FileFilter, null);
                if (!string.IsNullOrEmpty(path))
                {
                    SelectedNode.IconPath = ImportIcon(path);
                }
            });

            PickIconFromLibraryCommand = new RelayCommand(() =>
            {
                if (SelectedNode == null)
                {
                    return;
                }

                var picked = PickIconFromLibrary();
                if (!string.IsNullOrEmpty(picked))
                {
                    SelectedNode.IconPath = picked;
                }
            });

            ClearIconCommand = new RelayCommand(() =>
            {
                if (SelectedNode != null)
                {
                    SelectedNode.IconPath = null;
                }
            });

            BrowseTargetFileCommand = new RelayCommand(() =>
            {
                if (SelectedNode == null)
                {
                    return;
                }

                var path = OneMenuPlugin.Api?.Dialogs?.SelectFile(Loc.Get("LOCOneMenuAllFiles") + "|*.*", null);
                if (!string.IsNullOrEmpty(path))
                {
                    SelectedNode.TargetPath = path;
                }
            });

            BrowseTargetFolderCommand = new RelayCommand(() =>
            {
                if (SelectedNode == null)
                {
                    return;
                }

                var path = OneMenuPlugin.Api?.Dialogs?.SelectFolder();
                if (!string.IsNullOrEmpty(path))
                {
                    SelectedNode.TargetPath = path;
                }
            });

            ToggleMainIconEditorCommand = new RelayCommand(() =>
            {
                ShowMainIconEditor = !ShowMainIconEditor;
            });

            BrowseMainIconCommand = new RelayCommand(() =>
            {
                var path = OneMenuPlugin.Api?.Dialogs?.SelectFile(IconLibrary.FileFilter, null);
                if (!string.IsNullOrEmpty(path))
                {
                    MainIconPath = ImportIcon(path);
                }
            });

            PickMainIconFromLibraryCommand = new RelayCommand(() =>
            {
                var picked = PickIconFromLibrary();
                if (!string.IsNullOrEmpty(picked))
                {
                    MainIconPath = picked;
                }
            });

            ResetMainIconCommand = new RelayCommand(() =>
            {
                MainIconPath = null;
            });

            OpenAdvancedSettingsCommand = new RelayCommand(() =>
            {
                var window = new AdvancedSettingsWindow(this)
                {
                    Owner = WindowLauncher.GetActiveWindow()
                };
                window.ShowDialog();
            });

            ShowWelcomeGuideCommand = new RelayCommand(() => WindowLauncher.OpenWelcome());
        }

        public OneMenuSettings(OneMenuPlugin plugin) : this()
        {
            this.plugin = plugin;

            var savedSettings = plugin.LoadPluginSettings<OneMenuSettings>();
            if (savedSettings?.RootNodes != null)
            {
                RootNodes = savedSettings.RootNodes;
            }

            MainIconPath = savedSettings?.MainIconPath;
            TagSearchEnabled = savedSettings?.TagSearchEnabled ?? false;
            FollowPlayniteTheme = savedSettings?.FollowPlayniteTheme ?? false;
            TagBrowserOpacity = savedSettings?.TagBrowserOpacity ?? 1.0;
            TagBrowserSize = savedSettings?.TagBrowserSize ?? TagBrowserSizePreset.Default;
            TagBrowserPlacement = savedSettings?.TagBrowserPlacement ?? TagBrowserButtonPlacement.FlyoutMenu;
            TagGenreManagerTopPanel = savedSettings?.TagGenreManagerTopPanel ?? false;
            TagBrowserUseOneMenuTheme = savedSettings?.TagBrowserUseOneMenuTheme ?? false;
            WelcomeShown = savedSettings?.WelcomeShown ?? false;

            RefreshAvailableFilterPresets();
        }

        public void RefreshAvailableFilterPresets()
        {
            if (plugin?.PlayniteApi?.Database?.FilterPresets == null)
            {
                return;
            }

            AvailableFilterPresets = plugin.PlayniteApi.Database.FilterPresets.OrderBy(p => p.Name).ToList();
            OnPropertyChanged(nameof(AvailableFilterPresets));
        }

        public IEnumerable<MenuNode> EnumerateNodes()
        {
            return EnumerateNodes(RootNodes);
        }

        private static IEnumerable<MenuNode> EnumerateNodes(IEnumerable<MenuNode> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;

                foreach (var child in EnumerateNodes(node.Children))
                {
                    yield return child;
                }
            }
        }

        public List<MenuNode> GetSideButtonNodes()
        {
            var result = new List<MenuNode>();

            void Collect(IEnumerable<MenuNode> nodes)
            {
                foreach (var node in nodes)
                {
                    if (node.IsHidden)
                    {
                        continue;
                    }

                    if (node.ShowInSidebar)
                    {
                        result.Add(node);
                    }

                    Collect(node.Children);
                }
            }

            Collect(RootNodes);
            return result.OrderBy(SideButtonSortKey).ToList();
        }

        private static int SideButtonSortKey(MenuNode node)
        {
            return node.SidebarPosition > 0 ? node.SidebarPosition : int.MaxValue;
        }

        private List<MenuNode> GetAllSideButtonNodes()
        {
            return EnumerateNodes().Where(n => n.ShowInSidebar).OrderBy(SideButtonSortKey).ToList();
        }

        public void NormalizeSideButtonOrder()
        {
            if (normalizingSideButtons || rootNodes == null)
            {
                return;
            }

            normalizingSideButtons = true;
            try
            {
                var ordered = GetAllSideButtonNodes();
                for (var i = 0; i < ordered.Count; i++)
                {
                    ordered[i].SidebarPosition = i + 1;
                }

                foreach (var node in EnumerateNodes().ToList())
                {
                    if (!node.ShowInSidebar && node.SidebarPosition != 0)
                    {
                        node.SidebarPosition = 0;
                    }
                }
            }
            finally
            {
                normalizingSideButtons = false;
            }

            OnPropertyChanged(nameof(SideButtonPositionText));
        }

        private void MoveSideButton(int offset)
        {
            if (SelectedNode == null || !SelectedNode.ShowInSidebar)
            {
                return;
            }

            NormalizeSideButtonOrder();

            var ordered = GetAllSideButtonNodes();
            var index = ordered.IndexOf(SelectedNode);
            var newIndex = index + offset;
            if (index < 0 || newIndex < 0 || newIndex >= ordered.Count)
            {
                return;
            }

            var other = ordered[newIndex];
            var position = SelectedNode.SidebarPosition;
            SelectedNode.SidebarPosition = other.SidebarPosition;
            other.SidebarPosition = position;

            NormalizeSideButtonOrder();
        }

        private void SelectedNode_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MenuNode.ShowInSidebar))
            {
                NormalizeSideButtonOrder();
            }
        }

        public int CountIconUsage(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath))
            {
                return 0;
            }

            var count = EnumerateNodes().Count(n => string.Equals(n.IconPath, iconPath, StringComparison.OrdinalIgnoreCase));
            if (string.Equals(MainIconPath, iconPath, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }

            return count;
        }

        public void ClearIconReferences(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath))
            {
                return;
            }

            foreach (var node in EnumerateNodes().ToList())
            {
                if (string.Equals(node.IconPath, iconPath, StringComparison.OrdinalIgnoreCase))
                {
                    node.IconPath = null;
                }
            }

            if (string.Equals(MainIconPath, iconPath, StringComparison.OrdinalIgnoreCase))
            {
                MainIconPath = null;
            }
        }

        private string ImportIcon(string sourcePath)
        {
            try
            {
                return IconLibrary.Import(sourcePath);
            }
            catch (Exception ex)
            {
                LogManager.GetLogger().Error(ex, $"OneMenu: couldn't copy icon '{sourcePath}' into the icon library.");
                return sourcePath;
            }
        }

        private string PickIconFromLibrary()
        {
            var window = new IconPickerWindow(this)
            {
                Owner = WindowLauncher.GetActiveWindow()
            };

            return window.ShowDialog() == true ? window.SelectedPath : null;
        }

        private bool RemoveNode(ObservableCollection<MenuNode> collection, MenuNode target)
        {
            if (collection.Remove(target))
            {
                return true;
            }

            foreach (var node in collection)
            {
                if (RemoveNode(node.Children, target))
                {
                    return true;
                }
            }

            return false;
        }

        private ObservableCollection<MenuNode> FindParentCollection(ObservableCollection<MenuNode> collection, MenuNode target)
        {
            if (collection.Contains(target))
            {
                return collection;
            }

            foreach (var node in collection)
            {
                var found = FindParentCollection(node.Children, target);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void MoveSelected(int offset)
        {
            if (SelectedNode == null)
            {
                return;
            }

            var parentCollection = FindParentCollection(RootNodes, SelectedNode);
            if (parentCollection == null)
            {
                return;
            }

            var index = parentCollection.IndexOf(SelectedNode);
            var newIndex = index + offset;
            if (newIndex < 0 || newIndex >= parentCollection.Count)
            {
                return;
            }

            parentCollection.Move(index, newIndex);
        }

        public void BeginEdit()
        {
            editingSnapshot = RootNodes.Select(n => n.Clone()).ToList();
            editingMainIconPath = MainIconPath;
            editingTagSearchEnabled = TagSearchEnabled;
            editingFollowPlayniteTheme = FollowPlayniteTheme;
            editingTagBrowserOpacity = TagBrowserOpacity;
            editingTagBrowserSize = TagBrowserSize;
            editingTagBrowserPlacement = TagBrowserPlacement;
            editingTagGenreManagerTopPanel = TagGenreManagerTopPanel;
            editingTagBrowserUseOneMenuTheme = TagBrowserUseOneMenuTheme;
        }

        public void CancelEdit()
        {
            if (editingSnapshot != null)
            {
                RootNodes = new ObservableCollection<MenuNode>(editingSnapshot);
            }

            MainIconPath = editingMainIconPath;
            TagSearchEnabled = editingTagSearchEnabled;
            FollowPlayniteTheme = editingFollowPlayniteTheme;
            TagBrowserOpacity = editingTagBrowserOpacity;
            TagBrowserSize = editingTagBrowserSize;
            TagBrowserPlacement = editingTagBrowserPlacement;
            TagGenreManagerTopPanel = editingTagGenreManagerTopPanel;
            TagBrowserUseOneMenuTheme = editingTagBrowserUseOneMenuTheme;
            ShowMainIconEditor = false;
            SelectedNode = null;
            plugin?.ApplyUiSettings();
        }

        public void EndEdit()
        {
            plugin?.SavePluginSettings(this);
            plugin?.ApplyUiSettings();
        }

        public bool VerifySettings(out List<string> errors)
        {
            var foundErrors = new List<string>();

            void Validate(IEnumerable<MenuNode> nodes)
            {
                foreach (var node in nodes)
                {
                    if (string.IsNullOrWhiteSpace(node.Title))
                    {
                        foundErrors.Add(Loc.Get("LOCOneMenuErrorTitleRequired"));
                    }

                    if (!node.IsCategory && node.ActionType == MenuActionType.OpenPath &&
                        !string.IsNullOrEmpty(node.TargetPath) && !File.Exists(node.TargetPath) && !Directory.Exists(node.TargetPath))
                    {
                        foundErrors.Add(Loc.Format("LOCOneMenuErrorTargetNotFound", node.Title, node.TargetPath));
                    }

                    Validate(node.Children);
                }
            }

            Validate(RootNodes);

            if (GetSideButtonNodes().Count > MaxSideButtons)
            {
                foundErrors.Add(Loc.Format("LOCOneMenuErrorTooManySideButtons", MaxSideButtons));
            }

            errors = foundErrors;
            return errors.Count == 0;
        }
    }
}
