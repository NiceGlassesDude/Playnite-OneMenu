using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
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
