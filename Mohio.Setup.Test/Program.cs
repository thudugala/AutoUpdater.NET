using Mohio.Shared;
using System;
using System.Diagnostics;
using System.Net;

namespace Mohio.Setup.Test
{
    internal class Program
    {
        private static void Main(string[] args)
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

                var uri = new Uri("");
                Installer.Instance.UpdateInfoWebRequest = WebRequest.Create(uri);

                Installer.Instance.Start(new ProcessStartInfo(), appInfor);
            }
            catch
            {
                //Ignor
            }
        }
    }
}