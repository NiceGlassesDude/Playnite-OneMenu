using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OneMenu
{
    public class TagGenreEntry
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public string DisplayText => $"{Name} ({Count})";
    }

    public partial class TagGenreManagerView : UserControl
    {
        private bool showingTags = true;

        public TagGenreManagerView()
        {
            InitializeComponent();
            TagsRadio.IsChecked = true;
            Loaded += (s, e) => RefreshMasterList();
        }

        private void Mode_Changed(object sender, RoutedEventArgs e)
        {
            if (SearchBox == null)
            {
                return;
            }

            showingTags = TagsRadio.IsChecked == true;
            SearchBox.Text = string.Empty;
            RefreshMasterList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshMasterList();
        }

        private void RefreshMasterList()
        {
            if (MasterList == null || GamesList == null || SelectedHeaderText == null)
            {
                return;
            }

            var api = OneMenuPlugin.Api;
            if (api?.Database == null)
            {
                return;
            }

            var games = api.Database.Games;
            var entries = new List<TagGenreEntry>();

            if (showingTags)
            {
                foreach (var tag in api.Database.Tags)
                {
                    var count = games.Count(g => g.TagIds != null && g.TagIds.Contains(tag.Id));
                    entries.Add(new TagGenreEntry { Id = tag.Id, Name = tag.Name, Count = count });
                }
            }
            else
            {
                foreach (var genre in api.Database.Genres)
                {
                    var count = games.Count(g => g.GenreIds != null && g.GenreIds.Contains(genre.Id));
                    entries.Add(new TagGenreEntry { Id = genre.Id, Name = genre.Name, Count = count });
                }
            }

            var query = SearchBox.Text?.Trim();
            if (!string.IsNullOrEmpty(query))
            {
                entries = entries.Where(en => en.Name != null && en.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            MasterList.ItemsSource = entries.OrderBy(en => en.Name).ToList();
            GamesList.ItemsSource = null;
            SelectedHeaderText.Text = string.Empty;
        }

        private void MasterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var api = OneMenuPlugin.Api;
            if (api?.Database == null || !(MasterList.SelectedItem is TagGenreEntry entry))
            {
                GamesList.ItemsSource = null;
                SelectedHeaderText.Text = string.Empty;
                return;
            }

            List<Game> matches;
            if (showingTags)
            {
                matches = api.Database.Games.Where(g => g.TagIds != null && g.TagIds.Contains(entry.Id)).OrderBy(g => g.Name).ToList();
            }
            else
            {
                matches = api.Database.Games.Where(g => g.GenreIds != null && g.GenreIds.Contains(entry.Id)).OrderBy(g => g.Name).ToList();
            }

            GamesList.ItemsSource = matches;
            SelectedHeaderText.Text = $"Games tagged \"{entry.Name}\"";
        }

        private void MasterList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(MasterList.SelectedItem is TagGenreEntry entry))
            {
                return;
            }

            var dialog = new RenameDialog(entry.Name)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var api = OneMenuPlugin.Api;
            if (api?.Database == null)
            {
                return;
            }

            if (dialog.Result == RenameDialogResult.Renamed && !string.IsNullOrWhiteSpace(dialog.ResultName))
            {
                if (showingTags)
                {
                    var tag = api.Database.Tags[entry.Id];
                    if (tag != null)
                    {
                        tag.Name = dialog.ResultName.Trim();
                        api.Database.Tags.Update(tag);
                    }
                }
                else
                {
                    var genre = api.Database.Genres[entry.Id];
                    if (genre != null)
                    {
                        genre.Name = dialog.ResultName.Trim();
                        api.Database.Genres.Update(genre);
                    }
                }

                RefreshMasterList();
            }
            else if (dialog.Result == RenameDialogResult.RemoveRequested)
            {
                var kind = showingTags ? "tag" : "genre";
                var confirm = new ConfirmDialog(
                    "Remove " + kind,
                    $"Are you sure you want to remove this {kind}? It will be permanently removed from all games in your library.",
                    "Remove")
                {
                    Owner = Window.GetWindow(this)
                };

                if (confirm.ShowDialog() == true)
                {
                    RemoveTagOrGenreCompletely(entry);
                    RefreshMasterList();
                }
            }
        }

        private void GamesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(MasterList.SelectedItem is TagGenreEntry entry) || !(GamesList.SelectedItem is Game game))
            {
                return;
            }

            var confirm = new ConfirmDialog(
                "Remove from game",
                $"Do you want to remove \"{entry.Name}\" from \"{game.Name}\"?",
                "Remove")
            {
                Owner = Window.GetWindow(this)
            };

            if (confirm.ShowDialog() != true)
            {
                return;
            }

            var api = OneMenuPlugin.Api;
            if (api?.Database == null)
            {
                return;
            }

            if (showingTags)
            {
                game.TagIds?.Remove(entry.Id);
            }
            else
            {
                game.GenreIds?.Remove(entry.Id);
            }

            api.Database.Games.Update(game);

            var selectedId = entry.Id;
            RefreshMasterList();
            SelectEntryById(selectedId);
        }

        private void RemoveTagOrGenreCompletely(TagGenreEntry entry)
        {
            var api = OneMenuPlugin.Api;
            if (api?.Database == null)
            {
                return;
            }

            if (showingTags)
            {
                var affectedGames = api.Database.Games.Where(g => g.TagIds != null && g.TagIds.Contains(entry.Id)).ToList();
                foreach (var game in affectedGames)
                {
                    game.TagIds.Remove(entry.Id);
                    api.Database.Games.Update(game);
                }

                var tag = api.Database.Tags[entry.Id];
                if (tag != null)
                {
                    api.Database.Tags.Remove(tag);
                }
            }
            else
            {
                var affectedGames = api.Database.Games.Where(g => g.GenreIds != null && g.GenreIds.Contains(entry.Id)).ToList();
                foreach (var game in affectedGames)
                {
                    game.GenreIds.Remove(entry.Id);
                    api.Database.Games.Update(game);
                }

                var genre = api.Database.Genres[entry.Id];
                if (genre != null)
                {
                    api.Database.Genres.Remove(genre);
                }
            }
        }

        private void SelectEntryById(Guid id)
        {
            if (MasterList.ItemsSource is IEnumerable<TagGenreEntry> entries)
            {
                MasterList.SelectedItem = entries.FirstOrDefault(en => en.Id == id);
            }
        }
    }
}
