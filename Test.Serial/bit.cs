using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Test.Serial
{
    /// <summary>
    ///这个类主要用于那种将一个字节分成若干部分，每个部分表示一个数的类型
    /// </summary>
    class BitHelper
    {
        public static byte GetByte(byte data, byte index, byte count)
        {
            Contract.Requires(index + count <= 8);

            return (byte)((data >> (8 - index - count)) % (1 << count));
        }

        public static byte SetByte(int value, byte data, byte index, byte count)
        {
            Contract.Requires(index + count <= 8);
            Contract.Requires(value < (1 << count));

            var tail = data % (1 << (8 - index - count));
            return (byte)((((data >> (8 - index) << count) + value) << index) + tail);
        }

        //测试某一位是否为1
        public static bool TestBit(byte data, byte index)
        {
            return GetByte(data, index, 1) == 1;
        }

        public static byte SetBit(bool value, byte data, byte index)
        {
            return SetByte(value ? 1 : 0, data, index, 1);
        }
    }
}