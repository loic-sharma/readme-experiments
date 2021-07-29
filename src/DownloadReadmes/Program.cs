using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Octokit;

namespace DownloadReadmes
{
    class Program
    {
        private const int MaxResultsPerQuery = 1000;
        private const int MaxResultsPerPage = 100;
        private const int MinStars = 1000;

        static async Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("DownloadReadmes.exe <GitHub token> <path to readme directory>");
                return;
            }

            ServicePointManager.DefaultConnectionLimit = 64;
            ServicePointManager.MaxServicePointIdleTime = 10000;

            var client = new GitHubClient(new ProductHeaderValue("download-readmes"));
            client.Credentials = new Credentials(args[0]);

            var repositories = await DiscoverRepositoriesAsync(client);

            var work = new ConcurrentBag<Repository>(repositories);

            var tasks = Enumerable
                .Range(0, 32)
                .Select(async i =>
                {
                    while (work.TryTake(out var item))
                    {
                        var directory = Path.Combine(args[1], item.Owner, item.Name);
                        var readmePath = Path.Combine(directory, "README.md");
                        var notFoundPath = Path.Combine(directory, "_._");

                        if (File.Exists(readmePath) || File.Exists(notFoundPath))
                        {
                            Console.WriteLine($"Skipping {item.Owner}/{item.Name}");
                            continue;
                        }

                        Console.WriteLine($"Downloading {item.Owner}/{item.Name}");
                        Directory.CreateDirectory(directory);

                        try
                        {
                            var response = await client.Repository.Content.GetReadme(item.Owner, item.Name);

                            File.WriteAllText(
                                Path.Combine(directory, "README.md"),
                                response.Content);
                        }
                        catch (NotFoundException)
                        {
                            File.WriteAllText(
                                Path.Combine(directory, "_._"),
                                "");
                        }
                    }
                });

            await Task.WhenAll(tasks);
        }

        private static async Task<HashSet<Repository>> DiscoverRepositoriesAsync(GitHubClient client)
        {
            var repositories = new HashSet<Repository>();

            var maxPages = Math.Ceiling(MaxResultsPerQuery / (double)MaxResultsPerPage);
            var starCursor = int.MaxValue;

            while (starCursor >= MinStars)
            {
                for (var page = 0; page < maxPages; page++)
                {
                    Console.WriteLine($"Fetching page {page} of repositories with fewer than {starCursor} stars...");

                    var request = new SearchRepositoriesRequest
                    {
                        Stars = new Octokit.Range(MinStars, starCursor),
                        Language = Language.CSharp,
                        SortField = RepoSearchSort.Stars,
                        Order = SortDirection.Descending,

                        Page = page + 1,
                        PerPage = MaxResultsPerPage,
                    };

                    var searchResults = await client.Search.SearchRepo(request);

                    if (!searchResults.Items.Any())
                    {
                        return repositories;
                    }

                    repositories.UnionWith(
                        searchResults
                            .Items
                            .Where(i => i.Private == false)
                            .Select(i => new Repository(i.Owner.Login, i.Name)));

                    starCursor = searchResults.Items.Min(i => i.StargazersCount);
                }
            }

            return repositories;
        }
    }

    record Repository(string Owner, string Name);
}