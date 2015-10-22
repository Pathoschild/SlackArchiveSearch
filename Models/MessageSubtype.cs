namespace Pathoschild.SlackArchiveSearch.Models
{
    // ReSharper disable InconsistentNaming

    /// <summary>Represents message subtypes.</summary>
    public enum MessageSubtype
    {
        /// <summary>The message subtype isn't recognised.</summary>
        Unknown = 0,

        /// <summary>Someone joined the channel.</summary>
        Channel_Join = 1,

        /// <summary>Someone changed the channel purpose.</summary>
        Channel_Purpose = 2,

        /// <summary>Someone left the channel.</summary>
        Channel_Leave = 3,

        /// <summary>Someone archived the channel.</summary>
        Channel_Archive = 4,

        /// <summary>Someone renamed the channel.</summary>
        Channel_Name = 5,

        /// <summary>A message posted by an integration or through the API.</summary>
        Bot_Message = 6,

        /// <summary>Someone shared a file with the channel.</summary>
        File_Share = 7,

        /// <summary>Someone pinned a previous message to the channel.</summary>
        Pinned_Item = 8,

        /// <summary>Someone added a comment to a file previously shared with the channel.</summary>
        File_Comment = 9,

        /// <summary>Someone posted a message using the <c>/me</c> Slack command.</summary>
        Me_Message = 10,

        /// <summary>Someone added an integration to the channel.</summary>
        Bot_Add = 11,

        /// <summary>Someone removed an integration from the channel.</summary>
        Bot_Remove = 12,

        /// <summary>Someone mentioned a file in the channel?</summary>
        File_Mention = 13,

        /// <summary>Someone unarchived the channel.</summary>
        Channel_Unarchive = 14
    }
}