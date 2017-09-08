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
    public class FileAttachments
    {

        public static async Task ArchiveMessageFileAttachments(String aadAccessToken, String selectedTeamId, List<Combined.AttachmentsMapping> combinedAttachmentsMapping, string channelSubFolder, int maxDls = 10)
        {
            var tasks = new List<Task>();

            // semaphore, allow to run maxDLs (default 10) tasks in parallel
            SemaphoreSlim semaphore = new SemaphoreSlim(maxDls);

            foreach (var v in combinedAttachmentsMapping)
            {
                // await here until there is a room for this task
                await semaphore.WaitAsync();
                tasks.Add(GetAndUploadFileToTeamsChannel(aadAccessToken, selectedTeamId, semaphore, v, channelSubFolder));
            }

            // await for the rest of tasks to complete
            await Task.WhenAll(tasks);
        }

        static async Task GetAndUploadFileToTeamsChannel(String aadAccessToken, String selectedTeamId, SemaphoreSlim semaphore, Combined.AttachmentsMapping combinedAttachmentsMapping, string channelSubFolder)
        {
            try
            {
                string fileToUpload = "";
                if(!combinedAttachmentsMapping.attachmentUrl.StartsWith("/"))
                    {
                    Console.WriteLine("Downloading attachment to local file system " + combinedAttachmentsMapping.attachmentId);
                    var request = new HttpClient();
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
                }
                else
                {
                    fileToUpload = combinedAttachmentsMapping.attachmentUrl;
                }
                await UploadFileToTeamsChannel(aadAccessToken, selectedTeamId, fileToUpload, combinedAttachmentsMapping.msChannelName, channelSubFolder, combinedAttachmentsMapping.attachmentFileName, combinedAttachmentsMapping.attachmentId);
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

        static bool CheckIfFileExistsOnTeamsChannel(string aadAccessToken, string selectedTeamId, string channelName, string channelSubFolder, string fileName, string fileId = "")
        {
            var authHelper = new Utils.O365.AuthenticationHelper() { AccessToken = aadAccessToken };
            Microsoft.Graph.GraphServiceClient gcs = new Microsoft.Graph.GraphServiceClient(authHelper);

            Microsoft.Graph.DriveItem fileExistsResult = null;
            try
            {
                fileExistsResult = gcs.Groups[selectedTeamId].Drive.Root.ItemWithPath("/" + channelName + "/channelsurf/" + channelSubFolder + "/" + fileId + "/" + fileName).
                                    Request().GetAsync().Result;
            }
            catch
            {
                fileExistsResult = null;
            }
            
            if (fileExistsResult == null)
            {
                return false;
            }
            Console.WriteLine("Attachment already exists.  We won't replace it. " + fileId);
            return true;
        }

        public static async Task UploadFileToTeamsChannel(string aadAccessToken, string selectedTeamId, string filePath, string channelName, string channelSubFolder, string fileName, string fileId = "")
        {
            var authHelper = new Utils.O365.AuthenticationHelper() { AccessToken = aadAccessToken };
            Microsoft.Graph.GraphServiceClient gcs = new Microsoft.Graph.GraphServiceClient(authHelper);

            var fileExists = CheckIfFileExistsOnTeamsChannel(aadAccessToken, selectedTeamId, channelName, channelSubFolder, fileName, fileId);
            if (fileExists) 
            {
                return;
            }

            Microsoft.Graph.UploadSession uploadSession = null;

            if(channelName == "")
            {
            uploadSession = await gcs.Groups[selectedTeamId].Drive.Root.ItemWithPath("/channelsurf/" + fileName).
                                  CreateUploadSession().Request().PostAsync();
            }
            else
            {
            uploadSession = await gcs.Groups[selectedTeamId].Drive.Root.ItemWithPath("/" + channelName + "/" + "/channelsurf/"+ channelSubFolder + "/" + fileId + "/" + fileName).
                                  CreateUploadSession().Request().PostAsync();
            }

            try
            {
                Console.WriteLine("Trying to upload attachment to MS Teams " + fileId);
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    //upload a file in a single shot.  this is great if all files are below the allowed maximum size for a single shot upload.
                    //however, we're not going to be clever and chunk all files.  
                    //{
                    //var result = await gcs.Groups[selectedTeamId].Drive.Root.ItemWithPath("/" + channelName + "/channelsurf/fileattachments/" + fileId).
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
