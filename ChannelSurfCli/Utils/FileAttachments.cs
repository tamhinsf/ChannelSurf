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
            //string fileId = "";
            Tuple<string,string> fileIdAndUrl;
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
                var pathToItem = "/" + combinedAttachmentsMapping.msChannelName + "/channelsurf/fileattachments/" + combinedAttachmentsMapping.attachmentId + "/" + combinedAttachmentsMapping.attachmentFileName;
                fileIdAndUrl = await UploadFileToTeamsChannel(aadAccessToken, selectedTeamId, fileToUpload, pathToItem);
                combinedAttachmentsMapping.msSpoId = fileIdAndUrl.Item1;
                combinedAttachmentsMapping.msSpoUrl = fileIdAndUrl.Item2;
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
            return;
        }

        public static Tuple<string,string> CheckIfFileExistsOnTeamsChannel(string aadAccessToken, string selectedTeamId, string pathToItem)
        {
            var authHelper = new Utils.O365.AuthenticationHelper() { AccessToken = aadAccessToken };
            Microsoft.Graph.GraphServiceClient gcs = new Microsoft.Graph.GraphServiceClient(authHelper);

            Microsoft.Graph.DriveItem fileExistsResult = null;
            try
            {
                fileExistsResult = gcs.Groups[selectedTeamId].Drive.Root.ItemWithPath(pathToItem).
                                    Request().GetAsync().Result;
            }
            catch
            {
                fileExistsResult = null;
            }
            
            if (fileExistsResult == null)
            {
                return new Tuple<string,string>("","");
            }
            Console.WriteLine("Attachment already exists.  We won't replace it. " + pathToItem);
            return new Tuple<string,string>(fileExistsResult.Id,fileExistsResult.WebUrl);
        }

        public static async Task<Tuple<string,string>> UploadFileToTeamsChannel(string aadAccessToken, string selectedTeamId, string filePath, string pathToItem) 
        {
            var authHelper = new Utils.O365.AuthenticationHelper() { AccessToken = aadAccessToken };
            Microsoft.Graph.GraphServiceClient gcs = new Microsoft.Graph.GraphServiceClient(authHelper);

            var fileExists = CheckIfFileExistsOnTeamsChannel(aadAccessToken, selectedTeamId, pathToItem);
            if (fileExists.Item1 != "") 
            {
                return new Tuple<string,string>(fileExists.Item1,fileExists.Item2);
            }

            Microsoft.Graph.UploadSession uploadSession = null;

            uploadSession = await gcs.Groups[selectedTeamId].Drive.Root.ItemWithPath(pathToItem).
                CreateUploadSession().Request().PostAsync();

            try
            {
                Console.WriteLine("Trying to upload file to MS Teams SPo Folder " + filePath);
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
                    Console.WriteLine("Upload of attachment to MS Teams completed " + pathToItem);
                    Console.WriteLine("SPo ID is " + itemResult.Id);
                    return new Tuple<string,string>(itemResult.Id, itemResult.WebUrl);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: attachment could not be uploaded" + ex.InnerException);
            }

            return new Tuple<string, string>("","");
        }
    }
}
