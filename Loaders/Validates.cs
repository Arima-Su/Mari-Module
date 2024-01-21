namespace Alice_Module.Loaders
{
    public class Validates
    {
        public static bool IsYouTubePlaylistLink(string link)
        {
            return link.Contains("youtube.com") && link.Contains("list=");
        }

        public static bool IsYoutubeLink(string link)
        {
            return link.Contains("youtube.com") || link.Contains("youtu.be");
        }

        public static bool IsShortHandYoutubeLink(string link)
        {
            return link.Contains("youtu.be");
        }

        public static bool IsSpotifyPlaylistLink(string link)
        {
            return link.Contains("spotify.com") && link.Contains("playlist/");
        }
        
        public static bool IsSpotifyLink(string link)
        {
            return link.Contains("spotify.com") && link.Contains("track/");
        }

        public static bool HasOperation(char msg)
        {
            if (msg == '+' || msg == '-' || msg == '*' || msg == '/' || msg == '(' || msg == ')')
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool HasOperation(string msg)
        {
            if (msg.Contains("+") || msg.Contains("-") || msg.Contains("*") || msg.Contains("/") || msg.Contains("(") || msg.Contains(")"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
