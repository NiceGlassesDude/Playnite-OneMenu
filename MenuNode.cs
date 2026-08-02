using Playnite.SDK.Data;
using System;
using System.Collections.ObjectModel;

namespace OneMenu
{
    public enum MenuItemDisplayMode
    {
        IconOnly,
        TextOnly,
        IconAndText
    }

    public enum MenuActionType
    {
        FilterPreset,
        OpenPath
    }

    public enum TagBrowserSizePreset
    {
        Default,
        Bigger,
        MuchBigger
    }

    public class MenuNode : LocalObservableObject
    {
        private Guid id = Guid.NewGuid();
        public Guid Id { get => id; set => SetValue(ref id, value); }

        private string title = "New item";
        public string Title { get => title; set => SetValue(ref title, value); }

        private string iconPath;
        public string IconPath { get => iconPath; set => SetValue(ref iconPath, value); }

        private MenuItemDisplayMode displayMode = MenuItemDisplayMode.IconAndText;
        public MenuItemDisplayMode DisplayMode { get => displayMode; set => SetValue(ref displayMode, value); }

        private MenuActionType actionType = MenuActionType.FilterPreset;
        public MenuActionType ActionType
        {
            get => actionType;
            set
            {
                SetValue(ref actionType, value);
                OnPropertyChanged(nameof(IsFilterPresetAction));
                OnPropertyChanged(nameof(IsOpenPathAction));
            }
        }

        private Guid? filterPresetId;
        public Guid? FilterPresetId { get => filterPresetId; set => SetValue(ref filterPresetId, value); }

        private string targetPath;
        public string TargetPath { get => targetPath; set => SetValue(ref targetPath, value); }

        private bool isHidden;
        public bool IsHidden { get => isHidden; set => SetValue(ref isHidden, value); }

        private ObservableCollection<MenuNode> children = new ObservableCollection<MenuNode>();
        public ObservableCollection<MenuNode> Children
        {
            get => children;
            set
            {
                if (children != null)
                {
                    children.CollectionChanged -= Children_CollectionChanged;
                }

                SetValue(ref children, value);

                if (children != null)
                {
                    children.CollectionChanged += Children_CollectionChanged;
                }
            }
        }

        public MenuNode()
        {
            children.CollectionChanged += Children_CollectionChanged;
        }

        private void Children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsCategory));
            OnPropertyChanged(nameof(IsFilterPresetAction));
            OnPropertyChanged(nameof(IsOpenPathAction));
        }

        private bool isExpanded = true;
        [DontSerialize]
        public bool IsExpanded { get => isExpanded; set => SetValue(ref isExpanded, value); }

        [DontSerialize]
        public bool IsCategory => Children != null && Children.Count > 0;

        [DontSerialize]
        public bool IsFilterPresetAction => !IsCategory && ActionType == MenuActionType.FilterPreset;

        [DontSerialize]
        public bool IsOpenPathAction => !IsCategory && ActionType == MenuActionType.OpenPath;

        [DontSerialize]
        public bool ShowIcon => DisplayMode == MenuItemDisplayMode.IconOnly || DisplayMode == MenuItemDisplayMode.IconAndText;

        [DontSerialize]
        public bool ShowText => DisplayMode == MenuItemDisplayMode.TextOnly || DisplayMode == MenuItemDisplayMode.IconAndText;

        public MenuNode Clone()
        {
            var clone = new MenuNode
            {
                Id = Id,
                Title = Title,
                IconPath = IconPath,
                DisplayMode = DisplayMode,
                ActionType = ActionType,
                FilterPresetId = FilterPresetId,
                TargetPath = TargetPath,
                IsHidden = IsHidden
            };

            foreach (var child in Children)
            {
                clone.Children.Add(child.Clone());
            }

            return clone;
        }
    }
}
