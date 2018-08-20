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
    /// 串口通信类(仅包含重新连接线程)
    /// </summary>
    public class SerialPortInputSimple
    {
        private DataBufferObject _bufferStream = new DataBufferObject();
        /// <summary>
        /// 接收缓冲
        /// </summary>
        public DataBufferObject BufferStream
        {
            get { return _bufferStream; }
        }

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
                if (args.Data != null && args.Data.Buffer != null)
                {
                    logger.Debug(BitConverter.ToString(args.Data.Buffer));
                }
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
            if (MessageDataAdapterObject == null)
            {
                return;
            }
            if (SerialPortObject == null)
            {
                return;
            }

            //设置串口数据适配器
            MessageDataAdapterObject.SerialPortObject = this;
            MessageDataAdapterObject.BufferStream = BufferStream;

            //打开串口
            SerialPortObject.Open();

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

        /// <summary>
        /// 断开
        /// </summary>
        public void Disconnect()
        {
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

            _bufferStream = new DataBufferObject();
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
                        BufferStream.AddRangeWithLock(qo.Buffer);
                        IMessageEntity msg = MessageDataAdapterObject.Resolve();
                        if (msg != null)
                        {
                            OnMessageReceived(new MessageReceivedEventArgs(msg));
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
}