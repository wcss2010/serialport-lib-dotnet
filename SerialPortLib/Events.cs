/*
    This file is part of SerialPortLib source code.

    SerialPortLib is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    SerialPortLib is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with SerialPortLib.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: https://github.com/genielabs/serialport-lib-dotnet
 */

using System;

namespace SerialPortLib
{
    /// <summary>
    /// Connected state changed event arguments.
    /// </summary>
    public class ConnectionStatusChangedEventArgs
    {
        /// <summary>
        /// The connected state.
        /// </summary>
        public readonly bool Connected;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortLib.ConnectionStatusChangedEventArgs"/> class.
        /// </summary>
        /// <param name="state">State of the connection (true = connected, false = not connected).</param>
        public ConnectionStatusChangedEventArgs(bool state)
        {
            Connected = state;
        }
    }

    /// <summary>
    /// Message received event arguments.
    /// </summary>
    public class MessageReceivedEventArgs
    {
        /// <summary>
        /// The data.
        /// </summary>
        public readonly IMessageEntity Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialPortLib.MessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="data">Data.</param>
        public MessageReceivedEventArgs(IMessageEntity data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// 队列对象
    /// </summary>
    public class QueueObject
    {
        public QueueObject() { }

        public QueueObject(byte[] buf, int offset, int length)
        {
            this.Buffer = buf;
            this.Offset = offset;
            this.DataLength = length;
        }

        public byte[] Buffer { get; set; }

        public int Offset { get; set; }

        public int DataLength { get; set; }
    }

    /// <summary>
    /// 消息适配器
    /// </summary>
    public abstract class IMessageDataAdapter
    {
        /// <summary>
        /// 串口对象
        /// </summary>
        public SerialPortInput SerialPortInputObject { get; set; }

        /// <summary>
        /// 解析数据
        /// </summary>
        /// <returns></returns>
        public abstract IMessageEntity Resolve();

        /// <summary>
        /// 报告指定的 System.Byte[] 在此实例中的第一个匹配项的索引。
        /// </summary>
        /// <param name="srcBytes">被执行查找的 System.Byte[]。</param>
        /// <param name="offsets">被执行查找的 System.Byte[]的偏移</param>
        /// <param name="searchBytes">要查找的 System.Byte[]。</param>
        /// <returns>如果找到该字节数组，则为 searchBytes 的索引位置；如果未找到该字节数组，则为 -1。如果 searchBytes 为 null 或者长度为0，则返回值为 -1。</returns>
        public static int IndexOf(byte[] srcBytes, int offsets, byte[] searchBytes)
        {
            if (srcBytes == null) { return -1; }
            if (searchBytes == null) { return -1; }
            if (srcBytes.Length == 0) { return -1; }
            if (searchBytes.Length == 0) { return -1; }
            if (srcBytes.Length < searchBytes.Length) { return -1; }
            for (int i = offsets; i < srcBytes.Length - searchBytes.Length; i++)
            {
                if (srcBytes[i] == searchBytes[0])
                {
                    if (searchBytes.Length == 1) { return i; }
                    bool flag = true;
                    for (int j = 1; j < searchBytes.Length; j++)
                    {
                        if (srcBytes[i + j] != searchBytes[j])
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
    }

    /// <summary>
    /// 解析出来的消息
    /// </summary>
    public class IMessageEntity
    {
        public IMessageEntity() { }

        public IMessageEntity(byte[] buf, long id, long length, object tag)
        {
            Buffer = buf;
            Id = id;
            Length = length;
            Tag = tag;
        }

        /// <summary>
        /// 数据
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 长度
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// 附加数据
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// 附加数据
        /// </summary>
        public object Tag1 { get; set; }

        /// <summary>
        /// 附加数据
        /// </summary>
        public object Tag2 { get; set; }
    }
}