using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace SS14.Launcher.Utility.Algorithms.Search;

/// <summary>
/// Hardware-accelerated SIMD string processing (Vector128 / Vector256 / AVX2 / SSE2 / AdvSIMD).
/// </summary>
public static class SimdStringHelper
{
    public static void ToLowerAsciiSimd(ReadOnlySpan<char> source, Span<char> destination, bool forceScalar = false)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException("Destination span is too short");

        int i = 0;
        int length = source.Length;
        var srcSpanUshort = MemoryMarshal.Cast<char, ushort>(source);
        var dstSpanUshort = MemoryMarshal.Cast<char, ushort>(destination);

        if (!forceScalar && Vector256.IsHardwareAccelerated && length >= 16)
        {
            var lowerA = Vector256.Create((ushort)'A');
            var upperZ = Vector256.Create((ushort)'Z');
            var diff = Vector256.Create((ushort)('a' - 'A'));

            while (i <= length - 16)
            {
                var vec = Vector256.Create(srcSpanUshort.Slice(i, 16));
                var maskGe = Vector256.GreaterThanOrEqual(vec, lowerA);
                var maskLe = Vector256.LessThanOrEqual(vec, upperZ);
                var isUpper = maskGe & maskLe;
                var offset = isUpper & diff;
                var result = vec + offset;

                Unsafe.As<ushort, Vector256<ushort>>(ref dstSpanUshort[i]) = result;
                i += 16;
            }
        }
        else if (!forceScalar && Vector128.IsHardwareAccelerated && length >= 8)
        {
            var lowerA = Vector128.Create((ushort)'A');
            var upperZ = Vector128.Create((ushort)'Z');
            var diff = Vector128.Create((ushort)('a' - 'A'));

            while (i <= length - 8)
            {
                var vec = Vector128.Create(srcSpanUshort.Slice(i, 8));
                var maskGe = Vector128.GreaterThanOrEqual(vec, lowerA);
                var maskLe = Vector128.LessThanOrEqual(vec, upperZ);
                var isUpper = maskGe & maskLe;
                var offset = isUpper & diff;
                var result = vec + offset;

                Unsafe.As<ushort, Vector128<ushort>>(ref dstSpanUshort[i]) = result;
                i += 8;
            }
        }

        for (; i < length; i++)
        {
            char c = source[i];
            destination[i] = (c >= 'A' && c <= 'Z') ? (char)(c + 32) : c;
        }
    }

    public static string ToLowerAsciiSimd(string text, bool forceScalar = false)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return string.Create(text.Length, (text, forceScalar), (span, state) =>
        {
            ToLowerAsciiSimd(state.text.AsSpan(), span, state.forceScalar);
        });
    }
}
