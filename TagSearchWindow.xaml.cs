using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace OneMenu
{
    public class GameSearchResult : LocalObservableObject
    {
        private bool coverRequested;
        private ImageSource cover;

        public Game Game { get; }
        public string CoverPath { get; }

        public GameSearchResult(Game game, string coverPath)
        {
            Game = game;
            CoverPath = coverPath;
        }

        public ImageSource Cover
        {
            get
            {
                if (!coverRequested)
                {
                    coverRequested = true;

                    if (CoverCache.TryGet(CoverPath, out var cached))
                    {
                        cover = cached;
                    }
                    else if (!string.IsNullOrEmpty(CoverPath))
                    {
                        LoadCover();
                    }
                }

                return cover;
            }
        }

        private async void LoadCover()
        {
            var image = await CoverCache.LoadAsync(CoverPath);
            if (image != null)
            {
                cover = image;
                OnPropertyChanged(nameof(Cover));
            }
        }
    }

    public class GameSearchRow
    {
        public List<GameSearchResult> Items { get; set; }
    }

    public partial class TagSearchWindow : Window
    {
        private const double TileWidth = 132;

        private static readonly ILogger logger = LogManager.GetLogger();

        private readonly DispatcherTimer debounceTimer;
        private List<GameSearchResult> currentResults = new List<GameSearchResult>();
        private int currentColumns;
        private int searchToken;

        public TagSearchWindow(bool useOneMenuTheme)
        {
            InitializeComponent();
            OneMenuTheme.Apply(this, useOneMenuTheme);

            debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            debounceTimer.Tick += (s, e) =>
            {
                debounceTimer.Stop();
                RunSearch();
            };

            Loaded += TagSearchWindow_Loaded;
            Closed += (s, e) =>
            {
                debounceTimer.Stop();
                searchToken++;
                CoverCache.Clear();
            };
        }

        private async void TagSearchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SearchBox.Focus();

            var token = searchToken;
            SetBusy(true, Loc.Get("LOCOneMenuIndexingLibrary"));
            try
            {
                await LibraryIndex.GetAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "OneMenu: couldn't build the library index.");
            }
            finally
            {
                if (token == searchToken)
                {
                    SetBusy(false, null);
                }
            }
        }

        private void SetBusy(bool busy, string text)
        {
            BusyPanel.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            BusyText.Text = text ?? string.Empty;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        private async void RunSearch()
        {
            var token = ++searchToken;
            var query = SearchBox.Text?.Trim();

            if (string.IsNullOrEmpty(query))
            {
                currentResults = new List<GameSearchResult>();
                ResultsItemsControl.ItemsSource = null;
                ResultCountText.Text = string.Empty;
                SetBusy(false, null);
                return;
            }

            SetBusy(true, Loc.Get("LOCOneMenuSearching"));

            try
            {
                var index = await LibraryIndex.GetAsync();
                if (token != searchToken)
                {
                    return;
                }

                var results = await Task.Run(() => Search(index, query));
                if (token != searchToken)
                {
                    return;
                }

                currentResults = results;
                RebuildRows(true);
                ResultCountText.Text = Loc.Format("LOCOneMenuTagBrowserMatches", results.Count);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "OneMenu: tag search failed.");
            }
            finally
            {
                if (token == searchToken)
                {
                    SetBusy(false, null);
                }
            }
        }

        private static List<GameSearchResult> Search(LibraryIndexData index, string query)
        {
            var api = OneMenuPlugin.Api;
            var seen = new HashSet<Guid>();
            var games = new List<Game>();

            void Collect(Dictionary<Guid, string> names, bool tags)
            {
                foreach (var pair in names)
                {
                    if (pair.Value == null || pair.Value.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    foreach (var game in index.GamesFor(tags, pair.Key))
                    {
                        if (seen.Add(game.Id))
                        {
                            games.Add(game);
                        }
                    }
                }
            }

            Collect(index.TagNames, true);
            Collect(index.GenreNames, false);

            games.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));

            var results = new List<GameSearchResult>(games.Count);
            foreach (var game in games)
            {
                results.Add(new GameSearchResult(game, ResolveCoverPath(api, game)));
            }

            return results;
        }

        private static string ResolveCoverPath(IPlayniteAPI api, Game game)
        {
            var cover = game.CoverImage;
            if (api == null || string.IsNullOrEmpty(cover) || cover.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            try
            {
                return api.Database.GetFullFilePath(cover);
            }
            catch
            {
                return null;
            }
        }

        private int CalculateColumns()
        {
            var width = ResultsItemsControl.ActualWidth - 24;
            return Math.Max(1, (int)(width / TileWidth));
        }

        private void RebuildRows(bool force)
        {
            var columns = CalculateColumns();
            if (!force && columns == currentColumns)
            {
                return;
            }

            currentColumns = columns;

            var rows = new List<GameSearchRow>();
            for (var i = 0; i < currentResults.Count; i += columns)
            {
                rows.Add(new GameSearchRow
                {
                    Items = currentResults.GetRange(i, Math.Min(columns, currentResults.Count - i))
                });
            }

            ResultsItemsControl.ItemsSource = rows;
        }

        private void ResultsItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged && currentResults.Count > 0)
            {
                RebuildRows(false);
            }
        }

        private void ResultButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Game game)
            {
                OneMenuPlugin.Api?.StartGame(game.Id);
                Close();
            }
        }

        private void ResultButton_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is Game game))
            {
                return;
            }

            var api = OneMenuPlugin.Api;
            var menu = new ContextMenu();

            var playItem = new MenuItem { Header = Loc.Get("LOCOneMenuPlay") };
            playItem.Click += (s, ev) =>
            {
                api?.StartGame(game.Id);
                Close();
            };
            menu.Items.Add(playItem);

            var favoriteItem = new MenuItem
            {
                Header = game.Favorite ? Loc.Get("LOCOneMenuRemoveFavorite") : Loc.Get("LOCOneMenuAddFavorite")
            };
            favoriteItem.Click += (s, ev) =>
            {
                game.Favorite = !game.Favorite;
                api?.Database.Games.Update(game);
            };
            menu.Items.Add(favoriteItem);

            if (!string.IsNullOrEmpty(game.InstallDirectory))
            {
                var openFolderItem = new MenuItem { Header = Loc.Get("LOCOneMenuOpenInstallFolder") };
                openFolderItem.Click += (s, ev) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(game.InstallDirectory) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, $"OneMenu: couldn't open install folder '{game.InstallDirectory}'.");
                    }
                };
                menu.Items.Add(openFolderItem);
            }

            menu.Items.Add(new Separator());

            var showInLibraryItem = new MenuItem { Header = Loc.Get("LOCOneMenuShowInLibrary") };
            showInLibraryItem.Click += (s, ev) =>
            {
                if (api != null)
                {
                    var clearFilter = new FilterPreset { Settings = new FilterPresetSettings() };
                    api.MainView.ApplyFilterPreset(clearFilter);
                    api.MainView.SwitchToLibraryView();
                    api.MainView.SelectGame(game.Id);
                }

                var mainWindow = Application.Current?.MainWindow;
                if (mainWindow != null)
                {
                    if (mainWindow.WindowState == WindowState.Minimized)
                    {
                        mainWindow.WindowState = WindowState.Normal;
                    }

                    mainWindow.Activate();
                }

                Close();
            };
            menu.Items.Add(showInLibraryItem);

            button.ContextMenu = menu;
            menu.PlacementTarget = button;
            menu.IsOpen = true;
            e.Handled = true;
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
