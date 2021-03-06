using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace VSGUI.API
{
    internal class EncoderApi
    {
        private static JsonObject encoderJsonPub = null;

        public static void PurgeEncoderJsonCache()
        {
            encoderJsonPub = null;
        }


        private static JsonObject GetEncoderJson()
        {
            if (encoderJsonPub == null)
            {
                string path = MainWindow.binpath + @"\json\encoderprofiles.json";
                if (File.Exists(path))
                {
                    encoderJsonPub = JsonApi.ReadJsonObjectFromFile(path);
                    foreach (var item in encoderJsonPub)
                    {
                        foreach (JsonObject jsonitem in item.Value as JsonArray)
                        {
                            jsonitem["name"] = jsonitem["encodername"].ToString() + "-" + jsonitem["name"].ToString();
                            if (jsonitem.ContainsKey("tag"))
                            {
                                if (jsonitem["tag"].ToString() == "server")
                                {
                                    jsonitem["name"] = @"[s] " + jsonitem["name"].ToString();
                                }
                            }
                        }
                    }
                }
                return encoderJsonPub;
            }
            else
            {
                return encoderJsonPub;
            }
        }
        public static string GetEncoderSuffix(string type, int encoderid)
        {
            var encoderJson = GetEncoderJson();
            JsonObject thisJobj = encoderJson[type][encoderid].AsObject();
            return thisJobj["suffix"].ToString();
        }

        public static string GetEncoderName(string type, int encoderid)
        {
            var encoderJson = GetEncoderJson();
            JsonObject thisJobj = encoderJson[type][encoderid].AsObject();
            return thisJobj["encodername"].ToString();
        }

        public static string GetName(string type, int encoderid)
        {
            if (type == "mux")
            {
                return "混流";
            }
            var encoderJson = GetEncoderJson();
            JsonObject thisJobj = encoderJson[type][encoderid].AsObject();
            return thisJobj["name"].ToString();
        }

        public static bool GetNormalize(string type, int encoderid)
        {
            if (type != "audio")
            {
                return false;
            }
            var encoderJson = GetEncoderJson();
            JsonObject thisJobj = encoderJson[type][encoderid].AsObject();
            return (bool)thisJobj["normalize"];
        }

        public static List<string> GetEncoderProfiles(string type)
        {
            var encoderJson = GetEncoderJson();
            if (encoderJson == null)
            {
                return null;
            }
            List<string> videoProfileLists = new List<string>();
            for (int i = 0; i < encoderJson[type].AsArray().Count; i++)
            {
                videoProfileLists.Add(encoderJson[type][i]["name"].ToString());
            }
            return videoProfileLists;
        }

        public static JsonObject GetEncoderJsonObject()
        {
            string path = MainWindow.binpath + @"\json\encoderprofiles.json";
            if (File.Exists(path))
            {
                return JsonApi.ReadJsonObjectFromFile(path);
            }
            else
            {
                return JsonApi.ReadJsonFromString(@"{""video"":[{""name"":""Default"",""encodername"":""x264"",""parameter"":""--crf 22.5"",""suffix"":"".h264"",""tag"":""custom""}],""audio"":[{""name"":""Default"",""encodername"":""qaac"",""parameter"":""--tvbr 73"",""suffix"":"".aac"",""normalize"":true,""tag"":""custom""}]}");
            }
        }

        public static void SetEncoderJsonObject(JsonObject jobj)
        {
            JsonApi.SaveJsonToFile(jobj, MainWindow.binpath + @"\json\encoderprofiles.json");
            PurgeEncoderJsonCache();
        }
    }
}
