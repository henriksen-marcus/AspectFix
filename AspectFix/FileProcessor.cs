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
using System.Windows.Media.Media3D;

namespace AspectFix
{
    public class FileProcessor
    {
        public struct CropOptions
        {
            public bool isAuto;
            public bool shouldCrop;
            public int iterations;
        }

        private static string ffmpegPath = "ffmpeg.exe";
        private static string ffprobePath = "ffprobe.exe";
        private static bool debugEnabled = false;

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
            catch (Exception e) { }

            return (0, 0);
        }

        /// <returns>How many pixels on (left, top, right, bottom) are black</returns>
        public static (int, int, int, int) GetBlackPixels(VideoFile video)
        {
            var path = GetHQPreviewImage(video);
            if (!File.Exists(path)) return (0, 0, 0, 0);
            var image = new Bitmap(path);
            double tolerance = 0.01;

            int left = 0, top = 0, right = 0, bottom = 0;

            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image.GetPixel(x, image.Height / 2);
                if (pixel.GetBrightness() > tolerance) break;
                left++;
            }

            for (int x = image.Width - 1; x >= 0; x--)
            {
                var pixel = image.GetPixel(x, image.Height / 2);
                if (pixel.GetBrightness() > tolerance) break;
                right++;
            }

            for (int y = 0; y < image.Height; y++)
            {
                var pixel = image.GetPixel(image.Width / 2, y);
                if (pixel.GetBrightness() > tolerance) break;
                top++;
            }
            
            for (int y = image.Height - 1; y >= 0; y--)
            {
                var pixel = image.GetPixel(image.Width / 2, y);
                if (pixel.GetBrightness() > tolerance) break;
                bottom++;
            }

            image.Dispose();
            GC.Collect();
            File.Delete(path);

            Console.WriteLine($"Left: {left}, Top: {top}, Right: {right}, Bottom: {bottom}");
            return (left, top, right, bottom);
        }

        /// <returns>Video length in seconds</returns>
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

        /// <summary>
        /// Runs the crop and rotate operations on a copy of the video
        /// </summary>
        /// <returns>Path to the successfully processed video file, else null</returns>
        public static string Crop(VideoFile video, CropOptions options)
        {
            (int width, int height, int x, int y) = video.GetCroppedDimensions(options);

            string newFileName = $"{video.FileName}.cropped{video.Extension}";
            string newFilePath = Path.Combine(Path.GetDirectoryName(video.Path), newFileName);
            string position = (x != 0 || y != 0) ? (x + ":" + y) : null;
            string videoFilters = $"-filter:v ";

            if (options.shouldCrop)
            {
                videoFilters += $"\"crop={width}:{height}";
                if (position != null) videoFilters += $":{position},";
                else videoFilters += ",";
            }
            else videoFilters += $"\"";

            string rotateCommand = video.GetRotateCommand();
            if (rotateCommand != null)
                videoFilters += $"transpose={rotateCommand}\"";
            else
                videoFilters += "\"";

            if (options.shouldCrop == false && rotateCommand == null) videoFilters = "";

            // Saves to the same location as original file
            string arguments = $"-y -i \"{video.Path}\" {videoFilters} -c:a copy \"{newFilePath}\"";
            var success = Runffmpeg(arguments);

            return File.Exists(newFilePath) ? newFilePath : null;
        }

        /// <returns>Path to a low resolution, uncropped, non-black frame of the video. May be null
        /// if no preview could be generated</returns>
        public static string GetPreviewImage(VideoFile video)
        {
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "preview_old.jpg");
            string scale = video.Orientation == Orientation.Landscape ? "-1:360" : "360:-1";
            string arguments = $"-y -noaccurate_seek -copyts -ss {video.NonBlackFrame.ToString(CultureInfo.InvariantCulture)} -i \"{video.Path}\"  " +
                               $"-frames:v 1 -vf \"scale={video.GetLQScale(video.Width, video.Height)}\" \"{savePath}\"";
            Runffmpeg(arguments);
            return File.Exists(savePath) ? savePath : null;
        }

        /// <returns>Path to a full resolution, uncropped, non-black frame of the video. May be null
        /// if no preview could be generated</returns>
        public static string GetHQPreviewImage(VideoFile video)
        {
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "preview_old_hq.jpg");
            string arguments = $"-y -noaccurate_seek -copyts -ss {video.NonBlackFrame.ToString(CultureInfo.InvariantCulture)} -i \"{video.Path}\"  " +
                               $"-frames:v 1 \"{savePath}\"";
            Runffmpeg(arguments);
            return File.Exists(savePath) ? savePath : null;
        }


        /// <returns>Path to a low resolution, cropped and rotated, non-black frame of the video. May be null
        /// if no preview could be generated</returns>
        public static string GetCroppedPreviewImage(VideoFile video, CropOptions options)
        {
            (int width, int height, int x, int y) = video.GetCroppedDimensions(options);

            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "preview_new.jpg");
            string position = (x != 0 || y != 0) ? (x + ":" + y) : "";
            string videoFilters = $"-vf ";

            if (options.shouldCrop) videoFilters += $"\"crop={width}:{height}:{position}, scale={video.GetLQScale(width, height)}";
            else videoFilters += $"\"scale={video.GetLQScale(width, height)}";

            string rotateCommand = video.GetRotateCommand();
            if (rotateCommand != null)
                videoFilters += $",transpose={rotateCommand}\"";
            else
                videoFilters += "\"";

            if (options.shouldCrop == false && rotateCommand == null) videoFilters = "";

            string arguments = $"-y -noaccurate_seek -copyts -ss {video.NonBlackFrame.ToString(CultureInfo.InvariantCulture)} -i \"{video.Path}\"  " +
                               $"-frames:v 1 {videoFilters} \"{savePath}\"";
            Runffmpeg(arguments);

            return File.Exists(savePath) ? savePath : null;
        }

        /// <summary>
        /// Runs ffprobe with the given arguments
        /// </summary>
        /// <returns>The output</returns>
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
                MainWindow.Instance.ErrorMessage(ex.Message);
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
            startInfo.RedirectStandardOutput = debugEnabled;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Start the process
            Process process = new Process();
            process.StartInfo = startInfo;
            int exitCode = 1;
            string output = null;

            try
            {
                process.Start();
                MainWindow.Instance.CurrentPID = process.Id;
                output = debugEnabled ? process.StandardOutput.ReadToEnd() : null;
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

            if (debugEnabled)
            {
                Console.WriteLine(output);
                Console.WriteLine("Exit code: " + exitCode);
            }
            
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

        public static int Lerp(int a, int b, double t)
        {
            return (int)(a * (1 - t) + b * t);
        }

        public static double GetNonBlackFrameTime(VideoFile video)
        {
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "non_black_frame.jpg");
            double frameTime = 0;

            // We try 3 times to get a valid one
            for (int i = 0; i < 3; i++)
            {
                frameTime = Lerp(0, video.Length, i / 3.0);
                string arguments =
                    $"-y -noaccurate_seek -copyts -ss {frameTime.ToString(CultureInfo.InvariantCulture)} -i \"{video.Path}\" -frames:v 1 {savePath}";

                Runffmpeg(arguments);

                if (File.Exists(savePath))
                {
                    // Check if image is black at center
                    Bitmap image = new Bitmap(savePath);
                    var brightness = image.GetPixel(image.Size.Width / 2, image.Size.Height / 2).GetBrightness();
                    image.Dispose();
                    File.Delete(savePath);
                    if (brightness > 0.2) return frameTime;
                }
            }

            return video.Length / 2;
        }
    }
}
