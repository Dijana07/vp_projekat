using System;
using System.IO;
using System.Runtime.Serialization;

namespace Common
{
    public class ReadFileManipulation : IDisposable
    {
        public ReadFileManipulation(string relPath)
        {
            this.MemoryStream = new StreamReader(relPath);
        }

        public ReadFileManipulation()
        {
            this.MemoryStream = null;
        }

        [DataMember]
        public StreamReader MemoryStream { get; set; }
        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (MemoryStream != null)
                    {
                        MemoryStream.Dispose();

                    }
                }
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ReadFileManipulation()
        {
            Dispose(false);
        }
    }
}

