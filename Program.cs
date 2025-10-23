using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tsg;

namespace ReadFtpDirectory
{
    class Program
    {
        static Dictionary<string, Task> ftpTasks = new Dictionary<string, Task>();

        /// <summary>
        /// Get Ftp Servers from program configuration
        /// </summary>
        /// <returns>list of oblect</returns>
        static List<FtpConfig> GetFtpServerListFromParams()
        {
            #region Params
            string ftp1Url = ConfigurationManager.AppSettings.Get("ftp1Url");
            string ftp1Username = ConfigurationManager.AppSettings.Get("ftp1Username");
            string ftp1Password = ConfigurationManager.AppSettings.Get("ftp1Password");
            int ftp1TimeOut = 10;
            int.TryParse(ConfigurationManager.AppSettings.Get("ftp1ReadTimeOut"), out ftp1TimeOut);
            string localDirectory1 = ConfigurationManager.AppSettings.Get("localDirectory1");
            Directory.CreateDirectory(localDirectory1);

            string ftp2Url = ConfigurationManager.AppSettings.Get("ftp2Url");
            string ftp2Username = ConfigurationManager.AppSettings.Get("ftp2Username");
            string ftp2Password = ConfigurationManager.AppSettings.Get("ftp2Password");
            int ftp2TimeOut = 10;
            int.TryParse(ConfigurationManager.AppSettings.Get("ftp2ReadTimeOut"), out ftp2TimeOut);
            string localDirectory2 = ConfigurationManager.AppSettings.Get("localDirectory2");
            Directory.CreateDirectory(localDirectory2);

            string ftp3Url = ConfigurationManager.AppSettings.Get("ftp3Url");
            string ftp3Username = ConfigurationManager.AppSettings.Get("ftp3Username");
            string ftp3Password = ConfigurationManager.AppSettings.Get("ftp3Password");
            int ftp3TimeOut = 10;
            int.TryParse(ConfigurationManager.AppSettings.Get("ftp3ReadTimeOut"), out ftp3TimeOut);
            string localDirectory3 = ConfigurationManager.AppSettings.Get("localDirectory3");
            Directory.CreateDirectory(localDirectory3);
            #endregion

            List<FtpConfig> result = new List<FtpConfig>();

            if (!string.IsNullOrEmpty(ftp1Url) && !string.IsNullOrEmpty(ftp1Username) && !string.IsNullOrEmpty(ftp1Password) && !string.IsNullOrEmpty(localDirectory1))
                result.Add(new FtpConfig() { Url = ftp1Url, User = ftp1Username, Pass = ftp1Password, TimeOut = ftp1TimeOut, TargetDirectory = localDirectory1 });

            if (!string.IsNullOrEmpty(ftp2Url) && !string.IsNullOrEmpty(ftp2Username) && !string.IsNullOrEmpty(ftp2Password) && !string.IsNullOrEmpty(localDirectory2))
                result.Add(new FtpConfig() { Url = ftp2Url, User = ftp2Username, Pass = ftp2Password, TimeOut = ftp2TimeOut, TargetDirectory = localDirectory2 });

            if (!string.IsNullOrEmpty(ftp3Url) && !string.IsNullOrEmpty(ftp3Username) && !string.IsNullOrEmpty(ftp3Password) && !string.IsNullOrEmpty(localDirectory3))
                result.Add(new FtpConfig() { Url = ftp3Url, User = ftp3Username, Pass = ftp3Password, TimeOut = ftp3TimeOut, TargetDirectory = localDirectory3 });

            return result;
        }

        static void Main()
        {
            var ftpList = GetFtpServerListFromParams();

            foreach (var ftp in ftpList)
            {
                StartFtpMonitor(ftp.Url, ftp.User, ftp.Pass, ftp.TimeOut, ftp.TargetDirectory);
            }

            while (true)
            {
                foreach (var ftp in ftpList)
                {
                    string key = ftp.Url;
                    if (!ftpTasks.ContainsKey(key) || ftpTasks[key].IsFaulted || ftpTasks[key].IsCompleted)
                    {
                        Console.WriteLine($"⚠️ Потік для {key} неактивний — перезапуск...");
                        ReadFtpLog.Manager.WriteLine($"⚠️ Потік для {key} неактивний — перезапуск...");
                        MonitorFtp(ftp.Url, ftp.User, ftp.Pass, ftp.TimeOut, ftp.TargetDirectory);
                    }
                }

                Thread.Sleep(5000);
            }
        }

        static void StartFtpMonitor(string ftpUrl, string user, string pass, int timeout, string directory)
        {
            ftpTasks[ftpUrl] = Task.Run(() => MonitorFtp(ftpUrl, user, pass, timeout, directory));
        }

        static void MonitorFtp(string ftpUrl, string username, string password, int timeout, string localDirectory)
        {
            HashSet<string> knownFiles = new HashSet<string>();

            Console.WriteLine($"   Старт монiторингу: {ftpUrl}");
            ReadFtpLog.Manager.WriteLine($"   Старт монiторингу: {ftpUrl}");

            while (true)
            {
                try
                {
                    Console.WriteLine($"Пошук нових файлів: {ftpUrl}");
                    ReadFtpLog.Manager.WriteLine($"Пошук нових файлів: {ftpUrl}");
                    var newFiles = GetNewFiles(ftpUrl, username, password, ref knownFiles);
                    foreach (var file in newFiles)
                    {
                        Console.WriteLine($"[{ftpUrl}] Новий файл: {localDirectory}\\{file}");
                        ReadFtpLog.Manager.WriteLine($"[{ftpUrl}] Новий файл: {localDirectory}\\{file}");

                        DownloadFtpFile(ftpUrl + file, localDirectory + "\\" + file, username, password);

                        long remoteFileSize = GetFileSize(ftpUrl + file, username, password);
                        long localFileSize = new FileInfo(localDirectory + "\\" + file).Length;

                        if (localFileSize == remoteFileSize)
                        {
                            Console.WriteLine($"✅ Файл {file} завантажено успішно ({localFileSize} байт).");
                            ReadFtpLog.Manager.WriteLine($"✅ Файл {file} завантажено успішно ({localFileSize} байт).");

                            //deleteFtpFile(ftpUrl, username, password, file);
                        }
                        else
                        {
                            Console.WriteLine($"⚠️  Помилка: файл {file} завантажено некоректно (розмір не співпадає).");
                            ReadFtpLog.Manager.WriteLine($"⚠️  Помилка: файл {file} завантажено некоректно (розмір не співпадає).");
                        }
                    }

                    Thread.Sleep(timeout * 1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{ftpUrl}] Помилка: {ex.Message}");
                    ReadFtpLog.Manager.WriteLine($"[{ftpUrl}] Помилка: {ex.Message}");
                }
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
                ReadFtpLog.Manager.WriteLine($"🗑️  Видалено з FTP: {file}");
            }
        }

        static List<string> GetNewFiles(string ftpUrl, string username, string password, ref HashSet<string> knownFiles)
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
                request.Credentials = new NetworkCredential(username, password);

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (FileStream fileStream = File.Create(localPath))
                        {
                            responseStream.CopyTo(fileStream);
                        }
                    }
                    Console.WriteLine($"Download Complete. Status: {response.StatusDescription}");
                    ReadFtpLog.Manager.WriteLine($"Download Complete. Status: {response.StatusDescription}");
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine($"FTP Error: {ex.Message}");
                ReadFtpLog.Manager.WriteLine($"FTP Error: {ex.Message}");
                if (ex.Response != null)
                {
                    using (FtpWebResponse errorResponse = (FtpWebResponse)ex.Response)
                    {
                        Console.WriteLine($"FTP Status Description: {errorResponse.StatusDescription}");
                        ReadFtpLog.Manager.WriteLine($"FTP Status Description: {errorResponse.StatusDescription}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                ReadFtpLog.Manager.WriteLine($"General Error: {ex.Message}");
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
