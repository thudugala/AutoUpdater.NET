using Mohio.Shared;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Cache;
using System.Threading.Tasks;

namespace Mohio.Setup.Test
{
    internal class Program
    {       
        
        public static async Task Main(string[] args)
        {
            try
            {
                var appInfor = new AppInformation
                {
                    AppCompanyFolderName = "Thudugala",
                    AppExecutableName = "AutoUpdater.Wpf.Test.exe",
                    AppFolderName = "AutoUpdater",
                    AppVersionFolderNamePrefix = "AutoUpdater.Wpf.Test"
                };

                var uri = new Uri("https://raw.githubusercontent.com/thudugala/AutoUpdater.NET/master/Mohio.Setup.Test/UpdateInformation.json");

                Installer.Instance.UpdateInfoWebRequest = WebRequest.Create(uri);
                Installer.Instance.UpdateInfoWebRequest.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

                Installer.Instance.DownloadWebClient = new WebClient
                {
                    CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
                };
                
                await Installer.Instance.Start(new ProcessStartInfo(), appInfor);                
            }
            catch
            {
                //Ignor
            }
        }
    }
}