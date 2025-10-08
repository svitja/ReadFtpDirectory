using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ReadFtpDirectory
{
    class Program
    {
        static HashSet<string> knownFiles = new HashSet<string>();
/*
        static void Main(string[] args)
        {
            string ftp1Url = ConfigurationManager.AppSettings.Get("ftp1Url");
            string ftp1Username = ConfigurationManager.AppSettings.Get("ftp1Username");
            string ftp1Password = ConfigurationManager.AppSettings.Get("ftp1Password");
            string ftp1ReadTimeOut = ConfigurationManager.AppSettings.Get("ftp1ReadTimeOut");


            if (!string.IsNullOrEmpty(ftp1Url) && !string.IsNullOrEmpty(ftp1Username) && !string.IsNullOrEmpty(ftp1Password))

            var ftpList = new[] {
                new { Url = "ftp://server1.com/folder/", User = "user1", Pass = "pass1" },
            };

            List<Task> tasks = new List<Task>();

            foreach (var ftp in ftpList)
            {
                tasks.Add(Task.Run(() => MonitorFtp(ftp.Url, ftp.User, ftp.Pass)));
            }

            Console.WriteLine("Моніторинг FTP-серверів запущено...");
            Task.WaitAll(tasks.ToArray());


            int ftp1TimeOut = 0;
            int.TryParse(ftp1ReadTimeOut, out ftp1TimeOut);

            while (true)
            {
                try
                {
                    var newFiles = GetNewFiles(ftp1Url, ftp1Username, ftp1Password);
                    foreach (var file in newFiles)
                    {
                        Console.WriteLine($"Новий файл: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка: {ex.Message}");
                }
                
                Thread.Sleep(ftp1TimeOut * 1000);
            }
        }
        */
        static void Main()
        {
            string ftp1Url = ConfigurationManager.AppSettings.Get("ftp1Url");
            string ftp1Username = ConfigurationManager.AppSettings.Get("ftp1Username");
            string ftp1Password = ConfigurationManager.AppSettings.Get("ftp1Password");
            string ftp1ReadTimeOut = ConfigurationManager.AppSettings.Get("ftp1ReadTimeOut");
            int ftp1TimeOut = 10;
            int.TryParse(ftp1ReadTimeOut, out ftp1TimeOut);

            string ftp2Url = ConfigurationManager.AppSettings.Get("ftp2Url");
            string ftp2Username = ConfigurationManager.AppSettings.Get("ftp2Username");
            string ftp2Password = ConfigurationManager.AppSettings.Get("ftp2Password");
            string ftp2ReadTimeOut = ConfigurationManager.AppSettings.Get("ftp2ReadTimeOut");
            int ftp2TimeOut = 10;
            int.TryParse(ftp1ReadTimeOut, out ftp2TimeOut);

            string ftp3Url = ConfigurationManager.AppSettings.Get("ftp3Url");
            string ftp3Username = ConfigurationManager.AppSettings.Get("ftp3Username");
            string ftp3Password = ConfigurationManager.AppSettings.Get("ftp3Password");
            string ftp3ReadTimeOut = ConfigurationManager.AppSettings.Get("ftp3ReadTimeOut");
            int ftp3TimeOut = 10;
            int.TryParse(ftp1ReadTimeOut, out ftp3TimeOut);

            var ftpList = new[]
            {
                new { Url = ftp1Url, User = ftp1Username, Pass = ftp1Password, TimeOut = ftp1TimeOut },
                new { Url = ftp2Url, User = ftp2Username, Pass = ftp2Password, TimeOut = ftp2TimeOut },
                new { Url = ftp3Url, User = ftp3Username, Pass = ftp3Password, TimeOut = ftp3TimeOut },
            };

            List<Task> tasks = new List<Task>();

            foreach (var ftp in ftpList)
            {
                if (!string.IsNullOrEmpty(ftp.Url) && !string.IsNullOrEmpty(ftp.User) && !string.IsNullOrEmpty(ftp.Pass))
                    tasks.Add(Task.Run(() => MonitorFtp(ftp.Url, ftp.User, ftp.Pass, ftp.TimeOut)));
            }

            Console.WriteLine("Моніторинг FTP-серверів запущено...");
            Task.WaitAll(tasks.ToArray());
        }
        static void MonitorFtp(string ftpUrl, string username, string password, int timeout)
        {
            HashSet<string> knownFiles = new HashSet<string>();

            Console.WriteLine($"▶ Старт моніторингу: {ftpUrl}");

            while (true)
            {
                try
                {
                    var newFiles = GetNewFiles(ftpUrl, username, password, knownFiles);
                    foreach (var file in newFiles)
                    {
                        Console.WriteLine($"[{ftpUrl}] Новий файл: {file}");
                        // Тут можна викликати метод для завантаження або обробки файлу
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{ftpUrl}] Помилка: {ex.Message}");
                }

                Thread.Sleep(timeout);
            }
        }

        static List<string> GetNewFiles(string ftpUrl, string username, string password, HashSet<string> knownFiles)
        {
            List<string> newFiles = new List<string>();

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(username, password);

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!knownFiles.Contains(line))
                    {
                        knownFiles.Add(line);
                        newFiles.Add(line);
                    }
                }
            }

            return newFiles;
        }
    }
}
