using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ServiceStack.Text;

namespace AnalyzeReadmes
{
    class Program
    {
        public static readonly HashSet<string> CSharpCodeFences = new(StringComparer.OrdinalIgnoreCase)
        {
            "c#",
            "cs",
            "csharp"
        };

        // From: https://github.com/NuGet/NuGetGallery/blob/77c5f6e57e0e53e8ef52b3e433571b2469681e25/src/NuGetGallery/App_Data/Files/Content/Trusted-Image-Domains.json#L3-L30
        public static readonly HashSet<string> ImageHostsAllowList = new()
        {
            "api.bintray.com",
            "api.codacy.com",
            "app.codacy.com",
            "api.codeclimate.com",
            "api.dependabot.com",
            "api.travis-ci.com",
            "api.travis-ci.org",
            "app.fossa.io",
            "badge.fury.io",
            "badgen.net",
            "badges.gitter.im",
            "bettercodehub.com",
            "buildstats.info",
            "ci.appveyor.com",
            "circleci.com",
            "codecov.io",
            "codefactor.io",
            "coveralls.io",
            "gitlab.com",
            "img.shields.io",
            "isitmaintained.com",
            "opencollective.com",
            "snyk.io",
            "sonarcloud.io",
            "raw.github.com",
            "raw.githubusercontent.com",
            "user-images.githubusercontent.com",
            "camo.githubusercontent.com"
        };

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("AnalyzeReadmes.exe <path to readmes> <path to reports>");
                return;
            }

            var readmesPath = args[0];
            var reportsPath = args[1];

            var repositoriesCsv = File.ReadAllText(Path.Combine(readmesPath, "repositories.csv"));
            var repositories = CsvSerializer.DeserializeFromString<Repo[]>(repositoriesCsv);

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            var ctx = AnalysisContext.Create();
            foreach (var repository in repositories.OrderByDescending(r => r.Stars))
            {
                var readmePath = Path.Combine(readmesPath, repository.Owner, repository.Repository, "README.md");
                if (!File.Exists(readmePath))
                {
                    Console.WriteLine($"MISS {repository}");
                    continue;
                }

                var readme = File.ReadAllText(readmePath);
                var markdown = Markdown.Parse(readme, pipeline);

                Analyze(ctx, repository, markdown);
            }

            var disallowedHostRecords = ctx
                .DisallowedHosts
                .OrderByDescending(host => host.Value.Sum(r => r.Stars))
                .SelectMany(host => host
                    .Value
                    .Select(repository => new DisallowedHostRecord(
                        host.Key,
                        repository.GitHubProjectMd,
                        repository.NuGetPreviewMd,
                        repository.Stars)));
            var codeFenceRecords = ctx
                .CodeFences
                .OrderByDescending(f => f.Repository.Stars)
                .ThenBy(f => (f.Repository.Owner, f.Repository.Repository))
                .Select(f => new CodeFenceRecord(
                    f.Repository.GitHubProjectMd,
                    f.Repository.NuGetPreviewMd,
                    f.Repository.Stars,
                    f.Kind));
            var strikethroughRecords = ctx
                .Strikethroughs
                .OrderByDescending(r => r.Stars)
                .Select(r => new GenericRecord(r.GitHubProjectMd, r.NuGetPreviewMd, r.Stars));
            var tableRecords = ctx
                .Tables
                .OrderByDescending(r => r.Stars)
                .Select(r => new GenericRecord(r.GitHubProjectMd, r.NuGetPreviewMd, r.Stars));
            var htmlRecords = ctx
                .Htmls
                .OrderByDescending(r => r.Stars)
                .Select(r => new GenericRecord(r.GitHubProjectMd, r.NuGetPreviewMd, r.Stars));

            var disallowedHostsPath = Path.Combine(reportsPath, "disallowedImageHosts.csv");
            var strikethroughPath = Path.Combine(reportsPath, "strikethroughs.csv");
            var codeFencesPath = Path.Combine(reportsPath, "codeFences.csv");
            var tablesPath = Path.Combine(reportsPath, "tables.csv");
            var htmlsPath = Path.Combine(reportsPath, "htmls.csv");

            File.WriteAllText(disallowedHostsPath, CsvSerializer.SerializeToString(disallowedHostRecords));
            File.WriteAllText(strikethroughPath, CsvSerializer.SerializeToString(strikethroughRecords));
            File.WriteAllText(codeFencesPath, CsvSerializer.SerializeToString(codeFenceRecords));
            File.WriteAllText(tablesPath, CsvSerializer.SerializeToString(tableRecords));
            File.WriteAllText(htmlsPath, CsvSerializer.SerializeToString(htmlRecords));
        }

        public static void Analyze(AnalysisContext ctx, Repo repository, MarkdownDocument document)
        {
            foreach (var node in document.Descendants())
            {
                if (node is LinkInline { IsImage: true} imageLink)
                {
                    if (Uri.TryCreate(imageLink.Url, UriKind.Absolute, out var imageUri))
                    {
                        if (!ImageHostsAllowList.Contains(imageUri.Host))
                        {
                            ctx.TrackDisallowedImageHost(repository, imageUri.Host);
                        }
                    }
                }

                if (node is Table) ctx.Tables.Add(repository);
                if (node is HtmlBlock) ctx.Htmls.Add(repository);
                if (node is FencedCodeBlock code) ctx.CodeFences.Add((code.Info, repository));
                if (node is EmphasisInline { DelimiterChar: '~' }) ctx.Strikethroughs.Add(repository);
            }
        }
    }

    record Repo(string Owner, string Repository, int Stars)
    {
        public string GitHubProjectMd => $"[{Owner}/{Repository}](https://www.github.com/{Owner}/{Repository}#readme)";
        public string NuGetPreviewMd => $"[Preview](https://dev.nugettest.org/packages/loic.{Owner}.{Repository}?preview=1#show-readme-container)";

    }
    record DisallowedHostRecord(string Host, string GitHubProject, string NuGetPreview, int Stars);
    record CodeFenceRecord(string GitHubProject, string NuGetPreview, int Stars, string CodeFenceKind);
    record GenericRecord(string GitHubProject, string NuGetPreview, int Stars);

    record AnalysisContext(
        Dictionary<string, HashSet<Repo>> DisallowedHosts,
        HashSet<(string Kind, Repo Repository)> CodeFences,
        HashSet<Repo> Tables,
        HashSet<Repo> Htmls,
        HashSet<Repo> Strikethroughs)
    {
        // TODO: Better way??
        public static AnalysisContext Create() => new(
            new(StringComparer.OrdinalIgnoreCase),
            new(),
            new(),
            new(),
            new());

        public void TrackDisallowedImageHost(Repo repository, string host)
        {
            if (!DisallowedHosts.TryGetValue(host, out var repositories))
            {
                repositories = new HashSet<Repo>();
                DisallowedHosts.Add(host, repositories);
            }

            repositories.Add(repository);
        }
    }
}
