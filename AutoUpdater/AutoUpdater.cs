using AutoUpdater.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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

        public WebRequest UpdateInfoWebRequest { get; set; }

        private WebClient DownloadWebClient { get; set; }
        
        private static (string Message, string StackTrace) GetErrorData(Exception ex)
        {
            var message = $"{ex.Message}";
            var stackTrace = $"{ex.StackTrace}";

            var innerException = ex.InnerException;
            while (innerException != null)
            {
                message += $"{Environment.NewLine} { innerException.Message}";
                stackTrace += $"{Environment.NewLine} { innerException.StackTrace}";

                innerException = innerException.InnerException;
            }

            return (message, stackTrace);
        }

        public Func<string, Task<UpdateInfoEventArgs>> ConvertUpdateInfor { get; set;}
         
        public async Task Setup(string assemblyName, string appExePath)
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

                try
                {
                    ServicePointManager.SecurityProtocol |= (SecurityProtocolType)192 |
                                                            (SecurityProtocolType)768 | (SecurityProtocolType)3072;
                }
                catch (NotSupportedException) { }
                                
                var updateInfo = await GetUpdateInfo();

                if (updateInfo.IsUpdateAvailable == false)
                {
                    _logBuilder.AppendLine($"Update Not Available");
                    return;
                }

                var zipSetupFilePath = await DownloadZip(updateInfo);
                
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
                    File.WriteAllBytes(installerPath, Resources.AutoUpdater_ZipInstaller);
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
                    Arguments = $"\"{zipSetupFilePath}\" \"{assemblyName}\" \"{appExePath}\""
                };

                Process.Start(processStartInfo);

                _logBuilder.AppendLine("Finined");

                //              
            }
            finally
            {
                Running = false;
            }
        }

        private async Task<string> DownloadZip(UpdateInfoEventArgs updateInfo)
        {
            if (DownloadWebClient is null)
            {
                _logBuilder.AppendLine($"AutoUpdater Download WebClient is not set");
                return null;
            }
            if (updateInfo.DownloadURL is null)
            {
                _logBuilder.AppendLine($"AutoUpdater Download WebClient DownloadURL is not set");
                return null;
            }

            var fileName = Path.GetFileName(updateInfo.DownloadURL.LocalPath);

            var zipSetupFilePath = Path.Combine(Path.GetTempPath(), fileName);
            await DownloadWebClient.DownloadFileTaskAsync(updateInfo.DownloadURL, zipSetupFilePath);
            return zipSetupFilePath;
        }

        private async Task<UpdateInfoEventArgs> GetUpdateInfo()
        {
            if (UpdateInfoWebRequest is null)
            {
                _logBuilder.AppendLine($"AutoUpdater UpdateInfo WebRequest is not set");
                return null;
            }
            if (ConvertUpdateInfor is null)
            {
                _logBuilder.AppendLine($"AutoUpdater Convert UpdateInfor Funtion is not set");
                return null;
            }

            WebResponse webResponse;
            try
            {
                webResponse = UpdateInfoWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                _logBuilder.AppendLine($"Was unable to get Update Info");
                var errorData = GetErrorData(ex);
                _logBuilder.AppendLine($"{errorData.Message} {Environment.NewLine} {errorData.StackTrace}");
                return null;
            }

            UpdateInfoEventArgs updateInfo;
            using (var appCastStream = webResponse.GetResponseStream())
            {
                if (appCastStream is null)
                {
                    webResponse.Close();
                    _logBuilder.AppendLine($"Was unable to get Update Info");
                    return null;
                }

                using (var streamReader = new StreamReader(appCastStream))
                {
                    var data = streamReader.ReadToEnd();

                    updateInfo = await ConvertUpdateInfor(data);
                    if (updateInfo is null)
                    {
                        _logBuilder.AppendLine($"UpdateInfoEventArgs is null");
                        return null;
                    }
                }
            }

            return updateInfo;
        }

        public bool IsUpdateAvailable(Version installedVersion, Version newVersion)
        {
            return newVersion > installedVersion;            
        }
    }
}
