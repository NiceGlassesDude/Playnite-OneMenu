using System.Windows.Controls;

namespace OneMenu
{
    public partial class OneMenuSettingsView : UserControl
    {
        public OneMenuSettingsView()
        {
            InitializeComponent();
        }

        private void NodesTreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is OneMenuSettings settings)
            {
                settings.SelectedNode = e.NewValue as MenuNode;
            }
        }
    }
}
