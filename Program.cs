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
        static void Main()
        {
            #region Params
            string ftp1Url = ConfigurationManager.AppSettings.Get("ftp1Url");
            string ftp1Username = ConfigurationManager.AppSettings.Get("ftp1Username");
            string ftp1Password = ConfigurationManager.AppSettings.Get("ftp1Password");
            string ftp1ReadTimeOut = ConfigurationManager.AppSettings.Get("ftp1ReadTimeOut");
            int ftp1TimeOut = 10;
            int.TryParse(ftp1ReadTimeOut, out ftp1TimeOut);
            string localDirectory1 = ConfigurationManager.AppSettings.Get("localDirectory1");
            Directory.CreateDirectory(localDirectory1);

            string ftp2Url = ConfigurationManager.AppSettings.Get("ftp2Url");
            string ftp2Username = ConfigurationManager.AppSettings.Get("ftp2Username");
            string ftp2Password = ConfigurationManager.AppSettings.Get("ftp2Password");
            string ftp2ReadTimeOut = ConfigurationManager.AppSettings.Get("ftp2ReadTimeOut");
            int ftp2TimeOut = 10;
            int.TryParse(ftp1ReadTimeOut, out ftp2TimeOut);
            string localDirectory2 = ConfigurationManager.AppSettings.Get("localDirectory2");
            Directory.CreateDirectory(localDirectory2);

            string ftp3Url = ConfigurationManager.AppSettings.Get("ftp3Url");
            string ftp3Username = ConfigurationManager.AppSettings.Get("ftp3Username");
            string ftp3Password = ConfigurationManager.AppSettings.Get("ftp3Password");
            string ftp3ReadTimeOut = ConfigurationManager.AppSettings.Get("ftp3ReadTimeOut");
            int ftp3TimeOut = 10;
            int.TryParse(ftp1ReadTimeOut, out ftp3TimeOut);
            string localDirectory3 = ConfigurationManager.AppSettings.Get("localDirectory3");
            Directory.CreateDirectory(localDirectory3);
            #endregion

            var ftpList = new[]
            {
                new { Url = ftp1Url, User = ftp1Username, Pass = ftp1Password, TimeOut = ftp1TimeOut, LocalDirectory = localDirectory1 },
                new { Url = ftp2Url, User = ftp2Username, Pass = ftp2Password, TimeOut = ftp2TimeOut, LocalDirectory = localDirectory2 },
                new { Url = ftp3Url, User = ftp3Username, Pass = ftp3Password, TimeOut = ftp3TimeOut, LocalDirectory = localDirectory3 },
            };

            List<Task> tasks = new List<Task>();

            foreach (var ftp in ftpList)
            {
                if (!string.IsNullOrEmpty(ftp.Url) && !string.IsNullOrEmpty(ftp.User) && !string.IsNullOrEmpty(ftp.Pass) &&
                    !string.IsNullOrEmpty(ftp.LocalDirectory))
                    tasks.Add(Task.Run(() => MonitorFtp(ftp.Url, ftp.User, ftp.Pass, ftp.TimeOut, ftp.LocalDirectory)));
            }

            Console.WriteLine("Монiторинг FTP-серверiв запущено...");
            Task.WaitAll(tasks.ToArray());
        }

        static void MonitorFtp(string ftpUrl, string username, string password, int timeout, string localDirectory)
        {
            HashSet<string> knownFiles = new HashSet<string>();

            Console.WriteLine($"   Старт монiторингу: {ftpUrl}");

            while (true)
            {
                try
                {
                    var newFiles = GetNewFiles(ftpUrl, username, password, knownFiles);
                    foreach (var file in newFiles)
                    {
                        Console.WriteLine($"[{ftpUrl}] Новий файл: {localDirectory}\\{file}");

                        DownloadFtpFile(ftpUrl + file, localDirectory + "\\" + file, username, password);

                        long remoteFileSize = GetFileSize(ftpUrl + file, username, password);
                        long localFileSize = new FileInfo(localDirectory + "\\" + file).Length;

                        if (localFileSize == remoteFileSize)
                        {
                            Console.WriteLine($"✅ Файл {file} завантажено успішно ({localFileSize} байт).");

                            // === 4. Видаляємо з FTP ===
                            deleteFtpFile(ftpUrl, username, password, file);
                        }
                        else
                        {
                            Console.WriteLine($"⚠️  Помилка: файл {file} завантажено некоректно (розмір не співпадає).");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{ftpUrl}] Помилка: {ex.Message}");
                }

                Thread.Sleep(timeout);
            }
        }

        private static void deleteFtpFile(string ftpUrl, string username, string password, string file)
        {
            FtpWebRequest deleteRequest = (FtpWebRequest)WebRequest.Create(ftpUrl);
            deleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            deleteRequest.Credentials = new NetworkCredential(username, password);
            using (FtpWebResponse deleteResponse = (FtpWebResponse)deleteRequest.GetResponse())
            {
                Console.WriteLine($"🗑️  Видалено з FTP: {file}");
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
                    if (line.Contains(".") && !knownFiles.Contains(line))
                    {
                        knownFiles.Add(line);
                        newFiles.Add(line);
                    }
                }
            }

            return newFiles;
        }

        static void DownloadFtpFile(string ftpUrl, string localPath, string username, string password)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DownloadFile;

                // Set credentials if required
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    request.Credentials = new NetworkCredential(username, password);
                }
                else
                {
                    // For anonymous FTP, use "anonymous" as username and email as password
                    request.Credentials = new NetworkCredential("anonymous", "user@example.com");
                }

                // Get the FTP response
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    // Get the response stream
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        // Create a local file stream to save the downloaded content
                        using (FileStream fileStream = File.Create(localPath))
                        {
                            responseStream.CopyTo(fileStream); // Copy the content from FTP to local file
                        }
                    }
                    Console.WriteLine($"Download Complete. Status: {response.StatusDescription}");
                }


            }
            catch (WebException ex)
            {
                Console.WriteLine($"FTP Error: {ex.Message}");
                if (ex.Response != null)
                {
                    using (FtpWebResponse errorResponse = (FtpWebResponse)ex.Response)
                    {
                        Console.WriteLine($"FTP Status Description: {errorResponse.StatusDescription}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
            }
        }

        static long GetFileSize(string ftpUrl, string user, string pass)
        {
            FtpWebRequest sizeRequest = (FtpWebRequest)WebRequest.Create(ftpUrl);
            sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;
            sizeRequest.Credentials = new NetworkCredential(user, pass);

            using (FtpWebResponse response = (FtpWebResponse)sizeRequest.GetResponse())
            {
                return response.ContentLength;
            }
        }
    }
}
