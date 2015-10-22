using System.Collections.Generic;

namespace Pathoschild.SlackArchiveSearch.Models
{
    /// <summary>Cached Slack archive data.</summary>
    public class Cache
    {
        /// <summary>Metadata about every Slack channel.</summary>
        public Dictionary<string, Channel> Channels { get; set; }

        /// <summary>Metadata about every Slack user.</summary>
        public Dictionary<string, User> Users { get; set; }

        /// <summary>Metadata about all Slack message that's been sent.</summary>
        public Message[] Messages { get; set; }
    }
}