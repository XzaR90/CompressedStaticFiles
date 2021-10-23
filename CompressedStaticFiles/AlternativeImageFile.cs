using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace CompressedStaticFiles
{
    public class AlternativeImageFile : IFileAlternative
    {
        private readonly ILogger _logger;
        private readonly IFileInfo _originalFile;
        private readonly IFileInfo _alternativeFile;
        private readonly float _costRatio;

        public AlternativeImageFile(ILogger logger, IFileInfo originalFile, IFileInfo alternativeFile, float costRatio)
        {
            this._logger = logger;
            this._originalFile = originalFile;
            this._alternativeFile = alternativeFile;
            this._costRatio = costRatio;
        }

        public long Size => _alternativeFile.Length;

        public float Cost => Size * _costRatio;

        public void Apply(HttpContext context)
        {
            var path = context.Request.Path.Value;
            // Change file extension!
            var pathAndFilenameWithoutExtension = path.Substring(0, path.LastIndexOf('.'));
            var matchedPath = pathAndFilenameWithoutExtension + Path.GetExtension(_alternativeFile.Name);
            _logger.LogFileServed(context.Request.Path.Value, matchedPath, _originalFile.Length, _alternativeFile.Length);
            // Redirect the static file system to the alternative file
            context.Request.Path = new PathString(matchedPath);
            // Ensure that a caching proxy knows that it should cache based on the Accept header.
            context.Response.Headers.Add("Vary", "Accept");
        }

        public void Prepare(IContentTypeProvider contentTypeProvider, StaticFileResponseContext staticFileResponseContext)
        {
            // Method intentionally left empty.
        }
    }
}
