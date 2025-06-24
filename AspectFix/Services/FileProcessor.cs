using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Globalization;
using AspectFix.Services;
using AspectFix.Views;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AspectFix.Services
{
    public class FileProcessor
    {

        [Obsolete("CropOptions is deprecated and will be removed in a future release.")]
        public struct CropOptions
        {
            public bool IsAuto;
            public bool ShouldCrop;
            public int Iterations;

            // Operator overloads
            public static bool operator ==(CropOptions a, CropOptions b)
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
        private const bool DebugEnabled = true;
        private const string FilenameAddition = "aspectfix";

        public static (int, int) GetVideoDimensions(string videoPath)
        {
            var output = Runffprobe($"-v error -select_streams v -show_entries stream=width,height -of csv=p=0:s=x \"{videoPath}\"");

            try
            {
                string firstLine = output.Split('\n')[0];

                // Parse the output to retrieve the width and height values
                string[] dimensions = firstLine.Trim().Split('x');
                int width = int.Parse(dimensions[0]);
                int height = int.Parse(dimensions[1]);
                return (width, height);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in GetVideoDimensions(): {e.Message}");
            }

            return (0, 0);
        }

        /// <returns>How many pixels on (left, top, right, bottom) are black.</returns>
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
                Console.WriteLine("Exception: " + e.Message);
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

        public static string Crop(VideoFile video)
        {
            bool ret = Runffmpeg(FFmpegCommand.BuildArgument(video) + " -progress pipe:1 -nostats", video.Length.TotalMilliseconds, percent =>
            {
                MainWindow.Instance.LoadingOverlay.Dispatcher.Invoke(() => MainWindow.Instance.LoadingOverlay.UpdateProgress((int)percent));
            });

            return File.Exists(video.NewPath) ? video.NewPath : null;
        }

        public static void CropPrint(VideoFile video, CropOptions options)
        {
            (double width, double height, double x, double y) = video.GetCroppedDimensions(options);

            string newFileName = $"{video.FileName}.cropped{video.Extension}";
            string newFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(video.Path), newFileName);
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
            string savePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "preview_old.jpg");
            string scale = video.Orientation == Orientation.Landscape ? "-1:360" : "360:-1";

            string arguments = $"-noaccurate_seek -ss {video.NonBlackFrame.ToString(CultureInfo.InvariantCulture)} -i \"{video.Path}\"  " +
                               $"-frames:v 1 -q:v 1 -vf \"scale={video.GetLQScale(video.Width, video.Height)}\" \"{savePath}\""; 

            Runffmpeg(arguments);
            return File.Exists(savePath) ? savePath : null;
        }

        /// <returns>Path to a full resolution, uncropped, non-black frame of the video. May be null
        /// if no preview could be generated.</returns>
        public static string GetHQPreviewImage(VideoFile video)
        {
            string savePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "preview_old_hq.jpg");
            string arguments = $"-noaccurate_seek -ss {video.NonBlackFrame.ToString(CultureInfo.InvariantCulture)} -i \"{video.Path}\"  " +
                               $"-frames:v 1 -q:v 1 \"{savePath}\"";
            Runffmpeg(arguments);

            return File.Exists(savePath) ? savePath : null;
        }


        /// <returns>Path to a low resolution, cropped and rotated, non-black frame of the video. May be null
        /// if no preview could be generated.</returns>
        public static string GetCroppedPreviewImage(VideoFile video, CropOptions options)
        {
            (double width, double height, double x, double y) = video.GetCroppedDimensions(options);
            string savePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "preview_new.jpg");
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

            Console.WriteLine("NON BLACK FRAME: " + video.NonBlackFrame.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("Video: " + video.ToString());

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
            ProcessStartInfo startInfo = new()
            {
                FileName = ffprobePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new()
            {
                StartInfo = startInfo
            };

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
                Console.WriteLine("Exception: " + ex.Message);
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
                Console.WriteLine("Runffprobe output: " + output);
                Console.WriteLine("Runffprobe exit code: " + exitCode);
            }

            return output;
        }

        private static bool Runffmpeg(string arguments, double totalDurationMs = 0, Action<double> onProgress = null)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Start the process
            Process process = new()
            {
                StartInfo = startInfo
            };

            int exitCode = -1;
            string output = null;
            string err = null;

            try
            {
                DateTime startWallClock = DateTime.Now;

                process.Start();
                MainWindow.Instance.CurrentPID = process.Id;

                // For debug output
                var errorTask = DebugEnabled ? process.StandardError.ReadToEndAsync() : Task.FromResult<string>(null);

                // Read progress from stdout in real time
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (DebugEnabled) Console.WriteLine(line);

                    if (line.StartsWith("out_time_ms=") && onProgress != null)
                    {
                        if (long.TryParse(line.Substring("out_time_ms=".Length), out long outTimeUs))
                        {
                            double processedMs = Utils.Clamp(outTimeUs, 0, double.MaxValue) / 1000.0;
                            double percent = processedMs / totalDurationMs;

                            // Avoid division by zero
                            if (percent > 0 && percent <= 1)
                            {
                                TimeSpan elapsed = DateTime.Now - startWallClock;
                                double estimatedTotalMs = elapsed.TotalMilliseconds / percent;
                                double msLeft = estimatedTotalMs - elapsed.TotalMilliseconds;
                                msLeft = Math.Max(0, msLeft);
                                TimeSpan timeLeft = TimeSpan.FromMilliseconds(msLeft);
                                onProgress(timeLeft.TotalSeconds);
                            }
                        }
                    }
                    else if (line.StartsWith("progress=end") && onProgress != null)
                    {
                        onProgress(0);
                    }
                }

                // Read output and error ASYNCHRONOUSLY
                var outputTask = DebugEnabled ? process.StandardOutput.ReadToEndAsync() : Task.FromResult<string>(null);
                process.WaitForExit();
                output = outputTask.Result;
                err = errorTask.Result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Runffmpeg catch: " + e.Message);
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
                Trace.WriteLine("Runffmpeg output: " + output);
                Trace.WriteLine("Runffmpeg error: " + err);
                Trace.WriteLine("Runffmpeg exit code: " + exitCode);
            }

            return exitCode == 0;
        }

        

        public static bool IsVideoFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath);

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



        /// <summary>
        /// Uses ffmpeg's built-in algorithm for detecting black frames to find the best time for a preview frame.
        /// </summary>
        /// <returns>The time in seconds of the least black frame found.</returns>
        public static double GetNonBlackFrameTime(VideoFile video)
        {
            string args = $"-hide_banner -i \"{video.Path}\" -vf blackdetect=d=0.1:pic_th=0.92 -an -f null -";

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            /* Out goal with ffmpeg's blackdetect is to find the end of the first black segment,
               and the start of the second black segment or end of video. The segment between 
               those two times is considered non-black. */

            double start = 0;
            double end = 0;

            var regexStart = new Regex(@"black_start:([\d\.]+)");
            var regexEnd = new Regex(@"black_end:([\d\.]+)");

            using (var process = Process.Start(psi))
            {
                string line;
                while ((line = process.StandardError.ReadLine()) != null)
                {
                    if (line.Contains("black_start"))
                    {
                        Match matchStart;
                        Match matchEnd;


                        if (end == 0) {
                            matchEnd = regexEnd.Match(line);
                            if (matchEnd.Success) end = double.Parse(matchEnd.Groups[1].Value, CultureInfo.InvariantCulture);
                            
                            continue;
                        }

                        if (start == 0)
                        {
                            matchStart = regexStart.Match(line);
                            if (matchStart.Success) start = double.Parse(matchStart.Groups[1].Value, CultureInfo.InvariantCulture);
                            break;
                        }
                    }
                }
                process.WaitForExit();
            }

            // Check if we were able to find the beginning of another black section, otherwise start == 0
            if (start < end) start = video.Length.TotalSeconds;

            return end + ((start - end) / 2);
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

            if (string.IsNullOrEmpty(output)) return 0; // No audio
            else if (double.TryParse(output, out double bitrate))
            {
                // Parse output to double (bitrate is returned in bits per second, converting to kbps)
                return bitrate / 1000; // Return bitrate in kbps
            }
            else
            {
                throw new Exception("Failed to retrieve audio bitrate.");
            }
        }

        /// <summary>
        /// Generates a new file path by appending a unique filename addition to the original file name.
        /// </summary>
        /// <param name="path">Path to the input file.</param>
        /// <returns>A new unique name to the new file.</returns>
        public static string GetNewFilePath(string path)
        {
            // Rule 1: Add the FilenameAddition to the filename if it does not already contain it.
            // Rule 2: If the file already has the FilenameAddition, do not add it again.
            // This means we have to clean up the filename from FilenameAddition first, because GetName() needs to work from scratch each time.
            // Rule 3: If the file already exists, add a _2, _3, etc. to the filename until it is unique.

            /// returns>The absolute path to the new filename including the addition.</returns>
            string GetName(string addition)
            {
                string directory = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
                string filename = System.IO.Path.GetFileNameWithoutExtension(path);
                filename += "." + addition;
                string extension = System.IO.Path.GetExtension(path);
                return System.IO.Path.Combine(directory, filename + extension);
            }

            path = path.Replace($".{FilenameAddition}", ""); // Remove any existing addition to prevent duplicates
            string finalName = GetName(FilenameAddition);

            if (File.Exists(finalName))
            {
                // Prevent overwriting of previously processed files
                for (int i = 2; i < 100; i++)
                {
                    if (File.Exists(finalName)) finalName = GetName($"{FilenameAddition}_{i}");
                    else break;
                }

                if (File.Exists(finalName))
                {
                    // if you got to this point, what the fuck

                    Random rand = new();
                    string addition = "";

                    for (int i = 0; i < 10; i++)
                        addition += rand.Next(0, 9);

                    finalName = GetName($"{FilenameAddition}_{addition}");
                }
            }

            return finalName;
        }
        //public static async Task<(int left, int top, int right, int bottom)> DetectBlackBordersAsync(VideoFile video)
        //{
        //    string ffmpegArgs = $"-hide_banner -nostdin -ss {video.NonBlackFrame.ToString(CultureInfo.InvariantCulture)} -i \"{video.Path}\" -vframes 1 -vf cropdetect=24:16:0 -f null -";
        //    var psi = new ProcessStartInfo
        //    {
        //        FileName = ffmpegPath,
        //        Arguments = ffmpegArgs,
        //        RedirectStandardError = true,
        //        RedirectStandardOutput = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true
        //    };

        //    int left = 0, top = 0, right = 0, bottom = 0;
        //    double width = video.Width, height = video.Height;

        //    using (var process = new Process { StartInfo = psi })
        //    {
        //        process.Start();

        //        var stdErrTask = process.StandardError.ReadToEndAsync();
        //        var stdOutTask = process.StandardOutput.ReadToEndAsync();

        //        // Wait for the process to exit with timeout
        //        Task<bool> waitForExit = Task.Run(() => process.WaitForExit(10000));
        //        if (!await waitForExit)
        //        {
        //            try { process.Kill(); } catch { }
        //            throw new Exception("ffmpeg did not finish in time.");
        //        }

        //        await Task.WhenAll(stdErrTask, stdOutTask);

        //        string stdErr = stdErrTask.Result;
        //        string cropLine = null;
        //        foreach (var line in stdErr.Split('\n'))
        //        {
        //            if (line.Contains("crop="))
        //                cropLine = line.Trim();
        //        }

        //        if (cropLine != null)
        //        {
        //            var match = System.Text.RegularExpressions.Regex.Match(cropLine, @"crop=(\d+):(\d+):(\d+):(\d+)");
        //            if (match.Success)
        //            {
        //                int cropW = int.Parse(match.Groups[1].Value);
        //                int cropH = int.Parse(match.Groups[2].Value);
        //                int cropX = int.Parse(match.Groups[3].Value);
        //                int cropY = int.Parse(match.Groups[4].Value);

        //                left = cropX;
        //                top = cropY;
        //                right = (int)width - cropW - cropX;
        //                bottom = (int)height - cropH - cropY;
        //            }
        //        }
        //    }
        //    return (left, top, right, bottom);
        //}
    }
} // End of namespace
