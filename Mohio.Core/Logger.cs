using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mohio.Core
{
    public class Logger
    {
        private static readonly Lazy<Logger> MySingleton =
            new Lazy<Logger>(() => new Logger(),
                System.Threading.LazyThreadSafetyMode.PublicationOnly);

        private readonly AssemblyName _assemblyName;

        private readonly StringBuilder _logBuilder;

        public Logger()
        {
            _assemblyName = Assembly.GetEntryAssembly().GetName();
            _logBuilder = new StringBuilder();

            _logBuilder.AppendLine("--------------------------------------------------------------");
            _logBuilder.AppendLine(DateTime.Now.ToString("F", CultureInfo.InvariantCulture));
            _logBuilder.AppendLine();
            _logBuilder.AppendLine($"{_assemblyName.Name} started [{_assemblyName.Version}]");
        }

        public static Logger Instance => MySingleton.Value;

        public void WriteLog()
        {
            Task.Run(() =>
            {
                _logBuilder.AppendLine("--------------------------------------------------------------");

                var folderPathBaseDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                try
                {
                    File.AppendAllText(Path.Combine(folderPathBaseDirectory, $"{_assemblyName.Name}.log"), _logBuilder.ToString());
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    var folderPathCommonApplicationData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), _assemblyName.Name);
                    if (Directory.Exists(folderPathCommonApplicationData) == false)
                    {
                        Directory.CreateDirectory(folderPathCommonApplicationData);
                    }
                    File.AppendAllText(Path.Combine(folderPathCommonApplicationData, $"{_assemblyName.Name}.log"), _logBuilder.ToString());
                }
            });
        }

        public void TrackError(Exception ex)
        {
            TrackError(ex, null);
        }

        public void TrackError(Exception ex, string message)
        {
            if (ex is null)
            {
                return;
            }

            if (string.IsNullOrEmpty(message) == false)
            {
                message = $"{message} {Environment.NewLine} ";
            }
            else
            {
                message = string.Empty;
            }

            var (errorMessage, errorStackTrace) = GetErrorData(ex);
            message += $"{errorMessage} {Environment.NewLine} {errorStackTrace}";
            TrackEvent(message);
        }

        public void TrackEvent(string message)
        {
            _logBuilder.Append(message);
        }

        private static (string errorMessage, string errorStackTrace) GetErrorData(Exception ex)
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
    }
}