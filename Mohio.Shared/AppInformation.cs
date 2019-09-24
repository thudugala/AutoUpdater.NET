using System;
using System.IO;

namespace Mohio.Shared
{
    public class AppInformation
    {
        public string AppCompanyFolderName { get; set; }

        public string AppCompanyFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppCompanyFolderName);

        public string AppExecutableName { get; set; }

        public string AppFolderName { get; set; }

        public string AppFolderPath => Path.Combine(AppCompanyFolderPath, AppFolderName);

        public string AppVersionFolderNamePrefix { get; set; }

        public void CheckAndFix()
        {
            if (string.IsNullOrWhiteSpace(AppCompanyFolderName))
            {
                throw new InvalidDataException($"{nameof(AppCompanyFolderName)} missing");
            }
            if (string.IsNullOrWhiteSpace(AppExecutableName))
            {
                throw new InvalidDataException($"{nameof(AppExecutableName)} missing");
            }
            if (string.IsNullOrWhiteSpace(AppFolderName))
            {
                throw new InvalidDataException($"{nameof(AppFolderName)} missing");
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