using System.Windows;

namespace OneMenu
{
    public partial class IconPickerWindow : Window
    {
        public string SelectedPath { get; private set; }

        public IconPickerWindow(OneMenuSettings settings)
        {
            InitializeComponent();
            OneMenuTheme.Apply(this, !OneMenuPlugin.FollowPlayniteTheme);

            LibraryView.Initialize(settings, true);
            LibraryView.IconPicked += path =>
            {
                SelectedPath = path;
                DialogResult = true;
            };
            LibraryView.PickCancelled += () =>
            {
                DialogResult = false;
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
