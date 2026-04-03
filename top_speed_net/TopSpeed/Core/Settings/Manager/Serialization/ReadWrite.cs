using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static SettingsFileDocument? ReadDocument(string path)
        {
            var serializer = new DataContractJsonSerializer(typeof(SettingsFileDocument));
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return serializer.ReadObject(stream) as SettingsFileDocument;
            }
        }

        private static void WriteDocument(string path, SettingsFileDocument document)
        {
            var serializer = new DataContractJsonSerializer(typeof(SettingsFileDocument));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, document);
                stream.Position = 0;
                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    var compactJson = reader.ReadToEnd();
                    var prettyJson = PrettyPrintJson(compactJson);
                    File.WriteAllText(path, prettyJson + Environment.NewLine, new UTF8Encoding(false));
                }
            }
        }
    }
}

