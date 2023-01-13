using System;
using System.Runtime.Serialization;

namespace CompressedStaticFiles
{
    [Serializable]
    public class CompressedStaticFilesException : Exception
    {
        public CompressedStaticFilesException()
        {
        }

        public CompressedStaticFilesException(string message) : base(message)
        {
        }

        public CompressedStaticFilesException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CompressedStaticFilesException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}