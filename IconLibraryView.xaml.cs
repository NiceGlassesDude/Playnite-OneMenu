using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OneMenu
{
    public class IconTile
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public ImageSource Image { get; set; }
        public bool InUse { get; set; }
        public string UsageText { get; set; }
    }

    public partial class IconLibraryView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private OneMenuSettings settings;
        private bool pickerMode;

        public event Action<string> IconPicked;
        public event Action PickCancelled;

        public IconLibraryView()
        {
            InitializeComponent();
        }

        public void Initialize(OneMenuSettings settings, bool pickerMode)
        {
            this.settings = settings;
            this.pickerMode = pickerMode;

            IconList.SelectionMode = pickerMode ? SelectionMode.Single : SelectionMode.Extended;
            PickerButtons.Visibility = pickerMode ? Visibility.Visible : Visibility.Collapsed;
            DeleteButton.Visibility = pickerMode ? Visibility.Collapsed : Visibility.Visible;
            OpenFolderButton.Visibility = pickerMode ? Visibility.Collapsed : Visibility.Visible;
            HintText.Text = Loc.Get(pickerMode ? "LOCOneMenuIconsPickerHint" : "LOCOneMenuIconsManageHint");

            Reload(null);
        }

        public void Reload(string selectPath)
        {
            var tiles = new List<IconTile>();

            foreach (var path in IconLibrary.GetIcons())
            {
                var usage = settings?.CountIconUsage(path) ?? 0;
                tiles.Add(new IconTile
                {
                    Path = path,
                    FileName = System.IO.Path.GetFileName(path),
                    Image = ImageHelper.LoadFromFile(path, 128),
                    InUse = usage > 0,
                    UsageText = usage > 0 ? Loc.Format("LOCOneMenuIconsInUse", usage) : string.Empty
                });
            }

            IconList.ItemsSource = tiles;
            EmptyText.Visibility = tiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (!string.IsNullOrEmpty(selectPath))
            {
                var match = tiles.FirstOrDefault(t => string.Equals(t.Path, selectPath, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    IconList.SelectedItem = match;
                    IconList.ScrollIntoView(match);
                }
            }

            UpdateButtons();
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text ?? string.Empty;
            StatusText.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateButtons()
        {
            var count = IconList.SelectedItems.Count;
            DeleteButton.IsEnabled = count > 0;
            SelectButton.IsEnabled = count == 1;
        }

        private void IconList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtons();
        }

        private void IconList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!pickerMode)
            {
                return;
            }

            var item = VisualHelper.FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
            if (item?.DataContext is IconTile tile)
            {
                IconPicked?.Invoke(tile.Path);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var files = OneMenuPlugin.Api?.Dialogs?.SelectFiles(IconLibrary.FileFilter);
            if (files == null || files.Count == 0)
            {
                return;
            }

            string lastImported = null;
            var added = 0;
            var failed = 0;

            foreach (var file in files)
            {
                try
                {
                    if (!IconLibrary.IsImageFile(file))
                    {
                        failed++;
                        continue;
                    }

                    lastImported = IconLibrary.Import(file);
                    added++;
                }
                catch (Exception ex)
                {
                    failed++;
                    logger.Error(ex, $"OneMenu: couldn't add icon '{file}'.");
                }
            }

            SetStatus(failed > 0
                ? Loc.Format("LOCOneMenuIconsAddedWithErrors", added, failed)
                : Loc.Format("LOCOneMenuIconsAdded", added));

            Reload(lastImported);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var tiles = IconList.SelectedItems.Cast<IconTile>().ToList();
            if (tiles.Count == 0)
            {
                return;
            }

            var inUse = tiles.Count(t => t.InUse);
            var message = Loc.Format("LOCOneMenuIconsDeleteMessage", tiles.Count);
            if (inUse > 0)
            {
                message += Environment.NewLine + Environment.NewLine + Loc.Format("LOCOneMenuIconsDeleteInUseWarning", inUse);
            }

            var confirm = new ConfirmDialog(Loc.Get("LOCOneMenuIconsDeleteTitle"), message, Loc.Get("LOCOneMenuIconsDeleteConfirm"))
            {
                Owner = Window.GetWindow(this)
            };

            if (confirm.ShowDialog() != true)
            {
                return;
            }

            IconList.ItemsSource = null;

            var deleted = 0;
            var failed = 0;

            foreach (var tile in tiles)
            {
                if (IconLibrary.TryDelete(tile.Path))
                {
                    settings?.ClearIconReferences(tile.Path);
                    deleted++;
                }
                else
                {
                    failed++;
                }
            }

            SetStatus(failed > 0
                ? Loc.Format("LOCOneMenuIconsDeletedWithErrors", deleted, failed)
                : Loc.Format("LOCOneMenuIconsDeleted", deleted));

            Reload(null);
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(IconLibrary.FolderPath);
                Process.Start(new ProcessStartInfo(IconLibrary.FolderPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "OneMenu: couldn't open the icon folder.");
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (IconList.SelectedItem is IconTile tile)
            {
                IconPicked?.Invoke(tile.Path);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            PickCancelled?.Invoke();
        }
    }
}
