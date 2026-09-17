using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OneMenu
{
    public static class IconLibrary
    {
        private static readonly string[] extensions = { ".png", ".jpg", ".jpeg", ".ico", ".bmp" };

        public const string FilePattern = "*.png;*.jpg;*.jpeg;*.ico;*.bmp";

        public static string FolderPath => Path.Combine(OneMenuPlugin.PluginDataPath, "icons");

        public static string FileFilter => Loc.Get("LOCOneMenuImageFiles") + "|" + FilePattern;

        public static bool IsImageFile(string path)
        {
            var extension = Path.GetExtension(path)?.ToLowerInvariant();
            return extensions.Contains(extension);
        }

        public static List<string> GetIcons()
        {
            var folder = FolderPath;
            if (!Directory.Exists(folder))
            {
                return new List<string>();
            }

            return Directory.GetFiles(folder)
                .Where(IsImageFile)
                .OrderBy(p => Path.GetFileName(p), StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public static bool IsInLibrary(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(path));
                return string.Equals(directory, Path.GetFullPath(FolderPath).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static string Import(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                return sourcePath;
            }

            if (IsInLibrary(sourcePath))
            {
                return sourcePath;
            }

            var folder = FolderPath;
            Directory.CreateDirectory(folder);

            foreach (var existing in GetIcons())
            {
                if (FilesAreEqual(existing, sourcePath))
                {
                    return existing;
                }
            }

            var destPath = GetUniquePath(folder, Path.GetFileName(sourcePath));
            File.Copy(sourcePath, destPath, false);
            return destPath;
        }

        public static bool FilesAreEqual(string first, string second)
        {
            try
            {
                var firstInfo = new FileInfo(first);
                var secondInfo = new FileInfo(second);
                if (!firstInfo.Exists || !secondInfo.Exists || firstInfo.Length != secondInfo.Length)
                {
                    return false;
                }

                return File.ReadAllBytes(first).SequenceEqual(File.ReadAllBytes(second));
            }
            catch
            {
                return false;
            }
        }

        public static bool TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetUniquePath(string folder, string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var candidate = Path.Combine(folder, fileName);
            var counter = 2;

            while (File.Exists(candidate))
            {
                candidate = Path.Combine(folder, name + "_" + counter + extension);
                counter++;
            }

            return candidate;
        }
    }
}
