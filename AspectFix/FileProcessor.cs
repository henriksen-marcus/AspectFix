using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Drawing;
using System.Globalization;

namespace AspectFix
{
    public class FileProcessor
    {
        private enum ProgramType
        {
            FFMPEG,
            FFPROBE
        }

        private static string ffmpegPath = "ffmpeg.exe";
        private static string ffprobePath = "ffprobe.exe";

        public static (int, int) GetVideoDimensions(string videoPath)
        {
            var output = Runffprobe($"-v error -select_streams v -show_entries stream=width,height -of csv=p=0:s=x \"{videoPath}\"");

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
                MessageBox.Show(e.Message, "GetVideoDimensions error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return (0, 0);
        }

        public static double GetVideoLength(string videoPath)
        {
            var output = Runffprobe($"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"");
            double length = 0;

            try
            {
                //double.TryParse(output, out length);
                length = double.Parse(output, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                MainWindow.Instance.ErrorMessage(e.Message);
            }

            return length;
        }

        public static string Crop(VideoFile video)
        {
            (int width, int height) = video.GetCroppedDimensions(1);

            string newFileName = $"{video.FileName}.cropped{video.Extension}";
            string newFilePath = Path.Combine(Path.GetDirectoryName(video.Path), newFileName);

            // Saves to the same location as original file
            string arguments = $"-y -i \"{video.Path}\" -filter:v \"crop={width}:{height}\" -c:a copy \"{newFilePath}\"";
            var success = Runffmpeg(arguments);
            Console.WriteLine("Success: " + success);

            return File.Exists(newFilePath) ? newFilePath : null;
        }

        public static string GetCroppedPreviewImage(VideoFile video, int iterations)
        {
            (int width, int height) = video.GetCroppedDimensions(iterations);
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "preview_new.jpg");

            // We don't want a black preview image, so we try 3 times to get a valid one
            for (int i = 0; i < 3; i++)
            {
                string arguments = $"-y -i \"{video.Path}\" -ss {Lerp(0.1, video.Length, (double)i / 3.0).ToString(CultureInfo.InvariantCulture)} -frames:v 1 -vf \"crop={width}:{height}\" \"{savePath}\"";

                Runffmpeg(arguments);

                if (File.Exists(savePath))
                {
                    if (IsImageNotBlack(savePath)) return savePath;
                    File.Delete(savePath);
                }
            }

            return File.Exists(savePath) ? savePath : null;
        }

        public static string GetPreviewImage(VideoFile video)
        {
            (int width, int height) = video.GetDimensions();
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "preview_old.jpg");

            // We don't want a black preview image, so we try 3 times to get a valid one
            for (int i = 0; i < 3; i++)
            {
                string arguments = $"-y -i \"{video.Path}\" -ss {(int)Lerp(0.1, video.Length, (double)i / 3.0)/*.ToString(CultureInfo.InvariantCulture)*/} -frames:v 1 -vf \"crop={width}:{height}\" \"{savePath}\"";

                Runffmpeg(arguments);

                if (File.Exists(savePath))
                {
                    if (IsImageNotBlack(savePath)) return savePath;
                    File.Delete(savePath);
                }
            }

            return File.Exists(savePath) ? savePath : null;
        }

        private static bool IsImageNotBlack(string imagePath)
        {
            // Check if image is black at center
            Bitmap image = new Bitmap(imagePath);
            var brightness = image.GetPixel(image.Size.Width / 2, image.Size.Height / 2).GetBrightness();
            image.Dispose();
            return (brightness > 0.2);
        }

        private static string Runffprobe(string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = ffprobePath;
            startInfo.Arguments = arguments;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = startInfo;
            //int exitCode;
            string output = "";

            try
            {
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                // Handle the exception
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                //exitCode = process.ExitCode;
                process.Close();
            }

            return output;
        }

        private static bool Runffmpeg(string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = ffmpegPath;
            startInfo.Arguments = arguments;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Start the process
            Process process = new Process();
            process.StartInfo = startInfo;
            int exitCode = 1;
            string output = "";

            try
            {
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                MainWindow.Instance.ErrorMessage(e.Message);
            }
            finally
            {
                try
                { 
                    exitCode = process.ExitCode;
                }
                catch {}
                
                // Clean up the process
                process.Close();
            }

            Console.WriteLine(output);
            Console.WriteLine("Exit code: " + exitCode);

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

        public static double Lerp(double a, double b, double t)
        {
            return a * (1 - t) + b * t;
        }
    }
}
