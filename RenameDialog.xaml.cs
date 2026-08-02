using System.Windows;
using System.Windows.Input;

namespace OneMenu
{
    public enum RenameDialogResult
    {
        Cancelled,
        Renamed,
        RemoveRequested
    }

    public partial class RenameDialog : Window
    {
        public string ResultName { get; private set; }
        public RenameDialogResult Result { get; private set; } = RenameDialogResult.Cancelled;

        public RenameDialog(string currentName)
        {
            InitializeComponent();
            OneMenuTheme.Apply(this, !OneMenuPlugin.FollowPlayniteTheme);
            NameBox.Text = currentName;
            Loaded += (s, e) =>
            {
                NameBox.Focus();
                NameBox.SelectAll();
            };
        }

        private void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Ok_Click(sender, new RoutedEventArgs());
            }
            else if (e.Key == Key.Escape)
            {
                Cancel_Click(sender, new RoutedEventArgs());
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ResultName = NameBox.Text;
            Result = RenameDialogResult.Renamed;
            DialogResult = true;
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            Result = RenameDialogResult.RemoveRequested;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = RenameDialogResult.Cancelled;
            DialogResult = false;
        }
    }
}
