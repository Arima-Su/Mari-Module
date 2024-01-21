using System;

namespace Alice_Module.Loaders
{
    public class Converts
    {
        public static string ConvertToShortenedUrl(string fullUrl)
        {
            if (Validates.IsShortHandYoutubeLink(fullUrl))
            {
                return fullUrl;
            }

            if (!IsValidYouTubeUrl(fullUrl))
            {
                throw new ArgumentException("Invalid YouTube URL");
            }

            string videoId = ExtractYouTubeVideoId(fullUrl);

            string shortenedUrl = $"https://youtu.be/{videoId}";

            return shortenedUrl;
        }

        static bool IsValidYouTubeUrl(string url)
        {
            return url.Contains("youtube.com") && url.Contains("v=");
        }

        static string ExtractYouTubeVideoId(string url)
        {
            int startIndex = url.IndexOf("v=") + 2;
            int endIndex = url.IndexOf('&', startIndex);
            if (endIndex == -1)
            {
                endIndex = url.Length;
            }

            string videoId = url.Substring(startIndex, endIndex - startIndex);
            return videoId;
        }

        public static string ExtractSpotifyPlaylistId(string playlistLink)
        {
            const string startString = "playlist/";
            const string endString = "?si=";

            int startIndex = playlistLink.IndexOf(startString, StringComparison.OrdinalIgnoreCase);
            int endIndex = playlistLink.IndexOf(endString, StringComparison.OrdinalIgnoreCase);

            if (startIndex != -1 && endIndex != -1)
            {
                startIndex += startString.Length;
                return playlistLink.Substring(startIndex, endIndex - startIndex);
            }

            throw new ArgumentException("Invalid Spotify playlist link");
        }
    }
}
