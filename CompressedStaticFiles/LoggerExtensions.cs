using System;
using Microsoft.Extensions.Logging;

namespace CompressedStaticFiles
{
    internal static class LoggerExtensions
    {
        private static readonly Action<ILogger, string, string, long, long, Exception> s_logFileServed = LoggerMessage.Define<string, string, long, long>(
               logLevel: LogLevel.Information,
               eventId: 1,
               formatString: "Sending file. Request file: '{RequestedPath}'. Served file: '{ServedPath}'. Original file size: {OriginalFileSize}. Served file size: {ServedFileSize}");

        public static void LogFileServed(this ILogger logger, string requestedPath, string servedPath, long originalFileSize, long servedFileSize)
        {
            if (string.IsNullOrEmpty(requestedPath))
            {
                throw new ArgumentNullException(nameof(requestedPath));
            }

            if (string.IsNullOrEmpty(servedPath))
            {
                throw new ArgumentNullException(nameof(servedPath));
            }

            s_logFileServed(logger, requestedPath, servedPath, originalFileSize, servedFileSize, null);
        }
    }
}