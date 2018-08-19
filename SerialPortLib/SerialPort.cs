using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using NLog;
using System.Text;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;

namespace SerialPortLib
{
    /// <summary>
    /// 连接状态事件
    /// </summary>
    public delegate void ConnectionStatusChangedEventHandler(object sender, ConnectionStatusChangedEventArgs args);

    /// <summary>
    /// 数据接收事件
    /// </summary>
    public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs args);

    /// <summary>
    /// 串口通信类
    /// </summary>
    public class SerialPortInput
    {
        private List<byte> _bufferStream = new List<byte>();
        /// <summary>
        /// 接收缓冲
        /// </summary>
        public List<byte> BufferStream
        {
            get { return _bufferStream; }
        }

        /// <summary>
        /// 同步锁对象
        /// </summary>
        public static object lockObject = new object();

        /// <summary>
        /// 日志对象
        /// </summary>
        public static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// .net串口通信对象
        /// </summary>
        public SerialPort SerialPortObject { get; set; }

        /// <summary>
        /// 是否打印接收日志
        /// </summary>
        public bool EnabledPrintReceiveLog { get; set; }

        /// <summary>
        /// 消息适配器(用于从BufferStream中解析消息)
        /// </summary>
        public IMessageDataAdapter MessageDataAdapterObject { get; set; }

        /// <summary>
        /// 连接状态事件
        /// </summary>
        public event ConnectionStatusChangedEventHandler ConnectionStatusChanged;

        /// <summary>
        /// 数据接收事件
        /// </summary>
        public event MessageReceivedEventHandler MessageReceived;

        protected BackgroundWorker _resolveWorker = null;
        protected BackgroundWorker _connectionWorker = null;

        /// <summary>
        /// 投递连接状态事件
        /// </summary>
        /// <param name="args">Arguments.</param>
        protected virtual void OnConnectionStatusChanged(ConnectionStatusChangedEventArgs args)
        {
            logger.Debug(args.Connected);

            if (ConnectionStatusChanged != null)
            {
                ConnectionStatusChanged(this, args);
            }
        }

        /// <summary>
        /// 投递连接状态事件
        /// </summary>
        /// <param name="args">Arguments.</param>
        protected virtual void OnMessageReceived(MessageReceivedEventArgs args)
        {
            if (EnabledPrintReceiveLog)
            {
                logger.Debug(BitConverter.ToString(args.Data));
            }

            if (MessageReceived != null)
            {
                MessageReceived(this, args);
            }
        }

        /// <summary>
        /// 是否已经连接
        /// </summary>
        public bool IsConnected
        {
            get { return SerialPortObject != null && SerialPortObject.IsOpen; }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <returns>True=成功,False=失败</returns>
        /// <param name="message">数据</param>
        public bool SendMessage(byte[] message)
        {
            bool success = false;
            if (IsConnected)
            {
                try
                {
                    SerialPortObject.Write(message, 0, message.Length);
                    success = true;
                    logger.Debug(BitConverter.ToString(message));
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }
            return success;
        }

        /// <summary>
        /// 连接
        /// </summary>
        public void Connect()
        {
            //先断开连接
            Disconnect();

            if (MessageDataAdapterObject == null)
            {
                return;
            }
            if (SerialPortObject == null)
            {
                return;
            }

            //设置串口对象
            MessageDataAdapterObject.SerialPortInputObject = this;

            //打开串口
            SerialPortObject.Open();

            //创建解析线程
            _resolveWorker = new BackgroundWorker();
            _resolveWorker.WorkerSupportsCancellation = true;
            _resolveWorker.DoWork += _resolveWorker_DoWork;
            _resolveWorker.RunWorkerAsync();

            //断开重连线程
            _connectionWorker = new BackgroundWorker();
            _connectionWorker.WorkerSupportsCancellation = true;
            _connectionWorker.DoWork += _connectionWorker_DoWork;
            _connectionWorker.RunWorkerAsync();
        }

        void _connectionWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!((BackgroundWorker)sender).CancellationPending)
            {
                if (SerialPortObject != null)
                {
                    if (!SerialPortObject.IsOpen)
                    {
                        //重新连接
                        try
                        {
                            SerialPortObject.Open();
                        }
                        catch (Exception ex) { }
                    }
                }

                try
                {
                    Thread.Sleep(3000);
                }
                catch (Exception ex) { }
            }
        }

        void _resolveWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker obj = (BackgroundWorker)sender;
            while (!obj.CancellationPending)
            {
                try
                {
                    if (IsConnected)
                    {
                        //可以接收
                        if (BufferStream.Count > 0)
                        {
                            byte[] msg = MessageDataAdapterObject.Resolve();
                            if (msg != null && msg.Length >= 1)
                            {
                                OnMessageReceived(new MessageReceivedEventArgs(msg));
                            }
                        }
                        else
                        {
                            Thread.Sleep(5);
                        }
                    }
                    else
                    {
                        Thread.Sleep(5);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString(), ex);

                    Thread.Sleep(5);
                }
            }
        }

        /// <summary>
        /// 断开
        /// </summary>
        public void Disconnect()
        {
            if (_resolveWorker != null)
            {
                _resolveWorker.CancelAsync();
                _resolveWorker = null;
            }

            if (_connectionWorker != null)
            {
                _connectionWorker.CancelAsync();
                _connectionWorker = null;
            }

            try
            {
                SerialPortObject.Close();                
            }
            catch (Exception ex) { }
            SerialPortObject = null;
        }

        public void SetPort(string portName, int baudRate = 115200, StopBits stopBits = StopBits.One, Parity parity = Parity.None, int readTimeout = -1, int writeTimeout = -1)
        {
            SerialPortObject = new SerialPort();
            SerialPortObject.DataReceived += SerialPortObject_DataReceived;
            SerialPortObject.ErrorReceived += SerialPortObject_ErrorReceived;
            SerialPortObject.PortName = portName;
            SerialPortObject.BaudRate = baudRate;
            SerialPortObject.StopBits = stopBits;
            SerialPortObject.Parity = parity;
            SerialPortObject.ReadTimeout = readTimeout;
            SerialPortObject.WriteTimeout = writeTimeout;
        }

        void SerialPortObject_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (IsConnected)
                {
                    //可以接收
                    QueueObject qo = new QueueObject();
                    qo.Buffer = new byte[SerialPortObject.BytesToRead];
                    qo.DataLength = SerialPortObject.Read(qo.Buffer, 0, qo.Buffer.Length);
                    if (qo.DataLength > 0 && qo.Buffer.Length >= 1)
                    {
                        lock (lockObject)
                        {
                            BufferStream.AddRange(qo.Buffer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString(), ex);
            }
        }

        void SerialPortObject_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            logger.Error(e.EventType);
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
        public abstract byte[] Resolve();

        /// <summary>
        /// 在串口缓冲区实例中的第一个匹配项的索引。
        /// </summary>
        /// <param name="searchBytes">要查找的 System.Byte[]。</param>
        /// <returns>如果找到该字节数组，则为 searchBytes 的索引位置；如果未找到该字节数组，则为 -1。如果 searchBytes 为 null 或者长度为0，则返回值为 -1。</returns>
        public int SearchInBuffer(byte[] searchBytes)
        {
            if (SerialPortInputObject == null) { return -1; }
            if (SerialPortInputObject.BufferStream == null) { return -1; }
            if (searchBytes == null) { return -1; }
            if (SerialPortInputObject.BufferStream.Count == 0) { return -1; }
            if (searchBytes.Length == 0) { return -1; }
            if (SerialPortInputObject.BufferStream.Count < searchBytes.Length) { return -1; }
            for (int i = 0; i < SerialPortInputObject.BufferStream.Count - searchBytes.Length; i++)
            {
                if (SerialPortInputObject.BufferStream[i] == searchBytes[0])
                {
                    if (searchBytes.Length == 1) { return i; }
                    bool flag = true;
                    for (int j = 1; j < searchBytes.Length; j++)
                    {
                        if (SerialPortInputObject.BufferStream[i + j] != searchBytes[j])
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
}