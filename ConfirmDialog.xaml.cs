using System.Windows;

namespace OneMenu
{
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog(string title, string message, string confirmButtonText = "Confirm")
        {
            InitializeComponent();
            OneMenuTheme.Apply(this, !OneMenuPlugin.FollowPlayniteTheme);
            TitleText.Text = title;
            MessageText.Text = message;
            ConfirmButton.Content = confirmButtonText;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
