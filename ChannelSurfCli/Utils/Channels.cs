using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using ChannelSurfCli.Models;

namespace ChannelSurfCli.Utils
{
    public class Channels
    {
        public static List<Slack.Channels> ScanSlackChannelsJson(string combinedPath)
        {
            List<Slack.Channels> slackChannels = new List<Slack.Channels>();

            using (FileStream fs = new FileStream(combinedPath, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
            using (JsonTextReader reader = new JsonTextReader(sr))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        JObject obj = JObject.Load(reader);

                        // don't force use of the Slack channel id field in a channels.json only creation operation
                        // i.e. we're not importing from a Slack archive but simply bulk creating new channels
                        // this means we must check if "id" is null, otherwise we get an exception

                        var channelId = (string)obj.SelectToken("id");
                        if (channelId == null) {
                            channelId = "";
                        }

                        slackChannels.Add(new Models.Slack.Channels()
                        {
                            channelId = channelId,
                            channelName = obj["name"].ToString(),
                            channelDescription = obj["purpose"]["value"].ToString()
                        });
                    }
                }
            }
            return slackChannels;
        }

        public static List<MsTeams.Channel> GetExistingChannelsInMsTeams(string aadAccessToken, string teamId)
        {
            MsTeams.Team msTeamsTeam = new MsTeams.Team();

            Helpers.httpClient.DefaultRequestHeaders.Clear();
            Helpers.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aadAccessToken);
            Helpers.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var httpResponseMessage =
            Helpers.httpClient.GetAsync(O365.MsGraphBetaEndpoint + "groups/" + teamId + "/channels").Result;
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var httpResultString = httpResponseMessage.Content.ReadAsStringAsync().Result;
                msTeamsTeam = JsonConvert.DeserializeObject<MsTeams.Team>(httpResultString);
            }

            return msTeamsTeam.value;
        }

        public static List<Combined.ChannelsMapping>
                                 CreateChannelsInMsTeams(string aadAccessToken, string teamId, List<Slack.Channels> slackChannels)
        {
            List<Combined.ChannelsMapping> combinedChannelsMapping = new List<Combined.ChannelsMapping>();

            var msTeamsChannel = GetExistingChannelsInMsTeams(aadAccessToken, teamId);

            Helpers.httpClient.DefaultRequestHeaders.Clear();
            Helpers.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aadAccessToken);
            Helpers.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            foreach (var v in slackChannels)
            {
                var existingMsTeams = msTeamsChannel.Find(w => String.Equals(w.displayName, v.channelName, StringComparison.CurrentCultureIgnoreCase));
                if (existingMsTeams != null)
                {
                    Console.WriteLine("This channel already exists in MS Teams: " + v.channelName);
                    combinedChannelsMapping.Add(new Combined.ChannelsMapping()
                    {
                        id = existingMsTeams.id,
                        displayName = v.channelName,
                        description = existingMsTeams.description,
                        slackChannelId = v.channelId,
                        slackChannelName = v.channelName
                    });
                    continue;
                }

                Console.WriteLine("Creating Teams Channel " + v.channelName + " with this Description " + v.channelDescription);

                // this might break on some platforms
                dynamic slackChannelAsMsChannelObject = new JObject();
                slackChannelAsMsChannelObject.displayName = v.channelName;
                slackChannelAsMsChannelObject.description = v.channelDescription;

                var createTeamsChannelPostData = JsonConvert.SerializeObject(slackChannelAsMsChannelObject);
                var httpResponseMessage =
                    Helpers.httpClient.PostAsync(O365.MsGraphBetaEndpoint + "groups/" + teamId + "/channels",
                        new StringContent(createTeamsChannelPostData, Encoding.UTF8, "application/json")).Result;

                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    Console.WriteLine("ERROR: Teams Channel could not be created " + v.channelName + " with this Description " + v.channelDescription);
                    Console.WriteLine("REASON: " + httpResponseMessage.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    var createdMsTeamsChannel = JsonConvert.DeserializeObject<MsTeams.Channel>(httpResponseMessage.Content.ReadAsStringAsync().Result);

                    combinedChannelsMapping.Add(new Combined.ChannelsMapping()
                    {
                        id = createdMsTeamsChannel.id,
                        displayName = createdMsTeamsChannel.displayName,
                        description = createdMsTeamsChannel.description,
                        slackChannelId = v.channelId,
                        slackChannelName = v.channelName
                    });
                    CreateMsTeamsChannelFolder(aadAccessToken, teamId, createdMsTeamsChannel.displayName);
                }
                Thread.Sleep(2000); // pathetic attempt to prevent throttling
            }
            return combinedChannelsMapping;
        }

        public static bool CreateMsTeamsChannelFolder(string aadAccessToken, string teamId, string channelName)
        {
            var authHelper = new O365.AuthenticationHelper() { AccessToken = aadAccessToken };
            Microsoft.Graph.GraphServiceClient gcs = new Microsoft.Graph.GraphServiceClient(authHelper);

            Microsoft.Graph.DriveItem driveItem = new Microsoft.Graph.DriveItem();
            driveItem.Name = channelName;
            var folder = new Microsoft.Graph.Folder();
            driveItem.Folder = folder;

            try
            {
                var result = gcs.Groups[teamId].Drive.Root.Children.Request().AddAsync(driveItem).Result;
                Console.WriteLine("Folder ID is " + result.Id);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Folder creation failure: " + ex.InnerException);
                return false;
            }
        }

        public static string SelectJoinedTeam(string aadAccessToken)
        {
            MsTeams.Team msTeam = new MsTeams.Team();

            Helpers.httpClient.DefaultRequestHeaders.Clear();
            Helpers.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aadAccessToken);
            Helpers.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var httpResponseMessage =
                    Helpers.httpClient.GetAsync(O365.MsGraphBetaEndpoint + "me/joinedTeams").Result;
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var httpResultString = httpResponseMessage.Content.ReadAsStringAsync().Result;
                msTeam = JsonConvert.DeserializeObject<MsTeams.Team>(httpResultString);
            }
            else
            {
                return "";
            }

            if (msTeam.value.Count == 0)
            {
                Console.WriteLine("");
                Console.WriteLine("Whoops!");
                Console.WriteLine("You're not a member of any existing Microsoft Teams");
                Console.WriteLine("You must be a member of an existing Team before you can import channels.");
                Console.WriteLine("");
                Console.WriteLine("Sign in to Microsoft Teams at https://teams.microsoft.com and create a new Team.");
                Console.WriteLine("Then run this app again.");
                return "";
            }

            Console.WriteLine("You're currently a member of these Teams");
            Console.WriteLine("WARNING: If you don't have permission to create new channels for a given Team, your attempt to create or migrate channels will fail");
            for (int i = 0; i < msTeam.value.Count; i++)
            {
                Console.WriteLine("[" + i + "]" + " " + msTeam.value[i].displayName + " " + msTeam.value[i].description);
            }

            Console.Write("Enter the destination Team: ");
            var selectedTeamIndex = Console.ReadLine();
            var selectedTeamId = msTeam.value[Convert.ToInt16(selectedTeamIndex)].id;
            Console.WriteLine("Team ID is " + selectedTeamId);
            return selectedTeamId;
        }
    }
}
