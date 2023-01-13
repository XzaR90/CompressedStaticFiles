using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CompressedStaticFiles
{
    public class CompressedAlternativeFileProvider : IAlternativeFileProvider
    {
        public static Dictionary<string, string> CompressionTypes { get; } =
            new ()
            {
                { "gzip", ".gz" },
                { "br", ".br" }
            };

        private readonly ILogger _logger;
        private readonly IOptions<CompressedStaticFileOptions> _options;

        public CompressedAlternativeFileProvider(ILogger<CompressedAlternativeFileProvider> logger, IOptions<CompressedStaticFileOptions> options)
        {
            _logger = logger;
            _options = options;
        }

        public void Initialize(FileExtensionContentTypeProvider fileExtensionContentTypeProvider)
        {
            // the StaticFileProvider would not serve the file if it does not know the content-type
            fileExtensionContentTypeProvider.Mappings[".br"] = "application/brotli";
        }

        /// <summary>
        /// Find the encodings that are supported by the browser and by this middleware.
        /// </summary>
        private static IEnumerable<string> GetSupportedEncodings(HttpContext context)
        {
            var browserSupportedCompressionTypes = context.Request.Headers.GetCommaSeparatedValues("Accept-Encoding");
            var validCompressionTypes = CompressionTypes.Keys.Intersect(browserSupportedCompressionTypes, StringComparer.OrdinalIgnoreCase);
            return validCompressionTypes;
        }

        public IFileAlternative GetAlternative(HttpContext context, IFileProvider fileSystem, IFileInfo originalFile)
        {
            if (!_options.Value.EnablePrecompressedFiles)
            {
                return null;
            }

            var supportedEncodings = GetSupportedEncodings(context);
            IFileInfo matchedFile = originalFile;
            foreach (var compressionType in supportedEncodings)
            {
                var fileExtension = CompressionTypes[compressionType];
                var file = fileSystem.GetFileInfo(context.Request.Path + fileExtension);
                if (file.Exists && file.Length < matchedFile.Length)
                {
                    matchedFile = file;
                }
            }

            if (matchedFile != originalFile)
            {
                // A compressed version exists and is smaller, change the path to serve the compressed file.
                return new CompressedAlternativeFile(_logger, originalFile, matchedFile);
            }

            return null;
        }
    }
}