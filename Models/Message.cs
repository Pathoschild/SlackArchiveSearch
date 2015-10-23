using System;
using Newtonsoft.Json;
using Pathoschild.SlackArchiveSearch.Framework;

namespace Pathoschild.SlackArchiveSearch.Models
{
    /// <summary>Basic information about a Slack channel message.</summary>
    public class Message
    {
        /****
        ** Fields from archive
        ****/
        /// <summary>When the message was posted.</summary>
        [JsonProperty("ts")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime Date { get; set; }

        /// <summary>The message author's unique identifier.</summary>
        [JsonProperty("user")]
        public string UserID { get; set; }

        /// <summary>The message author's display name, if the username was overridden. This typically only applies for bot messages.</summary>
        [JsonProperty("username")]
        public string CustomUserName { get; set; }

        /// <summary>The message type (typically 'message').</summary>
        public MessageType Type { get; set; }

        /// <summary>The message subtype (like 'channel_purpose' or 'channel_join'); this is null for normal user messages.</summary>
        public MessageSubtype Subtype { get; set; }

        /// <summary>The message text (with Slack formatting markup).</summary>
        public string Text { get; set; }


        /****
        ** Injected fields
        ****/
        /// <summary>A unique ID for search lookup.</summary>
        public string MessageID { get; set; }

        /// <summary>The channel to which the message was posted.</summary>
        /// <remarks>This value is populated after parsing.</remarks>
        public string ChannelID { get; set; }

        /// <summary>The name of the channel to which the message was posted.</summary>
        /// <remarks>This value is populated after parsing.</remarks>
        public string ChannelName { get; set; }

        /// <summary>The display name of the message author.</summary>
        public string AuthorName { get; set; }

        /// <summary>The username of the message author.</summary>
        public string AuthorUsername { get; set; }
    }
}
