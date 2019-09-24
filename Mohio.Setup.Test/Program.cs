using Mohio.Shared;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Mohio.Setup.Test
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var appInfo = new AppInformation
                {
                    AppExecutableName = "AutoUpdater.Wpf.Test.exe",
                    AppVersionFolderNamePrefix = "AutoUpdater.Wpf.Test"
                };
                appInfo.CheckAndFix();

                var uri = new Uri("https://raw.githubusercontent.com/thudugala/AutoUpdater.NET/master/Mohio.Setup.Test/UpdateInformation.json");
                Installer.Instance.UpdateInfoWebRequest = WebRequest.Create(uri);
                Installer.Instance.DownloadWebClient = new WebClient();
                                
                await Installer.Instance.DownloadApp(appInfo);

                Installer.Instance.RunApp(new ProcessStartInfo(), appInfo);

                await Installer.Instance.WaitNewAppVerionToFinishDownload();
            }
            catch
            {
                //Ignor
            }
        }
    }
}