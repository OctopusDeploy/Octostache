using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Octostache
{
    public static class VariablesFileFormatter
    {
        public static Dictionary<string, string> ReadFrom(string variablesFilePath)
        {
            using (var sourceStream = new FileStream(variablesFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ReadFrom(sourceStream);
            }
        }

        public static Dictionary<string, string> ReadFrom(Stream stream)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var reader = new StreamReader(stream, Encoding.UTF8);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                var parts = line.Split(',');
                var name = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
                var value = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                result[name] = value;
            }

            return result;
        }

        public static void WriteTo(Dictionary<string, string> variables, string variablesFilePath)
        {
            using (var targetStream = new FileStream(variablesFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                WriteTo(variables, targetStream);
            }
        }

        public static void WriteTo(Dictionary<string, string> variables, Stream stream)
        {
            var writer = new StreamWriter(stream, Encoding.UTF8);
            foreach (var pair in variables)
            {
                var name = pair.Key;
                var value = pair.Value;

                if (string.IsNullOrEmpty(name)) { continue; }
                if (string.IsNullOrEmpty(value)) { value = ""; }

                name = Convert.ToBase64String(Encoding.UTF8.GetBytes(name));
                value = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
                writer.Write(name);
                writer.Write(",");
                writer.WriteLine(value);
            }
            writer.Flush();
        }
    }
}