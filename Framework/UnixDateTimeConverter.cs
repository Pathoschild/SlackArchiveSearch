using System;
using Newtonsoft.Json;

namespace Pathoschild.SlackArchiveSearch.Framework
{
    /// <summary>A JSON.NET value converter which converts Unix-style epoch timestamps into UTC <see cref="DateTimeOffset" /> values.</summary>
    /// <remarks>Derived from http://stackapps.com/a/1176 .</remarks>
    public class UnixDateTimeConverter : JsonConverter
    {
        /// <summary>Determines whether this instance can convert the specified object type.</summary>
        /// <param name="objectType">Type of the object.</param>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTimeOffset);
        }

        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // invalid value
            if (!(value is DateTimeOffset))
                throw new Exception("Expected date object value.");

            // convert to Unix time
            DateTime date = ((DateTimeOffset)value).UtcDateTime;
            DateTime epoch = new DateTime(1970, 1, 1);
            double timestamp = (date - epoch).TotalSeconds;
            writer.WriteValue(timestamp);
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // get epoch timestamp
            double timestamp;
            switch (reader.TokenType)
            {
                case JsonToken.Integer:
                case JsonToken.Float:
                    timestamp = (double)reader.Value;
                    break;

                case JsonToken.String:
                    try
                    {
                        timestamp = (double)Convert.ChangeType(reader.Value, typeof(double));
                    }
                    catch (FormatException ex)
                    {
                        throw new FormatException($"Can't parse string value '{reader.Value}' as a Unix timestamp.", ex);
                    }
                    break;

                default:
                    throw new Exception($"Can't parse '{reader.TokenType}' type as a Unix epoch timestamp, must be numeric.");
            }

            // convert to DateTime
            DateTimeOffset epoch = new DateTimeOffset(new DateTime(1970, 1, 1), TimeSpan.Zero);
            return epoch.AddSeconds(timestamp);
        }
    }
}
