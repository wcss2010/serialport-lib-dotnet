using SerialPortLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using WebSocketSharp;

namespace Test.Serial
{
    public partial class Form1 : Form
    {
        string conStr = "ws://10.1.180.54:8090/talking/offline/v1/feifei";
        MemoryStream ms = null;
        private SerialPortInput serialPort;
        WebSocket connection = null;

        public Form1()
        {
            InitializeComponent();

            connection = new WebSocket(conStr);
            connection.EmitOnPing = true;
            connection.OnMessage += connection_OnMessage;
            connection.OnError += connection_OnError;
            connection.OnClose += connection_OnClose;
            connection.Connect();
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
            System.Console.WriteLine("WS:" + e.Data);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            serialPort = new SerialPortInput();
            serialPort.ConnectionStatusChanged += SerialPort_ConnectionStatusChanged;
            serialPort.MessageReceived += SerialPort_MessageReceived;

            serialPort.SetPort("COM7", 921600);
            serialPort.Connect();

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

        void SerialPort_MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            if (IndexOf(args.Data, new byte[] { 0xFD, 0x00, 0x80, 0x00 }) >= 0)
            {
                if (ms != null)
                {
                    ms.Dispose();
                    ms = null;
                }

                ms = new MemoryStream();
            }

            if (ms != null)
            {
                ms.Write(args.Data, 0, args.Data.Length);
            }

            if (ms != null)
            {
                byte[] content = ms.ToArray();
                int endIndex = IndexOf(content, new byte[] { 0xFE, 0x7E, 0xFF, 0x7F });
                if (endIndex >= 0)
                {
                    byte[] result = new byte[endIndex + 4];
                    Array.Copy(content, 0, result, 0, result.Length);

                    if (ms != null)
                    {
                        ms.Dispose();
                        ms = null;
                    }

                    if (content.Length > result.Length)
                    {
                        byte[] elseData = new byte[content.Length - result.Length];
                        Array.Copy(content, result.Length, elseData, 0, elseData.Length);

                        ms = new MemoryStream();
                        ms.Write(elseData, 0, elseData.Length);
                    }

                    //需要发送数据出去
                    ProcessVoiceData(result);
                }
            }
        }

        private void ProcessVoiceData(byte[] voiceData)
        {
            System.Console.WriteLine("语音数据：" + BitConverter.ToString(voiceData));

            try
            {
                connection.Send(BitConverter.ToString(voiceData).Replace("-", " "));                
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
            serialPort.Disconnect();
            connection.Close();
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
    }
}