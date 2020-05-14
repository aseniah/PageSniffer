﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using PageSniffer.Models;
using PushoverClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;

namespace PageSniffer
{
    class Program
    {
        internal static PushoverOptions pushoverOptions;

        static void Main(string[] args)
        {
            // Load configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            var config = builder.Build();
            var psConfiguration = config.GetSection("configuration").Get<PsConfiguration>();
            pushoverOptions = config.GetSection("pushover").Get<PushoverOptions>();
            var webPages = config.GetSection("webpages").GetChildren().Select(x => x.Get<WebPage>()).ToList();

            HtmlWeb web = new HtmlWeb();

            // Process loop every 10 seconds
            while(true)
            {
                foreach (var page in webPages)
                {
                    if (page.Enabled && page.NextRun < DateTime.Now)
                    {
                        // Check the page
                        CheckPage(web, page);

                        // Calculate random variation within percentage
                        var range = psConfiguration.PeriodInSeconds * psConfiguration.PeriodVariationPercentage / 100;
                        Random random = new Random();
                        var variation = random.Next(-range, range);

                        // Set next run
                        page.NextRun = DateTime.Now.AddSeconds(psConfiguration.PeriodInSeconds).AddSeconds(variation);
                        Console.WriteLine($"Next Run: {page.NextRun}");
                    }
                }
                //Console.WriteLine("Loop ...");
                Thread.Sleep(10000);
            }
        }

        private static void CheckPage(HtmlWeb web, WebPage page)
        {
            Console.WriteLine();
            Console.WriteLine($"Loading webpage ... {page.Name}");
            var htmlDoc = web.Load(page.Url);

            // Show page title
            //var title = htmlDoc.DocumentNode.SelectSingleNode("//head/title");
            //Console.WriteLine($"{title.InnerHtml}");

            // Find cart button and output
            var htmlNodes = htmlDoc.DocumentNode.SelectNodes(page.NodePath);
            foreach (var node in htmlNodes)
            {
                if (node.OuterHtml.Contains(page.NodeFilter))
                {
                    Console.WriteLine($"Result: {RemoveHTMLTags(node.InnerHtml)}");
                    if (node.InnerHtml.ToLower().Contains(page.AlertTrigger.ToLower()))
                    {
                        if (!page.AlertActive)
                        {
                            page.AlertActive = true;

                            // Send alert
                            Pushover pclient = new Pushover(pushoverOptions.AppKey);
                            PushResponse response = pclient.Push(
                                $"[PageSniffer] {page.Name}",
                                $"Item available! 🎉\n{page.Url}",
                                pushoverOptions.UserKey
                            );

                            if (response.Errors != null && response.Errors.Any())
                            {
                                foreach (var error in response.Errors)
                                {
                                    Console.WriteLine($"Pushover Error: {error}");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (page.AlertActive)
                        {
                            // Send alert
                            Pushover pclient = new Pushover(pushoverOptions.AppKey);
                            PushResponse response = pclient.Push(
                                $"[PageSniffer] {page.Name}",
                                $"No longer available 😢\n{page.Url}",
                                pushoverOptions.UserKey
                            );

                            if (response.Errors != null && response.Errors.Any())
                            {
                                foreach (var error in response.Errors)
                                {
                                    Console.WriteLine($"Pushover Error: {error}");
                                }
                            }
                        }
                        page.AlertActive = false;
                    }
                }
            }
        }

        private static string RemoveHTMLTags(string value)
        {
            Regex regex = new Regex("\\<[^\\>]*\\>");
            value = regex.Replace(value, String.Empty);
            return value;
        }
    }
}