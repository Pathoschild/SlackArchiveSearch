using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Newtonsoft.Json;
using Pathoschild.SlackArchiveSearch.Framework;
using Pathoschild.SlackArchiveSearch.Models;
using Directory = System.IO.Directory;
using Version = Lucene.Net.Util.Version;

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

        /// <summary>The directory to which to write archive data.</summary>
        private static readonly string IndexDirectory = Path.Combine(DataDirectory, "index");

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

                data = Program.ImportArchive(Program.DataDirectory, archiveDirectory);
                Program.RebuildIndex(data, Program.IndexDirectory);
                break;
            }

            // search archive data
            while (true)
            {
                // show header
                Console.Clear();
                Console.WriteLine($"Found {data.Messages.Length} messages by {data.Users.Count} users in {data.Channels.Count} channels, posted between {data.Messages.Min(p => p.Date.LocalDateTime).ToString("yyyy-MM-dd HH:mm")} and {data.Messages.Max(p => p.Date.LocalDateTime).ToString("yyyy-MM-dd HH:mm")}.");
                Console.WriteLine($"All times are shown in {TimeZone.CurrentTimeZone.StandardName}.");
                Console.WriteLine();
                Console.WriteLine("┌───Search syntax──────────────");
                Console.WriteLine("│ You can enter a simple query to search the message text, or use Lucene");
                Console.WriteLine("│ search syntax: https://lucene.apache.org/core/2_9_4/queryparsersyntax.html");
                Console.WriteLine("│ Search is not case-sensitive.");
                Console.WriteLine("│");
                Console.WriteLine("│ Available fields:");
                Console.WriteLine("│   date (in ISO-8601 format like 2015-01-30T15:00:00, in UTC);");
                Console.WriteLine("│   channel (like 'lunch');");
                Console.WriteLine("│   user (like 'jesse.plamondon');");
                Console.WriteLine("│   text (in slack format).");
                Console.WriteLine("│");
                Console.WriteLine("│Example searches:");
                Console.WriteLine("│   pineapple");
                Console.WriteLine("│   channel:lunch user:jesse.plamondon pineapple");
                Console.WriteLine("│   channel:(developers OR deployment) text:\"deployed release\"");
                Console.WriteLine("└──────────────────────────────");
                Console.WriteLine();
                Console.WriteLine("\nWhat do you want to search?");
                Console.Write("> ");

                // get search string
                string search = Console.ReadLine();
                if (search == null)
                    continue;

                // show matches
                Message[] matches = Program.SearchIndex(search, data, Program.IndexDirectory).ToArray();
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
        private static Cache ImportArchive(string dataDirectory, string archiveDirectory)
        {
            // read metadata
            Console.WriteLine("Reading metadata...");
            Dictionary<string, User> users = Program.ReadFile<List<User>>(Path.Combine(archiveDirectory, "users.json")).ToDictionary(p => p.ID);
            Dictionary<string, Channel> channels = Program.ReadFile<List<Channel>>(Path.Combine(archiveDirectory, "channels.json")).ToDictionary(p => p.ID);

            // read channel messages
            Console.WriteLine("Reading channel data...");
            List<Message> messages = new List<Message>();
            foreach (Channel channel in channels.Values)
            {
                foreach (string path in Directory.EnumerateFiles(Path.Combine(archiveDirectory, channel.Name)))
                {
                    // read messages
                    var channelMessages = Program.ReadFile<List<Message>>(path);
                    foreach (Message message in channelMessages)
                    {
                        // inject message data
                        message.MessageID = Guid.NewGuid().ToString("N");

                        // inject channel data
                        message.ChannelID = channel.ID;
                        message.ChannelName = channel.Name;

                        // inject user data
                        User user = message.UserID != null && users.ContainsKey(message.UserID) ? users[message.UserID] : null;
                        if (user != null)
                        {
                            message.AuthorName = user.Name;
                            message.AuthorUsername = user.UserName;
                        }
                        else
                        {
                            message.AuthorName = message.CustomUserName ?? message.UserID;
                            message.AuthorUsername = message.CustomUserName ?? message.UserID;
                        }
                    }
                    messages.AddRange(channelMessages);
                }
            }

            // cache data
            Console.WriteLine("Writing cache...");
            Directory.CreateDirectory(dataDirectory);
            string cacheFile = Path.Combine(dataDirectory, "cache.json");
            Cache cache = new Cache
            {
                Channels = channels,
                Users = users,
                Messages = messages.OrderByDescending(p => p.Date).ToArray() // sort newest first for index
            };
            File.WriteAllText(cacheFile, JsonConvert.SerializeObject(cache));

            return cache;
        }

        /// <summary>Interactively rebuild the search index.</summary>
        /// <param name="data">The data to index.</param>
        /// <param name="indexDirectory">The directory containing the search index.</param>
        public static void RebuildIndex(Cache data, string indexDirectory)
        {
            // clear previous index
            foreach (string file in Directory.EnumerateFiles(indexDirectory))
                File.Delete(file);

            // build Lucene index
            Console.WriteLine("Building search index...");
            using (FSDirectory directory = FSDirectory.Open(indexDirectory))
            using (Analyzer analyzer = new StandardAnalyzer(Version.LUCENE_30))
            using (IndexWriter writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                foreach (var message in data.Messages)
                {
                    Document doc = new Document();
                    doc.Add(new Field("id", message.MessageID, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field("date", message.Date.ToString("o"), Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field("channel", message.ChannelName, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field("user", message.AuthorUsername ?? "", Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field("text", message.Text ?? "", Field.Store.YES, Field.Index.ANALYZED));
                    writer.AddDocument(doc);
                }
                writer.Optimize();
                writer.Flush(true, true, true);
            }
        }

        /// <summary>Find messages matching an index search.</summary>
        /// <param name="search">The search query.</param>
        /// <param name="data">The data to search.</param>
        /// <param name="indexDirectory">The directory containing the search index.</param>
        public static IEnumerable<Message> SearchIndex(string search, Cache data, string indexDirectory)
        {
            // search index
            using (FSDirectory directory = FSDirectory.Open(indexDirectory))
            using (IndexReader reader = IndexReader.Open(directory, true))
            using (Searcher searcher = new IndexSearcher(reader))
            using (Analyzer analyzer = new StandardAnalyzer(Version.LUCENE_30))
            {
                // build query parser
                QueryParser parser = new MultiFieldQueryParser(Version.LUCENE_30, new[] { "id", "date", "channel", "user", "text"}, analyzer);
                // search index
                Query query = parser.Parse(search);
                ScoreDoc[] hits = searcher.Search(query, null, 1000, Sort.INDEXORDER).ScoreDocs;

                // return matches
                foreach (ScoreDoc hit in hits)
                {
                    Document document = searcher.Doc(hit.Doc);
                    string messageID = document.Get("id");
                    Message message = data.Messages.FirstOrDefault(p => p.MessageID == messageID);
                    if (message == null)
                    {
                        Console.WriteLine($"ERROR: couldn't find message #{messageID} matching the search index. The search index may be out of sync.");
                        continue;
                    }
                    yield return message;
                }
            }
        }

        /// <summary>Interactively display search results.</summary>
        /// <param name="matches">The search results.</param>
        /// <param name="channels">The known channels.</param>
        /// <param name="users">The known users.</param>
        /// <param name="pageSize">The number of items to show on the screen at one time.</param>
        private static void DisplayResults(Message[] matches, IDictionary<string, Channel> channels, IDictionary<string, User> users, int pageSize)
        {
            // no matches
            if (!matches.Any())
            {
                Console.WriteLine("No matches found. :(");
                Console.WriteLine("Hit enter to continue.");
                Console.ReadLine();
                return;
            }

            // format matches for output
            string[] output = matches.Select(message =>
            {
                string formattedText = String.Join("\n│ ", message.Text.Split('\n'));
                return
                    "┌──────────────────────────────\n"
                    + $"│ Date:    {message.Date.LocalDateTime.ToString("yyyy-MM-dd HH:mm")}\n"
                    + $"│ Channel: #{message.ChannelName}\n"
                    + $"│ User:    {message.AuthorUsername}\n"
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
