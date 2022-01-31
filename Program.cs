using HtmlAgilityPack;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Net;

namespace WaterChecker
{
    public static class Program
    {
        private static Timer? _timer = null;
        private static string? location = string.Empty;

        public static void Main()
        {
            Console.Write("Plese enter the name of a location you want to check: ");
            location = Console.ReadLine();

            _timer = new Timer(TimerCallback, null, 0, 3600000);
            Console.ReadLine();
        }

        private static async void TimerCallback(object? o)
        {
            await StartWaterChecker(location);
        }

        private static async Task StartWaterChecker(string location)
        {
            Console.OutputEncoding = Encoding.UTF8;
                        
            var url = "https://vikvarna.com/bg/messages.html?region_id=15&sub_region_id=&type=breakdown";
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            var failures = htmlDocument.DocumentNode.Descendants("div").Where(node => node.GetAttributeValue("class", "").Equals("list-item")).ToList();

            var text = string.Empty;

            foreach (var failure in failures)
            {
                var message = failure.InnerText;

                if (message.Contains(location))
                {
                    text = message.Replace("\t", " ");
                    
                    while (text.IndexOf("  ") >= 0)
                    {
                        text = text.Replace("  ", " ");
                    }

                    Console.WriteLine(text);

                    await WriteToFileAndSendEmail(text);
                }
            }

            if (string.IsNullOrEmpty(text))
                Console.WriteLine("Няма планирани прекъсвания или аварии.");

            Console.WriteLine($"Следващата проверка в сайта на ВИК Варна ще е в {DateTime.Now.AddHours(1).ToString("HH:mm")}");
        }

        private static async Task WriteToFileAndSendEmail(string content)
        {
            string fileContents = File.ReadAllText(@"C:\Users\User\source\repos\WaterChecker\bin\Debug\net6.0\WaterDisruptions.txt");

            if (!fileContents.Contains(content))
            {
                using var file = new StreamWriter("WaterDisruptions.txt", append: true);
                await file.WriteLineAsync(content);
                await SendEmail(content);
            }
        }

        private static async Task SendEmail(string emailMessage)
        {
            MailMessage email = new()
            {
                Subject =  $"Без вода на: {Regex.Match(emailMessage, @"(\d+)[.](\d+)[.](\d+)")} г.",
                Body = emailMessage
            };

            string? emailRecipient = ConfigurationManager.AppSettings["Recipient"];
            email.To.Add(emailRecipient);

            using var client = new SmtpClient("smtp-mail.outlook.com", 587)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(ConfigurationManager.AppSettings["Username"], ConfigurationManager.AppSettings["Password"]),
                EnableSsl = true
            };

            try
            {
                await client.SendMailAsync(email);
                Console.WriteLine("Email successfully sent");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}