using System;
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
        internal static string DATETIME_FORMAT = "M/d/yy h:mm:ss tt";
        internal static string NOTIFICATION_TITLE = "[PageSniffer]";
        internal static string ITEM_AVAILABLE = "✅ Item available!";
        internal static string ITEM_NOT_AVAILABLE = "❌ Not available";
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
                        //WriteToConsole($"Next Run: {page.NextRun.ToString(DATETIME_FORMAT)}");
                    }
                }
                //WriteToConsole("Loop ...");
                Thread.Sleep(10000);
            }
        }

        private static void CheckPage(HtmlWeb web, WebPage page)
        {
            //Console.WriteLine();
            //WriteToConsole($"Loading webpage ... {page.Name}");
            var htmlDoc = web.Load(page.Url);

            // Show page title
            //var title = htmlDoc.DocumentNode.SelectSingleNode("//head/title");
            //WriteToConsole($"{title.InnerHtml}");

            // Find cart button and output
            var htmlNodes = htmlDoc.DocumentNode.SelectNodes(page.NodePath);
            foreach (var node in htmlNodes)
            {
                if (node.OuterHtml.Contains(page.NodeFilter))
                {
                    //WriteToConsole($"Result: {node.InnerText}");
                    if (node.InnerHtml.ToLower().Contains(page.AlertTrigger.ToLower()))
                    {
                        // Item is available
                        if (!page.AlertActive)
                        {
                            // Log and notify on status change
                            WriteToConsole($"{page.Name} ... {ITEM_AVAILABLE}");
                            SendNoti(page, ITEM_AVAILABLE);
                        }
                        page.AlertActive = true;
                    }
                    else
                    {
                        // Item is NOT available
                        if (page.AlertActive)
                        {
                            // Log and notify on status change
                            WriteToConsole($"{page.Name} ... {ITEM_NOT_AVAILABLE}");
                            SendNoti(page, ITEM_NOT_AVAILABLE);
                        }
                        page.AlertActive = false;
                    }

                    // Log for new result and add to known
                    if (!page.KnownResults.Contains(node.InnerText))
                    {
                        page.KnownResults.Add(node.InnerText);
                        WriteToConsole($"New Result: {page.Name} --- {node.InnerText}");
                    }
                }
            }
        }

        private static void WriteToConsole(string text)
        {
            Console.WriteLine($"[{DateTime.Now.ToString(DATETIME_FORMAT)}] {text}");
        }

        private static void SendNoti(WebPage page, string title)
        {
            // Send alert
            Pushover pclient = new Pushover(pushoverOptions.AppKey);
            PushResponse response = pclient.Push(
                $"{NOTIFICATION_TITLE} {title}",
                $"{page.Name}\n{page.Url}",
                pushoverOptions.UserKey
            );

            if (response.Errors != null && response.Errors.Any())
            {
                foreach (var error in response.Errors)
                {
                    WriteToConsole($"Pushover Error: {error}");
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
