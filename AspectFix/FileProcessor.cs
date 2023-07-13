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
            startInfo.Arguments = $"-v error -select_streams v -show_entries stream=width,height -of csv=p=0:s=x \"{videoPath}\"";
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

            try
            {
                int width = int.Parse(dimensions[0]);
                int height = int.Parse(dimensions[1]);
                return (width, height);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error:GetVideoDimensions: " + e.Message);
                MessageBox.Show(output);
            }

            return (0, 0);
        }

        public static string Crop(VideoFile video)
        {
            (int width, int height) = video.GetCroppedDimensions(1);

            string newFileName = $"{video.FileName}.cropped{video.Extension}";
            string newFilePath = Path.Combine(Path.GetDirectoryName(video.Path), newFileName);

            // Saves to the same location as original file
            string arguments = $"-y -i \"{video.Path}\" -filter:v \"crop={width}:{height}\" -c:a copy \"{newFilePath}\"";
            MessageBox.Show(arguments);
            var success = Runffmpeg(arguments);
            Console.WriteLine("Success: " + success);

            return File.Exists(newFilePath) ? newFilePath : null;
        }

        public static string GetCroppedPreviewImage(VideoFile video, int iterations)
        {
            (int width, int height) = video.GetCroppedDimensions(iterations);
            Console.WriteLine($"Cropped dimensions: {width}x{height}");
            Console.WriteLine($"Iterations: {iterations}");
            Console.WriteLine($"Original dimensions: {video.Width}x{video.Height}");
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "preview_new.jpg");
            string arguments = $"-y -i \"{video.Path}\" -ss 00:00:03 -frames:v 1 -vf \"crop={width}:{height}\" \"{savePath}\"";

            Runffmpeg(arguments);

            return File.Exists(savePath) ? savePath : null;
        }

        public static string GetPreviewImage(VideoFile video)
        {
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "preview_old.jpg");
            string arguments = $"-y -i \"{video.Path}\" -ss 00:00:03 -vframes 1 \"{savePath}\"";

            Runffmpeg(arguments);

            return File.Exists(savePath) ? savePath : null;
        }

        private static bool Runffmpeg(string arguments)
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
            int exitCode;
            string output = "";

            try
            {
                process.Start();
                //output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                // Handle the exception
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                exitCode = process.ExitCode;
                // Clean up the process
                process.Close();
            }

            //Console.WriteLine(output);

            return exitCode == 0;
        }

        // Truncates the string to maxLength and adds "..." in the middle
        public static string ShortenString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
                return input;

            if (maxLength < 4)
                return input.Substring(0, maxLength); // Return the start of the string if maxLength is too small

            int startLength = (maxLength - 3) / 2;
            int endLength = (maxLength - 3) - startLength;

            return input.Substring(0, startLength) + "..." + input.Substring(input.Length - endLength);
        }

        public static bool IsVideoFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            // Supported extensions by ffmpeg
            string[] supportedVideoTypes = {
                ".3g2", ".3gp", ".asf", ".avi", ".flv", ".m2v", ".m4v", ".mkv", ".mov", ".mp4",
                ".mpeg", ".mpg", ".rm", ".swf", ".vob", ".webm", ".wmv"
            };

            // Compare the file extension (ignoring case)
            return supportedVideoTypes.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsVideoSquare(string videoPath)
        {
            var video = new VideoFile(videoPath);
            return video.Width == video.Height;
        }
    }
}
