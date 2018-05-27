using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LogClient
{
    class Program
    {
        static HttpClient client = new HttpClient();
        static async Task SendEventAsync(string message)
        {
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            var content = new ByteArrayContent(data);
            HttpResponseMessage response = await client.PostAsync("", content);
            response.EnsureSuccessStatusCode();
            //Console.WriteLine(response.StatusCode);
        }
        static string DoAutomatically()
        {
            string message = null;
            Random randomizer = new Random();
            string[] types = { "AXZ", "BCW", "CBN", "DIG" };
            string[] data = { "100", "0", "80", "300", "500", "120", "270" };
            message = DateTime.Now.ToString() + "\r\n" + types[randomizer.Next(4)];

            for (int iterator = 0; iterator < 5; iterator++)
                message += "\r\n" + data[randomizer.Next(7)];

            return message;
        }

        static string DoManually(int numberOfColumns)
        {
            string message = DateTime.Now.ToString() + "\r\n";

            for (int i = 0; i <= numberOfColumns; i++)
            {
                if (i == 0)
                {
                    Console.WriteLine("Podaj nazwę typu zdarzenia: ");
                    message += Console.ReadLine() + "\r\n";
                }
                else if (i != 0 && i != numberOfColumns)
                {
                    Console.WriteLine($"Podaj zawartość {i}. kolumny tekstowej: ");
                    message += Console.ReadLine() + "\r\n";
                }
                else if (i == numberOfColumns)
                {
                    Console.WriteLine($"Podaj zawartość {i}. kolumny tekstowej: ");
                    message += Console.ReadLine();
                }
            }
            return message;
        }
        static void Main(string[] args)
        {
            RunAsync().Wait();
        }

        static async Task RunAsync()
        {
            string userAgent = null;
            client.BaseAddress = new Uri("http://localhost:55268/");
            client.DefaultRequestHeaders.Accept.Clear();

            int x = 0;

            while (x != 3)
            {
                Console.Clear();
                Console.WriteLine("1 - Generuj 100 losowych zdarzeń.");
                Console.WriteLine("2 - Wprowadź nowe zdarzenie z klawiatury.");
                Console.WriteLine("3 - Wyjście.");
                try
                {
                    x = int.Parse(Console.ReadLine());
                    switch (x)
                    {
                        case 1:
                            userAgent = null;
                            Console.WriteLine("Podaj wartość nagłówka User-Agent: ");
                            userAgent = Console.ReadLine();
                            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

                            for (int i = 0; i < 100; i++)
                            {
                                string autoMessage = DoAutomatically();
                                await SendEventAsync(autoMessage);
                                Console.WriteLine("Sent: {0}", autoMessage);
                                Thread.Sleep(100);
                            }

                            Console.WriteLine("Wciśnij ENTER, by kontynuować...");
                            Console.Read();
                            Console.Clear();
                            break;
                        case 2:
                            userAgent = null;
                            Console.WriteLine("Podaj wartość nagłówka User-Agent: ");
                            userAgent = Console.ReadLine();
                            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                            Console.WriteLine("Podaj liczbę kolumn tekstowych: ");
                            int numberOfColumns = int.Parse(Console.ReadLine());
                            string manuMessage = DoManually(numberOfColumns);
                            await SendEventAsync(manuMessage);
                            Console.WriteLine("Sent: {0}", manuMessage);
                            Console.WriteLine("Wciśnij ENTER, by kontynuować...");
                            Console.Read();
                            Console.Clear();
                            break;
                        case 3:
                            break;
                        default:
                            Console.Clear();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            Console.WriteLine("Wciśnij ENTER, by wyjść...");
            Console.ReadLine();
        }
    }
}
