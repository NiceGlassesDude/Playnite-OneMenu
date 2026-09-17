using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OneMenu
{
    public static class ImageHelper
    {
        public static BitmapSource LoadFromFile(string path, int decodeWidth)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    return null;
                }

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    if (decodeWidth > 0)
                    {
                        bitmap.DecodePixelWidth = decodeWidth;
                    }

                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            catch
            {
                return null;
            }
        }
    }

    public static class CoverCache
    {
        private const int Capacity = 300;
        private const int DecodeWidth = 200;

        private static readonly object sync = new object();
        private static readonly Dictionary<string, ImageSource> items = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        private static readonly Queue<string> order = new Queue<string>();
        private static readonly SemaphoreSlim gate = new SemaphoreSlim(4);

        public static bool TryGet(string path, out ImageSource image)
        {
            image = null;
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            lock (sync)
            {
                return items.TryGetValue(path, out image);
            }
        }

        public static async Task<ImageSource> LoadAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            await gate.WaitAsync();
            try
            {
                if (TryGet(path, out var cached))
                {
                    return cached;
                }

                var image = await Task.Run(() => ImageHelper.LoadFromFile(path, DecodeWidth));
                if (image != null)
                {
                    Store(path, image);
                }

                return image;
            }
            catch
            {
                return null;
            }
            finally
            {
                gate.Release();
            }
        }

        public static void Clear()
        {
            lock (sync)
            {
                items.Clear();
                order.Clear();
            }
        }

        private static void Store(string path, ImageSource image)
        {
            lock (sync)
            {
                if (items.ContainsKey(path))
                {
                    return;
                }

                items[path] = image;
                order.Enqueue(path);

                while (order.Count > Capacity)
                {
                    items.Remove(order.Dequeue());
                }
            }
        }
    }
}
