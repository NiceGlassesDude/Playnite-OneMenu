using System.Windows;

namespace OneMenu
{
    public partial class TagGenreManagerWindow : Window
    {
        public TagGenreManagerWindow()
        {
            InitializeComponent();
            OneMenuTheme.Apply(this, !OneMenuPlugin.FollowPlayniteTheme);
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
