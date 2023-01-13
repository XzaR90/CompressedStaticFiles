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
    public class AlternativeImageFileProvider : IAlternativeFileProvider
    {
        private static readonly Dictionary<string, string[]> s_imageFormats =
            new (StringComparer.OrdinalIgnoreCase)
            {
                { "image/avif", new[] { ".avif" } },
                { "image/webp", new[] { ".webp" } },
                { "image/jpeg", new[] { ".jpg", ".jpeg", ".jfif", ".pjpeg", ".pjp" } },
                { "image/png", new[] { ".png" } },
                { "image/bmp", new[] { ".bmp" } },
                { "image/apng", new[] { ".apng" } },
                { "image/gif", new[] { ".gif" } },
                { "image/x-icon", new[] { ".ico", ".cur" } },
                { "image/tiff", new[] { ".tif", ".tiff" } }
            };

        private readonly ILogger _logger;
        private readonly IOptions<CompressedStaticFileOptions> _options;

        public AlternativeImageFileProvider(ILogger<AlternativeImageFileProvider> logger, IOptions<CompressedStaticFileOptions> options)
        {
            _logger = logger;
            _options = options;
        }

        public void Initialize(FileExtensionContentTypeProvider fileExtensionContentTypeProvider)
        {
            // Ensure that all image mime types are known!
            foreach (var mimeType in s_imageFormats.Keys)
            {
                foreach (var fileExtension in s_imageFormats[mimeType])
                {
                    if (!fileExtensionContentTypeProvider.Mappings.ContainsKey(fileExtension))
                    {
                        fileExtensionContentTypeProvider.Mappings.Add(fileExtension, mimeType);
                    }
                }
            }
        }

        private float GetCostRatioForFileExtension(string fileExtension)
        {
            foreach (var mimeType in s_imageFormats.Keys)
            {
                if (s_imageFormats[mimeType].Contains(fileExtension))
                {
                    if (_options.Value.ImageSubstitutionCostRatio.TryGetValue(mimeType, out var cost))
                    {
                        return cost;
                    }

                    return 1;
                }
            }

            return 1;
        }

        private float GetCostRatioForPath(string path)
        {
            var fileExtension = Path.GetExtension(path);
            return GetCostRatioForFileExtension(fileExtension);
        }

        public IFileAlternative GetAlternative(HttpContext context, IFileProvider fileSystem, IFileInfo originalFile)
        {
            if (!_options.Value.EnableImageSubstitution)
            {
                return null;
            }

            var matchingFileExtensions = context.Request.Headers.GetCommaSeparatedValues("Accept")
                                                        .Where(mimeType => s_imageFormats.ContainsKey(mimeType))
                                                        .SelectMany(mimeType => s_imageFormats[mimeType]);

            var originalAlternativeImageFile = new AlternativeImageFile(_logger, originalFile, originalFile, GetCostRatioForPath(originalFile.PhysicalPath));

            AlternativeImageFile matchedFile = originalAlternativeImageFile;
            var path = context.Request.Path.ToString();
            if (!path.Contains('.'))
            {
                return null;
            }

            var withoutExtension = path.Substring(0, path.LastIndexOf('.'));
            foreach (var fileExtension in matchingFileExtensions)
            {
                var file = fileSystem.GetFileInfo(withoutExtension + fileExtension);
                if (file.Exists)
                {
                    var alternativeFile = new AlternativeImageFile(_logger, originalFile, file, GetCostRatioForFileExtension(fileExtension));
                    if (matchedFile.Cost > alternativeFile.Cost)
                    {
                        matchedFile = alternativeFile;
                    }
                }
            }

            if (matchedFile != originalAlternativeImageFile)
            {
                return matchedFile;
            }

            return null;
        }
    }
}