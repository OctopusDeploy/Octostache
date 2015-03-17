using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Octostache
{
    public static class VariablesFileFormatter
    {
        public static VariableDictionary ReadFrom(string variablesFilePath)
        {
            using (var sourceStream = new FileStream(variablesFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ReadFrom(sourceStream);
            }
        }

        public static VariableDictionary ReadFrom(Stream stream)
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

            return new VariableDictionary(result);
        }

        public static void WriteTo(VariableDictionary variables, string variablesFilePath)
        {
            using (var targetStream = new FileStream(variablesFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                WriteTo(variables, targetStream);
            }
        }

        public static void WriteTo(VariableDictionary variables, Stream stream)
        {
            var writer = new StreamWriter(stream, Encoding.UTF8);
            foreach (var name in variables.GetNames())
            {
                var value = variables.Get(name);

                if (string.IsNullOrEmpty(name)) { continue; }
                if (string.IsNullOrEmpty(value)) { value = ""; }

                var encodedName = Convert.ToBase64String(Encoding.UTF8.GetBytes(name));
                var encodedValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
                writer.Write(encodedName);
                writer.Write(",");
                writer.WriteLine(encodedValue);
            }
            writer.Flush();
        }
    }
}