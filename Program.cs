using System;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using PageSniffer.Models;
using PushoverClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;
using System.Net;

namespace PageSniffer
{
    class Program
    {
        internal static string DATETIME_FORMAT = "M/d/yy h:mm:ss tt";
        internal static string NOTIFICATION_TITLE = "[PageSniffer]";
        internal static string ITEM_AVAILABLE = "✅ Item available!";
        internal static string ITEM_NOT_AVAILABLE = "❌ Not available";
        internal static PushoverOptions pushoverOptions;
        internal static HttpStatusCode lastStatusCode;

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
            web.PostResponse = (request, response) =>
            {
                if (response != null)
                {
                    lastStatusCode = response.StatusCode;
                }
            };

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
                        int variation = 0;
                        if (psConfiguration.PeriodVariationPercentage <= 100 && psConfiguration.PeriodVariationPercentage >= 1)
                        {
                            var range = psConfiguration.PeriodInSeconds * psConfiguration.PeriodVariationPercentage / 100;
                            Random random = new Random();
                            variation = random.Next(-range, range);
                        }

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
            try
            {
                Console.WriteLine();
                WriteToConsole($"Checking ... {page.Name}");
                lastStatusCode = HttpStatusCode.OK;
                //var htmlDoc = web.Load("https://httpstat.us/400");
                var htmlDoc = web.Load(page.Url);

                // Log if anything other than HTTP OK
                if (lastStatusCode != HttpStatusCode.OK)
                {
                    WriteToConsole($"HTTP Status Code {(int)lastStatusCode} / {ToSentenceCase(lastStatusCode.ToString())}");
                }

                // Show page title
                //var title = htmlDoc.DocumentNode.SelectSingleNode("//head/title");
                //WriteToConsole($"{title.InnerHtml}");

                // Find cart button and output
                var htmlNodes = htmlDoc.DocumentNode.SelectNodes(page.NodePath);
                
                if (htmlNodes == null)
                {
                    WriteToConsole($"Node not found on page: {page.NodePath}");
                }
                else
                {
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
                                    WriteToConsole($"{ITEM_AVAILABLE}");
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
                                    WriteToConsole($"{ITEM_NOT_AVAILABLE}");
                                    SendNoti(page, ITEM_NOT_AVAILABLE);
                                }
                                page.AlertActive = false;
                            }

                            // Log for new result and add to known
                            if (!page.KnownResults.Contains(node.InnerText))
                            {
                                page.KnownResults.Add(node.InnerText);
                                WriteToConsole($"New Result: {node.InnerText}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToConsole($"{ex}");
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

        public static string ToSentenceCase(string value)
        {
            return Regex.Replace(value, "[a-z][A-Z]", m => $"{m.Value[0]} {char.ToLower(m.Value[1])}");
        }

        private static string RemoveHTMLTags(string value)
        {
            Regex regex = new Regex("\\<[^\\>]*\\>");
            value = regex.Replace(value, String.Empty);
            return value;
        }
    }
}
