using System;
using Newtonsoft.Json;
using Pathoschild.SlackArchiveSearch.Framework;

namespace Pathoschild.SlackArchiveSearch.Models
{
    /// <summary>Basic information about a Slack channel.</summary>
    public class Channel
    {
        /// <summary>The internal unique identifier.</summary>
        public string ID { get; set; }

        /// <summary>The channel name (like 'random').</summary>
        public string Name { get; set; }

        /// <summary>When the channel was created (in the current timezone).</summary>
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime Created { get; set; }

        /// <summary>The unique identifier for the user who created the channel.</summary>
        [JsonProperty("creator")]
        public string CreatorID { get; set; }

        /// <summary>Whether the channel is archived and readonly.</summary>
        [JsonProperty("is_archived")]
        public bool IsArchived { get; set; }

        /// <summary>The unique identifiers for the users currently in the channel.</summary>
        [JsonProperty("members")]
        public string[] MemberIds { get; set; }

        /// <summary>The messages in the channel.</summary>
        public Message[] Messages { get; set; }
    }
}
