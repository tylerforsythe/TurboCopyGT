using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;

namespace TurboCopyGT
{
    internal class StringTasks
    {
        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string SizeSuffix(Int64 value, int decimalPlaces = 1) {
            if (value < 0) {
                return "-" + SizeSuffix(-value, decimalPlaces);
            }

            int i = 0;
            decimal dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1000) {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
        }

        public static bool DoAnyPatternsMatch(List<string> patterns, string strToCheck) {
            if (patterns == null || patterns.Count == 0)
                return false;
            return patterns.Any(exp => FileSystemName.MatchesSimpleExpression(exp, strToCheck));
        }
    }
}