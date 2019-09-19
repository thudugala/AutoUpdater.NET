namespace AutoUpdaterDotNET
{
    /// <summary>
    ///     Enum representing the effect of Mandatory flag.
    /// </summary>
    public enum Mode
    {
        /// <summary>
        /// In this mode, it ignores Remind Later and Skip values set previously and hide both buttons.
        /// </summary>
        Normal,

        /// <summary>
        /// In this mode, it won't show close button in addition to Normal mode behaviour.
        /// </summary>
        Forced,

        /// <summary>
        /// In this mode, it will start downloading and applying update without showing standarad update dialog in addition to Forced mode behaviour.
        /// </summary>
        ForcedDownload
    }
}