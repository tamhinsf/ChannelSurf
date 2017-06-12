using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net.Http;
using ChannelSurfCli.Models;

namespace ChannelSurfCli.Utils
{
    public class Attachments
    {

        public static List<Combined.AttachmentsMapping> ScanSlackChannelsForAttachments(String basePath, List<Combined.ChannelsMapping> combinedChannelsMapping)
        {

            List<Combined.AttachmentsMapping> combinedAttachmentsMapping = new List<Combined.AttachmentsMapping>();
            combinedAttachmentsMapping.Clear();

            foreach (var channelToScan in combinedChannelsMapping)
            {
                Console.WriteLine("Scanning messages in channel " + channelToScan.slackChannelName);
                foreach (var file in Directory.GetFiles(Path.Combine(basePath, channelToScan.slackChannelName)))
                {
                    Console.WriteLine("Scanning message file " + file);
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    using (StreamReader sr = new StreamReader(fs))
                    using (JsonTextReader reader = new JsonTextReader(sr))
                    {
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonToken.StartObject)
                            {
                                JObject obj = JObject.Load(reader);

                                // SelectToken returns null not an empty string if nothing is found
                                var fileUrl = (string)obj.SelectToken("file.url_private");
                                var fileId = (string)obj.SelectToken("file.id");
                                var fileMode = (string)obj.SelectToken("file.mode");

                                if (fileMode != "external" && fileId != null && fileUrl != null)
                                {
                                    Console.WriteLine("Message attachment found with ID " + fileId);
                                    var msTeamsChannelResult = combinedChannelsMapping.First(w => w.displayName == channelToScan.slackChannelName);
                                    combinedAttachmentsMapping.Add(new Combined.AttachmentsMapping
                                    {
                                        attachmentId = fileId,
                                        attachmentUrl = fileUrl,
                                        attachmentChannelId = channelToScan.slackChannelId,
                                        msChannelName = msTeamsChannelResult.displayName
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return combinedAttachmentsMapping;
        }


        public static async Task ArchiveMessageAttachments(String aadAccessToken, String selectedTeamId, List<Combined.AttachmentsMapping> combinedAttachmentsMapping, int maxDls = 10)
        {
            var tasks = new List<Task>();

            // semaphore, allow to run maxDLs (default 10) tasks in parallel
            SemaphoreSlim semaphore = new SemaphoreSlim(maxDls);

            foreach (var v in combinedAttachmentsMapping)
            {
                // await here until there is a room for this task
                await semaphore.WaitAsync();
                tasks.Add(GetAndUploadAttachment(aadAccessToken, selectedTeamId, semaphore, v));
            }

            // await for the rest of tasks to complete
            await Task.WhenAll(tasks);
        }

        public static async Task GetAndUploadAttachment(String aadAccessToken, String selectedTeamId, SemaphoreSlim semaphore, Combined.AttachmentsMapping combinedAttachmentsMapping)
        {
            try
            {
                Console.WriteLine("Downloading attachment to local file system " + combinedAttachmentsMapping.attachmentId);
                var request = new HttpClient();
                string fileToUpload = "";
                using (HttpResponseMessage response =
                    await request.GetAsync(combinedAttachmentsMapping.attachmentUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    // do something with response   
                    using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                    {
                        fileToUpload = Path.GetTempFileName();
                        using (Stream streamToWriteTo = File.Open(fileToUpload, FileMode.Create))
                        {
                            await streamToReadFrom.CopyToAsync(streamToWriteTo);
                        }
                    }
                }

                await UploadFile(aadAccessToken, selectedTeamId, fileToUpload, combinedAttachmentsMapping.attachmentId, combinedAttachmentsMapping.msChannelName);
                File.Delete(fileToUpload);
                Console.WriteLine("Deleting local copy of attachment " + combinedAttachmentsMapping.attachmentId);
            }
            catch (Exception ex)
            {
                // do something
                Console.WriteLine("Exception " + ex);
                Console.WriteLine("On this file " + combinedAttachmentsMapping.attachmentId + " " + combinedAttachmentsMapping.attachmentUrl);
            }
            finally
            {
                // don't forget to release
                semaphore.Release();
            }
        }

        static bool CheckIfFileExistsAsync(string aadAccessToken, string selectedTeamId, string fileId, string channelName)
        {
            var authHelper = new Utils.O365.AuthenticationHelper() { AccessToken = aadAccessToken };
            Microsoft.Graph.GraphServiceClient gcs = new Microsoft.Graph.GraphServiceClient(authHelper);

            Microsoft.Graph.DriveItem fileExistsResult = null;
            fileExistsResult = gcs.Groups[selectedTeamId].Drive.Root.ItemWithPath("/" + channelName + "/channelsurf/" + fileId).
                                   Request().GetAsync().Result;
            if (fileExistsResult == null)
            {
                return false;
            }
            Console.WriteLine("Attachment already exists.  We won't replace it. " + fileId);
            return true;
        }

        static async Task UploadFile(string aadAccessToken, string selectedTeamId, string filePath, string fileId, string channelName)
        {
            var authHelper = new Utils.O365.AuthenticationHelper() { AccessToken = aadAccessToken };
            Microsoft.Graph.GraphServiceClient gcs = new Microsoft.Graph.GraphServiceClient(authHelper);


            var fileExists = CheckIfFileExistsAsync(aadAccessToken, selectedTeamId, fileId, channelName);
            if (fileExists) 
            {
                return;
            }

            var uploadSession = await gcs.Groups[selectedTeamId].Drive.Root.ItemWithPath("/" + channelName + "/" + "/channelsurf/" + fileId).
                                  CreateUploadSession().Request().PostAsync();

            try
            {
                Console.WriteLine("Trying to upload attachment to MS Teams " + fileId);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    //upload a file in a single shot.  this is great if all files are below the allowed maximum size for a single shot upload.
                    //however, we're not going to be clever and chunk all files.  
                    //{
                    //var result = await gcs.Groups[selectedTeamId].Drive.Root.ItemWithPath("/" + channelName + "/channelsurf/" + fileId).
                    //Content.Request().PutAsync<Microsoft.Graph.DriveItem>(fs);
                    //}

                    // don't be clever: assume you have to chunk all files, even those below the single shot maximum
                    // credit to https://stackoverflow.com/questions/43974320/maximum-request-length-exceeded-when-uploading-a-file-to-onedrive/43983895

                    var maxChunkSize = 320 * 1024; // 320 KB - Change this to your chunk size. 5MB is the default.

                    var chunkedUploadProvider = new Microsoft.Graph.ChunkedUploadProvider(uploadSession, gcs, fs, maxChunkSize);

                    var chunkRequests = chunkedUploadProvider.GetUploadChunkRequests();
                    var readBuffer = new byte[maxChunkSize];
                    var trackedExceptions = new List<Exception>();

                    Microsoft.Graph.DriveItem itemResult = null;

                    //upload the chunks
                    foreach (var request in chunkRequests)
                    {
                        var result = await chunkedUploadProvider.GetChunkRequestResponseAsync(request, readBuffer, trackedExceptions);

                        if (result.UploadSucceeded)
                        {
                            itemResult = result.ItemResponse;
                        }
                    }
                }
                Console.WriteLine("Upload of attachment to MS Teams completed " + fileId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: attachment could not be uploaded" + ex.InnerException);
            }

            return;
        }
    }
}
