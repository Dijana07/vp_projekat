using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
