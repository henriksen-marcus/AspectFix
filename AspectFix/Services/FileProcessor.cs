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
using AspectFix.Views;

namespace AspectFix
{
    public class FileProcessor
    {
        public struct CropOptions
        {
            public bool IsAuto;
            public bool ShouldCrop;
            public int Iterations;

            // Operator overloads
            public static bool operator == (CropOptions a, CropOptions b)
            {
                return a.IsAuto == b.IsAuto && a.ShouldCrop == b.ShouldCrop && a.Iterations == b.Iterations;
            }

            public static bool operator !=(CropOptions a, CropOptions b) => !(a == b);

            public bool Equals(CropOptions other)
            {
                return IsAuto == other.IsAuto && ShouldCrop == other.ShouldCrop && Iterations == other.Iterations;
            }

            public override bool Equals(object obj)
            {
                return obj is CropOptions other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = IsAuto.GetHashCode();
                    hashCode = (hashCode * 397) ^ ShouldCrop.GetHashCode();
                    hashCode = (hashCode * 397) ^ Iterations;
                    return hashCode;
                }
            }
        }

        private const string ffmpegPath = "ffmpeg.exe";
        private const string ffprobePath = "ffprobe.exe";
        private const bool DebugEnabled = false;
        private const string FilenameAddition = "aspectfix";

        public static (int, int) GetVideoDimensions(string videoPath)
        {
            var output = Runffprobe($"-v error -select_streams v -show_entries stream=width,height -of csv=p=0:s=x \"{videoPath}\"");

            try
            {
                Trace.WriteLine(output);
                string firstLine = output.Split('\n')[0];
                Trace.WriteLine(firstLine);

                // Parse the output to retrieve the width and height values
                string[] dimensions = firstLine.Trim().Split('x');

                foreach (string i in dimensions )
                    Trace.WriteLine(i);

                int width = int.Parse(dimensions[0]);
                int height = int.Parse(dimensions[1]);
                return (width, height);
            }
            catch (Exception e)
            {
                MainWindow.Instance.ErrorMessage($"Error in GetVideoDimensions(): {e.Message}");
            }

            return (0, 0);
        }

        /// <returns>How many pixels on (left, top, right, bottom) are black</returns>
        public static (int, int, int, int) GetBlackPixels(VideoFile video)
        {
            var path = GetHQPreviewImage(video);
            if (!File.Exists(path)) return (0, 0, 0, 0);
            var image = new Bitmap(path);
            double tolerance = 0.08;

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

            //Console.WriteLine($"Left: {left}, Top: {top}, Right: {right}, Bottom: {bottom}");
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
                MainWindow.Instance.ErrorMessage("Exception: " + e.Message);
            }

            return length;
        }

        /// <summary>
        /// Runs the crop and rotate operations on a copy of the video
        /// </summary>
        /// <returns>Path to the successfully processed video file, else null</returns>
        //public static string Crop(VideoFile video, CropOptions options)
        //{
        //    (int width, int height, int x, int y) = video.GetCroppedDimensions(options);

        //    string newFileName = $"{video.FileName}.cropped{video.Extension}";
        //    string newFilePath = Path.Combine(Path.GetDirectoryName(video.Path), newFileName);
        //    string position = (x != 0 || y != 0) ? (x + ":" + y) : null;
        //    string videoFilters = $"-filter:v ";

        //    if (options.shouldCrop)
        //    {
        //        videoFilters += $"\"crop={width}:{height}";
        //        if (position != null) videoFilters += $":{position},";
        //        else videoFilters += ",";
        //    }
        //    else videoFilters += $"\"";

        //    string rotateCommand = video.GetRotateCommand();
        //    if (rotateCommand != null)
        //        videoFilters += $"transpose={rotateCommand}\"";
        //    else
        //        videoFilters += "\"";

        //    if (options.shouldCrop == false && rotateCommand == null) videoFilters = "";

        //    // Saves to the same location as original file
        //    string arguments = $"-y -i \"{video.Path}\" {videoFilters} -c:a copy \"{newFilePath}\"";
        //    var success = Runffmpeg(arguments);

        //    return File.Exists(newFilePath) ? newFilePath : null;
        //}

        public static string Crop(VideoFile video, CropOptions options)
        {
            Runffmpeg(FFmpegCommand.Build(video, options));

            Trace.WriteLine("Process:   " + FFmpegCommand.Build(video, options));

            return File.Exists(video.NewPath) ? video.NewPath : null;
        }

        public static void CropPrint(VideoFile video, CropOptions options)
        {
            (int width, int height, int x, int y) = video.GetCroppedDimensions(options);

            string newFileName = $"{video.FileName}.cropped{video.Extension}";
            string newFilePath = Path.Combine(Path.GetDirectoryName(video.Path), newFileName);
            string position = (x != 0 || y != 0) ? (x + ":" + y) : null;
            string videoFilters = $"-filter:v ";

            if (options.ShouldCrop)
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

            if (options.ShouldCrop == false && rotateCommand == null) videoFilters = "";

            // Saves to the same location as original file
            string arguments = $"-y -i \"{video.Path}\" {videoFilters} -c:a copy \"{newFilePath}\"";
            Trace.WriteLine(arguments);
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

            if (options.ShouldCrop) videoFilters += $"\"crop={width}:{height}:{position}";//, scale={video.GetLQScale(width, height)}";
            else videoFilters += $"\"scale={video.GetLQScale(width, height)}";

            string rotateCommand = video.GetRotateCommand();
            if (rotateCommand != null)
                videoFilters += $",transpose={rotateCommand}\"";
            else
                videoFilters += "\"";

            if (options.ShouldCrop == false && rotateCommand == null) videoFilters = "";

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
            int exitCode = 1;
            string output = "";

            try
            {
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ErrorMessage("Exception: " + ex.Message);
            }
            finally
            {
                try
                {
                    exitCode = process.ExitCode;
                }
                catch { }

                process.Close();
            }

            if (DebugEnabled)
            {
                Console.WriteLine(output);
                Console.WriteLine("Exit code: " + exitCode);
                MainWindow.Instance.ErrorMessage("Runffprobe output: " + output);
                MainWindow.Instance.ErrorMessage("Runffprobe exit code: " + exitCode);
            }

            return output;
        }

        private static bool Runffmpeg(string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = ffmpegPath;
            startInfo.Arguments = arguments;
            startInfo.RedirectStandardOutput = DebugEnabled;
            startInfo.RedirectStandardError = DebugEnabled;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            //MainWindow.Instance.ErrorMessage("FFMPEG ARGUMENTS: " +arguments);

            // Start the process
            Process process = new Process();
            process.StartInfo = startInfo;
            int exitCode = 1;
            string output = null;
            string err = null;

            try
            {
                process.Start();
                MainWindow.Instance.CurrentPID = process.Id;
                output = DebugEnabled ? process.StandardOutput.ReadToEnd() : null;
                err = DebugEnabled ? process.StandardError.ReadToEnd() : null;
                process.WaitForExit();
            }
            catch (Exception e)
            {
                MainWindow.Instance.ErrorMessage("Runffmpeg catch: " + e.Message);
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

                MainWindow.Instance.CurrentPID = -1;
            }

            if (DebugEnabled)
            {
                Trace.WriteLine(output);
                Trace.WriteLine("Exit code: " + exitCode);
                //MainWindow.Instance.ErrorMessage("Runffmpeg output: " + output);
                //MainWindow.Instance.ErrorMessage("Runffmpeg error: " + err);
                //MainWindow.Instance.ErrorMessage("Runffmpeg exit code: " + exitCode);
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
                ".mp4",    // MPEG-4 Part 14
                ".m4v",    // MPEG-4 Part 14 (iTunes Video)
                ".mpeg",   // MPEG-2 Video
                ".mpg",    // MPEG-1
                ".m2v",    // MPEG-2 Video
                ".mkv",    // Matroska
                ".webm",   // WebM
                ".vob",    // DVD Video Object
                ".ts",     // MPEG Transport Stream
                ".m2ts",   // MPEG-2 Transport Stream (Blu-ray)
                ".mts",    // AVCHD MPEG-2 Transport Stream
                ".3gp",    // 3GPP Video
                ".3g2",    // 3GPP2 Video

                // QuickTime / Apple formats
                ".mov",    // QuickTime Movie
                ".qt",     // QuickTime File

                // AVI formats
                ".avi",    // Audio Video Interleave
                ".divx",   // DivX Video
                ".xvid",   // Xvid Video

                // Windows Media formats
                ".wmv",    // Windows Media Video
                ".asf",    // Advanced Systems Format

                // Flash formats
                ".flv",    // Flash Video
                ".f4v",    // Flash MP4 Video

                // Ogg / OGM formats
                ".ogv",    // Ogg Video
                ".ogg",    // Ogg Media
                ".ogm",
                ".gif"
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
                    $"-y -noaccurate_seek -copyts -ss {frameTime.ToString(CultureInfo.InvariantCulture)} -i \"{video.Path}\" -frames:v 1 \"{savePath}\"";

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

        public static double GetVideoBitrate(string path)
        {
            var output = Runffprobe($"-v error -select_streams v:0 -show_entries stream=bit_rate -of default=noprint_wrappers=1:nokey=1 \"{path}\"");

            // Parse output to double (bitrate is returned in bits per second, converting to kbps)
            if (double.TryParse(output, out double bitrate))
            {
                return bitrate / 1000; // Return bitrate in kbps
            }
            else
            {
                throw new Exception("Failed to retrieve video bitrate.");
            }
        }

        public static double GetAudioBitrate(string path)
        {
            var output = Runffprobe($"-v error -select_streams a:0 -show_entries stream=bit_rate -of default=noprint_wrappers=1:nokey=1 \"{path}\"");

            // Parse output to double (bitrate is returned in bits per second, converting to kbps)
            if (double.TryParse(output, out double bitrate))
            {
                return bitrate / 1000; // Return bitrate in kbps
            }
            else
            {
                throw new Exception("Failed to retrieve audio bitrate.");
            }
        }

        /// <summary>
        /// Returns a new unique name to the given file.
        /// </summary>
        public static string GetNewFilePath(string path)
        {
            string GetName(string addition)
            {
                return Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(path) + "." + addition + Path.GetExtension(path));
            }

            string finalName = path;

            // If already processed and contains the addition
            if (path.Contains(FilenameAddition + Path.GetExtension(path)))
            {
                path = path.Replace("." + FilenameAddition, "");

                // Prevent overwriting of previously processed files
                for (int i = 2; i < 100; i++)
                {
                    if (File.Exists(finalName))
                    {
                        finalName = GetName($".{FilenameAddition}_" + i);
                    }
                    else break;
                }

                // if you got to this point, what the fuck
                if (File.Exists(finalName))
                {
                    Random rand = new Random();
                    string addition = "";

                    for (int i = 0; i < 10; i++)
                        addition += rand.Next(0, 9);

                    finalName = GetName($".{FilenameAddition}_" + addition);
                }
            }
            else
            {
                finalName = GetName(FilenameAddition);
            }

            return finalName;
        }
    }
}
