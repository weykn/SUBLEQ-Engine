using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace OneInsArch
{
    public static class Helper
    {
        public static bool StartsWith(string s, string value)
            => string.Equals(RemoveAtSpace(s), value, StringComparison.OrdinalIgnoreCase);

        public static string RemoveAtSpace(string value)
        {
            int pos = value.IndexOf(' ');
            return pos >= 0 ? value.Remove(pos) : value;
        }

        public static string? SubstringAtSpace(string value)
        {
            int pos = value.IndexOf(' ');
            return pos >= 0 ? value.Substring(pos + 1) : null;
        }
    }
}
