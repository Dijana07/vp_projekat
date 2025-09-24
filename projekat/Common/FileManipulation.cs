using System;
using System.IO;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class FileManipulation : IDisposable
    {
        public FileManipulation(string relPath)
        {
            this.MemoryStream = new StreamWriter(relPath, true);
        }

        public FileManipulation()
        {
            this.MemoryStream = null;
        }

        [DataMember]
        public StreamWriter MemoryStream { get; set; }
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

        ~FileManipulation()
        {
            Dispose(false);
        }
    }
}
