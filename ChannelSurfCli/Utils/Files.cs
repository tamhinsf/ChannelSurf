using System;
using System.IO;
using System.IO.Compression;

namespace ChannelSurfCli.Utils
{
    public class Files
    {
        public static string DecompressSlackArchiveFile(string zipFilePath, string tempPath)
        {

            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
                Console.WriteLine("Deleting pre-existing temp directory");
            }

            Directory.CreateDirectory(tempPath);
            Console.WriteLine("Creating temp directory for Slack archive decompression");
            Console.WriteLine("Temp path is " + tempPath);
            ZipFile.ExtractToDirectory(zipFilePath, tempPath);
            Console.WriteLine("Slack archive decompression done");

            return tempPath;
        }

        public static void CleanUpTempDirectoriesAndFiles(string tempPath)
        {
            Console.WriteLine("\n");
            Console.WriteLine("Cleaning up Slack archive temp directories and files");
            Directory.Delete(tempPath, true);
            File.Delete(tempPath);
            Console.WriteLine("Deleted " + tempPath + " and subdirectories");
        }
    }
}
