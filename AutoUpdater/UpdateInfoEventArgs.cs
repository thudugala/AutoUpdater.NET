using System;
using System.Collections.Generic;
using System.Text;

namespace AutoUpdater
{
    public class UpdateInfoEventArgs
    {
        /// <summary>
        ///     If new update is available then returns true otherwise false.
        /// </summary>
        public bool IsUpdateAvailable => NewestVersionVersion > InstalledVersion;

        /// <summary>
        ///     Download URL of the update file.
        /// </summary>
        public Uri DownloadURL { get; set; }

        /// <summary>
        ///     Returns newest version of the application available to download.
        /// </summary>
        public Version NewestVersionVersion { get; set; }

        /// <summary>
        ///     Returns version of the application currently installed on the user's PC.
        /// </summary>
        public Version InstalledVersion { get; set; }
    }
}
