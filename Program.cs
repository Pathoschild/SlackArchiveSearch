using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Pathoschild.SlackArchiveSearch.Framework;
using Pathoschild.SlackArchiveSearch.Models;

namespace Pathoschild.SlackArchiveSearch
{
    /// <summary>The console app entry point.</summary>
    public static class Program
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The directory to which to write archive data.</summary>
        private static readonly string DataDirectory = Path.Combine(Path.GetTempPath(), "slack archive search");

        /// <summary>The number of matches to show on the screen at a time.</summary>
        const int PageSize = 5;


        /*********
        ** Public methods
        *********/
        /// <summary>The console app entry point.</summary>
        public static void Main()
        {
            // load archive data
            Cache data;
            while (true)
            {
                // choose archive directory
                Console.WriteLine("Enter the directory path of the Slack archive (or leave it blank to use the last import):");
                string archiveDirectory = Console.ReadLine();

                // load previous import
                if (string.IsNullOrWhiteSpace(archiveDirectory))
                {
                    Console.WriteLine("Reading cache...");
                    data = Program.FetchCache(Program.DataDirectory);
                    if (data == null)
                    {
                        Console.WriteLine("There's no cached import available.");
                        continue;
                    }
                    break;
                }

                // import new data
                if (!Directory.Exists(archiveDirectory))
                {
                    Console.WriteLine("There's no directory at that path.");
                    continue;
                }

                Console.WriteLine("Reading archive...");
                data = Program.RebuildArchive(Program.DataDirectory, archiveDirectory);
                break;
            }

            // search archive data
            while (true)
            {
                // show header
                Console.Clear();
                Console.WriteLine($"Found {data.Messages.Length} messages by {data.Users.Count} users in {data.Channels.Count} channels, posted between {data.Messages.Min(p => p.Date).ToString("yyyy-MM-dd HH:mm")} and {data.Messages.Max(p => p.Date).ToString("yyyy-MM-dd HH:mm")}.");
                Console.WriteLine($"All times are shown in {TimeZone.CurrentTimeZone.StandardName}.");
                Console.WriteLine("\nWhat message text do you want to search?");

                // get search string
                string search = Console.ReadLine();
                if (search == null)
                    continue;

                // get matches
                Message[] matches = (
                    from message in data.Messages
                    where message.Text != null && message.Text.Contains(search)
                    orderby message.Date descending
                    select message
                ).ToArray();
                Program.DisplayResults(matches, data.Channels, data.Users, Program.PageSize);
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Interactively read data from a Slack archive into the cache.</summary>
        /// <param name="dataDirectory">The directory from which to load archive data.</param>
        /// <returns>Returns the cached data if it exists, else <c>null</c>.</returns>
        private static Cache FetchCache(string dataDirectory)
        {
            string cacheFile = Path.Combine(dataDirectory, "cache.json");
            if (!File.Exists(cacheFile))
                return null;

            string json = File.ReadAllText(cacheFile);
            return JsonConvert.DeserializeObject<Cache>(json);
        }

        /// <summary>Interactively read data from a Slack archive into the cache.</summary>
        /// <param name="dataDirectory">The directory to which to write archive data.</param>
        /// <param name="archiveDirectory">The archive directory to import.</param>
        private static Cache RebuildArchive(string dataDirectory, string archiveDirectory)
        {
            // read metadata
            Dictionary<string, User> users = Program.ReadFile<List<User>>(Path.Combine(archiveDirectory, "users.json")).ToDictionary(p => p.ID);
            Dictionary<string, Channel> channels = Program.ReadFile<List<Channel>>(Path.Combine(archiveDirectory, "channels.json")).ToDictionary(p => p.ID);

            // read channel messages
            List<Message> messages = new List<Message>();
            foreach (Channel channel in channels.Values)
            {
                foreach (string path in Directory.EnumerateFiles(Path.Combine(archiveDirectory, channel.Name)))
                {
                    var channelMessages = Program.ReadFile<List<Message>>(path);
                    foreach (Message message in channelMessages)
                        message.ChannelID = channel.ID;
                    messages.AddRange(channelMessages);
                }
            }

            // cache data
            Directory.CreateDirectory(dataDirectory);
            string cacheFile = Path.Combine(dataDirectory, "cache.json");
            Cache cache = new Cache { Channels = channels, Users = users, Messages = messages.ToArray() };
            File.WriteAllText(cacheFile, JsonConvert.SerializeObject(cache));

            return cache;
        }

        /// <summary>Interactively display search results.</summary>
        /// <param name="matches">The search results.</param>
        /// <param name="channels">The known channels.</param>
        /// <param name="users">The known users.</param>
        /// <param name="pageSize">The number of items to show on the screen at one time.</param>
        private static void DisplayResults(Message[] matches, IDictionary<string, Channel> channels, IDictionary<string, User> users, int pageSize)
        {
            // format matches for output
            string[] output = matches.Select(message =>
            {
                string username = message.CustomUserName ?? (message.UserID != null ? users[message.UserID].Name : "<no name>");
                string channelName = channels[message.ChannelID].Name;
                string formattedText = String.Join("\n│ ", message.Text.Split('\n'));

                return
                    "┌──────────────────────────────\n"
                    + $"│ Date:    {message.Date.ToString("yyyy-MM-dd HH:mm")}\n"
                    + $"│ Channel: #{channelName}\n"
                    + $"│ User:    {username}\n"
                    + $"| {formattedText}\n"
                    + "└──────────────────────────────\n";
            }).ToArray();

            // write matches to console
            int offset = 0;
            int count = matches.Length;
            while (true)
            {
                Console.Clear();

                // print header
                Console.WriteLine($"Found {count} matches.");

                // print results
                Console.WriteLine(String.Join("\n", output.Skip(offset).Take(pageSize)));

                // print footer
                if (matches.Length <= pageSize)
                    break;
                Console.WriteLine($"Viewing matches {offset + 1}–{Math.Min(count, offset + 1 + pageSize)} of {count}.");
                string question = "";
                if (offset > 0)
                    question += "[p]revious page  ";
                if (offset + pageSize < count)
                    question += "[n]ext page  ";
                question += "[q]uit";

                switch (Program.ReadOption(question, 'p', 'n', 'q'))
                {
                    case 'p':
                        offset = Math.Max(0, offset - pageSize);
                        continue;

                    case 'n':
                        offset = Math.Min(count, offset + pageSize);
                        continue;

                    case 'q':
                        return;
                }
            }
        }

        /// <summary>Read an option from the command line.</summary>
        /// <param name="message">The question to ask.</param>
        /// <param name="options">The accepted values.</param>
        private static char ReadOption(string message, params char[] options)
        {
            while (true)
            {
                Console.WriteLine(message);
                string response = Console.ReadLine();
                if (response == null || response.Length > 1 || !options.Contains(response[0]))
                {
                    Console.WriteLine("Invalid answer.");
                    continue;
                }

                return response[0];
            }
        }

        /// <summary>Parse a file's JSON contents.</summary>
        /// <typeparam name="T">The model type matching the file contents.</typeparam>
        /// <param name="path">The file path.</param>
        private static T ReadFile<T>(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json, new UnixDateTimeConverter());
        }
    }
}
