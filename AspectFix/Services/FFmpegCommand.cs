using System;
using System.Collections.Generic;
using System.Text;

namespace AspectFix.Services
{
    /// <summary>
    /// This class is responsible for building the ffmpeg command based on the settings of the VideoFile.
    /// Handles trimming, cropping, rotation, and scaling of the video.
    /// </summary>
    public static class FFmpegCommand
    {
        /// <summary>
        /// Builds the complete ffmpeg command based on the settings in the VideoFile.
        /// </summary>
        /// <param name="video">The VideoFile object containing all the settings.</param>
        /// <param name="outputPath">The output path where the processed video will be saved.</param>
        /// <returns>A string representing the ffmpeg command to execute.</returns>
        public static string BuildArgument(VideoFile video)
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
            string videoFilters = GetVideoFilters(video);
            if (!string.IsNullOrEmpty(videoFilters))
            {
                commandBuilder.Append(videoFilters + " ");
            }

            // Ensure the output path is specified and add to the command
            string uniqueOutputPath = string.IsNullOrEmpty(video.NewPath) ? FileProcessor.GetNewFilePath(video.Path) : video.NewPath;
            commandBuilder.Append($"-c:v libx264 -c:a copy \"{uniqueOutputPath}\"");

            return commandBuilder.ToString().Trim();
        }

        /// <summary>
        /// Generates the video filters (crop, rotate, scale) based on the provided VideoFile and CropOptions.
        /// </summary>
        /// <param name="video">The VideoFile containing video properties.</param>
        /// <returns>A string representing the video filters for ffmpeg, or an empty string if no filters are needed.</returns>
        private static string GetVideoFilters(VideoFile video)
        {
            var filters = new List<string>();

            var (w, h, x, y) = video.GetFinalDimensions();
            filters.Add($"crop={(int)w}:{(int)h}:{(int)x}:{(int)y}");

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
}





