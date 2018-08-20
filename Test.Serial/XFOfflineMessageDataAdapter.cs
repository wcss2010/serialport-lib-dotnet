using SerialPortLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Test.Serial
{
    class XFOfflineMessageDataAdapter : IMessageDataAdapter
    {
        bool resAssembling = false;
        int headerIndex = 0;

        public override IMessageEntity Resolve()
        {
            DataBufferObject _recievedData = BufferStream;
            if (!resAssembling)
            {
                while (headerIndex + 3 < _recievedData.Buffer.Count && !(_recievedData.Buffer[headerIndex] == 0xFD && _recievedData.Buffer[headerIndex + 1] == 0x00 && _recievedData.Buffer[headerIndex + 2] == 0x80 && _recievedData.Buffer[headerIndex + 3] == 0x00))
                {
                    headerIndex++;
                }

                    if (headerIndex >= _recievedData.Buffer.Count)
                    {
                        _recievedData.ClearWithLock();
                    }
                
                resAssembling = true;
            }

            if (headerIndex + 2 >= _recievedData.Buffer.Count)
            {
                Thread.Sleep(10);
            }

            // 帧长度=数据区长度+1
            int length = 264;
            if (headerIndex + length > _recievedData.Buffer.Count)
            {
                Thread.Sleep(10);
            }

            if (_recievedData.Buffer.Count >= 264)
            {
                return new IMessageEntity(_recievedData.GetAndRemoveRangeWithLock(headerIndex, 264), 0, 264, null);
            }
            else
            {
                return null;
            }
        }
    }
}