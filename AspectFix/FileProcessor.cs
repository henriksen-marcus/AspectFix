using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AspectFix
{
    public class FileProcessor
    {
        private static string ffmpegPath = "ffmpeg.exe";
        private static string ffprobePath = "ffprobe.exe";

        public static (int, int) GetVideoDimensions(string filePath)
        {
            // Create the process start info
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = ffprobePath;
            startInfo.Arguments = $"-v error -select_streams v -show_entries stream=width,height -of csv=p=0:s=x {filePath}";
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

        public static void Crop(string filePath)
        {
            (int width, int height) = GetVideoDimensions(filePath);

            // Cropping from 16:9 to 9:16
            int newWidth = height * 9 / 16;

            string fileName = Path.GetFileName(filePath);
            string extension = Path.GetExtension(filePath);
            string newFileName = fileName.Replace(extension, ".cropped.mp4");
            string newFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileName);

            // Saves to the same location as original file
            string arguments = $"-i {filePath} -filter:v \"crop={newWidth}:{height}\" {newFilePath}";

            string ffmpegPath = "ffmpeg.exe";

            // Create the process start info
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = ffmpegPath;
            startInfo.Arguments = arguments;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Start the process
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            // Read the output
            string output = process.StandardOutput.ReadToEnd();

            // Wait for the process to exit
            process.WaitForExit();

            // Clean up the process
            process.Close();
        }

    }
}
