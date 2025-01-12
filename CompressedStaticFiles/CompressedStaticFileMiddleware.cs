﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IHost = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;

namespace CompressedStaticFiles
{
    public class CompressedStaticFileMiddleware
    {
        private readonly AsyncLocal<IFileAlternative> _alternativeFile = new();
        private readonly IOptions<StaticFileOptions> _staticFileOptions;
        private readonly IEnumerable<IAlternativeFileProvider> _alternativeFileProviders;
        private readonly StaticFileMiddleware _base;
        private readonly ILogger logger;

        public CompressedStaticFileMiddleware(
            RequestDelegate next,
            IHost hostingEnv,
            IOptions<StaticFileOptions> staticFileOptions,
            IOptions<CompressedStaticFileOptions> compressedStaticFileOptions,
            ILoggerFactory loggerFactory,
            IEnumerable<IAlternativeFileProvider> alternativeFileProviders)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (hostingEnv == null)
            {
                throw new ArgumentNullException(nameof(hostingEnv));
            }

            ThrowIfNoAltProviders(alternativeFileProviders);

            logger = loggerFactory.CreateLogger<CompressedStaticFileMiddleware>();

            _staticFileOptions = staticFileOptions ?? throw new ArgumentNullException(nameof(staticFileOptions));
            _alternativeFileProviders = alternativeFileProviders;
            InitializeStaticFileOptions(hostingEnv, staticFileOptions);

            _base = new StaticFileMiddleware(next, hostingEnv, staticFileOptions, loggerFactory);
        }

        private static void ThrowIfNoAltProviders(IEnumerable<IAlternativeFileProvider> alternativeFileProviders)
        {
            if (!alternativeFileProviders.Any())
            {
                throw new CompressedStaticFilesException("No IAlternativeFileProviders where found, did you forget to add AddCompressedStaticFiles() in ConfigureServices?");
            }
        }

        private void InitializeStaticFileOptions(IHost hostingEnv, IOptions<StaticFileOptions> staticFileOptions)
        {
            staticFileOptions.Value.FileProvider = staticFileOptions.Value.FileProvider ?? hostingEnv.WebRootFileProvider;
            var contentTypeProvider = staticFileOptions.Value.ContentTypeProvider ?? new FileExtensionContentTypeProvider();
            if (contentTypeProvider is FileExtensionContentTypeProvider fileExtensionContentTypeProvider)
            {
                foreach (var alternativeFileProvider in _alternativeFileProviders)
                {
                    alternativeFileProvider.Initialize(fileExtensionContentTypeProvider);
                }
            }

            staticFileOptions.Value.ContentTypeProvider = contentTypeProvider;

            var originalPrepareResponse = staticFileOptions.Value.OnPrepareResponse;
            staticFileOptions.Value.OnPrepareResponse = context =>
            {
                originalPrepareResponse(context);
                var alternativeFile = _alternativeFile.Value;
                alternativeFile?.Prepare(contentTypeProvider, context);
            };
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.HasValue)
            {
                ProcessRequest(context);
            }

            return _base.Invoke(context);
        }

        private void ProcessRequest(HttpContext context)
        {
            var fileSystem = _staticFileOptions.Value.FileProvider;
            var originalFile = fileSystem.GetFileInfo(context.Request.Path);

            if (!originalFile.Exists || originalFile.IsDirectory)
            {
                return;
            }

            // Find the smallest file from all our alternative file providers
            var smallestAlternativeFile = _alternativeFileProviders.Select(alternativeFileProvider => alternativeFileProvider.GetAlternative(context, fileSystem, originalFile))
                                                                  .Where(af => af != null)
                                                                  .OrderBy(alternativeFile => alternativeFile?.Cost)
                                                                  .FirstOrDefault();
            if (smallestAlternativeFile != null)
            {
                smallestAlternativeFile.Apply(context);
                _alternativeFile.Value = smallestAlternativeFile;
            }
        }
    }
}