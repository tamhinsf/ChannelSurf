using System;
using System.IO;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Globalization;
using System.Collections.Generic;
using ChannelSurfCli.Models;

namespace ChannelSurfCli
{

    class Program
    {

        // Replace these values with the name of your Azure AD Tenant and 
        // the application ID of the client you registered in Azure AD for use
        // with this program.  If you don't supply these values here, we'll ask 
        // you for them when you run this app.


        static string aadTenant = "taminlahotmail.onmicrosoft.com";
        static string aadAppClientId = "f7304085-fbaf-4cf9-be56-6207f8207c7f";

        // Don't change this constant
        // It is a constant that corresponds to fixed values in AAD that corresponds to Microsoft Graph

        // Required Permissions - Microsoft Graph -> API
        // Read all users' full profiles
        // Read and write all groups

        const string aadResourceAppId = "00000003-0000-0000-c000-000000000000";
        const string aadRedirectUri = "https://channelsurf-cli";

        const string aadInstance = "https://login.microsoftonline.com/{0}";

        static AuthenticationContext authenticationContext = null;
        static AuthenticationResult authenticationResult = null;

        static void Main(string[] args)
        {
            string slackArchiveBasePath = "";
            string slackArchiveTempPath = "";
            string channelsPath = "";
            bool channelsOnly = false;

            List<Slack.Channels> slackChannelsToMigrate = new List<Slack.Channels>();

            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                if (!channelsOnly)
                {
                    try
                    {
                        Utils.Files.CleanUpTempDirectoriesAndFiles(slackArchiveTempPath);
                    }
                    catch
                    {
                        // to-do: something 
                    }
                }
            };

            if (args.Length > 0)
            {
                Console.WriteLine("");
                Console.WriteLine("****************************************************************************************************");
                Console.WriteLine("Welcome to Channel Master!");
                Console.WriteLine("This tool makes it easy to bulk create channels in an existing Microsoft Team.");
                Console.WriteLine("All we need a Slack Team export ZIP file whose channels you wish to re-create.");
                Console.WriteLine("Or, you can define new channels in a file called channels.json.");
                Console.WriteLine("****************************************************************************************************");
                Console.WriteLine("");

                while (aadTenant == "" || aadAppClientId == "")
                {
                    Console.WriteLine("");
                    Console.WriteLine("****************************************************************************************************");
                    Console.WriteLine("You need to provide your Azure Active Directory Tenant Name and the Application ID you created for");
                    Console.WriteLine("use with application to continue.  You can do this by altering Program.cs and re-compiling this app.");
                    Console.WriteLine("Or, you can provide it right now.");
                    Console.Write("Azure Active Directory Tenant Name (i.e your-domain.onmicrosoft.com): ");
                    aadTenant = Console.ReadLine();
                    Console.Write("Azure Active Directory Application ID: ");
                    aadAppClientId = Console.ReadLine();
                Console.WriteLine("****************************************************************************************************");
                }

                Console.WriteLine("**************************************************");
                Console.WriteLine("Tenant is " + aadTenant);
                Console.WriteLine("Application ID is " + aadAppClientId);
                Console.WriteLine("Redirect URI is " + aadRedirectUri);
                Console.WriteLine("**************************************************");

                Console.WriteLine("");
                Console.WriteLine("****************************************************************************************************");
                Console.WriteLine("Your tenant admin consent URL is https://login.microsoftonline.com/common/oauth2/authorize?response_type=id_token" +
                    "&client_id=" + aadAppClientId + "&redirect_uri=" + aadRedirectUri + "&prompt=admin_consent" + "&nonce=" + Guid.NewGuid().ToString());
                Console.WriteLine("****************************************************************************************************");
                Console.WriteLine("");


                Console.WriteLine("");
                Console.WriteLine("****************************************************************************************************");
                Console.WriteLine("Let's get started! Sign in to Microsoft with your Teams credentials:");

                authenticationResult = UserLogin();
                var aadAccessToken = authenticationResult.AccessToken;

                if (String.IsNullOrEmpty(authenticationResult.AccessToken))
                {
                    Console.WriteLine("Something went wrong.  Please try again!");
                    Environment.Exit(1);
                }
                else
                {
                    Console.WriteLine("You've successfully signed in.  Welcome " + authenticationResult.UserInfo.DisplayableId);
                }

                var selectedTeamId = Utils.Channels.SelectJoinedTeam(aadAccessToken);
                if (selectedTeamId == "")
                {
                    Environment.Exit(0);
                }

                if (args[0].EndsWith("channels.json", StringComparison.CurrentCulture))
                {
                    channelsPath = args[0];
                    channelsOnly = true;
                }
                else
                {
                    slackArchiveTempPath = Path.GetTempFileName();
                    slackArchiveBasePath = Utils.Files.DecompressSlackArchiveFile(args[0], slackArchiveTempPath);
                    channelsPath = Path.Combine(slackArchiveBasePath, "channels.json");
                }

                Console.WriteLine("Scanning channels.json");
                slackChannelsToMigrate = Utils.Channels.ScanSlackChannelsJson(channelsPath);
                Console.WriteLine("Scanning channels.json - done");

                Console.WriteLine("Creating channels in MS Teams");
                var msTeamsChannelsWithSlackProps = Utils.Channels.CreateChannelsInMsTeams(aadAccessToken, selectedTeamId, slackChannelsToMigrate);
                Console.WriteLine("Creating channels in MS Teams - done");

                if (channelsOnly)
                {
                    Environment.Exit(0);
                }

                return;

                // Uncomment the remainder of this "if" block, if you want to upload files associated with
                // your Slack channel archive to the corresponding re-created Microsoft Team channel
          
                //Console.Write("Test Feature - Upload Slack message attachments to MS Teams? (y/n):");
                //var uploadAnswer = Console.ReadLine();
                //if (uploadAnswer.StartsWith("n", StringComparison.CurrentCultureIgnoreCase))
                //{
                //    Utils.Files.CleanUpTempDirectoriesAndFiles(slackArchiveTempPath);
                //    Environment.Exit(0);
                //}

                //Console.WriteLine("Scanning messages in Slack channels for attachments");
                //var attachmentList = Utils.Attachments.ScanSlackChannelsForAttachments(slackArchiveTempPath, msTeamsChannelsWithSlackProps);
                //Console.WriteLine("Scanning messages in Slack channels for attachments - done");

                //Console.WriteLine("Uploading attachments to MS Teams channels");
                //Utils.Attachments.ArchiveMessageAttachments(aadAccessToken, selectedTeamId, attachmentList, 10).Wait();
                //Console.WriteLine("Uploading attachments to MS Teams channels - done");

                //Utils.Files.CleanUpTempDirectoriesAndFiles(slackArchiveTempPath);
            }
            else
            {
                Console.WriteLine("Please give us a path to your Slack archive: i.e. /path/to/your/slack-archive-zip-file/slack.zip");
                Console.WriteLine("Or, give us a path to your channels.json file: i.e. /path/to/your/channels.json");
            }
        }

        static AuthenticationResult UserLogin()
        {
            authenticationContext = new AuthenticationContext
                    (String.Format(CultureInfo.InvariantCulture, aadInstance, aadTenant));
            authenticationContext.TokenCache.Clear();
            DeviceCodeResult deviceCodeResult = authenticationContext.AcquireDeviceCodeAsync(aadResourceAppId, aadAppClientId).Result;
            Console.WriteLine(deviceCodeResult.Message);
            return authenticationContext.AcquireTokenByDeviceCodeAsync(deviceCodeResult).Result;
        }

    }
}