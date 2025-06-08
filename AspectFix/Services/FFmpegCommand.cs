using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspectFix.Services;
using static AspectFix.FileProcessor;

namespace AspectFix;

/// <summary>
/// Object containing an ffmpeg string command. Automatically handles
/// order of operations. Use the Command property to get the processed string.
/// </summary>
//internal class FFmpegCommand
//{
//    public FFmpegCommand(string filePath)
//    {
//        _filePath = filePath;
//    }

//    private string _command;

//    public string Command
//    {
//        get
//        {
//            Append("ffmpeg");
//            Append("-i " + _filePath);
//            _finalFileName = Path.Combine(Path.GetDirectoryName(_filePath) ?? string.Empty,
//                Path.GetFileName(_filePath) + ".cropped" + Path.GetExtension(_filePath));

//            Append("-y"); // Skip any dialogs
//            return _command + "1";
//        }
//    }

//    private static string _filenameAddition = "aspectfix";

//    private string _filePath = null;
//    private string _rotateCommand = null;
//    private string _cropCommand = null;
//    private string _trimCommand = null;
//    private string _finalFileName = null;

//    private void Append(string command)
//    {
//        _command += " " + command;
//    }

//    /// <summary>
//    /// Adds an FFmpeg rotation command to the object.
//    /// </summary>
//    /// <param name="times">How many 90 degree turns to perform (to the right)</param>
//    public void Rotate90(int times)
//    {
//        int rotation = 90 * times;

//        _rotateCommand = rotation switch
//        {
//            0 => null,
//            90 => "1",
//            180 => "2,transpose=2",
//            270 => "2",
//            _ => null
//        };
//    }


//    public static string Crop(CropOptions options)
//    {
//        (int width, int height, int x, int y) = video.GetCroppedDimensions(options);

//        string position = (x != 0 || y != 0) ? (x + ":" + y) : null;
//        string videoFilters = $"-filter:v ";

//        if (options.shouldCrop)
//        {
//            videoFilters += $"\"crop={width}:{height}";
//            if (position != null) videoFilters += $":{position},";
//            else videoFilters += ",";
//        }
//        else videoFilters += $"\"";

//        string rotateCommand = video.GetRotateCommand();
//        if (rotateCommand != null)
//            videoFilters += $"transpose={rotateCommand}\"";
//        else
//            videoFilters += "\"";

//        if (options.shouldCrop == false && rotateCommand == null) videoFilters = "";

//        // Saves to the same location as original file
//        string arguments = $"-y -i \"{video.Path}\" {videoFilters} -c:a copy \"{newFilePath}\"";
//        var success = Runffmpeg(arguments);

//        return File.Exists(newFilePath) ? newFilePath : null;
//    }

//    public void Trim(double startTime, double endTime)
//    {
//        if (startTime > endTime)
//        {
//            throw new ArgumentException("startTime cannot be later then endTime.");
//        }

//        if (startTime != null) _cropCommand = $"-ss {startTime}";
//        if (endTime != null) _cropCommand += $" -t {endTime}";
//    }

//    public static string GetNewFileName(string path)
//    {
//        string GetName(string addition)
//        {
//            return Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
//                Path.GetFileNameWithoutExtension(path) + addition + Path.GetExtension(path));
//        }

//        string finalName = GetName($".{_filenameAddition}");

//        // Prevent overwriting of previously processed files
//        for (int i = 2; i < 100; i++)
//        {
//            if (File.Exists(finalName))
//            {
//                finalName = GetName($".{_filenameAddition}_" + i);
//            }
//            else break;
//        }

//        // if you got to this point, what the fuck
//        if (File.Exists(finalName))
//        {
//            Random rand = new Random();
//            string addition = "";

//            for (int i = 0; i < 10; i++)
//                addition += rand.Next(0, 9);

//            finalName = GetName($".{_filenameAddition}_" + addition);
//        }

//        return finalName;
//    }
//}

using System;
using System.Text;

//public class FFmpegCommand
//{
//    private readonly StringBuilder _commandBuilder;
//    private string _filePath;
//    private bool _isVideoModified;
//    private bool _isAudioModified;

//    private const string FilenameAddition = "aspectfix";

//    public FFmpegCommand(string filePath)
//    {
//        _commandBuilder = new StringBuilder("ffmpeg -i \"" + filePath + "\" ");
//        Trace.WriteLine("Constructed. So far: " + _commandBuilder);
//        _filePath = filePath;
//        _isVideoModified = false; 
//        _isAudioModified = false;  
//    }

//    /// <summary>
//    /// Adds cropping parameters to the ffmpeg command and marks video as modified.
//    /// </summary>
//    /// <param name="width">Width of the cropped area.</param>
//    /// <param name="height">Height of the cropped area.</param>
//    /// <param name="x">X-coordinate of the top-left corner of the cropping rectangle.</param>
//    /// <param name="y">Y-coordinate of the top-left corner of the cropping rectangle.</param>
//    /// <returns>The updated FFmpegCommand instance.</returns>
//    public FFmpegCommand Crop(int width, int height, int x, int y)
//    {
//        _commandBuilder.AppendFormat("-vf \"crop={0}:{1}:{2}:{3}\" ", width, height, x, y);
//        _isVideoModified = true;
//        return this;
//    }

//    /// <summary>
//    /// Adds rotation parameters to the ffmpeg command and marks video as modified.
//    /// </summary>
//    /// <param name="angle">Angle to rotate the video (90, 180, or 270).</param>
//    /// <returns>The updated FFmpegCommand instance.</returns>
//    public FFmpegCommand Rotate(int angle)
//    {
//        switch (angle)
//        {
//            case 0:
//                break;
//            case 90:
//                _commandBuilder.Append("-vf \"transpose=1\" ");
//                break;
//            case 180:
//                _commandBuilder.Append("-vf \"transpose=2,transpose=2\" ");
//                break;
//            case 270:
//                _commandBuilder.Append("-vf \"transpose=2\" ");
//                break;
//            default:
//                throw new ArgumentException("Only 90, 180, and 270 degrees are supported.");
//        }

//        _isVideoModified = true;
//        return this;
//    }

//    /// <summary>
//    /// Adds trimming parameters to the ffmpeg command.
//    /// This applies to both audio and video, so no stream can be copied.
//    /// </summary>
//    /// <param name="startTime">Start time of the trim operation.</param>
//    /// <param name="duration">Duration of the trim operation.</param>
//    /// <returns>The updated FFmpegCommand instance.</returns>
//    public FFmpegCommand Trim(TimeSpan startTime, TimeSpan duration)
//    {
//        _commandBuilder.AppendFormat("-ss {0:hh\\:mm\\:ss} -t {1:hh\\:mm\\:ss} ", startTime, duration);
//        _isVideoModified = true;
//        _isAudioModified = true;
//        return this;
//    }

//    /// <summary>
//    /// Automatically adds stream copying for video and audio if neither has been modified.
//    /// </summary>
//    private void AddStreamCopyingIfPossible()
//    {
//        if (!_isVideoModified)
//            _commandBuilder.Append("-c:v copy ");

//        if (!_isAudioModified)
//            _commandBuilder.Append("-c:a copy ");
//    }

//    /// <summary>
//    /// Gives a new unique name to the given file.
//    /// </summary>
//    public static string GetNewFilePath(string path)
//    {
//        string GetName(string addition)
//        {
//            return Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
//                Path.GetFileNameWithoutExtension(path) + addition + Path.GetExtension(path));
//        }

//        string finalName = path;

//        // If already processed and contains the addition
//        if (path.Contains(FilenameAddition + Path.GetExtension(path)))
//        {
//            path = path.Replace("." + FilenameAddition, "");

//            // Prevent overwriting of previously processed files
//            for (int i = 2; i < 100; i++)
//            {
//                if (File.Exists(finalName))
//                {
//                    finalName = GetName($".{FilenameAddition}_" + i);
//                }
//                else break;
//            }

//            // if you got to this point, what the fuck
//            if (File.Exists(finalName))
//            {
//                Random rand = new Random();
//                string addition = "";

//                for (int i = 0; i < 10; i++)
//                    addition += rand.Next(0, 9);

//                finalName = GetName($".{FilenameAddition}_" + addition);
//            }
//        }
//        else
//        {
//            finalName = GetName(FilenameAddition);
//        }

//        return finalName;
//    }

//    /// <summary>
//    /// Builds the complete ffmpeg command string.
//    /// </summary>
//    /// <returns>The final ffmpeg command string.</returns>
//    public string BuildArgument()
//    {
//        // Ensure stream copying is added if applicable
//        AddStreamCopyingIfPossible();

//        return _commandBuilder.ToString().Trim() + " " + "\"" + GetNewFilePath(_filePath) + "\"" ;
//    }

//    /// <summary>
//    /// Clears the current command, allowing reuse of the FFmpegCommand instance.
//    /// </summary>
//    /// <returns>The updated FFmpegCommand instance.</returns>
//    public FFmpegCommand Clear()
//    {
//        _commandBuilder.Clear();
//        _commandBuilder.Append("ffmpeg -i \"" + _filePath + "\" ");
//        _isVideoModified = false;
//        _isAudioModified = false;
//        return this;
//    }
//}

public static class FFmpegCommand
{
    /// <summary>
    /// Builds the complete ffmpeg command based on the settings in the VideoFile.
    /// </summary>
    /// <param name="video">The VideoFile object containing all the settings.</param>
    /// <param name="options">The CropOptions for cropping the video.</param>
    /// <param name="outputPath">The output path where the processed video will be saved.</param>
    /// <returns>A string representing the ffmpeg command to execute.</returns>
    public static string BuildArgument(VideoFile video, CropOptions options)
    {
        var commandBuilder = new StringBuilder();

        // Start the ffmpeg command
        commandBuilder.Append($"-y -i \"{video.Path}\" ");

        // Add trimming options (if applicable)
        if (video.TrimStart != TimeSpan.Zero || video.TrimEnd != video.Length)
        {
            commandBuilder.AppendFormat("-ss {0:hh\\:mm\\:ss} -to {1:hh\\:mm\\:ss} ", video.TrimStart, video.TrimEnd);
        }

        // Generate the video filters (crop, rotate, scale)
        string videoFilters = GetVideoFilters(video, options);
        if (!string.IsNullOrEmpty(videoFilters))
        {
            commandBuilder.Append(videoFilters + " ");
        }

        // Ensure the output path is specified and add to the command
        string uniqueOutputPath = string.IsNullOrEmpty(video.NewPath) ? FileProcessor.GetNewFilePath(video.Path): video.NewPath;
        commandBuilder.Append($"-c:v libx264 -c:a copy \"{uniqueOutputPath}\"");

        return commandBuilder.ToString().Trim();
    }

    /// <summary>
    /// Generates the video filters (crop, rotate, scale) based on the provided VideoFile and CropOptions.
    /// </summary>
    /// <param name="video">The VideoFile containing video properties.</param>
    /// <param name="options">The CropOptions for cropping and other video adjustments.</param>
    /// <returns>A string representing the video filters for ffmpeg, or an empty string if no filters are needed.</returns>
    private static string GetVideoFilters(VideoFile video, CropOptions options)
    {
        var filters = new List<string>();

        // Add crop filter if cropping is enabled
        if (options.ShouldCrop)
        {
            var (w, h, x, y) = video.GetFinalDimensions(options);
            filters.Add($"crop={(int)w}:{(int)h}:{(int)x}:{(int)y}");
        }

        // Add rotation filter based on the rotation setting of the video
        string rotateFilter = GetRotationFilter(video.Rotation);
        if (!string.IsNullOrEmpty(rotateFilter))
        {
            filters.Add($"transpose={rotateFilter}");
        }

        //// Add scale filter based on the cropped dimensions (if applicable)
        //string scaleCommand = video.GetLQScale(video.CroppedWidth, video.CroppedHeight);
        //if (!string.IsNullOrEmpty(scaleCommand))
        //{
        //    filters.Add($"scale={scaleCommand}");
        //}

        // Join all filters with commas
        return filters.Count > 0 ? $"-vf \"{string.Join(",", filters)}\"" : string.Empty;
    }

    /// <summary>
    /// Returns the appropriate transpose value for rotation based on the degrees in the VideoFile.
    /// </summary>
    /// <param name="rotation">The rotation angle in degrees (90, 180, or 270).</param>
    /// <returns>The appropriate ffmpeg transpose value or an empty string if no rotation is needed.</returns>
    private static string GetRotationFilter(int rotation)
    {
        return rotation switch
        {
            90 => "1",                 // 90 degrees clockwise
            180 => "2,transpose=2",     // 180 degrees
            270 => "2",                 // 270 degrees clockwise (or 90 degrees counterclockwise)
            _ => string.Empty           // No rotation
        };
    }
}





