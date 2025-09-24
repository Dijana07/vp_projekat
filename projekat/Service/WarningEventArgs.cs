using System;

namespace Service
{
    public class WarningEventArgs : EventArgs
    {
        public string Message { get; }
        public WarningEventArgs(string message)
        {
            Message = message;
        }
    }
}
