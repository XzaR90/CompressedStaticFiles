using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace CompressedStaticFiles
{
    public interface IAlternativeFileProvider
    {
        void Initialize(FileExtensionContentTypeProvider fileExtensionContentTypeProvider);
        IFileAlternative GetAlternative(HttpContext context, IFileProvider fileSystem, IFileInfo originalFile);
    }
}