using Mohio.Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
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

        public async Task Start(ProcessStartInfo process, AppInformation appInfor)
        {
            try
            {
                appInfor.CheckAndFix();

                await DownloadApp(appInfor);

                RunApp(process, appInfor);

                await WaitUpdateTofinish();
            }
            catch (Exception ex)
            {
                Logger.Instance.TrackError(ex);
            }
            finally
            {
                Logger.Instance.WriteLog();
            }
        }

        private async Task DownloadApp(AppInformation appInformation)
        {
            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)192 |
                                                        (SecurityProtocolType)768 | (SecurityProtocolType)3072;
            }
            catch (NotSupportedException) { }

            if (Directory.Exists(appInformation.AppFolderPath) == false)
            {
                Directory.CreateDirectory(appInformation.AppFolderPath);

                // Wait untill download.
                await DownloadAppTask(appInformation, new Version());
                return;
            }

            var maxVesion = GetAppInstalledMaxVersion(appInformation);
            if (maxVesion is null)
            {
                // Wait untill download.
                await DownloadAppTask(appInformation, maxVesion);
            }
            else
            {
                // Don't wait, run exising app, Download new version is exist for next time.
                DownloadApp(appInformation, maxVesion);
            }
        }

        private async void DownloadApp(AppInformation appInformation, Version installedMaxVersion)
        {
            await DownloadAppTask(appInformation, installedMaxVersion);
        }

        private async Task DownloadAppTask(AppInformation appInformation, Version installedMaxVersion)
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

                var appVersionFolderPath = Path.Combine(appInformation.AppFolderPath, $"{appInformation.AppVersionFolderNamePrefix}{updateInfo.NewestVersionVersion}");

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

            return zipSetupFilePath;
        }

        private Version GetAppInstalledMaxVersion(AppInformation appInformation)
        {
            var appVersionFolderPathList = Directory.GetDirectories(appInformation.AppFolderPath, $"{appInformation.AppVersionFolderNamePrefix}*", SearchOption.TopDirectoryOnly).ToList();
            if (appVersionFolderPathList.Any() == false)
            {
                return null;
            }
            var maxVesion = appVersionFolderPathList.Select(p => new Version(p.Split(appInformation.AppVersionFolderNamePrefix).LastOrDefault())).Max();
            return maxVesion;
        }

        private string GetAppMaxVersionExePath(AppInformation appInformation)
        {
            var maxVesion = GetAppInstalledMaxVersion(appInformation);
            if (maxVesion is null)
            {
                throw new FileNotFoundException($"{maxVesion} not found");
            }
            var appVersionExePath = Path.Combine(appInformation.AppFolderPath, $"{appInformation.AppVersionFolderNamePrefix}{maxVesion}", appInformation.AppExecutableName);
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

        private void RunApp(ProcessStartInfo process, AppInformation appInfor)
        {
            var appVersionExePath = GetAppMaxVersionExePath(appInfor);
            process.FileName = appVersionExePath;
            Process.Start(process);
        }

        private async Task WaitUpdateTofinish()
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