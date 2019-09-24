using Mohio.Core;
using System;
using System.Diagnostics;
using System.Globalization;
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
            if (appInfo is null)
            {
                throw new ArgumentNullException(nameof(appInfo));
            }

            if (Directory.Exists(AppInformation.AppFolderPath) == false)
            {
                Directory.CreateDirectory(AppInformation.AppFolderPath);

                // Wait untill download.
                await DownloadAppTask(appInfo, new Version()).ConfigureAwait(false);
                return;
            }

            var maxVesion = GetAppInstalledMaxVersion(appInfo);
            if (maxVesion is null)
            {
                // Wait untill download.
                await DownloadAppTask(appInfo, maxVesion).ConfigureAwait(false);
            }
            else
            {
                // Don't wait, run exising app, Download new version is exist for next time.
                DownloadApp(appInfo, maxVesion);
            }
        }

        private static string CalculateCheckSum(string filePath)
        {
            using (var sha = SHA512.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = sha.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "", StringComparison.InvariantCultureIgnoreCase).ToUpperInvariant();
                }
            }
        }

        private async void DownloadApp(AppInformation appInfo, Version installedMaxVersion)
        {
            await DownloadAppTask(appInfo, installedMaxVersion).ConfigureAwait(false);
        }

        private async Task DownloadAppTask(AppInformation appInfo, Version installedMaxVersion)
        {
            try
            {
                IsUpdateInProgress = true;

                var updateInfo = GetUpdateInfo();
                if (updateInfo is null)
                {
                    throw new InvalidDataException(Properties.Resources.UpdateNotAvailable);
                }
                updateInfo.Check();
                if (updateInfo.IsUpdateAvailable(installedMaxVersion) == false)
                {
                    throw new InvalidDataException(Properties.Resources.UpdateNotAvailable);
                }
                var zipSetupFilePath = await DownloadZip(updateInfo).ConfigureAwait(false);

                var appVersionFolderPath = Path.Combine(AppInformation.AppFolderPath, $"{appInfo.AppVersionFolderNamePrefix}{updateInfo.NewestVersionVersion}");

                ZipFile.ExtractToDirectory(zipSetupFilePath, appVersionFolderPath, true);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
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
                throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.NotSet, nameof(DownloadWebClient)));
            }
            DownloadWebClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

            var url = new Uri(updateInfo.DownloadURL);

            var fileName = Path.GetFileName(url.LocalPath);

            var zipSetupFilePath = Path.Combine(Path.GetTempPath(), fileName);
            if (File.Exists(zipSetupFilePath))
            {
                File.Delete(zipSetupFilePath);
            }
            await DownloadWebClient.DownloadFileTaskAsync(updateInfo.DownloadURL, zipSetupFilePath).ConfigureAwait(false);
            if (File.Exists(zipSetupFilePath) == false)
            {
                throw new FileNotFoundException($"[{zipSetupFilePath}] does not exist");
            }

            var extension = Path.GetExtension(zipSetupFilePath);
            if (extension.ToUpperInvariant() != ".ZIP")
            {
                throw new FileNotFoundException($"Wrong File type [{extension}], it need to be zip file.");
            }

            // use https://emn178.github.io/online-tools/sha512_file_hash.html
            var checkSum = CalculateCheckSum(zipSetupFilePath);
            if(updateInfo.Checksum.ToUpperInvariant() != checkSum)
            {
                throw new FileNotFoundException($"Zip file integrity check failed, [{updateInfo.Checksum}/{checkSum}]");
            }

            return zipSetupFilePath;
        }

        private static Version GetAppInstalledMaxVersion(AppInformation appInfo)
        {
            var appVersionFolderPathList = Directory.GetDirectories(AppInformation.AppFolderPath, $"{appInfo.AppVersionFolderNamePrefix}*", SearchOption.TopDirectoryOnly).ToList();
            if (appVersionFolderPathList.Any() == false)
            {
                return null;
            }
            var maxVesion = appVersionFolderPathList.Select(p => new Version(p.Split(appInfo.AppVersionFolderNamePrefix).LastOrDefault())).Max();
            return maxVesion;
        }

        private static string GetAppMaxVersionExePath(AppInformation appInfo)
        {
            var maxVesion = GetAppInstalledMaxVersion(appInfo);
            if (maxVesion is null)
            {
                throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.NotFound, nameof(maxVesion)));
            }
            var appVersionExePath = Path.Combine(AppInformation.AppFolderPath, $"{appInfo.AppVersionFolderNamePrefix}{maxVesion}", appInfo.AppExecutableName);
            if (File.Exists(appVersionExePath) == false)
            {
                throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.NotFound, nameof(appVersionExePath)));
            }
            return appVersionExePath;
        }

        private UpdateInformation GetUpdateInfo()
        {
            if (UpdateInfoWebRequest is null)
            {
                throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.NotSet, nameof(UpdateInfoWebRequest)));
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

        public static void RunApp(ProcessStartInfo process, AppInformation appInfo)
        {
            if (process is null)
            {
                throw new ArgumentNullException(nameof(process));
            }
            if (appInfo is null)
            {
                throw new ArgumentNullException(nameof(appInfo));
            }

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
                await Task.Delay(2000).ConfigureAwait(false);
            }
            stopWatch.Stop();
        }
    }
}