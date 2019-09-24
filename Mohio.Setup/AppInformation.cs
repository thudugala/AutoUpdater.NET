using System.IO;
using System.Reflection;

namespace Mohio.Setup
{
    public class AppInformation
    {
        public string AppExecutableName { get; set; }

        public string AppFolderPath => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public string AppVersionFolderNamePrefix { get; set; }

        public void CheckAndFix()
        {
            if (string.IsNullOrWhiteSpace(AppExecutableName))
            {
                throw new InvalidDataException($"{nameof(AppExecutableName)} missing");
            }
            if (string.IsNullOrWhiteSpace(AppVersionFolderNamePrefix))
            {
                throw new InvalidDataException($"{nameof(AppVersionFolderNamePrefix)} missing");
            }

            if (AppVersionFolderNamePrefix.EndsWith("_") == false)
            {
                AppVersionFolderNamePrefix += "_";
            }
        }
    }
}