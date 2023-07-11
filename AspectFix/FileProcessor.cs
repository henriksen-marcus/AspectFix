using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace AspectFix
{
    public class FileProcessor
    {
        private static string ffmpegPath = "ffmpeg.exe";
        private static string ffprobePath = "ffprobe.exe";

        public static (int, int) GetVideoDimensions(string videoPath)
        {
            // Create the process start info
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = ffprobePath;
            startInfo.Arguments = $"-v error -select_streams v -show_entries stream=width,height -of csv=p=0:s=x {videoPath}";
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Start the process
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            string output = "";
            while (!process.HasExited)
            {
                output += process.StandardOutput.ReadToEnd();
            }
            // Read the output
            //string output = process.StandardOutput.ReadToEnd();

            // Wait for the process to exit
            process.WaitForExit();

            // Clean up the process
            process.Close();

            // Parse the output to retrieve the width and height values
            string[] dimensions = output.Trim().Split('x');
            int width = int.Parse(dimensions[0]);
            int height = int.Parse(dimensions[1]);

            return (width, height);
        }

        public static (int, int) GetCroppedVideoDimensions(string videoPath, int iterations)
        {
            (int width, int height) = GetVideoDimensions(videoPath);

            // Cropping from 16:9 to 9:16
            int newWidth = height * 9 / 16;

            return (newWidth, height);
        }

        public static string Crop(string videoPath)
        {
            (int width, int height) = GetCroppedVideoDimensions(videoPath, 1);

            string fileName = Path.GetFileName(videoPath);
            string extension = Path.GetExtension(videoPath);
            string newFileName = fileName.Replace(extension, ".cropped.mp4");
            string newFilePath = Path.Combine(Path.GetDirectoryName(videoPath), newFileName);

            // Saves to the same location as original file
            string arguments = $"-i {videoPath} -filter:v \"crop={width}:{height}\" {newFilePath}";

            Runffmpeg(arguments);

            return File.Exists(newFilePath) ? newFilePath : null;
        }

        public static string GetCroppedPreviewImage(string videoPath, int iterations)
        {
            (int width, int height) = GetCroppedVideoDimensions(videoPath, iterations);
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "preview_new.jpg");
            string arguments = $"-i {videoPath} -ss 00:00:00 -frames:v 1 -vf \"crop={width}:{height}\" {savePath}";

            Runffmpeg(arguments);
            
            return File.Exists(savePath) ? savePath : null;
        }

        public static string GetPreviewImage(string videoPath)
        {
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "preview_old.jpg");
            string arguments = $"-i {videoPath} -ss 00:00:00 -vframes 1 {savePath}";

            Runffmpeg(arguments);

            return File.Exists(savePath) ? savePath : null;
        }

        private static void Runffmpeg(string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = ffmpegPath;
            startInfo.Arguments = arguments;
            startInfo.RedirectStandardOutput = false;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Start the process
            Process process = new Process();
            process.StartInfo = startInfo;

            try
            {
                process.Start();
                //string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                // Handle the exception
                MessageBox.Show(ex.Message);
            }
            finally
            {
                // Clean up the process
                process.Close();
            }
        }
    }
}
