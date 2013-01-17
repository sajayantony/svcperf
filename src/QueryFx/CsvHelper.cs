using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EtlViewer.QueryFx
{
    class CsvWriterSettings
    {
        public string[] ExcludeColumns { get; set; }

        public readonly Dictionary<Type, PropertyInfo[]> Properties = new Dictionary<Type, PropertyInfo[]>();

        public TextWriter Writer { get; set; }
    }

    static class CsvHelper
    {
        public static void PrintHeader(Type type, CsvWriterSettings settings)
        {
            string[] exclude = settings.ExcludeColumns;
            string s = string.Empty;
            foreach (PropertyInfo propertyInfo in type.GetProperties())
            {
                if (exclude != null && exclude.Count(x => x.Contains(propertyInfo.Name)) > 0)
                    continue;

                if (s.Length > 0)
                    s = String.Join(",", new string[] { s, propertyInfo.Name });
                else
                    s = propertyInfo.Name;
            }

            settings.Writer.WriteLine(s);
            settings.Writer.Flush();
        }

        public static void Dump(object obj, CsvWriterSettings settings)
        {
            ExportToCSV(obj, settings);
        }

        static void ExportToCSV(object obj, CsvWriterSettings settings)
        {
            string[] exclude = settings.ExcludeColumns;

            Type type = obj.GetType();
            PropertyInfo[] properties;
            if (!settings.Properties.TryGetValue(type, out properties))
            {
                properties = type.GetProperties();
                settings.Properties.Add(type, properties);
            }

            string s = string.Empty;
            for (int idx = 0; idx < properties.Length; idx++)
            {
                if (exclude != null && exclude.Count(x => x.Contains(properties[idx].Name)) > 0)
                    continue;

                var value = properties[idx].GetValue(obj, null);
                var formattedValue = value == null ? String.Empty : value.ToString();

                if (value != null)
                {
                    if (value.GetType() == typeof(string))
                        formattedValue = "\"" + formattedValue + "\"";
                }

                if (s.Length > 0)
                    s = String.Join(",", new string[] { s, formattedValue });
                else
                    s = formattedValue;

            }

            settings.Writer.WriteLine(s);
            settings.Writer.Flush();
        }
    }
}
