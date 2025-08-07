using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VideoTools
{
    /// <summary>
    /// Provides helper methods for downloading videos and extracting their audio tracks.
    /// </summary>
    public class VideoFetcher
    {
        private readonly HttpClient _httpClient;

        public VideoFetcher(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Downloads a video by its identifier and extracts the audio track as an MP3 file.
        /// </summary>
        /// <param name="videoId">Identifier of the video on the remote service.</param>
        /// <param name="outputDirectory">Directory where the audio file will be saved.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The full path to the extracted audio file.</returns>
        /// <exception cref="ArgumentException">Thrown when required parameters are missing.</exception>
        /// <exception cref="InvalidOperationException">Thrown when ffmpeg fails to extract audio.</exception>
        public async Task<string> DownloadAndExtractAudioAsync(string videoId, string outputDirectory, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(videoId))
            {
                throw new ArgumentException("Video identifier must be provided.", nameof(videoId));
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("Output directory must be provided.", nameof(outputDirectory));
            }

            Directory.CreateDirectory(outputDirectory);

            string tempVideoPath = Path.Combine(Path.GetTempPath(), $"{videoId}.mp4");
            string audioOutputPath = Path.Combine(outputDirectory, $"{videoId}.mp3");

            // Construct a basic URL; real implementation may vary depending on service.
            string videoUrl = $"https://www.youtube.com/watch?v={videoId}";

            using (HttpResponseMessage response = await _httpClient.GetAsync(videoUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using FileStream fileStream = File.Create(tempVideoPath);
                await responseStream.CopyToAsync(fileStream, cancellationToken);
            }

            var ffmpegInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-y -i \"{tempVideoPath}\" -vn -acodec libmp3lame \"{audioOutputPath}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process? process = Process.Start(ffmpegInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start ffmpeg process.");
                }

                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    string errorMessage = await process.StandardError.ReadToEndAsync();
                    throw new InvalidOperationException($"ffmpeg exited with code {process.ExitCode}: {errorMessage}");
                }
            }

            File.Delete(tempVideoPath);
            return audioOutputPath;
        }
    }
}
