using AIUISerials;
using SerialPortLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WebSocketSharp;

namespace Test.Serial
{
    public partial class Form1 : Form
    {
        public static AIUIConnection AIUIConnectionObj = null;

        string conStr = "ws://10.1.180.54:8090/talking/offline/v1/feifei";
        MemoryStream ms = null;
        private SerialPortInput serialPort;
        WebSocket connection = null;
        private string[] stringTeam;
        private int sendIndex;
        private int sendCount;
        private int byteCount;
        private int headerCount;
        private int endCount;

        public Form1()
        {
            InitializeComponent();

            connection = new WebSocket(conStr);
            connection.EmitOnPing = true;
            connection.OnMessage += connection_OnMessage;
            connection.OnError += connection_OnError;
            connection.OnClose += connection_OnClose;
            //connection.Connect();
        }

        void connection_OnClose(object sender, CloseEventArgs e)
        {
            System.Console.WriteLine("WS:" + e.Reason);
        }

        void connection_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            System.Console.WriteLine("WS:" + e.Exception);
        }

        void connection_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.IsText)
            {
                System.Console.WriteLine("WS:" + e.Data);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AIUIConnectionObj = new AIUIConnection("COM4");
            AIUIConnectionObj.SerialPort.SerialPortObject.ReceivedBytesThreshold = 100;
            AIUIConnectionObj.SerialPort.SerialPortObject.ReadBufferSize = 50 * 1024 * 10;
            AIUIConnectionObj.AIUIConnectionReceivedEvent += AIUIConnectionObj_AIUIConnectionReceivedEvent;
            AIUIConnectionObj.SerialPort.Connect();

            serialPort = new SerialPortInput();
            //serialPort.ConnectionStatusChanged += SerialPort_ConnectionStatusChanged;
            //serialPort.MessageReceived += SerialPort_MessageReceived;
            //serialPort.MessageDataAdapterObject = new XFOnlineMessageDataAdapter();

            //serialPort.SetPort("COM4", 115200, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None, -1, -1);
            //serialPort.EnabledPrintReceiveLog = false;
            //serialPort.Connect();

            //byte[] content = GetCommand(0, 20);
            //StringBuilder sb = new StringBuilder();
            //sb.Append("[");
            //foreach (byte b in content)
            //{
            //    sb.Append("0x").Append(Convert.ToString(b, 16).Length == 1 ? "0" : "").Append(Convert.ToString(b, 16)).Append(",");
            //}
            //sb.Append("]");
            //System.Console.WriteLine(sb.ToString());

            //content = GetCommand(5, 30);
            //sb = new StringBuilder();
            //sb.Append("[");
            //foreach (byte b in content)
            //{
            //    sb.Append("0x").Append(Convert.ToString(b, 16).Length == 1 ? "0" : "").Append(Convert.ToString(b, 16)).Append(",");
            //}
            //sb.Append("]");
            //System.Console.WriteLine(sb.ToString());

            //content = GetCommand(8, 40);
            //sb = new StringBuilder();
            //sb.Append("[");
            //foreach (byte b in content)
            //{
            //    sb.Append("0x").Append(Convert.ToString(b, 16).Length == 1 ? "0" : "").Append(Convert.ToString(b, 16)).Append(",");
            //}
            //sb.Append("]");
            //System.Console.WriteLine(sb.ToString());

            //content = GetCommand(15, 50);
            //sb = new StringBuilder();
            //sb.Append("[");
            //foreach (byte b in content)
            //{
            //    sb.Append("0x").Append(Convert.ToString(b, 16).Length == 1 ? "0" : "").Append(Convert.ToString(b, 16)).Append(",");
            //}
            //sb.Append("]");
            //System.Console.WriteLine(sb.ToString());
        }

        void AIUIConnectionObj_AIUIConnectionReceivedEvent(object sender, AIUIConnectionReceivedEventArgs args)
        {
            System.Console.WriteLine("Recv:" + args.Json);
        }

        void SerialPort_MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            HandleVoiceData(args.Data);
        }

        private void HandleVoiceData(byte[] data)
        {
            byteCount += data.Length;
            //int startIndex = IndexOf(data, new byte[] { 0xFD, 0x00, 0x80, 0x00 });
            //if (startIndex >= 0)
            //{
            //    headerCount++;
            //}
            //int endIndex = IndexOf(data, new byte[] { 0xFE, 0x7E, 0xFF, 0x7F });
            //if (endIndex >= 0)
            //{
            //    endCount++;
            //}

            if (IsHandleCreated)
            {
                try
                {
                    Invoke(new MethodInvoker(delegate()
                    {
                        label1.Text = "收到总字节数：" + byteCount + "\n,包头数：" + headerCount + "\n,包尾数：" + endCount;
                    }));
                }
                catch (Exception ex) { }
            }

            ProcessVoiceData(data);

            //startIndex = IndexOf(data, new byte[] { 0xFD, 0x00, 0x80, 0x00 });
            //if (startIndex >= 0)
            //{
            //    if (ms != null)
            //    {
            //        ms.Dispose();
            //        ms = null;
            //    }

            //    ms = new MemoryStream();
            //    ms.Write(data, startIndex, data.Length - startIndex);
            //}
            //else
            //{
            //    ms.Write(data, 0, data.Length);
            //}

            //if (ms != null)
            //{
            //    byte[] content = ms.ToArray();
            //    endIndex = IndexOf(content, new byte[] { 0xFE, 0x7E, 0xFF, 0x7F });
            //    if (endIndex >= 0)
            //    {
            //        byte[] result = new byte[endIndex + 4];
            //        Array.Copy(content, 0, result, 0, result.Length);

            //        if (ms != null)
            //        {
            //            ms.Dispose();
            //            ms = null;
            //        }

            //        if (content.Length > result.Length)
            //        {
            //            byte[] elseData = new byte[content.Length - result.Length];
            //            Array.Copy(content, result.Length, elseData, 0, elseData.Length);

            //            ms = new MemoryStream();
            //            ms.Write(elseData, 0, elseData.Length);
            //        }

            //        //需要发送数据出去
            //        ProcessVoiceData(result);
            //    }
            //}
        }

        private void ProcessVoiceData(byte[] voiceData)
        {
            //System.Console.WriteLine("语音数据：" + BitConverter.ToString(voiceData));

            try
            {
                //connection.Send(BitConverter.ToString(voiceData).Replace("-", " "));
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }

        void SerialPort_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
        {
            Console.WriteLine("Serial port connection status = {0}", args.Connected);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            byte[] cmd = GetCommand(0, 20);

            serialPort.SendMessage(cmd);
        }

        private byte[] GetCommand(int motorIndex, short value)
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                //固定头部
                ms.WriteByte(0x01);
                ms.WriteByte(0x06);

                //电机序号
                byte ba1 = 0x00;
                byte bb2 = 0x00;
                if (motorIndex >= 0 && motorIndex < 16)
                {
                    if (motorIndex >= 8)
                    {
                        //b1
                        switch (motorIndex)
                        {
                            case 8:
                                ba1 = BitHelper.SetBit(true, ba1, 7);
                                break;
                            case 9:
                                ba1 = BitHelper.SetBit(true, ba1, 6);
                                break;
                            case 10:
                                ba1 = BitHelper.SetBit(true, ba1, 5);
                                break;
                            case 11:
                                ba1 = BitHelper.SetBit(true, ba1, 4);
                                break;
                            case 12:
                                ba1 = BitHelper.SetBit(true, ba1, 3);
                                break;
                            case 13:
                                ba1 = BitHelper.SetBit(true, ba1, 2);
                                break;
                            case 14:
                                ba1 = BitHelper.SetBit(true, ba1, 1);
                                break;
                            case 15:
                                ba1 = BitHelper.SetBit(true, ba1, 0);
                                break;
                        }
                    }
                    else
                    {
                        //b2;
                        switch (motorIndex)
                        {
                            case 0:
                                bb2 = BitHelper.SetBit(true, bb2, 7);
                                break;
                            case 1:
                                bb2 = BitHelper.SetBit(true, bb2, 6);
                                break;
                            case 2:
                                bb2 = BitHelper.SetBit(true, bb2, 5);
                                break;
                            case 3:
                                bb2 = BitHelper.SetBit(true, bb2, 4);
                                break;
                            case 4:
                                bb2 = BitHelper.SetBit(true, bb2, 3);
                                break;
                            case 5:
                                bb2 = BitHelper.SetBit(true, bb2, 2);
                                break;
                            case 6:
                                bb2 = BitHelper.SetBit(true, bb2, 1);
                                break;
                            case 7:
                                bb2 = BitHelper.SetBit(true, bb2, 0);
                                break;
                        }
                    }
                }
                ms.WriteByte(ba1);
                ms.WriteByte(bb2);

                //值
                byte[] vBytes = BitConverter.GetBytes(value);
                Array.Reverse(vBytes);
                ms.Write(vBytes, 0, 2);

                //生成CRC
                byte[] crcs = CRC.CRC16(ms.ToArray());
                Array.Reverse(crcs);
                ms.Write(crcs, 0, 2);
            }
            finally
            {
                ms.Dispose();
            }
            return ms.ToArray();
        }

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            AIUIConnectionObj.SerialPort.Disconnect();
            serialPort.Disconnect();
            connection.Close();
            //_worker.CancelAsync();
        }

        /// <summary>
        /// 报告指定的 System.Byte[] 在此实例中的第一个匹配项的索引。
        /// </summary>
        /// <param name="srcBytes">被执行查找的 System.Byte[]。</param>
        /// <param name="searchBytes">要查找的 System.Byte[]。</param>
        /// <returns>如果找到该字节数组，则为 searchBytes 的索引位置；如果未找到该字节数组，则为 -1。如果 searchBytes 为 null 或者长度为0，则返回值为 -1。</returns>
        public int IndexOf(byte[] srcBytes, byte[] searchBytes)
        {
            if (srcBytes == null) { return -1; }
            if (searchBytes == null) { return -1; }
            if (srcBytes.Length == 0) { return -1; }
            if (searchBytes.Length == 0) { return -1; }
            if (srcBytes.Length < searchBytes.Length) { return -1; }
            for (int i = 0; i < srcBytes.Length - searchBytes.Length; i++)
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

        private void button2_Click(object sender, EventArgs e)
        {
            string temp = File.ReadAllText(@"D:\MyCode\16ktest.txt");
            stringTeam = temp.Split(new string[] { "FE 7E FF 7F" }, StringSplitOptions.None);
            sendIndex = 0;
            sendCount = 0;

            timer1.Enabled = !timer1.Enabled;
            if (timer1.Enabled)
            {
                button2.BackColor = Color.Red;
            }
            else
            {
                button2.BackColor = Color.LightGray;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (sendIndex >= stringTeam.Length)
            {
                sendIndex = 0;
            }

            sendCount++;
            connection.Send(stringTeam[sendIndex].Trim() + " FE 7E FF 7F");
            sendIndex++;

            this.Text = "Count:" + sendCount;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            byteCount = 0;
            headerCount = 0;
            endCount = 0;
        }
    }
}