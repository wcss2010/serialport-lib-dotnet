using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialPortLib
{
    /// <summary>
    /// 数据缓冲区对象
    /// </summary>
    public class DataBufferObject
    {
        private List<byte> _buffer = new List<byte>();
        /// <summary>
        /// 缓冲区
        /// </summary>
        public List<byte> Buffer
        {
            get { return _buffer; }
        }

        /// <summary>
        /// 线程安全的添加数据
        /// </summary>
        /// <param name="buf"></param>
        public void AddRangeWithLock(byte[] buf)
        {
            if (buf != null)
            {
                lock (Buffer)
                {
                    Buffer.AddRange(buf);
                }
            }
        }

        /// <summary>
        /// 线程安全的删除指定范围的数据
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void RemoveRangeWithLock(int offset, int count)
        {
            if (offset >= 0 && count >= 1)
            {
                lock (Buffer)
                {
                    Buffer.RemoveRange(offset, count);
                }
            }
        }

        /// <summary>
        /// 线程安全的清空数据
        /// </summary>
        public void ClearWithLock()
        {
            lock (Buffer)
            {
                Buffer.Clear();
            }
        }

        /// <summary>
        /// 在缓冲区中查找要搜索的数组的第一个开始位置
        /// </summary>
        /// <param name="searchBytes">要查找的 System.Byte[]。</param>
        /// <returns>如果找到该字节数组，则为 searchBytes 的索引位置；如果未找到该字节数组，则为 -1。如果 searchBytes 为 null 或者长度为0，则返回值为 -1。</returns>
        public int IndexOf(byte[] searchBytes)
        {
            if (searchBytes == null) { return -1; }
            if (Buffer.Count == 0) { return -1; }
            if (searchBytes.Length == 0) { return -1; }
            if (Buffer.Count < searchBytes.Length) { return -1; }
            for (int i = 0; i < Buffer.Count - searchBytes.Length; i++)
            {
                if (Buffer[i] == searchBytes[0])
                {
                    if (searchBytes.Length == 1) { return i; }
                    bool flag = true;
                    for (int j = 1; j < searchBytes.Length; j++)
                    {
                        if (Buffer[i + j] != searchBytes[j])
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag) { return i; }
                }
            }
            return -1;
        }

        /// <summary>
        /// 线程安全的从缓冲区中取数据然后再删除
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public byte[] GetAndRemoveRangeWithLock(int offset, int count)
        {
            if (offset >= 0 && count >= 1)
            {
                //复制数据
                byte[] result = new byte[count];
                Buffer.CopyTo(offset, result, 0, count);

                //删除数据
                RemoveRangeWithLock(offset, count);

                return result;
            }
            else
            {
                return null;
            }
        }
    }
}