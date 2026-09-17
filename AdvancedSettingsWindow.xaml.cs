using System.Windows;
using System.Windows.Controls;

namespace OneMenu
{
    public partial class AdvancedSettingsWindow : Window
    {
        private readonly OneMenuSettings settings;

        public AdvancedSettingsWindow(OneMenuSettings settings)
        {
            InitializeComponent();
            this.settings = settings;
            DataContext = settings;
            OneMenuTheme.Apply(this, !OneMenuPlugin.FollowPlayniteTheme);
            ManageIconsView.Initialize(settings, false);
            CategoryList.SelectedIndex = 0;
        }

        private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TagBrowserPanel == null || ThemePanel == null || TagsGenresPanel == null ||
                UiSettingsPanel == null || ManageIconsPanel == null || ImportExportPanel == null)
            {
                return;
            }

            TagBrowserPanel.Visibility = Visibility.Collapsed;
            ThemePanel.Visibility = Visibility.Collapsed;
            TagsGenresPanel.Visibility = Visibility.Collapsed;
            UiSettingsPanel.Visibility = Visibility.Collapsed;
            ManageIconsPanel.Visibility = Visibility.Collapsed;
            ImportExportPanel.Visibility = Visibility.Collapsed;

            switch (CategoryList.SelectedIndex)
            {
                case 0:
                    TagBrowserPanel.Visibility = Visibility.Visible;
                    break;
                case 1:
                    ThemePanel.Visibility = Visibility.Visible;
                    break;
                case 2:
                    TagsGenresPanel.Visibility = Visibility.Visible;
                    break;
                case 3:
                    UiSettingsPanel.Visibility = Visibility.Visible;
                    break;
                case 4:
                    ManageIconsView.Reload(null);
                    ManageIconsPanel.Visibility = Visibility.Visible;
                    break;
                case 5:
                    ImportExportPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void FollowPlayniteThemeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var followPlaynite = FollowPlayniteThemeCheckBox.IsChecked == true;
            OneMenuTheme.Apply(this, !followPlaynite);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var includeTags = ExportTagsCheckBox.IsChecked == true;
            var includeGenres = ExportGenresCheckBox.IsChecked == true;
            var includeConfig = ExportConfigCheckBox.IsChecked == true;
            var includeIcons = ExportIconsCheckBox.IsChecked == true;

            if (!includeTags && !includeGenres && !includeConfig && !includeIcons)
            {
                ImportExportStatusText.Text = Loc.Get("LOCOneMenuBackupSelectSomething");
                return;
            }

            var folder = OneMenuPlugin.Api?.Dialogs?.SelectFolder();
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            var result = BackupManager.Export(settings, includeTags, includeGenres, includeConfig, includeIcons, folder);
            ImportExportStatusText.Text = result.Message;
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var filePath = OneMenuPlugin.Api?.Dialogs?.SelectFile(
                Loc.Get("LOCOneMenuBackupFiles") + "|*.zip;*.json;" + IconLibrary.FilePattern, null);

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            var confirm = new ConfirmDialog(
                Loc.Get("LOCOneMenuBackupImportTitle"),
                Loc.Get("LOCOneMenuBackupImportMessage"),
                Loc.Get("LOCOneMenuBackupImportConfirm"))
            {
                Owner = this
            };

            if (confirm.ShowDialog() != true)
            {
                return;
            }

            var result = BackupManager.Import(settings, filePath);
            ImportExportStatusText.Text = result.Message;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
