using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using NLog;
using System.Text;
using System.Collections.Concurrent;
using System.ComponentModel;

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
        /// <summary>
        /// 接收队列
        /// </summary>
        protected ConcurrentQueue<QueueObject> ReceiveQueues { get; set; }

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
        /// 连接状态事件
        /// </summary>
        public event ConnectionStatusChangedEventHandler ConnectionStatusChanged;

        /// <summary>
        /// 数据接收事件
        /// </summary>
        public event MessageReceivedEventHandler MessageReceived;

        protected BackgroundWorker _receiveWorker = null;
        protected BackgroundWorker _resolveWorker = null;

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
            ReceiveQueues = new ConcurrentQueue<QueueObject>();

            if (_receiveWorker != null)
            {
                return;
            }

            //打开串口
            SerialPortObject.Open();

            //创建接收线程
            _receiveWorker = new BackgroundWorker();
            _receiveWorker.WorkerSupportsCancellation = true;
            _receiveWorker.DoWork += _receiveWorker_DoWork;
            _receiveWorker.RunWorkerAsync();

            //创建解析线程
            _resolveWorker = new BackgroundWorker();
            _resolveWorker.WorkerSupportsCancellation = true;
            _resolveWorker.DoWork += _resolveWorker_DoWork;
            _resolveWorker.RunWorkerAsync();
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
                        if (ReceiveQueues.Count > 0)
                        {
                            QueueObject buffer = null;
                            ReceiveQueues.TryDequeue(out buffer);

                            if (buffer != null)
                            {
                                try
                                {
                                    //投递消息
                                    byte[] msg = new byte[buffer.DataLength];
                                    Buffer.BlockCopy(buffer.Buffer, 0, msg, 0, buffer.DataLength);

                                    OnMessageReceived(new MessageReceivedEventArgs(msg));
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex.ToString(), ex);
                                }
                            }
                        }
                        else
                        {
                            Thread.Sleep(2);
                        }
                    }
                    else
                    {
                        Thread.Sleep(2);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString(), ex);

                    Thread.Sleep(2);
                }
            }
        }

        void _receiveWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker obj = (BackgroundWorker)sender;
            while (!obj.CancellationPending)
            {
                try
                {
                    if (IsConnected)
                    {
                        //可以接收
                        QueueObject qo = new QueueObject();
                        qo.Buffer = new byte[SerialPortObject.ReadBufferSize + 1];
                        qo.DataLength = SerialPortObject.Read(qo.Buffer, 0, qo.Buffer.Length);
                        if (qo.DataLength > 0)
                        {
                            ReceiveQueues.Enqueue(qo);
                        }
                    }
                    else
                    {
                        Thread.Sleep(2);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString(), ex);

                    Thread.Sleep(2);
                }
            }
        }

        /// <summary>
        /// 断开
        /// </summary>
        public void Disconnect()
        {
            _receiveWorker.CancelAsync();
            _resolveWorker.CancelAsync();
            _receiveWorker = null;
            _resolveWorker = null;

            try
            {
                SerialPortObject.Close();
            }
            catch (Exception ex) { }
        }

        public void SetPort(string portName, int baudRate = 115200, StopBits stopBits = StopBits.One, Parity parity = Parity.None, int readTimeout = -1, int writeTimeout = -1)
        {
            SerialPortObject = new SerialPort();
            SerialPortObject.ErrorReceived += SerialPortObject_ErrorReceived;
            SerialPortObject.PortName = portName;
            SerialPortObject.BaudRate = baudRate;
            SerialPortObject.StopBits = stopBits;
            SerialPortObject.Parity = parity;
            SerialPortObject.ReadTimeout = readTimeout;
            SerialPortObject.WriteTimeout = writeTimeout;
        }

        void SerialPortObject_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            logger.Error(e.EventType);
        }
    }

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
}