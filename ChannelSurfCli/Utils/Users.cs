using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;


namespace ChannelSurfCli.Utils
{
    public class Users
    {
        public static List<ViewModels.SimpleUser> ScanUsers(string combinedPath)
        {
            var simpleUserList = new List<ViewModels.SimpleUser>();
            using (FileStream fs = new FileStream(combinedPath, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
            using (JsonTextReader reader = new JsonTextReader(sr))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        JObject obj = JObject.Load(reader);

                        // SelectToken returns null not an empty string if nothing is found

                        var userId = (string)obj.SelectToken("id");
                        var name = (string)obj.SelectToken("name");
                        var email = (string)obj.SelectToken("profile.email");
                        var real_name = (string)obj.SelectToken("profile.real_name_normalized");
                        var is_bot = (bool)obj.SelectToken("is_bot");

                        simpleUserList.Add(new ViewModels.SimpleUser()
                        {
                            userId = userId,
                            name = name,
                            email = email,
                            real_name = real_name,
                            is_bot = is_bot,
                        });

                    }
                }
            }
            return simpleUserList;
        }
    }
}
