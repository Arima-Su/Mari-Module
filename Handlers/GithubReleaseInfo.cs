using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mari_Module.Handlers
{
    public class GitHubReleaseInfo
    {
        public string tag_name { get; set; }
        public GitHubReleaseAsset[] assets { get; set; }
    }

    public class GitHubReleaseAsset
    {
        public string name { get; set; }
        public string browser_download_url { get; set; }
    }
}
