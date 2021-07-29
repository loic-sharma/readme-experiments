using System;
using System.Collections.Generic;
using System.IO;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

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

            readmesPath = @"D:\Code\readme-experiments\readmes\ClosedXML\ClosedXML";
            var readmes = Directory.GetFiles(readmesPath, "*.md", SearchOption.AllDirectories);

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            foreach (var readme in readmes)
            {
                var owner = "TODO";
                var repository = "TODO";

                var markdown = File.ReadAllText(readme);
                var document = Markdown.Parse(markdown, pipeline);

                var hasCodeFence = false;
                var hasCSharpCodeFence = false;
                var hasHtml = false;
                var hasTable = false;

                var disallowedImageHosts = new HashSet<string>();

                foreach (var node in document.Descendants())
                {
                    if (node is LinkInline linkInline && linkInline.IsImage)
                    {
                        var imageHost = new Uri(linkInline.Url).Host;

                        if (!ImageHostsAllowList.Contains(imageHost))
                        {
                            disallowedImageHosts.Add(imageHost);
                        }
                    }

                    if (node is HtmlBlock)
                    {
                        hasHtml = true;
                    }

                    if (node is Table)
                    {
                        hasTable = true;
                    }

                    if (node is FencedCodeBlock code)
                    {
                        hasCodeFence = true;

                        if (CSharpCodeFences.Contains(code.Info?.Trim()))
                        {
                            hasCSharpCodeFence = true;
                        }
                    }
                }
            }
        }
    }
}
