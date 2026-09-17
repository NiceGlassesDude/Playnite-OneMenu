using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace OneMenu
{
    public class TagsExport
    {
        public string Kind { get; set; } = "OneMenuTags";
        public List<string> Names { get; set; } = new List<string>();
    }

    public class GenresExport
    {
        public string Kind { get; set; } = "OneMenuGenres";
        public List<string> Names { get; set; } = new List<string>();
    }

    public class ConfigExport
    {
        public string Kind { get; set; } = "OneMenuConfig";
        public List<MenuNode> RootNodes { get; set; } = new List<MenuNode>();
        public string MainIconFileName { get; set; }
    }

    public class KindProbe
    {
        public string Kind { get; set; }
    }

    public class BackupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public static class BackupManager
    {
        public static BackupResult Export(OneMenuSettings settings, bool includeTags, bool includeGenres, bool includeConfig, bool includeIcons, string destinationFolder)
        {
            var api = OneMenuPlugin.Api;
            if (api?.Database == null)
            {
                return new BackupResult { Success = false, Message = Loc.Get("LOCOneMenuBackupNoApi") };
            }

            var tempFolder = Path.Combine(Path.GetTempPath(), "OneMenuExport_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);

            var writtenFiles = new List<string>();

            try
            {
                if (includeTags)
                {
                    var tagsExport = new TagsExport { Names = api.Database.Tags.Select(t => t.Name).OrderBy(n => n).ToList() };
                    var path = Path.Combine(tempFolder, "tags.json");
                    File.WriteAllText(path, Serialization.ToJson(tagsExport));
                    writtenFiles.Add(path);
                }

                if (includeGenres)
                {
                    var genresExport = new GenresExport { Names = api.Database.Genres.Select(g => g.Name).OrderBy(n => n).ToList() };
                    var path = Path.Combine(tempFolder, "genres.json");
                    File.WriteAllText(path, Serialization.ToJson(genresExport));
                    writtenFiles.Add(path);
                }

                var iconPaths = new HashSet<string>();

                if (includeConfig)
                {
                    var clonedRoots = settings.RootNodes.Select(n => n.Clone()).ToList();
                    CollectIconPaths(clonedRoots, iconPaths);
                    if (!string.IsNullOrEmpty(settings.MainIconPath))
                    {
                        iconPaths.Add(settings.MainIconPath);
                    }

                    RewriteIconPathsToFileNames(clonedRoots);

                    var configExport = new ConfigExport
                    {
                        RootNodes = clonedRoots,
                        MainIconFileName = !string.IsNullOrEmpty(settings.MainIconPath) ? Path.GetFileName(settings.MainIconPath) : null
                    };

                    var path = Path.Combine(tempFolder, "onemenu-config.json");
                    File.WriteAllText(path, Serialization.ToJson(configExport));
                    writtenFiles.Add(path);
                }

                if (includeIcons)
                {
                    if (iconPaths.Count == 0)
                    {
                        CollectIconPaths(settings.RootNodes, iconPaths);
                        if (!string.IsNullOrEmpty(settings.MainIconPath))
                        {
                            iconPaths.Add(settings.MainIconPath);
                        }
                    }

                    if (iconPaths.Count > 0)
                    {
                        var iconsFolder = Path.Combine(tempFolder, "icons");
                        Directory.CreateDirectory(iconsFolder);
                        foreach (var iconPath in iconPaths)
                        {
                            if (File.Exists(iconPath))
                            {
                                var destPath = Path.Combine(iconsFolder, Path.GetFileName(iconPath));
                                File.Copy(iconPath, destPath, true);
                                writtenFiles.Add(destPath);
                            }
                        }
                    }
                }

                if (writtenFiles.Count == 0)
                {
                    return new BackupResult { Success = false, Message = Loc.Get("LOCOneMenuBackupNothingToExport") };
                }

                string finalPath;
                if (writtenFiles.Count == 1)
                {
                    var singleFile = writtenFiles[0];
                    finalPath = Path.Combine(destinationFolder, Path.GetFileName(singleFile));
                    File.Copy(singleFile, finalPath, true);
                }
                else
                {
                    var zipName = "OneMenu-Backup-" + DateTime.Now.ToString("yyyy-MM-dd_HHmmss") + ".zip";
                    finalPath = Path.Combine(destinationFolder, zipName);
                    if (File.Exists(finalPath))
                    {
                        File.Delete(finalPath);
                    }

                    ZipFile.CreateFromDirectory(tempFolder, finalPath, CompressionLevel.Optimal, false);
                }

                return new BackupResult { Success = true, Message = Loc.Format("LOCOneMenuBackupExported", finalPath) };
            }
            catch (Exception ex)
            {
                return new BackupResult { Success = false, Message = Loc.Format("LOCOneMenuBackupExportFailed", ex.Message) };
            }
            finally
            {
                TryDeleteFolder(tempFolder);
            }
        }

        public static BackupResult Import(OneMenuSettings settings, string sourceFilePath)
        {
            var api = OneMenuPlugin.Api;
            if (api?.Database == null)
            {
                return new BackupResult { Success = false, Message = Loc.Get("LOCOneMenuBackupNoApi") };
            }

            var tempFolder = Path.Combine(Path.GetTempPath(), "OneMenuImport_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);

            try
            {
                var extension = Path.GetExtension(sourceFilePath)?.ToLowerInvariant();
                if (extension == ".zip")
                {
                    ZipFile.ExtractToDirectory(sourceFilePath, tempFolder);
                }
                else
                {
                    File.Copy(sourceFilePath, Path.Combine(tempFolder, Path.GetFileName(sourceFilePath)), true);
                }

                var summary = new List<string>();
                var tagsAdded = 0;
                var genresAdded = 0;
                var configImported = false;

                foreach (var file in Directory.GetFiles(tempFolder, "*.json", SearchOption.AllDirectories))
                {
                    var json = File.ReadAllText(file);
                    if (!Serialization.TryFromJson<KindProbe>(json, out var probe) || probe?.Kind == null)
                    {
                        continue;
                    }

                    if (probe.Kind == "OneMenuTags")
                    {
                        var tagsExport = Serialization.FromJson<TagsExport>(json);
                        var existingTags = new HashSet<string>(api.Database.Tags.Select(t => t.Name ?? string.Empty), StringComparer.OrdinalIgnoreCase);
                        using (api.Database.BufferedUpdate())
                        {
                            foreach (var name in tagsExport.Names ?? new List<string>())
                            {
                                if (string.IsNullOrWhiteSpace(name) || !existingTags.Add(name))
                                {
                                    continue;
                                }

                                api.Database.Tags.Add(new Tag { Name = name });
                                tagsAdded++;
                            }
                        }
                    }
                    else if (probe.Kind == "OneMenuGenres")
                    {
                        var genresExport = Serialization.FromJson<GenresExport>(json);
                        var existingGenres = new HashSet<string>(api.Database.Genres.Select(g => g.Name ?? string.Empty), StringComparer.OrdinalIgnoreCase);
                        using (api.Database.BufferedUpdate())
                        {
                            foreach (var name in genresExport.Names ?? new List<string>())
                            {
                                if (string.IsNullOrWhiteSpace(name) || !existingGenres.Add(name))
                                {
                                    continue;
                                }

                                api.Database.Genres.Add(new Genre { Name = name });
                                genresAdded++;
                            }
                        }
                    }
                    else if (probe.Kind == "OneMenuConfig")
                    {
                        var configExport = Serialization.FromJson<ConfigExport>(json);
                        var importedRoots = new ObservableCollection<MenuNode>(configExport.RootNodes ?? new List<MenuNode>());
                        RelinkIconPaths(importedRoots);
                        settings.RootNodes = importedRoots;

                        settings.MainIconPath = !string.IsNullOrEmpty(configExport.MainIconFileName)
                            ? Path.Combine(IconLibrary.FolderPath, configExport.MainIconFileName)
                            : null;

                        configImported = true;
                    }
                }

                var iconsCount = 0;
                var extractedIconsFolder = Path.Combine(tempFolder, "icons");
                if (Directory.Exists(extractedIconsFolder))
                {
                    var dataIconsFolder = IconLibrary.FolderPath;
                    Directory.CreateDirectory(dataIconsFolder);
                    foreach (var iconFile in Directory.GetFiles(extractedIconsFolder))
                    {
                        var destPath = Path.Combine(dataIconsFolder, Path.GetFileName(iconFile));
                        if (!IconLibrary.FilesAreEqual(iconFile, destPath))
                        {
                            File.Copy(iconFile, destPath, true);
                        }

                        iconsCount++;
                    }
                }
                else if (IconLibrary.IsImageFile(sourceFilePath))
                {
                    IconLibrary.Import(sourceFilePath);
                    iconsCount++;
                }

                if (tagsAdded > 0)
                {
                    summary.Add(Loc.Format("LOCOneMenuBackupSummaryTags", tagsAdded));
                }

                if (genresAdded > 0)
                {
                    summary.Add(Loc.Format("LOCOneMenuBackupSummaryGenres", genresAdded));
                }

                if (configImported)
                {
                    summary.Add(Loc.Get("LOCOneMenuBackupSummaryConfig"));
                }

                if (iconsCount > 0)
                {
                    summary.Add(Loc.Format("LOCOneMenuBackupSummaryIcons", iconsCount));
                }

                if (summary.Count == 0)
                {
                    return new BackupResult { Success = false, Message = Loc.Get("LOCOneMenuBackupNothingFound") };
                }

                return new BackupResult { Success = true, Message = Loc.Format("LOCOneMenuBackupImported", string.Join(", ", summary)) };
            }
            catch (Exception ex)
            {
                return new BackupResult { Success = false, Message = Loc.Format("LOCOneMenuBackupImportFailed", ex.Message) };
            }
            finally
            {
                TryDeleteFolder(tempFolder);
            }
        }

        private static void CollectIconPaths(IEnumerable<MenuNode> nodes, HashSet<string> iconPaths)
        {
            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.IconPath))
                {
                    iconPaths.Add(node.IconPath);
                }

                CollectIconPaths(node.Children, iconPaths);
            }
        }

        private static void RewriteIconPathsToFileNames(IEnumerable<MenuNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.IconPath))
                {
                    node.IconPath = Path.GetFileName(node.IconPath);
                }

                RewriteIconPathsToFileNames(node.Children);
            }
        }

        private static void RelinkIconPaths(IEnumerable<MenuNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.IconPath))
                {
                    node.IconPath = Path.Combine(IconLibrary.FolderPath, node.IconPath);
                }

                RelinkIconPaths(node.Children);
            }
        }

        private static void TryDeleteFolder(string folder)
        {
            try
            {
                Directory.Delete(folder, true);
            }
            catch
            {
            }
        }
    }
}
