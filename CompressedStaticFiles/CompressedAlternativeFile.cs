using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace CompressedStaticFiles
{
    public class CompressedAlternativeFile : IFileAlternative
    {
        private readonly ILogger _logger;
        private readonly IFileInfo _originalFile;
        private readonly IFileInfo _alternativeFile;

        public CompressedAlternativeFile(ILogger logger, IFileInfo originalFile, IFileInfo alternativeFile)
        {
            this._logger = logger;
            this._originalFile = originalFile;
            this._alternativeFile = alternativeFile;
        }

        public long Size => _alternativeFile.Length;

        public float Cost => Size;

        public void Apply(HttpContext context)
        {
            var matchedPath = context.Request.Path.Value + Path.GetExtension(_alternativeFile.Name);
            _logger.LogFileServed(context.Request.Path.Value, matchedPath, _originalFile.Length, _alternativeFile.Length);
            context.Request.Path = new PathString(matchedPath);
        }

        public void Prepare(IContentTypeProvider contentTypeProvider, StaticFileResponseContext staticFileResponseContext)
        {
            foreach (var compressionType in CompressedAlternativeFileProvider.CompressionTypes.Keys)
            {
                var fileExtension = CompressedAlternativeFileProvider.CompressionTypes[compressionType];
                if (staticFileResponseContext.File.Name.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    // we need to restore the original content type, otherwise it would be based on the compression type
                    // (for example "application/brotli" instead of "text/html")
                    if (contentTypeProvider.TryGetContentType(staticFileResponseContext.File.PhysicalPath.Remove(
                        staticFileResponseContext.File.PhysicalPath.Length - fileExtension.Length, fileExtension.Length), out var contentType))
                    {
                        staticFileResponseContext.Context.Response.ContentType = contentType;
                    }

                    staticFileResponseContext.Context.Response.Headers.Add("Content-Encoding", new[] { compressionType });
                }
            }
        }
    }
}
