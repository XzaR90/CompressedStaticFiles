using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace CompressedStaticFiles
{
    public interface IFileAlternative
    {
        long Size { get; }

        /// <summary>
        /// Gets used to give some files a higher priority.
        /// </summary>
        float Cost { get; }
        void Apply(HttpContext context);
        void Prepare(IContentTypeProvider contentTypeProvider, StaticFileResponseContext staticFileResponseContext);
    }
}