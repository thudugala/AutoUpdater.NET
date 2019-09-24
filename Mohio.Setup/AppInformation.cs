using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Mohio.Setup
{
    public class AppInformation
    {
        public static string AppFolderPath => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public string AppExecutableName { get; set; }

        public string AppVersionFolderNamePrefix { get; set; }

        public void CheckAndFix()
        {
            if (string.IsNullOrWhiteSpace(AppExecutableName))
            {
                throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.Missing, nameof(AppExecutableName)));
            }
            if (string.IsNullOrWhiteSpace(AppVersionFolderNamePrefix))
            {
                throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.Missing, nameof(AppVersionFolderNamePrefix)));
            }

            if (AppVersionFolderNamePrefix.EndsWith("_", StringComparison.InvariantCultureIgnoreCase) == false)
            {
                AppVersionFolderNamePrefix += "_";
            }
        }
    }
}