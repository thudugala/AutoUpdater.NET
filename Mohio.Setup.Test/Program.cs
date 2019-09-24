using Mohio.Shared;
using System.Diagnostics;

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

                Installer.Instance.Start(new ProcessStartInfo(), appInfor);
            }
            catch
            {
                //Ignor
            }
        }
    }
}