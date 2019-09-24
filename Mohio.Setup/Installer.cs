using Mohio.Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Mohio.Setup
{
    public class Installer
    {
        private static readonly Lazy<Installer> MySingleton =
            new Lazy<Installer>(() => new Installer(),
                System.Threading.LazyThreadSafetyMode.PublicationOnly);

        public static Installer Instance => MySingleton.Value;

        public void Start(ProcessStartInfo process)
        {
            try
            {
                var settingFileName = "AppInformation.json";

                var appSettingsJsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, settingFileName);
                if (File.Exists(appSettingsJsonFilePath) == false)
                {
                    throw new FileNotFoundException($"{settingFileName} does not exist");
                }
                var appInforJason = File.ReadAllText(appSettingsJsonFilePath);
                var appInfor = JsonSerializer.Deserialize<AppInformation>(appInforJason);
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
            if (Directory.Exists(appInformation.AppFolderPath) == false)
            {
                Directory.CreateDirectory(appInformation.AppFolderPath);

                //TODO download app

                return;
            }

            var maxVesion = GetAppInstalledMaxVersion(appInformation);
            if (maxVesion.ToString() != "0.0.0.0")
            {
                return;
            }

            //TODO download app
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
    }
}