using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Emoji;
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
                .UseEmojiAndSmiley(enableSmileys: false)
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
            var htmlElementRecords = ctx
                .HtmlElements
                .OrderByDescending(element => element.Value.Sum(r => r.Stars))
                .SelectMany(element => element
                    .Value
                    .Select(repository => new HtmlElementRecord(
                        element.Key,
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
            var specialLinkRecords = ctx
                .SpecialLinks
                .Select(l => new SpecialLinkRecord(
                    l.Repository.GitHubProjectMd,
                    l.Repository.NuGetPreviewMd,
                    l.Repository.Stars,
                    l.Link.Scheme,
                    l.Link.Scheme == "mailto"
                        ? "mailto:REDACTED"
                        : l.Link.AbsoluteUri));
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
                .Select(r=> new GenericRecord(r.GitHubProjectMd, r.NuGetPreviewMd, r.Stars));

            var disallowedHostsPath = Path.Combine(reportsPath, "disallowedImageHosts.csv");
            var strikethroughPath = Path.Combine(reportsPath, "strikethroughs.csv");
            var specialLinksPath = Path.Combine(reportsPath, "specialLinks.csv");
            var htmlElementPath = Path.Combine(reportsPath, "htmlElements.csv");
            var codeFencesPath = Path.Combine(reportsPath, "codeFences.csv");
            var tablesPath = Path.Combine(reportsPath, "tables.csv");
            var htmlsPath = Path.Combine(reportsPath, "htmls.csv");

            File.WriteAllText(disallowedHostsPath, CsvSerializer.SerializeToString(disallowedHostRecords));
            File.WriteAllText(strikethroughPath, CsvSerializer.SerializeToString(strikethroughRecords));
            File.WriteAllText(specialLinksPath, CsvSerializer.SerializeToString(specialLinkRecords));
            File.WriteAllText(htmlElementPath, CsvSerializer.SerializeToString(htmlElementRecords));
            File.WriteAllText(codeFencesPath, CsvSerializer.SerializeToString(codeFenceRecords));
            File.WriteAllText(tablesPath, CsvSerializer.SerializeToString(tableRecords));
            File.WriteAllText(htmlsPath, CsvSerializer.SerializeToString(htmlRecords));
        }

        public static void Analyze(AnalysisContext ctx, Repo repository, MarkdownDocument document)
        {
            foreach (var node in document.Descendants())
            {
                if (node is LinkInline link && Uri.TryCreate(link.Url, UriKind.Absolute, out var linkUri))
                {
                    if (link.IsImage)
                    {
                        if (!ImageHostsAllowList.Contains(linkUri.Host))
                        {
                            ctx.TrackDisallowedImageHost(repository, linkUri.Host);
                        }
                    }
                    else if (linkUri.Scheme != "http" && linkUri.Scheme != "https")
                    {
                        ctx.SpecialLinks.Add((linkUri, repository));
                    }
                }

                if (node is Table) ctx.Tables.Add(repository);
                if (node is HtmlBlock html) ctx.TrackHtmlBlock(repository, html);

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
    record SpecialLinkRecord(string GitHubProject, string NuGetPreview, int Stars, string Scheme, string Link);
    record HtmlElementRecord(string HtmlElement, string GitHubProject, string NuGetPreview, int Stars);
    record CodeFenceRecord(string GitHubProject, string NuGetPreview, int Stars, string CodeFenceKind);
    record GenericRecord(string GitHubProject, string NuGetPreview, int Stars);

    record AnalysisContext(
        Dictionary<string, HashSet<Repo>> DisallowedHosts,
        Dictionary<string, HashSet<Repo>> HtmlElements,
        HashSet<(string Kind, Repo Repository)> CodeFences,
        HashSet<(Uri Link, Repo Repository)> SpecialLinks,
        HashSet<Repo> Htmls,
        HashSet<Repo> Tables,
        HashSet<Repo> Strikethroughs)
    {
        // TODO: Better way??
        public static AnalysisContext Create() => new(
            new(StringComparer.OrdinalIgnoreCase),
            new(StringComparer.OrdinalIgnoreCase),
            new(),
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

        public void TrackHtmlBlock(Repo repository, HtmlBlock block)
        {
            Htmls.Add(repository);

            // Thankfully regex is perfect to parse HTML
            // See: https://stackoverflow.com/a/1732454
            var html = block.Lines.ToString();
            var match = Regex.Match(html, @"<(([a-zA-Z]+[1-9]?)|!--)");

            while (match.Success)
            {
                var element = match.Groups[1].Value;

                if (!HtmlElements.TryGetValue(element, out var repositories))
                {
                    repositories = new HashSet<Repo>();
                    HtmlElements.Add(element, repositories);
                }

                repositories.Add(repository);

                match = match.NextMatch();
            }
        }
    }
}
