using Microsoft.VisualBasic;
using System;
using System.Collections.Concurrent;
using System.Text;

namespace TLuaServer
{
    // 先用全文同步的模式。如果有性能问题，就要用增量式的，增量式的，就要考虑一个方便修改的类。比如
    // https://github.com/BLK10/StringBuffer/blob/master/StringBuffer.cs
    class BufferManager
    {
        private ConcurrentDictionary<string, StringBuffer> _buffers = new ConcurrentDictionary<string, StringBuffer>();

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

        public void UpdateText(string documentPath, string text)
        {
            _buffers.AddOrUpdate(documentPath, new StringBuffer(text), (k, oldValue) =>
            {
                oldValue.UpdateText(text);
                return oldValue;
            });

        }

        public string GetText(string documentPath)
        {
            StringBuffer buf = GetBuffer(documentPath);
            if (buf != null)
            {
                return buf.Text;
            }
            else
            {
                return null;
            }
        }

        public StringBuffer GetBuffer(string documentPath)
        {
            StringBuffer buf;
            if (_buffers.TryGetValue(documentPath, out buf))
            {
                return buf;
            }
            else
            {
                return null;
            }
        }
    }
}