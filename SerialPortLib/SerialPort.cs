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
        /// 空闲队列
        /// </summary>
        protected ConcurrentQueue<ReceiveQueueObject> FreeReceiveQueues { get; set; }

        /// <summary>
        /// 等待解析队列
        /// </summary>
        protected ConcurrentQueue<ReceiveQueueObject> WaitResolveQueues { get; set; }

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
        /// 初始化缓冲区
        /// </summary>
        /// <param name="bufferQueueCount"></param>
        /// <param name="bufferSize"></param>
        public void SetBuffers(int bufferQueueCount, int bufferSize)
        {
            //检查初始值
            if (bufferQueueCount <= 0 || bufferSize <= 0)
            {
                bufferQueueCount = 60;
                bufferSize = 4096;
            }

            //生成队列对象
            FreeReceiveQueues = new ConcurrentQueue<ReceiveQueueObject>();
            WaitResolveQueues = new ConcurrentQueue<ReceiveQueueObject>();

            for (int kk = 0; kk < bufferQueueCount; kk++)
            {
                ReceiveQueueObject rqo = new ReceiveQueueObject();
                rqo.Buffer = new byte[bufferSize];
                FreeReceiveQueues.Enqueue(rqo);
            }
        }

        /// <summary>
        /// 连接
        /// </summary>
        public void Connect()
        {
            if (FreeReceiveQueues == null)
            {
                return;
            }

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
            ReceiveQueueObject queueObject = null;
            BackgroundWorker obj = (BackgroundWorker)sender;
            while (!obj.CancellationPending)
            {
                try
                {
                    if (IsConnected)
                    {
                        //可以接收
                        if (WaitResolveQueues.Count > 0)
                        {
                            WaitResolveQueues.TryDequeue(out queueObject);

                            if (queueObject != null)
                            {
                                //尝试解析数据
                                byte[] msg = new byte[queueObject.DataLength];
                                if (queueObject.DataLength > 0)
                                {
                                    Array.Copy(queueObject.Buffer, 0, msg, 0, msg.Length);
                                }
                                
                                //将数据还给Free队列
                                queueObject.Offset = 0;
                                queueObject.DataLength = 0;
                                FreeReceiveQueues.Enqueue(queueObject);

                                try
                                {
                                    //投递消息
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
            ReceiveQueueObject queueObject = null;
            BackgroundWorker obj = (BackgroundWorker)sender;
            while (!obj.CancellationPending)
            {
                try
                {
                    if (IsConnected)
                    {
                        //可以接收

                        if (FreeReceiveQueues.Count > 0)
                        {
                            FreeReceiveQueues.TryDequeue(out queueObject);

                            if (queueObject != null)
                            {
                                queueObject.DataLength = SerialPortObject.Read(queueObject.Buffer, 0, queueObject.Buffer.Length);
                            }
                        }
                        else
                        {
                            Thread.Sleep(2);
                            throw new Exception("对不起，队列已满!");
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

                if (queueObject != null)
                {
                    if (queueObject.DataLength > 0)
                    {
                        //有数据
                        WaitResolveQueues.Enqueue(queueObject);
                    }
                    else
                    {
                        //没有数据
                        FreeReceiveQueues.Enqueue(queueObject);
                    }

                    queueObject = null;
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

    /// <summary>
    /// 接收队列对象
    /// </summary>
    public class ReceiveQueueObject
    {
        /// <summary>
        /// 缓冲区
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// 偏移
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// 数据大小
        /// </summary>
        public int DataLength { get; set; }
    }
}