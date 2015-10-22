using Newtonsoft.Json;

namespace Pathoschild.SlackArchiveSearch.Models
{
    /// <summary>Basic information about a Slack user.</summary>
    public class User
    {
        /// <summary>The internal unique identifier.</summary>
        public string ID { get; set; }

        /// <summary>The user's login username.</summary>
        [JsonProperty("name")]
        public string UserName { get; set; }

        /// <summary>The user's real full name.</summary>
        [JsonProperty("real_name")]
        public string Name { get; set; }

        /// <summary>Whether the user's account has been deactivated.</summary>
        [JsonProperty("deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>Whether the user has administrator access.</summary>
        [JsonProperty("is_admin")]
        public bool IsAdmin { get; set; }

        /// <summary>Whether the user has enabled two-factor authentication.</summary>
        [JsonProperty("has_2fa")]
        public bool HasTwoFactorAuth { get; set; }
    }
}
