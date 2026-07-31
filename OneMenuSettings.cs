using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace OneMenu
{
    public class OneMenuSettings : LocalObservableObject, ISettings
    {
        private readonly OneMenuPlugin plugin;

        private List<MenuNode> editingSnapshot;
        private string editingMainIconPath;

        private ObservableCollection<MenuNode> rootNodes = new ObservableCollection<MenuNode>();
        public ObservableCollection<MenuNode> RootNodes { get => rootNodes; set => SetValue(ref rootNodes, value); }

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
        public string MainIconPreviewPath => !string.IsNullOrEmpty(MainIconPath) ? MainIconPath : plugin?.DefaultIconPath;

        private MenuNode selectedNode;
        [DontSerialize]
        public MenuNode SelectedNode { get => selectedNode; set => SetValue(ref selectedNode, value); }

        private bool showMainIconEditor;
        [DontSerialize]
        public bool ShowMainIconEditor { get => showMainIconEditor; set => SetValue(ref showMainIconEditor, value); }

        [DontSerialize]
        public List<FilterPreset> AvailableFilterPresets { get; private set; } = new List<FilterPreset>();

        [DontSerialize]
        public Array DisplayModeValues => Enum.GetValues(typeof(MenuItemDisplayMode));

        [DontSerialize]
        public Array ActionTypeValues => Enum.GetValues(typeof(MenuActionType));

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
        public RelayCommand BrowseIconCommand { get; }

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
        public RelayCommand ResetMainIconCommand { get; }

        public OneMenuSettings()
        {
            AddRootNodeCommand = new RelayCommand(() =>
            {
                var node = new MenuNode { Title = "New category" };
                RootNodes.Add(node);
                SelectedNode = node;
            });

            AddChildNodeCommand = new RelayCommand(() =>
            {
                if (SelectedNode == null)
                {
                    return;
                }

                var child = new MenuNode { Title = "New item" };
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

                if (!RemoveNode(RootNodes, SelectedNode))
                {
                    RemoveNodeRecursive(RootNodes, SelectedNode);
                }

                SelectedNode = null;
            });

            MoveSelectedUpCommand = new RelayCommand(() => MoveSelected(-1));
            MoveSelectedDownCommand = new RelayCommand(() => MoveSelected(1));

            BrowseIconCommand = new RelayCommand(() =>
            {
                if (SelectedNode == null)
                {
                    return;
                }

                var path = plugin?.PlayniteApi?.Dialogs?.SelectFile("Image files|*.png;*.jpg;*.jpeg;*.ico;*.bmp", null);
                if (!string.IsNullOrEmpty(path))
                {
                    SelectedNode.IconPath = CopyIconToDataFolder(path);
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

                var path = plugin?.PlayniteApi?.Dialogs?.SelectFile("All files|*.*", null);
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

                var path = plugin?.PlayniteApi?.Dialogs?.SelectFolder();
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
                var path = plugin?.PlayniteApi?.Dialogs?.SelectFile("Image files|*.png;*.jpg;*.jpeg;*.ico;*.bmp", null);
                if (!string.IsNullOrEmpty(path))
                {
                    MainIconPath = CopyIconToDataFolder(path);
                }
            });

            ResetMainIconCommand = new RelayCommand(() =>
            {
                MainIconPath = null;
            });
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

        private string CopyIconToDataFolder(string sourcePath)
        {
            if (plugin == null || string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                return sourcePath;
            }

            var dataFolder = Path.Combine(plugin.GetPluginUserDataPath(), "icons");
            Directory.CreateDirectory(dataFolder);

            var extension = Path.GetExtension(sourcePath);
            var destPath = Path.Combine(dataFolder, Guid.NewGuid().ToString("N") + extension);

            File.Copy(sourcePath, destPath, true);
            return destPath;
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

        private void RemoveNodeRecursive(ObservableCollection<MenuNode> collection, MenuNode target)
        {
            RemoveNode(collection, target);
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
        }

        public void CancelEdit()
        {
            if (editingSnapshot != null)
            {
                RootNodes = new ObservableCollection<MenuNode>(editingSnapshot);
            }

            MainIconPath = editingMainIconPath;
            ShowMainIconEditor = false;
            SelectedNode = null;
        }

        public void EndEdit()
        {
            plugin?.SavePluginSettings(this);
            plugin?.RefreshSidebarIcon();
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
                        foundErrors.Add("Every menu item needs a title.");
                    }

                    if (node.ShowIcon && !string.IsNullOrEmpty(node.IconPath) && !File.Exists(node.IconPath))
                    {
                        foundErrors.Add($"Icon file not found for '{node.Title}': {node.IconPath}");
                    }

                    if (!node.IsCategory && node.ActionType == MenuActionType.OpenPath &&
                        !string.IsNullOrEmpty(node.TargetPath) && !File.Exists(node.TargetPath) && !Directory.Exists(node.TargetPath))
                    {
                        foundErrors.Add($"File or folder not found for '{node.Title}': {node.TargetPath}");
                    }

                    Validate(node.Children);
                }
            }

            Validate(RootNodes);

            if (!string.IsNullOrEmpty(MainIconPath) && !File.Exists(MainIconPath))
            {
                foundErrors.Add($"Main icon file not found: {MainIconPath}");
            }

            errors = foundErrors;
            return errors.Count == 0;
        }
    }
}
