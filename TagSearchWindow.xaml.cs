using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OneMenu
{
    public class GameSearchResult
    {
        public Game Game { get; set; }
        public string CoverPath { get; set; }
    }

    public partial class TagSearchWindow : Window
    {
        private readonly DispatcherTimer debounceTimer;

        public TagSearchWindow()
        {
            InitializeComponent();

            debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            debounceTimer.Tick += (s, e) =>
            {
                debounceTimer.Stop();
                RunSearch();
            };

            Loaded += (s, e) => SearchBox.Focus();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        private void RunSearch()
        {
            var api = OneMenuPlugin.Api;
            if (api == null)
            {
                return;
            }

            var query = SearchBox.Text?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                ResultsItemsControl.ItemsSource = null;
                ResultCountText.Text = string.Empty;
                return;
            }

            var matches = api.Database.Games.Where(g =>
                (g.Tags != null && g.Tags.Any(t => t.Name != null && t.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)) ||
                (g.Genres != null && g.Genres.Any(genre => genre.Name != null && genre.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
            ).OrderBy(g => g.Name).ToList();

            var results = new List<GameSearchResult>();
            foreach (var game in matches)
            {
                string coverPath = null;
                if (!string.IsNullOrEmpty(game.CoverImage))
                {
                    try
                    {
                        coverPath = api.Database.GetFullFilePath(game.CoverImage);
                    }
                    catch
                    {
                        coverPath = null;
                    }
                }

                results.Add(new GameSearchResult { Game = game, CoverPath = coverPath });
            }

            ResultsItemsControl.ItemsSource = results;
            ResultCountText.Text = $"{results.Count} match(es)";
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

            var playItem = new MenuItem { Header = "Play" };
            playItem.Click += (s, ev) =>
            {
                api?.StartGame(game.Id);
                Close();
            };
            menu.Items.Add(playItem);

            var favoriteItem = new MenuItem { Header = game.Favorite ? "Remove from Favorites" : "Add to Favorites" };
            favoriteItem.Click += (s, ev) =>
            {
                game.Favorite = !game.Favorite;
                api?.Database.Games.Update(game);
            };
            menu.Items.Add(favoriteItem);

            if (!string.IsNullOrEmpty(game.InstallDirectory))
            {
                var openFolderItem = new MenuItem { Header = "Open Install Folder" };
                openFolderItem.Click += (s, ev) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(game.InstallDirectory) { UseShellExecute = true });
                    }
                    catch
                    {
                    }
                };
                menu.Items.Add(openFolderItem);
            }

            menu.Items.Add(new Separator());

            var showInLibraryItem = new MenuItem { Header = "Show in Library" };
            showInLibraryItem.Click += (s, ev) =>
            {
                if (api != null)
                {
                    var clearFilter = new FilterPreset { Settings = new FilterPresetSettings() };
                    api.MainView.ApplyFilterPreset(clearFilter);
                    api.MainView.SwitchToLibraryView();
                    api.MainView.SelectGame(game.Id);
                }

                var mainWindow = System.Windows.Application.Current?.MainWindow;
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
