using Playnite.SDK;
using System;

namespace OneMenu
{
    public static class Loc
    {
        public static string Get(string key)
        {
            return ResourceProvider.GetString(key);
        }

        public static string Format(string key, params object[] args)
        {
            var text = Get(key);
            try
            {
                return string.Format(text, args);
            }
            catch (FormatException)
            {
                return text;
            }
        }
    }
}
