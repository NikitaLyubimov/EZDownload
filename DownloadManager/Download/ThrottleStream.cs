using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DownloadManager.Download
{
    class ThrottleStream : Stream
    {
        public const long Infinite = 0;

        #region Private Memebers

        private Stream _baseStream;

        private long _byteCount;

        private long _start;

        private long _maximumBytesPerSeconds;

        #endregion

        #region Properties

        protected long CurrentMilliseconds
        {
            get { return Environment.TickCount; }
        }

        public long MaximumBytesPerSeconds
        {
            get { return _maximumBytesPerSeconds; }
            set
            {
                if (_maximumBytesPerSeconds != value)
                {
                    _maximumBytesPerSeconds = value;
                    Reset();
                }
            }
        }

        public override bool CanRead
        {
            get
            {
                return _baseStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _baseStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _baseStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return _baseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return _baseStream.Position;
            }
            set
            {
                _baseStream.Position = value;
            }
        }


        #endregion

        #region Constructor

        public ThrottleStream(Stream baseStream)
            : this(baseStream, ThrottleStream.Infinite)
        {

        }

        public ThrottleStream(Stream baseStream, long maximumBytesPerSecond)
        {
            if (maximumBytesPerSecond < 0)
                throw new ArgumentOutOfRangeException("maximumBytesPerSeconds", maximumBytesPerSecond, "The maximum bytes per seconds can't be negative");

            _baseStream = baseStream ?? throw new ArgumentNullException("baseStream");
            MaximumBytesPerSeconds = maximumBytesPerSecond;
            _start = CurrentMilliseconds;
            _byteCount = 0;
        }

        #endregion

        #region Public Methods

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Throttle(count);
            return _baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _baseStream.Write(buffer, offset, count);
        }

        public override string ToString()
        {
            return _baseStream.ToString();
        }



        #endregion

        #region Protected Methods

        protected void Throttle(int bufferSize)
        {
            if (_maximumBytesPerSeconds <= 0 || bufferSize <= 0)
                return;

            _byteCount += bufferSize;

            long elapsedMilliseconds = CurrentMilliseconds - _start;

            if(elapsedMilliseconds > 0)
            {
                long bps = _byteCount * 1000L / elapsedMilliseconds;

                if(bps > _maximumBytesPerSeconds)
                {
                    long wakeElapsed = _byteCount * 1000L / _maximumBytesPerSeconds;
                    int toSleep = (int)(wakeElapsed - elapsedMilliseconds);

                    if(toSleep > 1)
                    {
                        try
                        {
                            Thread.Sleep(toSleep);
                        }
                        catch(ThreadAbortException)
                        {

                        }

                        Reset();
                    }
                }
            }
        }

        protected void Reset()
        {
            long difference = CurrentMilliseconds - _start;

            if(difference > 1000)
            {
                _byteCount = 0;
                _start = CurrentMilliseconds;
            }
        }

        #endregion
    }
}
