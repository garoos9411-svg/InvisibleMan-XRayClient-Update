using System;

namespace InvisibleManXRay.ViewModels
{
    using Models;

    public class StatusEventArgs : EventArgs
    {
        public Status Status { get; }

        public StatusEventArgs(Status status)
        {
            Status = status;
        }
    }
}
