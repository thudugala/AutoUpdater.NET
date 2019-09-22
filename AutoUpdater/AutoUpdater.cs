using AutoUpdater.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
namespace AutoUpdater
{
    public class AutoUpdater : IAutoUpdater
    {
        private static readonly Lazy<AutoUpdater> MySingleton =
            new Lazy<AutoUpdater>(() => new AutoUpdater(),
                System.Threading.LazyThreadSafetyMode.PublicationOnly);

        public static AutoUpdater Instance => MySingleton.Value;

        private readonly StringBuilder _logBuilder;

        public AutoUpdater()
        {
            _logBuilder = new StringBuilder();
        }

        public bool Running { get; private set; }

        public string Log => _logBuilder.ToString();

        public Version InstalledVersion { get; private set; }
        
        /// <summary>
        ///     Start checking for new version of application and display dialog to the user if update is available.
        /// </summary>
        /// <param name="appCast">URL of the xml file that contains information about latest version of the application.</param>
        /// <param name="mainAssembly">Assembly to use for version checking.</param>
        public void Setup(string zipSetupFilePath, string appExePath)
        {
            try
            {
                if (Running)
                {
                    return;
                }
                Running = true;

                _logBuilder.AppendLine();
                _logBuilder.AppendLine("Started");
                _logBuilder.AppendLine(DateTime.Now.ToString("F"));
                _logBuilder.AppendLine();

                if (File.Exists(zipSetupFilePath))
                {
                    _logBuilder.AppendLine($"[{zipSetupFilePath}] does not exist");
                    return;
                }

                var extension = Path.GetExtension(zipSetupFilePath);
                if(extension.ToUpperInvariant() != ".ZIP")
                {
                    _logBuilder.AppendLine($"Wrong File type [{extension}], it need to be zip file.");
                    return;
                }

                var installerPath = Path.Combine(Path.GetDirectoryName(zipSetupFilePath), "ZipInstaller.exe");

                try
                {
                    File.WriteAllBytes(installerPath, Resources.ZipInstaller);
                }
                catch (Exception ex)
                {
                    _logBuilder.AppendLine();
                    _logBuilder.AppendLine(ex.ToString());
                    return;
                }
                                
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true,
                    Arguments = $"\"{zipSetupFilePath}\" \"{appExePath}\""
                };

                Process.Start(processStartInfo);

                _logBuilder.AppendLine("Finined");

                Environment.Exit(0);                
            }
            finally
            {
                Running = false;
            }
        }
        
        public bool IsUpdateAvailable(Version installedVersion, Version newVersion)
        {
            return newVersion > installedVersion;            
        }
    }
}
