// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities
{
    /// <summary>
    /// Writes to the <see cref="Stream"/> using the supplied <see cref="Encoding"/>.
    /// It does not write the BOM and also does not close the stream.
    /// </summary>
    public class HttpResponseStreamWriter : TextWriter
    {
        private const int MinBufferSize = 128;

        /// <summary>
        /// Default buffer size.
        /// </summary>
        public const int DefaultBufferSize = 1024;

        private Stream _stream;
        private readonly Encoder _encoder;
        private readonly ArrayPool<byte> _bytePool;
        private readonly ArrayPool<char> _charPool;
        private readonly int _charBufferSize;

        private byte[] _byteBuffer;
        private char[] _charBuffer;

        private int _charBufferCount;

        public HttpResponseStreamWriter(Stream stream, Encoding encoding)
            : this(stream, encoding, DefaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared)
        {
        }

        public HttpResponseStreamWriter(Stream stream, Encoding encoding, int bufferSize)
            : this(stream, encoding, bufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared)
        {
        }

        public HttpResponseStreamWriter(
            Stream stream,
            Encoding encoding,
            int bufferSize,
            ArrayPool<byte> bytePool,
            ArrayPool<char> charPool)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanWrite)
            {
                throw new ArgumentException(Resources.HttpResponseStreamWriter_StreamNotWritable, nameof(stream));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            if (bytePool == null)
            {
                throw new ArgumentNullException(nameof(bytePool));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            _stream = stream;
            Encoding = encoding;

            _encoder = encoding.GetEncoder();
            _bytePool = bytePool;
            _charPool = charPool;

            _charBuffer = charPool.Rent(bufferSize);
            _charBufferSize = _charBuffer.Length;

            try
            {
                var requiredLength = encoding.GetMaxByteCount(_charBufferSize);
                _byteBuffer = bytePool.Rent(requiredLength);
            }
            catch
            {
                charPool.Return(_charBuffer);
                _charBuffer = null;

                if (_byteBuffer != null)
                {
                    bytePool.Return(_byteBuffer);
                    _byteBuffer = null;
                }

                throw;
            }
        }

        public override Encoding Encoding { get; }

        public override void Write(char value)
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException("stream");
            }

            if (_charBufferCount == _charBufferSize)
            {
                FlushInternal(flushEncoder: false);
            }

            _charBuffer[_charBufferCount] = value;
            _charBufferCount++;
        }

        public override void Write(char[] values, int index, int count)
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException("stream");
            }

            if (values == null)
            {
                return;
            }

            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    FlushInternal(flushEncoder: false);
                }

                CopyToCharBuffer(values, ref index, ref count);
            }
        }

        public override void Write(string value)
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException("stream");
            }

            if (value == null)
            {
                return;
            }

            var count = value.Length;
            var index = 0;
            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    FlushInternal(flushEncoder: false);
                }

                CopyToCharBuffer(value, ref index, ref count);
            }
        }

        public override async Task WriteAsync(char value)
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException("stream");
            }

            if (_charBufferCount == _charBufferSize)
            {
                await FlushInternalAsync(flushEncoder: false);
            }

            _charBuffer[_charBufferCount] = value;
            _charBufferCount++;
        }

        public override async Task WriteAsync(char[] values, int index, int count)
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException("stream");
            }

            if (values == null)
            {
                return;
            }

            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    await FlushInternalAsync(flushEncoder: false);
                }

                CopyToCharBuffer(values, ref index, ref count);
            }
        }

        public override async Task WriteAsync(string value)
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException("stream");
            }

            if (value == null)
            {
                return;
            }

            var count = value.Length;
            var index = 0;
            while (count > 0)
            {
                if (_charBufferCount == _charBufferSize)
                {
                    await FlushInternalAsync(flushEncoder: false);
                }

                CopyToCharBuffer(value, ref index, ref count);
            }
        }

        // We want to flush the stream when Flush/FlushAsync is explicitly
        // called by the user (example: from a Razor view).

        public override void Flush()
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException("stream");
            }

            FlushInternal(flushEncoder: true);
        }

        public override Task FlushAsync()
        {
            if (_stream == null)
            {
                throw new ObjectDisposedException("stream");
            }

            return FlushInternalAsync(flushEncoder: true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _stream != null)
            {
                try
                {
                    FlushInternal(flushEncoder: true);
                }
                finally
                {
                    _stream = null;

                    if (_bytePool != null)
                    {
                        _bytePool.Return(_byteBuffer);
                        _byteBuffer = null;
                    }

                    if (_charPool != null)
                    {
                        _charPool.Return(_charBuffer);
                        _charBuffer = null;
                    }
                }
            }
        }

        // Note: our FlushInternal method does NOT flush the underlying stream. This would result in
        // chunking.
        private void FlushInternal(bool flushEncoder)
        {
            if (_charBufferCount == 0)
            {
                return;
            }

            var count = _encoder.GetBytes(
                _charBuffer,
                0,
                _charBufferCount,
                _byteBuffer,
                0,
                flush: flushEncoder);

            _charBufferCount = 0;

            if (count > 0)
            {
                _stream.Write(_byteBuffer, 0, count);
            }
        }

        // Note: our FlushInternalAsync method does NOT flush the underlying stream. This would result in
        // chunking.
        private async Task FlushInternalAsync(bool flushEncoder)
        {
            if (_charBufferCount == 0)
            {
                return;
            }

            var count = _encoder.GetBytes(
                _charBuffer,
                0,
                _charBufferCount,
                _byteBuffer,
                0,
                flush: flushEncoder);

            _charBufferCount = 0;

            if (count > 0)
            {
                await _stream.WriteAsync(_byteBuffer, 0, count);
            }
        }

        private void CopyToCharBuffer(string value, ref int index, ref int count)
        {
            var remaining = Math.Min(_charBufferSize - _charBufferCount, count);

            value.CopyTo(
                sourceIndex: index,
                destination: _charBuffer,
                destinationIndex: _charBufferCount,
                count: remaining);

            _charBufferCount += remaining;
            index += remaining;
            count -= remaining;
        }

        private void CopyToCharBuffer(char[] values, ref int index, ref int count)
        {
            var remaining = Math.Min(_charBufferSize - _charBufferCount, count);

            Buffer.BlockCopy(
                src: values,
                srcOffset: index * sizeof(char),
                dst: _charBuffer,
                dstOffset: _charBufferCount * sizeof(char),
                count: remaining * sizeof(char));

            _charBufferCount += remaining;
            index += remaining;
            count -= remaining;
        }
    }
}
