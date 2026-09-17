using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OneMenu
{
    public class LibraryIndexData
    {
        private static readonly List<Game> emptyGames = new List<Game>();

        public int Version { get; set; }
        public Dictionary<Guid, string> TagNames { get; } = new Dictionary<Guid, string>();
        public Dictionary<Guid, string> GenreNames { get; } = new Dictionary<Guid, string>();
        public Dictionary<Guid, List<Game>> GamesByTag { get; } = new Dictionary<Guid, List<Game>>();
        public Dictionary<Guid, List<Game>> GamesByGenre { get; } = new Dictionary<Guid, List<Game>>();

        public List<Game> GamesFor(bool tags, Guid id)
        {
            var source = tags ? GamesByTag : GamesByGenre;
            return source.TryGetValue(id, out var games) ? games : emptyGames;
        }

        public int CountFor(bool tags, Guid id)
        {
            return GamesFor(tags, id).Count;
        }
    }

    public static class LibraryIndex
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static readonly object sync = new object();

        private static LibraryIndexData current;
        private static Task<LibraryIndexData> pending;
        private static int pendingVersion = -1;
        private static int version;
        private static bool attached;

        public static void Attach(IPlayniteAPI api)
        {
            if (attached || api?.Database == null)
            {
                return;
            }

            attached = true;

            api.Database.Games.ItemCollectionChanged += (s, e) => Invalidate();
            api.Database.Games.ItemUpdated += (s, e) =>
            {
                if (e?.UpdatedItems == null)
                {
                    Invalidate();
                    return;
                }

                foreach (var update in e.UpdatedItems)
                {
                    if (!SameIds(update.OldData?.TagIds, update.NewData?.TagIds) ||
                        !SameIds(update.OldData?.GenreIds, update.NewData?.GenreIds))
                    {
                        Invalidate();
                        return;
                    }
                }
            };

            api.Database.Tags.ItemCollectionChanged += (s, e) => Invalidate();
            api.Database.Tags.ItemUpdated += (s, e) => Invalidate();
            api.Database.Genres.ItemCollectionChanged += (s, e) => Invalidate();
            api.Database.Genres.ItemUpdated += (s, e) => Invalidate();
        }

        public static void Invalidate()
        {
            Interlocked.Increment(ref version);
        }

        public static async void Warm()
        {
            try
            {
                await GetAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "OneMenu: couldn't build the library index.");
            }
        }

        public static Task<LibraryIndexData> GetAsync()
        {
            var api = OneMenuPlugin.Api;
            if (api?.Database == null)
            {
                return Task.FromResult(new LibraryIndexData());
            }

            lock (sync)
            {
                var wanted = Volatile.Read(ref version);

                if (current != null && current.Version == wanted)
                {
                    return Task.FromResult(current);
                }

                if (pending != null && pendingVersion == wanted && !pending.IsFaulted && !pending.IsCanceled)
                {
                    return pending;
                }

                var games = api.Database.Games.ToList();
                var tags = api.Database.Tags.ToList();
                var genres = api.Database.Genres.ToList();

                pendingVersion = wanted;
                pending = Task.Run(() => Build(wanted, games, tags, genres));
                return pending;
            }
        }

        private static LibraryIndexData Build(int buildVersion, List<Game> games, List<Tag> tags, List<Genre> genres)
        {
            var data = new LibraryIndexData { Version = buildVersion };

            foreach (var tag in tags)
            {
                data.TagNames[tag.Id] = tag.Name ?? string.Empty;
            }

            foreach (var genre in genres)
            {
                data.GenreNames[genre.Id] = genre.Name ?? string.Empty;
            }

            foreach (var game in games)
            {
                var tagIds = game.TagIds;
                if (tagIds != null)
                {
                    foreach (var id in tagIds.ToArray())
                    {
                        if (!data.GamesByTag.TryGetValue(id, out var list))
                        {
                            list = new List<Game>();
                            data.GamesByTag[id] = list;
                        }

                        list.Add(game);
                    }
                }

                var genreIds = game.GenreIds;
                if (genreIds != null)
                {
                    foreach (var id in genreIds.ToArray())
                    {
                        if (!data.GamesByGenre.TryGetValue(id, out var list))
                        {
                            list = new List<Game>();
                            data.GamesByGenre[id] = list;
                        }

                        list.Add(game);
                    }
                }
            }

            lock (sync)
            {
                if (current == null || current.Version <= buildVersion)
                {
                    current = data;
                }
            }

            return data;
        }

        private static bool SameIds(List<Guid> first, List<Guid> second)
        {
            var firstCount = first?.Count ?? 0;
            var secondCount = second?.Count ?? 0;

            if (firstCount != secondCount)
            {
                return false;
            }

            if (firstCount == 0)
            {
                return true;
            }

            var set = new HashSet<Guid>(first);
            return second.All(set.Contains);
        }
    }
}
