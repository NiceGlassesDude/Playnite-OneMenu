using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OneMenu
{
    public partial class PickEntryDialog : Window
    {
        private readonly List<TagGenreEntry> entries;
        private readonly bool allowNew;

        public TagGenreEntry SelectedEntry { get; private set; }
        public string NewName { get; private set; }

        public PickEntryDialog(string title, string prompt, IEnumerable<TagGenreEntry> entries, bool allowNew, string confirmText)
        {
            InitializeComponent();
            OneMenuTheme.Apply(this, !OneMenuPlugin.FollowPlayniteTheme);

            this.entries = entries.ToList();
            this.allowNew = allowNew;

            TitleText.Text = title;
            PromptText.Text = prompt;
            ConfirmButton.Content = confirmText;
            NewHintText.Visibility = allowNew ? Visibility.Visible : Visibility.Collapsed;
            EntryList.ItemsSource = this.entries;

            Loaded += (s, e) => FilterBox.Focus();
        }

        private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = FilterBox.Text?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                EntryList.ItemsSource = entries;
                return;
            }

            EntryList.ItemsSource = entries
                .Where(en => en.Name != null && en.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private void FilterBox_KeyDown(object sender, KeyEventArgs e)
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

        private void EntryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VisualHelper.FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) != null &&
                EntryList.SelectedItem is TagGenreEntry)
            {
                Ok_Click(sender, new RoutedEventArgs());
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (EntryList.SelectedItem is TagGenreEntry selected)
            {
                SelectedEntry = selected;
                DialogResult = true;
                return;
            }

            var typed = FilterBox.Text?.Trim();
            if (string.IsNullOrEmpty(typed))
            {
                return;
            }

            var exact = entries.FirstOrDefault(en => string.Equals(en.Name, typed, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                SelectedEntry = exact;
                DialogResult = true;
                return;
            }

            if (allowNew)
            {
                NewName = typed;
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
