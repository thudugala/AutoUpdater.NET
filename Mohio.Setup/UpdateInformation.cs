using System;
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
                throw new InvalidDataException($"{nameof(DownloadURL)} missing");
            }
            if (string.IsNullOrWhiteSpace(NewestVersionVersion))
            {
                throw new InvalidDataException($"{nameof(NewestVersionVersion)} missing");
            }
            if (string.IsNullOrWhiteSpace(Checksum))
            {
                throw new InvalidDataException($"{nameof(Checksum)} missing");
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
            catch
            {
                return false;
            }
        }
    }
}