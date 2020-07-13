using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NiftyPE
{
    public static class NiftyPEWeeklyTimer
    {
        [FunctionName("NiftyPEWeeklyTimer")]
        public static async void Run([TimerTrigger("0 0 8 * * 2")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            //0 0 8 * * 2

            string URI = "https://craytheon.com/charts/nifty_pe_ratio_pb_value_dividend_yield_chart.php";
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetByteArrayAsync(URI);

            String source = Encoding.GetEncoding("utf-8").GetString(response, 0, response.Length - 1);

            source = WebUtility.HtmlDecode(source);
            HtmlDocument resultat = new HtmlDocument();
            resultat.LoadHtml(source);

            string pe = "";
            var peNum = 0.0;
            string[] sentences = Regex.Split(source, @"(?<=[\.!\?])\s+");
            foreach (string sentence in sentences)
            {
                if (sentence.Contains("Current Nifty PE Ratio"))
                {
                    pe = sentence.Substring(sentence.IndexOf("Current Nifty PE Ratio"));
                    int pFrom = pe.IndexOf("<b>") + "<b>".Length;
                    int pTo = pe.LastIndexOf("</b>");
                    peNum = Convert.ToDouble(pe.Substring(pFrom, pTo - pFrom));
                }
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var imgbytes = "https://craytheon.com/pics/nifty_pe_study_research.png";

            string body = createEmailBody(imgbytes, pe, peNum, log, context);
            log.LogInformation("Constructed email body..");
            Execute("Nifty50 PE Ratio", body, config, pe).Wait();
            log.LogInformation("Sent email..");
        }

        private static string createEmailBody(string imgbytes, string pe, double peNum, ILogger log, ExecutionContext context)
        {
            log.LogInformation("Inside email body..");
            var body = "";
            try
            {
                var path = $"{context.FunctionDirectory}\\..\\HtmlTemplate.html";
                log.LogInformation("Path : " + path);
                body = File.ReadAllText(path);
                log.LogInformation("Read file completed..");
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.StackTrace);
            }
            body = body.Replace("{pe}", pe); //replacing the required things  
            body = body.Replace("{imgbytes}", imgbytes);
            if (peNum > 30)
                body = body.Replace("{32}", "bgcolor");
            else if (peNum > 28)
                body = body.Replace("{30}", "bgcolor");
            else if (peNum > 26)
                body = body.Replace("{28}", "bgcolor");
            else if (peNum > 24)
                body = body.Replace("{26}", "bgcolor");
            else if (peNum > 22)
                body = body.Replace("{24}", "bgcolor");
            else if (peNum > 20)
                body = body.Replace("{22}", "bgcolor");
            else if (peNum > 18)
                body = body.Replace("{20}", "bgcolor");
            else if (peNum > 16)
                body = body.Replace("{18}", "bgcolor");
            else if (peNum > 14)
                body = body.Replace("{16}", "bgcolor");
            return body;
        }

        static async Task Execute(string subject, string body, IConfigurationRoot config, string pe)
        {
            var apiKey = config["api-key"];
            var toEmail1 = config["to-email1"];
            var toEmail2 = config["to-email2"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("niftype@example.com", "Nifty PE");
            List<EmailAddress> tolist = new List<EmailAddress>();
            tolist.Add(new EmailAddress(toEmail1));
            tolist.Add(new EmailAddress(toEmail2));
            var plainTextContent = pe;
            var htmlContent = body;
            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, tolist, subject, plainTextContent, htmlContent);
            await client.SendEmailAsync(msg);
        }
    }
}
