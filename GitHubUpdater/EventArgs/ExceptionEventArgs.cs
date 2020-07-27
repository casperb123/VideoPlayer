using System;
using System.Collections.Generic;
using System.Text;

namespace GitHubUpdater
{
    public class ExceptionEventArgs<T> : EventArgs where T : Exception
    {
        public T OriginalException { get; private set; }
        public string Message { get; private set; }

        public ExceptionEventArgs(T originalException)
        {
            OriginalException = originalException;
        }

        public ExceptionEventArgs(T originalException, string message) : this(originalException)
        {
            Message = message;
        }
    }
}
