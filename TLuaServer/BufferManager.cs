using System;
using System.Collections.Concurrent;
using System.Text;

namespace TLuaServer
{
    // 先用全文同步的模式。如果有性能问题，就要用增量式的，增量式的，就要考虑一个方便修改的类。比如
    // https://github.com/BLK10/StringBuffer/blob/master/StringBuffer.cs
    class BufferManager
    {
        private ConcurrentDictionary<string, string> _buffers = new ConcurrentDictionary<string, string>();

        private static BufferManager _buffer;

        public static BufferManager instance
        {
            get
            {
                if (_buffer == null)
                {
                    _buffer = new BufferManager();
                }
                return _buffer;
            }
        }

        public static void Init()
        {
            _buffer = new BufferManager();
        }

        public void UpdateBuffer(string documentPath, string buffer)
        {
            _buffers.AddOrUpdate(documentPath, buffer, (k, v) => buffer);
        }

        public string GetBuffer(string documentPath)
        {
            return _buffers.TryGetValue(documentPath, out var buffer) ? buffer : null;
        }
    }
}