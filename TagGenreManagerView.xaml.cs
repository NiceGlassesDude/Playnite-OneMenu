using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

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
        private const string DragFormat = "OneMenuTagGenreEntries";

        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly DispatcherTimer filterTimer;
        private bool showingTags = true;
        private bool initialized;
        private LibraryIndexData index;
        private List<TagGenreEntry> allEntries = new List<TagGenreEntry>();

        private Point dragStart;
        private TagGenreEntry pressedEntry;
        private bool collapseOnRelease;
        private List<TagGenreEntry> dragEntries;

        public TagGenreManagerView()
        {
            InitializeComponent();

            filterTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            filterTimer.Tick += (s, e) =>
            {
                filterTimer.Stop();
                ApplyFilter(null);
            };

            TagsRadio.IsChecked = true;
            Loaded += TagGenreManagerView_Loaded;
        }

        private async void TagGenreManagerView_Loaded(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            await ReloadAsync(null);
        }

        private async void Mode_Changed(object sender, RoutedEventArgs e)
        {
            if (!initialized || SearchBox == null)
            {
                return;
            }

            showingTags = TagsRadio.IsChecked == true;
            SearchBox.Text = string.Empty;
            filterTimer.Stop();
            await ReloadAsync(null);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!initialized)
            {
                return;
            }

            filterTimer.Stop();
            filterTimer.Start();
        }

        private async Task ReloadAsync(ICollection<Guid> reselectIds)
        {
            BusyPanel.Visibility = Visibility.Visible;

            try
            {
                index = await LibraryIndex.GetAsync();

                var tags = showingTags;
                var names = tags ? index.TagNames : index.GenreNames;

                allEntries = names
                    .Select(pair => new TagGenreEntry { Id = pair.Key, Name = pair.Value, Count = index.CountFor(tags, pair.Key) })
                    .OrderBy(en => en.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                ApplyFilter(reselectIds);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "OneMenu: couldn't load tags and genres.");
            }
            finally
            {
                BusyPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyFilter(ICollection<Guid> reselectIds)
        {
            var query = SearchBox.Text?.Trim();
            var visible = string.IsNullOrEmpty(query)
                ? allEntries
                : allEntries.Where(en => en.Name != null && en.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            MasterList.ItemsSource = visible;

            MasterHeaderText.Text = Loc.Format(
                showingTags ? "LOCOneMenuManagerTagsHeader" : "LOCOneMenuManagerGenresHeader",
                visible.Count);

            if (reselectIds != null && reselectIds.Count > 0)
            {
                foreach (var entry in visible.Where(en => reselectIds.Contains(en.Id)))
                {
                    MasterList.SelectedItems.Add(entry);
                }

                if (MasterList.SelectedItems.Count > 0)
                {
                    MasterList.ScrollIntoView(MasterList.SelectedItems[0]);
                }
            }

            UpdateSelectionState();
        }

        private List<TagGenreEntry> GetSelectedEntries()
        {
            return MasterList.SelectedItems.Cast<TagGenreEntry>().ToList();
        }

        private List<Game> GetSelectedGames()
        {
            return GamesList.SelectedItems.Cast<Game>().ToList();
        }

        private void UpdateSelectionState()
        {
            var selected = GetSelectedEntries();

            RenameButton.IsEnabled = selected.Count == 1;
            MergeButton.IsEnabled = selected.Count >= 2;
            RemoveButton.IsEnabled = selected.Count >= 1;

            if (selected.Count == 1 && index != null)
            {
                var entry = selected[0];
                var games = index.GamesFor(showingTags, entry.Id).ToList();
                games.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));

                GamesList.ItemsSource = games;
                SelectedHeaderText.Text = Loc.Format("LOCOneMenuManagerGamesHeader", entry.Name, games.Count);
            }
            else
            {
                GamesList.ItemsSource = null;
                SelectedHeaderText.Text = selected.Count > 1
                    ? Loc.Format("LOCOneMenuManagerMultiSelected", selected.Count)
                    : string.Empty;
            }

            UpdateGameButtons();
        }

        private void UpdateGameButtons()
        {
            var hasGames = GamesList.SelectedItems.Count > 0 && MasterList.SelectedItems.Count == 1;
            AddToGamesButton.IsEnabled = hasGames;
            RemoveFromGamesButton.IsEnabled = hasGames;
        }

        private void MasterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectionState();
        }

        private void GamesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGameButtons();
        }

        private string KindText()
        {
            return Loc.Get(showingTags ? "LOCOneMenuKindTag" : "LOCOneMenuKindGenre");
        }

        private static string DescribeEntries(IList<TagGenreEntry> entries)
        {
            var names = entries.Take(5).Select(en => "\"" + en.Name + "\"").ToList();
            var text = string.Join(", ", names);
            if (entries.Count > 5)
            {
                text += ", …";
            }

            return text;
        }

        private bool Confirm(string title, string message, string confirmText)
        {
            var dialog = new ConfirmDialog(title, message, confirmText)
            {
                Owner = Window.GetWindow(this)
            };

            return dialog.ShowDialog() == true;
        }

        private static List<Guid> GetIds(Game game, bool tags)
        {
            return tags ? game.TagIds : game.GenreIds;
        }

        private static void SetIds(Game game, bool tags, List<Guid> ids)
        {
            if (tags)
            {
                game.TagIds = ids;
            }
            else
            {
                game.GenreIds = ids;
            }
        }

        private static void RemoveDefinition(IPlayniteAPI api, bool tags, Guid id)
        {
            if (tags)
            {
                var tag = api.Database.Tags.Get(id);
                if (tag != null)
                {
                    api.Database.Tags.Remove(tag);
                }
            }
            else
            {
                var genre = api.Database.Genres.Get(id);
                if (genre != null)
                {
                    api.Database.Genres.Remove(genre);
                }
            }
        }

        private static void RunWithProgress(IPlayniteAPI api, Action action)
        {
            var options = new GlobalProgressOptions(Loc.Get("LOCOneMenuManagerWorking"), false)
            {
                IsIndeterminate = true
            };

            var result = api.Dialogs.ActivateGlobalProgress(args =>
            {
                using (api.Database.BufferedUpdate())
                {
                    action();
                }
            }, options);

            if (result?.Error != null)
            {
                logger.Error(result.Error, "OneMenu: a tag/genre operation failed.");
                api.Dialogs.ShowErrorMessage(result.Error.Message, "OneMenu");
            }

            LibraryIndex.Invalidate();
        }

        private async Task MergeAsync(TagGenreEntry target, IList<TagGenreEntry> sources)
        {
            var api = OneMenuPlugin.Api;
            if (api?.Database == null || index == null || target == null)
            {
                return;
            }

            var tags = showingTags;
            var sourceIds = new HashSet<Guid>(sources.Select(s => s.Id));
            sourceIds.Remove(target.Id);
            if (sourceIds.Count == 0)
            {
                return;
            }

            var games = new HashSet<Game>();
            foreach (var id in sourceIds)
            {
                foreach (var game in index.GamesFor(tags, id))
                {
                    games.Add(game);
                }
            }

            RunWithProgress(api, () =>
            {
                foreach (var game in games)
                {
                    var ids = GetIds(game, tags);
                    if (ids == null)
                    {
                        continue;
                    }

                    var updated = ids.Where(id => !sourceIds.Contains(id)).ToList();
                    if (!updated.Contains(target.Id))
                    {
                        updated.Add(target.Id);
                    }

                    SetIds(game, tags, updated);
                    api.Database.Games.Update(game);
                }

                foreach (var id in sourceIds)
                {
                    RemoveDefinition(api, tags, id);
                }
            });

            await ReloadAsync(new[] { target.Id });
        }

        private async Task RemoveEntriesAsync(IList<TagGenreEntry> entries)
        {
            var api = OneMenuPlugin.Api;
            if (api?.Database == null || index == null || entries.Count == 0)
            {
                return;
            }

            var tags = showingTags;
            var removeIds = new HashSet<Guid>(entries.Select(en => en.Id));

            var games = new HashSet<Game>();
            foreach (var id in removeIds)
            {
                foreach (var game in index.GamesFor(tags, id))
                {
                    games.Add(game);
                }
            }

            RunWithProgress(api, () =>
            {
                foreach (var game in games)
                {
                    var ids = GetIds(game, tags);
                    if (ids == null)
                    {
                        continue;
                    }

                    SetIds(game, tags, ids.Where(id => !removeIds.Contains(id)).ToList());
                    api.Database.Games.Update(game);
                }

                foreach (var id in removeIds)
                {
                    RemoveDefinition(api, tags, id);
                }
            });

            await ReloadAsync(null);
        }

        private async Task RemoveFromGamesAsync(TagGenreEntry entry, IList<Game> games)
        {
            var api = OneMenuPlugin.Api;
            if (api?.Database == null || games.Count == 0)
            {
                return;
            }

            var tags = showingTags;

            RunWithProgress(api, () =>
            {
                foreach (var game in games)
                {
                    var ids = GetIds(game, tags);
                    if (ids == null || !ids.Contains(entry.Id))
                    {
                        continue;
                    }

                    SetIds(game, tags, ids.Where(id => id != entry.Id).ToList());
                    api.Database.Games.Update(game);
                }
            });

            await ReloadAsync(new[] { entry.Id });
        }

        private async Task RenameAsync(TagGenreEntry entry, string requestedName)
        {
            var api = OneMenuPlugin.Api;
            var newName = requestedName?.Trim();

            if (api?.Database == null || string.IsNullOrWhiteSpace(newName) ||
                string.Equals(newName, entry.Name, StringComparison.Ordinal))
            {
                return;
            }

            var duplicate = allEntries.FirstOrDefault(en =>
                en.Id != entry.Id && string.Equals(en.Name, newName, StringComparison.OrdinalIgnoreCase));

            if (duplicate != null)
            {
                var message = Loc.Format("LOCOneMenuManagerDuplicateMessage", duplicate.Name, entry.Name);
                if (Confirm(Loc.Get("LOCOneMenuManagerDuplicateTitle"), message, Loc.Get("LOCOneMenuManagerMergeConfirm")))
                {
                    await MergeAsync(duplicate, new List<TagGenreEntry> { entry });
                }

                return;
            }

            if (showingTags)
            {
                var tag = api.Database.Tags.Get(entry.Id);
                if (tag != null)
                {
                    tag.Name = newName;
                    api.Database.Tags.Update(tag);
                }
            }
            else
            {
                var genre = api.Database.Genres.Get(entry.Id);
                if (genre != null)
                {
                    genre.Name = newName;
                    api.Database.Genres.Update(genre);
                }
            }

            LibraryIndex.Invalidate();
            await ReloadAsync(new[] { entry.Id });
        }

        private async Task ConfirmAndRemoveAsync(IList<TagGenreEntry> entries)
        {
            if (entries.Count == 0)
            {
                return;
            }

            var message = entries.Count == 1
                ? Loc.Format("LOCOneMenuManagerRemoveMessage", KindText(), entries[0].Name)
                : Loc.Format("LOCOneMenuManagerRemoveManyMessage", entries.Count, DescribeEntries(entries));

            if (Confirm(Loc.Get("LOCOneMenuManagerRemoveTitle"), message, Loc.Get("LOCOneMenuRemove")))
            {
                await RemoveEntriesAsync(entries);
            }
        }

        private async Task ConfirmAndMergeAsync(TagGenreEntry target, IList<TagGenreEntry> sources)
        {
            var realSources = sources.Where(s => s.Id != target.Id).ToList();
            if (realSources.Count == 0)
            {
                return;
            }

            var message = Loc.Format("LOCOneMenuManagerMergeMessage", DescribeEntries(realSources), target.Name);
            if (Confirm(Loc.Get("LOCOneMenuManagerMergeTitle"), message, Loc.Get("LOCOneMenuManagerMergeConfirm")))
            {
                await MergeAsync(target, realSources);
            }
        }

        private async Task EditEntryAsync(TagGenreEntry entry)
        {
            var dialog = new RenameDialog(entry.Name)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            if (dialog.Result == RenameDialogResult.Renamed)
            {
                await RenameAsync(entry, dialog.ResultName);
            }
            else if (dialog.Result == RenameDialogResult.RemoveRequested)
            {
                await ConfirmAndRemoveAsync(new List<TagGenreEntry> { entry });
            }
        }

        private async void MasterList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = VisualHelper.FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
            if (item == null || !(item.DataContext is TagGenreEntry entry))
            {
                return;
            }

            await EditEntryAsync(entry);
        }

        private async void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedEntries();
            if (selected.Count == 1)
            {
                await EditEntryAsync(selected[0]);
            }
        }

        private async void MergeButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedEntries();
            if (selected.Count < 2)
            {
                return;
            }

            var dialog = new PickEntryDialog(
                Loc.Get("LOCOneMenuManagerMergeTitle"),
                Loc.Get("LOCOneMenuManagerMergePickPrompt"),
                selected,
                false,
                Loc.Get("LOCOneMenuManagerMergeConfirm"))
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true || dialog.SelectedEntry == null)
            {
                return;
            }

            await ConfirmAndMergeAsync(dialog.SelectedEntry, selected);
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            await ConfirmAndRemoveAsync(GetSelectedEntries());
        }

        private async void GamesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = VisualHelper.FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
            var selected = GetSelectedEntries();
            if (item == null || !(item.DataContext is Game game) || selected.Count != 1)
            {
                return;
            }

            var entry = selected[0];
            var message = Loc.Format("LOCOneMenuManagerRemoveFromGameMessage", entry.Name, game.Name);
            if (Confirm(Loc.Get("LOCOneMenuManagerRemoveFromGameTitle"), message, Loc.Get("LOCOneMenuRemove")))
            {
                await RemoveFromGamesAsync(entry, new List<Game> { game });
            }
        }

        private async void RemoveFromGamesButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedEntries();
            var games = GetSelectedGames();
            if (selected.Count != 1 || games.Count == 0)
            {
                return;
            }

            var entry = selected[0];
            var message = Loc.Format("LOCOneMenuManagerRemoveFromGamesMessage", entry.Name, games.Count);
            if (Confirm(Loc.Get("LOCOneMenuManagerRemoveFromGameTitle"), message, Loc.Get("LOCOneMenuRemove")))
            {
                await RemoveFromGamesAsync(entry, games);
            }
        }

        private async void AddToGamesButton_Click(object sender, RoutedEventArgs e)
        {
            var api = OneMenuPlugin.Api;
            var selected = GetSelectedEntries();
            var games = GetSelectedGames();
            if (api?.Database == null || selected.Count != 1 || games.Count == 0)
            {
                return;
            }

            var current = selected[0];
            var tags = showingTags;

            var dialog = new PickEntryDialog(
                Loc.Get("LOCOneMenuManagerAddToGamesTitle"),
                Loc.Format("LOCOneMenuManagerAddToGamesPrompt", KindText(), games.Count),
                allEntries.Where(en => en.Id != current.Id),
                true,
                Loc.Get("LOCOneMenuAdd"))
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            Guid targetId;
            if (dialog.SelectedEntry != null)
            {
                targetId = dialog.SelectedEntry.Id;
            }
            else if (!string.IsNullOrWhiteSpace(dialog.NewName))
            {
                if (tags)
                {
                    var tag = new Tag { Name = dialog.NewName.Trim() };
                    api.Database.Tags.Add(tag);
                    targetId = tag.Id;
                }
                else
                {
                    var genre = new Genre { Name = dialog.NewName.Trim() };
                    api.Database.Genres.Add(genre);
                    targetId = genre.Id;
                }
            }
            else
            {
                return;
            }

            RunWithProgress(api, () =>
            {
                foreach (var game in games)
                {
                    var ids = GetIds(game, tags);
                    if (ids != null && ids.Contains(targetId))
                    {
                        continue;
                    }

                    var updated = ids != null ? ids.ToList() : new List<Guid>();
                    updated.Add(targetId);
                    SetIds(game, tags, updated);
                    api.Database.Games.Update(game);
                }
            });

            await ReloadAsync(new[] { current.Id });
        }

        private static TagGenreEntry GetEntryFromSource(object originalSource)
        {
            var item = VisualHelper.FindAncestor<ListBoxItem>(originalSource as DependencyObject);
            return item?.DataContext as TagGenreEntry;
        }

        private void MasterList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            pressedEntry = null;
            collapseOnRelease = false;

            if (e.ClickCount != 1)
            {
                return;
            }

            var entry = GetEntryFromSource(e.OriginalSource);
            if (entry == null)
            {
                return;
            }

            dragStart = e.GetPosition(null);
            pressedEntry = entry;

            if (Keyboard.Modifiers == ModifierKeys.None &&
                MasterList.SelectedItems.Count > 1 &&
                MasterList.SelectedItems.Contains(entry))
            {
                collapseOnRelease = true;
                e.Handled = true;
            }
        }

        private void MasterList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (collapseOnRelease && pressedEntry != null)
            {
                var entry = pressedEntry;
                MasterList.SelectedItems.Clear();
                MasterList.SelectedItem = entry;
            }

            pressedEntry = null;
            collapseOnRelease = false;
        }

        private void MasterList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || pressedEntry == null)
            {
                return;
            }

            var position = e.GetPosition(null);
            if (Math.Abs(position.X - dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(position.Y - dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            var entries = MasterList.SelectedItems.Contains(pressedEntry)
                ? GetSelectedEntries()
                : new List<TagGenreEntry> { pressedEntry };

            pressedEntry = null;
            collapseOnRelease = false;
            dragEntries = entries;

            try
            {
                DragDrop.DoDragDrop(MasterList, new DataObject(DragFormat, "entries"), DragDropEffects.Move);
            }
            finally
            {
                dragEntries = null;
            }
        }

        private void MasterList_DragOver(object sender, DragEventArgs e)
        {
            var target = GetEntryFromSource(e.OriginalSource);
            var valid = dragEntries != null && target != null &&
                        e.Data.GetDataPresent(DragFormat) &&
                        !dragEntries.Any(en => en.Id == target.Id);

            e.Effects = valid ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void MasterList_Drop(object sender, DragEventArgs e)
        {
            var target = GetEntryFromSource(e.OriginalSource);
            var sources = dragEntries?.ToList();
            e.Handled = true;

            if (target == null || sources == null || sources.Count == 0 || sources.Any(en => en.Id == target.Id))
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    await ConfirmAndMergeAsync(target, sources);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "OneMenu: merge failed.");
                }
            }));
        }
    }
}
