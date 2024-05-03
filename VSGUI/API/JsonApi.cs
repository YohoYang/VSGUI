using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;

namespace VSGUI.API
{
    internal class JsonApi
    {
        public static void SaveJsonToFile(string jsonstr, string filepath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            File.WriteAllText(filepath, jsonstr);
        }

        public static void SaveJsonToFile(JsonNode jsonobj, string filepath)
        {
            string tempjsonstr = jsonobj.ToJsonString(new System.Text.Json.JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            File.WriteAllText(filepath, tempjsonstr);
        }

        public static JsonObject ReadJsonObjectFromFile(string filepath)
        {
            if (!File.Exists(filepath))
            {
                return null;
            }
            try
            {
                JsonObject obj = JsonNode.Parse(File.ReadAllText(filepath)).AsObject();
                return obj;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static JsonArray ReadJsonArrayFromFile(string filepath)
        {
            if (!File.Exists(filepath))
            {
                return new JsonArray();
            }
            JsonArray obj = JsonNode.Parse(File.ReadAllText(filepath)).AsArray();
            return obj;
        }

        public static JsonObject ReadJsonFromString(string jsonstr)
        {
            JsonObject obj = JsonNode.Parse(jsonstr).AsObject();
            return obj;
        }
    }
}
