using System;
using System.Globalization;
using System.IO;

namespace Mohio.Setup
{
    public class UpdateInformation
    {
        /// <summary>
        /// Checksum of the update file.
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>
        /// Download URL of the update file.
        /// </summary>
        public string DownloadURL { get; set; }

        /// <summary>
        /// Returns newest version of the application available to download.
        /// </summary>
        public string NewestVersionVersion { get; set; }

        public void Check()
        {
            if (string.IsNullOrWhiteSpace(DownloadURL))
            {
                throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.Missing, nameof(DownloadURL)));
            }
            if (string.IsNullOrWhiteSpace(NewestVersionVersion))
            {
                throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.Missing, nameof(NewestVersionVersion)));
            }
            if (string.IsNullOrWhiteSpace(Checksum))
            {
                throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.Missing, nameof(Checksum)));
            }
        }

        /// <summary>
        ///     If new update is available then returns true otherwise false.
        /// </summary>
        public bool IsUpdateAvailable(Version installedVersion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewestVersionVersion))
                {
                    return false;
                }

                return Version.Parse(NewestVersionVersion) > installedVersion;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return false;
            }
        }
    }
}