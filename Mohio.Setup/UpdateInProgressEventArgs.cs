using System;

namespace Mohio.Setup
{
    public class UpdateInProgressEventArgs : EventArgs
    {
        public bool InProgress { get; set; }
    }
}