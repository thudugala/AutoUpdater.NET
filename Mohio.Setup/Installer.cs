using Mohio.Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mohio.Setup
{
    public class Installer
    {
        private static readonly Lazy<Installer> MySingleton =
            new Lazy<Installer>(() => new Installer(),
                System.Threading.LazyThreadSafetyMode.PublicationOnly);

        private bool IsUpdateInProgress = true;

        public static Installer Instance => MySingleton.Value;

        public WebClient DownloadWebClient { get; set; }

        public WebRequest UpdateInfoWebRequest { get; set; }
               
        public async Task DownloadApp(AppInformation appInfo)
        {
            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)192 |
                                                        (SecurityProtocolType)768 | (SecurityProtocolType)3072;
            }
            catch (NotSupportedException) { }

            if (Directory.Exists(appInfo.AppFolderPath) == false)
            {
                Directory.CreateDirectory(appInfo.AppFolderPath);

                // Wait untill download.
                await DownloadAppTask(appInfo, new Version());
                return;
            }

            var maxVesion = GetAppInstalledMaxVersion(appInfo);
            if (maxVesion is null)
            {
                // Wait untill download.
                await DownloadAppTask(appInfo, maxVesion);
            }
            else
            {
                // Don't wait, run exising app, Download new version is exist for next time.
                DownloadApp(appInfo, maxVesion);
            }
        }

        private string CalculateMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                }
            }
        }

        private async void DownloadApp(AppInformation appInfo, Version installedMaxVersion)
        {
            await DownloadAppTask(appInfo, installedMaxVersion);
        }

        private async Task DownloadAppTask(AppInformation appInfo, Version installedMaxVersion)
        {
            try
            {
                IsUpdateInProgress = true;

                var updateInfo = GetUpdateInfo();
                if (updateInfo is null)
                {
                    throw new InvalidDataException($"Update Not Available");
                }
                updateInfo.Check();
                if (updateInfo.IsUpdateAvailable(installedMaxVersion) == false)
                {
                    throw new InvalidDataException($"Update Not Available");
                }
                var zipSetupFilePath = await DownloadZip(updateInfo);

                var appVersionFolderPath = Path.Combine(appInfo.AppFolderPath, $"{appInfo.AppVersionFolderNamePrefix}{updateInfo.NewestVersionVersion}");

                ZipFile.ExtractToDirectory(zipSetupFilePath, appVersionFolderPath, true);
            }
            catch (Exception ex)
            {
                Logger.Instance.TrackError(ex);
            }
            finally
            {
                IsUpdateInProgress = false;
            }
        }

        private async Task<string> DownloadZip(UpdateInformation updateInfo)
        {
            if (DownloadWebClient is null)
            {
                throw new InvalidDataException($"{nameof(DownloadWebClient)} is not set");
            }
            DownloadWebClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

            var url = new Uri(updateInfo.DownloadURL);

            var fileName = Path.GetFileName(url.LocalPath);

            var zipSetupFilePath = Path.Combine(Path.GetTempPath(), fileName);
            if (File.Exists(zipSetupFilePath))
            {
                File.Delete(zipSetupFilePath);
            }
            await DownloadWebClient.DownloadFileTaskAsync(updateInfo.DownloadURL, zipSetupFilePath);
            if (File.Exists(zipSetupFilePath) == false)
            {
                throw new FileNotFoundException($"[{zipSetupFilePath}] does not exist");
            }

            var extension = Path.GetExtension(zipSetupFilePath);
            if (extension.ToUpperInvariant() != ".ZIP")
            {
                throw new FileNotFoundException($"Wrong File type [{extension}], it need to be zip file.");
            }

            var checkSum = CalculateMD5(zipSetupFilePath);
            if(updateInfo.Checksum != checkSum)
            {
                throw new FileNotFoundException($"Zip file integrity check failed, [{updateInfo.Checksum}/{checkSum}]");
            }

            return zipSetupFilePath;
        }

        private Version GetAppInstalledMaxVersion(AppInformation appInfo)
        {
            var appVersionFolderPathList = Directory.GetDirectories(appInfo.AppFolderPath, $"{appInfo.AppVersionFolderNamePrefix}*", SearchOption.TopDirectoryOnly).ToList();
            if (appVersionFolderPathList.Any() == false)
            {
                return null;
            }
            var maxVesion = appVersionFolderPathList.Select(p => new Version(p.Split(appInfo.AppVersionFolderNamePrefix).LastOrDefault())).Max();
            return maxVesion;
        }

        private string GetAppMaxVersionExePath(AppInformation appInfo)
        {
            var maxVesion = GetAppInstalledMaxVersion(appInfo);
            if (maxVesion is null)
            {
                throw new FileNotFoundException($"{maxVesion} not found");
            }
            var appVersionExePath = Path.Combine(appInfo.AppFolderPath, $"{appInfo.AppVersionFolderNamePrefix}{maxVesion}", appInfo.AppExecutableName);
            if (File.Exists(appVersionExePath) == false)
            {
                throw new FileNotFoundException($"{appVersionExePath} not found");
            }
            return appVersionExePath;
        }

        private UpdateInformation GetUpdateInfo()
        {
            if (UpdateInfoWebRequest is null)
            {
                throw new InvalidDataException($"{nameof(UpdateInfoWebRequest)} is not set");
            }
            UpdateInfoWebRequest.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

            var webResponse = UpdateInfoWebRequest.GetResponse();

            UpdateInformation updateInfo = null;
            using (var appCastStream = webResponse.GetResponseStream())
            {
                if (appCastStream != null)
                {
                    using (var streamReader = new StreamReader(appCastStream))
                    {
                        var data = streamReader.ReadToEnd();

                        updateInfo = JsonSerializer.Deserialize<UpdateInformation>(data);
                    }
                }
                webResponse.Close();
            }
            return updateInfo;
        }

        public void RunApp(ProcessStartInfo process, AppInformation appInfo)
        {
            var appVersionExePath = GetAppMaxVersionExePath(appInfo);
            process.FileName = appVersionExePath;
            Process.Start(process);
        }

        public async Task WaitNewAppVerionToFinishDownload()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (IsUpdateInProgress)
            {
                if (stopWatch.Elapsed > TimeSpan.FromMinutes(10))
                {                    
                    break;
                }
                await Task.Delay(2000);
            }
            stopWatch.Stop();
        }
    }
}