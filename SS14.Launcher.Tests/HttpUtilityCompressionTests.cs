#nullable enable
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class HttpUtilityCompressionTests
{
    [Test]
    public void TestApplyCompressionHeaders()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "https://hub.spacestation14.com/api/servers");
        req.ApplyCompressionHeaders();

        var encodings = req.Headers.AcceptEncoding.ToString();
        Assert.That(encodings, Does.Contain("zstd"));
        Assert.That(encodings, Does.Contain("br"));
        Assert.That(encodings, Does.Contain("gzip"));
        Assert.That(encodings, Does.Contain("deflate"));
    }

    [Test]
    public async Task TestGZipDecompressionWrapper()
    {
        var rawText = "Hello from compressed space station 14 test server payload!";
        var rawBytes = Encoding.UTF8.GetBytes(rawText);

        using var compressedStream = new MemoryStream();
        using (var gzip = new GZipStream(compressedStream, CompressionMode.Compress, leaveOpen: true))
        {
            await gzip.WriteAsync(rawBytes);
        }
        compressedStream.Position = 0;

        var content = new ByteArrayContent(compressedStream.ToArray());
        content.Headers.ContentEncoding.Add("gzip");

        var response = new HttpResponseMessage { Content = content };
        var wrapped = response.WrapDecompressedContent();

        var decompressedText = await wrapped.Content.ReadAsStringAsync();
        Assert.That(decompressedText, Is.EqualTo(rawText));
    }

    [Test]
    public async Task TestBrotliDecompressionWrapper()
    {
        var rawText = "Brotli compressed JSON data for fast server list loading!";
        var rawBytes = Encoding.UTF8.GetBytes(rawText);

        using var compressedStream = new MemoryStream();
        using (var br = new BrotliStream(compressedStream, CompressionMode.Compress, leaveOpen: true))
        {
            await br.WriteAsync(rawBytes);
        }
        compressedStream.Position = 0;

        var content = new ByteArrayContent(compressedStream.ToArray());
        content.Headers.ContentEncoding.Add("br");

        var response = new HttpResponseMessage { Content = content };
        var wrapped = response.WrapDecompressedContent();

        var decompressedText = await wrapped.Content.ReadAsStringAsync();
        Assert.That(decompressedText, Is.EqualTo(rawText));
    }
}
