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
    public class Groups
    {
        public static string CreateGroupAndTeam(string aadAccessToken, string newMSGroupAndTeamName) 
        {
            Helpers.httpClient.DefaultRequestHeaders.Clear();
            Helpers.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aadAccessToken);
            Helpers.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // this might break on some platforms
            dynamic newGroupObject = new JObject();
            newGroupObject.displayName = newMSGroupAndTeamName;
            newGroupObject.mailEnabled = "true";
            newGroupObject.groupTypes = new JArray("Unified");            
            newGroupObject.mailNickname = newMSGroupAndTeamName.Replace(" ","");
            newGroupObject.securityEnabled = "false";

            var createMsGroupPostData = JsonConvert.SerializeObject(newGroupObject);
            var httpResponseMessage =
                Helpers.httpClient.PostAsync(O365.MsGraphBetaEndpoint + "groups",
                    new StringContent(createMsGroupPostData, Encoding.UTF8, "application/json")).Result;

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Console.WriteLine("ERROR: Teams Group could not be created " + newMSGroupAndTeamName);
                Console.WriteLine("REASON: " + httpResponseMessage.Content.ReadAsStringAsync().Result);
                return "";
            }

            // this might break on some platforms
            dynamic createGroupResponse = JObject.Parse(httpResponseMessage.Content.ReadAsStringAsync().Result);
            string newGroupId = createGroupResponse.id;

            dynamic newTeamsObject = new JObject();
            dynamic memberSettings = new JObject();
            memberSettings.allowCreateUpdateChannels = true;
            newTeamsObject.memberSettings = memberSettings;

            var createTeamsPutData = JsonConvert.SerializeObject(newTeamsObject);
            httpResponseMessage =
                Helpers.httpClient.PutAsync(O365.MsGraphBetaEndpoint + "groups/" + newGroupId + "/team",
                    new StringContent(createTeamsPutData, Encoding.UTF8, "application/json")).Result;

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Console.WriteLine("ERROR: Group could not be converted into Team" + newMSGroupAndTeamName);
                Console.WriteLine("REASON: " + httpResponseMessage.Content.ReadAsStringAsync().Result);
                return "";
            }
            
            // this might break on some platforms
            dynamic newUserObject = new JObject();
            newUserObject.Add("@odata.id","https://graph.microsoft.com/beta/directoryObjects/" + O365.getUserGuid(aadAccessToken,"me"));

            var addUserToTeamPostData = JsonConvert.SerializeObject(newUserObject);
             httpResponseMessage =
                Helpers.httpClient.PostAsync(O365.MsGraphBetaEndpoint + "groups/" + newGroupId + "/members/$ref",
                    new StringContent(addUserToTeamPostData, Encoding.UTF8, "application/json")).Result;

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Console.WriteLine("ERROR: Teams Membership could not be updated");
                Console.WriteLine("REASON: " + httpResponseMessage.Content.ReadAsStringAsync().Result);
                return "";
            }

            return newGroupId;
        }
    }
}