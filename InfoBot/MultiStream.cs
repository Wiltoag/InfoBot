using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Infobot
{
    internal class MultiStream : Stream
    {
        #region Private Fields

        private IEnumerable<Stream> substreams;

        #endregion Private Fields

        #region Public Constructors

        public MultiStream(IEnumerable<Stream> streams)
        {
            substreams = new LinkedList<Stream>(streams);
        }

        public MultiStream(params Stream[] streams) :
            this(streams.AsEnumerable())
        {
        }

        #endregion Public Constructors

        #region Public Properties

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        #endregion Public Properties

        #region Public Methods

        public override void Flush()
        {
            foreach (var stream in substreams)
                stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            foreach (var stream in substreams)
                stream.Write(buffer, offset, count);
        }

        #endregion Public Methods
    }
}