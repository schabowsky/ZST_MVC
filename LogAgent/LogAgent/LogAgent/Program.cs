using System;
using System.Net;
using System.Threading;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace LogAgent
{
    class Program
    {
        private static HttpListener server = new HttpListener();
        private static List<String> listedData = new List<string>();
        private static bool threadIsRunning = true;
        private static bool responseCompleted = false;
        private static string connectionString = "Server=.\\SQLEXPRESS;Database=logs;Trusted_Connection=true";

        public static void ListenerCallback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            PrepareRequestData(request);
            var response = context.Response;
            System.IO.Stream output = response.OutputStream;
            output.Close();
            responseCompleted = true;
        }

        public static void PrepareRequestData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                Console.WriteLine("Nie wysłano żadnych danych.");
                return;
            }

            System.IO.Stream body = request.InputStream;
            System.Text.Encoding encoding = request.ContentEncoding;
            System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);

            if (request.ContentType != null)
            {
                Console.WriteLine($"Rodzaj danych (content type): {request.ContentType}");
            }

            Console.WriteLine($"Długość odebranych danych: {request.ContentLength64}");
            Console.WriteLine("Początek odebranych danych:");
            string receivedData = reader.ReadToEnd();
            Console.WriteLine(receivedData);
            Console.WriteLine("Koniec odebranych danych.");
            body.Close();
            reader.Close();
            string[] separator = { "\r\n" };
            string[] splittedData = receivedData.Split(separator, StringSplitOptions.None);

            for (int iterator = 0; iterator < splittedData.Length; iterator++)
                listedData.Add(splittedData[iterator]);

            listedData.Add(request.UserAgent);
        }

        public static void WaitForRequest()
        {
            while (threadIsRunning)
            {
                Console.Clear();
                IAsyncResult result = server.BeginGetContext(new AsyncCallback(ListenerCallback), server);
                Console.WriteLine("Oczekiwanie na asynchroniczne wykonanie żądania.");
                result.AsyncWaitHandle.WaitOne(3000);
                Thread.Sleep(100);

                if (responseCompleted == true)
                {
					Console.WriteLine("Wykonano żądanie w sposób asynchroniczny.");
                    AddToDB();
                    listedData.Clear();
                    responseCompleted = false;
                }
            }
        }

        public static void AddToDB()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string[] tables = GetAllTables(conn);
                bool tableExists = false;

                foreach (string table in tables)
                {
                    if (table == listedData[1])
                    {
                        tableExists = true;
                    }
                }

                if (tableExists == false)
                {
                    try
                    {
                        using (SqlCommand createCommand = new SqlCommand($"CREATE TABLE {listedData[1]} (Date datetime, Source_ID varchar(255), Text_Column_1 char(255));", conn))
                            createCommand.ExecuteNonQuery();
                        for (int iterator = 2; iterator < listedData.Count - 2; iterator++)
                        {
                            using (SqlCommand addColumn = new SqlCommand($"ALTER TABLE {listedData[1]} ADD Text_Column_{iterator} varchar(255)", conn))
                                addColumn.ExecuteNonQuery();
                        }
                    }
                    catch (SqlException er)
                    {
                        Console.WriteLine(er.Message);
                    }

                    try
                    {
                        string insertString = GenerateInsertString();
                        using (SqlCommand insertCommand = new SqlCommand(insertString, conn))
                        {
                            insertCommand.Parameters.Add(new SqlParameter("0", DateTime.Parse(listedData[0])));
                            insertCommand.Parameters.Add(new SqlParameter("1", listedData[listedData.Count - 1]));
                            insertCommand.Parameters.Add(new SqlParameter("2", listedData[2]));

                            for (int iterator = 3; iterator < listedData.Count - 1; iterator++)
                                insertCommand.Parameters.Add(new SqlParameter($"{iterator}", listedData[iterator]));

                            insertCommand.ExecuteNonQuery();
                        }
                    }
                    catch (SqlException err)
                    {
                        Console.WriteLine(err.Message);
                    }
                }
                else
                {
                    try
                    {
                        string insertString = GenerateInsertString();
                        using (SqlCommand insertCommand = new SqlCommand(insertString, conn))
                        {
                            insertCommand.Parameters.Add(new SqlParameter("0", DateTime.Parse(listedData[0])));
                            insertCommand.Parameters.Add(new SqlParameter("1", listedData[listedData.Count - 1]));
                            insertCommand.Parameters.Add(new SqlParameter("2", listedData[2]));

                            for (int iterator = 3; iterator < listedData.Count - 1; iterator++)
                                insertCommand.Parameters.Add(new SqlParameter($"{iterator}", listedData[iterator]));

                            insertCommand.ExecuteNonQuery();
                        }
                    }
                    catch (SqlException err)
                    {
                        Console.WriteLine(err.Message);
                    }
                }
            }
        }
        public static string[] GetAllTables(SqlConnection conn)
        {
            List<string> tableNames = new List<string>();
            using (SqlCommand command = new SqlCommand("SELECT name FROM sys.Tables", conn))
            {
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                    tableNames.Add(reader["name"].ToString());

                reader.Close();
            }
            return tableNames.ToArray();
        }

        public static string GenerateInsertString()
        {
            string insertString = $"INSERT INTO {listedData[1]} (:columns:) VALUES (:values:)";
            string[] columns = new string[listedData.Count - 1];
            string[] values = new string[listedData.Count - 1];
            columns[0] = "Date";
            columns[1] = "Source_ID";
            columns[2] = "Text_Column_1";
            values[0] = "@0";
            values[1] = "@1";
            values[2] = "@2";

            for (int iterator = 3; iterator < listedData.Count - 1; iterator++)
            {
                columns[iterator] = $"Text_Column_{iterator - 1}";
                values[iterator] = $"@{iterator}";
            }

            string columnNames = string.Join(",", columns);
            string valueNames = string.Join(",", values);
            insertString = insertString.Replace(":columns:", columnNames).Replace(":values:", valueNames);

            return insertString;
        }

        static void Main(string[] args)
        {
            server.Prefixes.Add("http://localhost:55268/");
            Console.WriteLine("Nasłuchiwanie...");
            server.Start();
            Thread listenThread = new Thread(WaitForRequest);
            listenThread.Start();
            Console.Read();
            threadIsRunning = false;
            listenThread.Join();
            Console.Read();
            server.Close();
        }
    }
}
