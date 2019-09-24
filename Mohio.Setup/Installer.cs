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

        public event EventHandler<UpdateInProgressEventArgs> UpdateInProgress;

        public static Installer Instance => MySingleton.Value;

        public WebClient DownloadWebClient { get; set; }

        public WebRequest UpdateInfoWebRequest { get; set; }

        public async void Start(ProcessStartInfo process, AppInformation appInfor)
        {
            try
            {
                appInfor.CheckAndFix();

                DownloadApp(appInfor);
                var appVersionExePath = GetAppMaxVersionExePath(appInfor);

                process.FileName = appVersionExePath;
                Process.Start(process);
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

        private void DownloadApp(AppInformation appInformation)
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
                DownloadAppTask(appInformation, new Version(), true);
                return;
            }

            var maxVesion = GetAppInstalledMaxVersion(appInformation);
            if (maxVesion.ToString() == "0.0.0.0")
            {
                // Wait untill download.
                DownloadAppTask(appInformation, maxVesion, true);
            }
            else
            {
                // Don't wait, run exising app, Download new version is exist for next time.
                DownloadAppTask(appInformation, maxVesion, false);
            }
        }
        
        private void DownloadAppTask(AppInformation appInformation, Version installedMaxVersion, bool IsWaitingToFinish)
        {
            try
            {
                UpdateInProgress?.Invoke(null, new UpdateInProgressEventArgs
                {
                    InProgress = true,
                    IsWaitingToFinish = IsWaitingToFinish
                });

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
                var zipSetupFilePath = DownloadZip(updateInfo);

                var appVersionFolderPath = Path.Combine(appInformation.AppFolderPath, $"{appInformation.AppVersionFolderNamePrefix}{updateInfo.NewestVersionVersion}");

                ZipFile.ExtractToDirectory(zipSetupFilePath, appVersionFolderPath, true);
            }
            catch (Exception ex)
            {
                Logger.Instance.TrackError(ex);
            }
            finally
            {
                UpdateInProgress?.Invoke(null, new UpdateInProgressEventArgs
                {
                    InProgress = false,
                    IsWaitingToFinish = IsWaitingToFinish
                });
                Logger.Instance.WriteLog();
            }
        }

        private string DownloadZip(UpdateInformation updateInfo)
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
            DownloadWebClient.DownloadFile(updateInfo.DownloadURL, zipSetupFilePath);
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
                return new Version();
            }
            var maxVesion = appVersionFolderPathList.Select(p => new Version(p.Split(appInformation.AppVersionFolderNamePrefix).LastOrDefault())).Max();
            return maxVesion;
        }

        private string GetAppMaxVersionExePath(AppInformation appInformation)
        {
            var maxVesion = GetAppInstalledMaxVersion(appInformation);
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
    }
}