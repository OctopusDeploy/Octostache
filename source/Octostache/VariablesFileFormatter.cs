using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Octostache
{
    static class VariablesFileFormatter
    {
        static readonly JsonSerializer Serializer = new JsonSerializer { Formatting = Formatting.Indented };
        static readonly Encoding FileEncoding = Encoding.UTF8;

        public static void Populate(Dictionary<string, string?> variables, string variablesFilePath)
        {
            var fullPath = Path.GetFullPath(variablesFilePath);
            if (!File.Exists(fullPath))
                return;

            using (var sourceStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new JsonTextReader(new StreamReader(sourceStream, FileEncoding)))
                {
                    Serializer.Populate(reader, variables);
                }
            }
        }

        public static void Persist(Dictionary<string, string?> variables, TextWriter output)
        {
            Serializer.Serialize(new JsonTextWriter(output), variables);
            output.Flush();
        }

        public static void Persist(Dictionary<string, string?> variables, string variablesFilePath)
        {
            var fullPath = Path.GetFullPath(variablesFilePath);
            var parentDirectory = Path.GetDirectoryName(fullPath);
            if (parentDirectory != null && !Directory.Exists(parentDirectory))
                Directory.CreateDirectory(parentDirectory);

            using (var targetStream = new FileStream(variablesFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var writer = new StreamWriter(targetStream, FileEncoding))
                {
                    Persist(variables, writer);
                }
            }
        }
    }
}