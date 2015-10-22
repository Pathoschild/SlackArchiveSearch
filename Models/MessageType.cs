namespace Pathoschild.SlackArchiveSearch.Models
{
    /// <summary>Represents message types.</summary>
    public enum MessageType
    {
        /// <summary>The message type isn't recognised.</summary>
        Unknown = 0,

        /// <summary>A message. This is the only known type.</summary>
        Message = 1
    }
}
