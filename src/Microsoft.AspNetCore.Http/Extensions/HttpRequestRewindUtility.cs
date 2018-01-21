// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Internal;

namespace Microsoft.AspNetCore.Http.Extensions
{
    public static class HttpRequestRewindUtility
    {
        /// <summary>
        /// Ensure the <paramref name="request"/> <see cref="HttpRequest.Body"/> can be read multiple times. Normally
        /// stores request bodies in memory; writes requests larger than 30K bytes to disk.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> to prepare.</param>
        /// <returns>The updated <paramref name="request"/>.</returns>
        /// <remarks>
        /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
        /// environment variable, if any. If that environment variable is not defined, these files are written to the
        /// current user's temporary folder. Files are automatically deleted at the end of the associated requests.
        /// </remarks>
        public static HttpRequest EnableRewind(HttpRequest request)
        {
            return BufferingHelper.EnableRewind(request);
        }

        /// <summary>
        /// Ensure the <paramref name="request"/> <see cref="HttpRequest.Body"/> can be read multiple times. Normally
        /// stores request bodies in memory; writes requests larger than <paramref name="bufferThreshold"/> bytes to
        /// disk.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> to prepare.</param>
        /// <param name="bufferThreshold">
        /// The maximum size in bytes of the in-memory <see cref="System.Buffers.ArrayPool{Byte}"/> used to buffer the
        /// stream. Larger request bodies are written to disk.
        /// </param>
        /// <returns>The updated <paramref name="request"/>.</returns>
        /// <remarks>
        /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
        /// environment variable, if any. If that environment variable is not defined, these files are written to the
        /// current user's temporary folder. Files are automatically deleted at the end of the associated requests.
        /// </remarks>
        public static HttpRequest EnableRewind(HttpRequest request, int bufferThreshold)
        {
            return BufferingHelper.EnableRewind(request, bufferThreshold);
        }

        /// <summary>
        /// Ensure the <paramref name="request"/> <see cref="HttpRequest.Body"/> can be read multiple times. Normally
        /// stores request bodies in memory; writes requests larger than 30K bytes to disk.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> to prepare.</param>
        /// <param name="bufferLimit">
        /// The maximum size in bytes of the request body. An attempt to read beyond this limit will cause an
        /// <see cref="System.IO.IOException"/>.
        /// </param>
        /// <returns>The updated <paramref name="request"/>.</returns>
        /// <remarks>
        /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
        /// environment variable, if any. If that environment variable is not defined, these files are written to the
        /// current user's temporary folder. Files are automatically deleted at the end of the associated requests.
        /// </remarks>
        public static HttpRequest EnableRewind(HttpRequest request, long bufferLimit)
        {
            return BufferingHelper.EnableRewind(request, bufferLimit: bufferLimit);
        }

        /// <summary>
        /// Ensure the <paramref name="request"/> <see cref="HttpRequest.Body"/> can be read multiple times. Normally
        /// stores request bodies in memory; writes requests larger than <paramref name="bufferThreshold"/> bytes to
        /// disk.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> to prepare.</param>
        /// <param name="bufferThreshold">
        /// The maximum size in bytes of the in-memory <see cref="System.Buffers.ArrayPool{Byte}"/> used to buffer the
        /// stream. Larger request bodies are written to disk.
        /// </param>
        /// <param name="bufferLimit">
        /// The maximum size in bytes of the request body. An attempt to read beyond this limit will cause an
        /// <see cref="System.IO.IOException"/>.
        /// </param>
        /// <returns>The updated <paramref name="request"/>.</returns>
        /// <remarks>
        /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
        /// environment variable, if any. If that environment variable is not defined, these files are written to the
        /// current user's temporary folder. Files are automatically deleted at the end of the associated requests.
        /// </remarks>
        public static HttpRequest EnableRewind(HttpRequest request, int bufferThreshold, long? bufferLimit)
        {
            return BufferingHelper.EnableRewind(request, bufferThreshold, bufferLimit);
        }
    }
}
