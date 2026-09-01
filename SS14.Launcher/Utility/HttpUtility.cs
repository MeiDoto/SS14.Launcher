using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SS14.Launcher.Utility;

/// <summary>
/// HTTP utility extensions for network requests, compression handling and stream management.
/// </summary>
public static class HttpUtility
{
    private static readonly StringWithQualityHeaderValue ZStdHeader = new("zstd", 1.0);
    private static readonly StringWithQualityHeaderValue BrHeader = new("br", 0.9);
    private static readonly StringWithQualityHeaderValue GzipHeader = new("gzip", 0.8);
    private static readonly StringWithQualityHeaderValue DeflateHeader = new("deflate", 0.7);

    /// <summary>
    /// Injects standard compression headers (zstd, br, gzip, deflate) into the request.
    /// </summary>
    public static void ApplyCompressionHeaders(this HttpRequestMessage message)
    {
        message.Headers.AcceptEncoding.Add(ZStdHeader);
        message.Headers.AcceptEncoding.Add(BrHeader);
        message.Headers.AcceptEncoding.Add(GzipHeader);
        message.Headers.AcceptEncoding.Add(DeflateHeader);
    }

    /// <summary>
    /// Wraps the response content in an automatic decompressor if encoded with zstd, br, gzip, or deflate.
    /// </summary>
    public static HttpResponseMessage WrapDecompressedContent(this HttpResponseMessage response)
    {
        var encoding = response.Content.Headers.ContentEncoding.LastOrDefault()?.ToLowerInvariant();
        response.Content = encoding switch
        {
            "zstd" => new ZStdHttpContent(response.Content),
            "br" => new BrotliHttpContent(response.Content),
            "gzip" => new GZipHttpContent(response.Content),
            "deflate" => new DeflateHttpContent(response.Content),
            _ => response.Content
        };

        return response;
    }

    /// <summary>
    /// Sends an HTTP request with ZStd, Brotli, GZip and Deflate compression accepted,
    /// automatically decompressing the response stream.
    /// </summary>
    public static async Task<HttpResponseMessage> SendCompressedAsync(
        this HttpClient client,
        HttpRequestMessage message,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead,
        CancellationToken cancel = default)
    {
        message.ApplyCompressionHeaders();
        var response = await client.SendAsync(message, completionOption, cancel);
        return response.WrapDecompressedContent();
    }

    /// <summary>
    /// Sends an HTTP request with ZStandard (<c>zstd</c>) compression accepted,
    /// automatically wrapping the response stream in a transparent decompressor if encoded with zstd.
    /// </summary>
    public static async Task<HttpResponseMessage> SendZStdAsync(
        this HttpClient client,
        HttpRequestMessage message,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead,
        CancellationToken cancel = default)
    {
        return await client.SendCompressedAsync(message, completionOption, cancel);
    }

    // Taken from https://github.com/dotnet/runtime/blob/f89fbb96cabe95db5869e3d44c6b48c1c0f8fc1a/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/DecompressionHandler.cs
    // The original code is Copyright © .NET Foundation and Contributors. All rights reserved. Licensed under the MIT License (MIT).
    public abstract class DecompressedContent : HttpContent
    {
        private readonly HttpContent _originalContent;
        private bool _contentConsumed;

        public DecompressedContent(HttpContent originalContent)
        {
            _originalContent = originalContent;
            _contentConsumed = false;

            // Copy original response headers, but with the following changes:
            //   Content-Length is removed, since it no longer applies to the decompressed content
            //   The last Content-Encoding is removed, since we are processing that here.
            foreach (var (h, v) in originalContent.Headers)
            {
                Headers.Add(h, v);
            }

            Headers.ContentLength = null;
            Headers.ContentEncoding.Clear();
            string? prevEncoding = null;
            foreach (string encoding in originalContent.Headers.ContentEncoding)
            {
                if (prevEncoding != null)
                {
                    Headers.ContentEncoding.Add(prevEncoding);
                }

                prevEncoding = encoding;
            }
        }

        protected abstract Stream GetDecompressedStream(Stream originalStream);

        protected override void SerializeToStream(Stream stream, TransportContext? context,
            CancellationToken cancellationToken)
        {
            using Stream decompressedStream = CreateContentReadStream(cancellationToken);
            decompressedStream.CopyTo(stream);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context,
            CancellationToken cancellationToken)
        {
            using Stream decompressedStream = await CreateContentReadStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            await decompressedStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
        {
            ValueTask<Stream> task = CreateContentReadStreamAsyncCore(async: false, cancellationToken);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken) =>
            CreateContentReadStreamAsyncCore(async: true, cancellationToken).AsTask();

        private async ValueTask<Stream> CreateContentReadStreamAsyncCore(bool async,
            CancellationToken cancellationToken)
        {
            if (_contentConsumed)
            {
                throw new InvalidOperationException("Stream already read");
            }

            _contentConsumed = true;

            Stream originalStream;
            if (async)
            {
                originalStream = await _originalContent.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                originalStream = _originalContent.ReadAsStream(cancellationToken);
            }

            return GetDecompressedStream(originalStream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _originalContent.Dispose();
            }

            base.Dispose(disposing);
        }
    }


    public sealed class ZStdHttpContent : DecompressedContent
    {
        public ZStdHttpContent(HttpContent originalContent) : base(originalContent)
        {
        }

        protected override Stream GetDecompressedStream(Stream originalStream)
        {
            return new ZStdDecompressStream(originalStream);
        }
    }

    public sealed class BrotliHttpContent : DecompressedContent
    {
        public BrotliHttpContent(HttpContent originalContent) : base(originalContent)
        {
        }

        protected override Stream GetDecompressedStream(Stream originalStream)
        {
            return new System.IO.Compression.BrotliStream(originalStream, System.IO.Compression.CompressionMode.Decompress);
        }
    }

    public sealed class GZipHttpContent : DecompressedContent
    {
        public GZipHttpContent(HttpContent originalContent) : base(originalContent)
        {
        }

        protected override Stream GetDecompressedStream(Stream originalStream)
        {
            return new System.IO.Compression.GZipStream(originalStream, System.IO.Compression.CompressionMode.Decompress);
        }
    }

    public sealed class DeflateHttpContent : DecompressedContent
    {
        public DeflateHttpContent(HttpContent originalContent) : base(originalContent)
        {
        }

        protected override Stream GetDecompressedStream(Stream originalStream)
        {
            return new System.IO.Compression.DeflateStream(originalStream, System.IO.Compression.CompressionMode.Decompress);
        }
    }
}
