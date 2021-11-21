using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SocketClient
{
    
    public partial class ClientForm : Form
    {
        public Socket ClientSocket { get; set; }
        public ClientForm()
        {
            InitializeComponent();
        }

        private void btnContact_Click(object sender, EventArgs e)
        {
            //客户端连接服务器端
            //1.创建Socket
            //使用指定的地址族、套接字类型和协议初始化 Socket 类的新实例
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ClientSocket = socket;
            //2连接
            try
            {
                socket.Connect(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text));
            }
            catch(Exception ex)
            {
                //Thread.Sleep(500); 
                //btnContact_Click(this,e);
                MessageBox.Show("连接失败，重新连接！");
                return;
            }
            //3.发送消息，接收消息
            Thread thread = new Thread(new ParameterizedThreadStart(Receivedata));
            thread.IsBackground = true;
            thread.Start(ClientSocket);

            
        }
        public void Receivedata(object socket)
        {
            var proxSocket = socket as Socket;
            byte[] data = new byte[1024 * 1024];
            while (true)
            {
                int len = 0;
                try
                {
                    len = proxSocket.Receive(data, 0, data.Length, SocketFlags.None);
                }
                catch (Exception e)
                {
                    AppendTextToTxtLog(String.Format("服务器端：{0}异常退出！", proxSocket.RemoteEndPoint.ToString()));
                    StopContent();
                    return;//结束
                }


                if (len <= 0)
                {
                    //客户端正常退出
                    AppendTextToTxtLog(String.Format("服务器端：{0}正常退出！", proxSocket.RemoteEndPoint.ToString()));
                    StopContent();
                    return;//结束
                }
                //把接收到的数据放在文本框上
                string str = Encoding.Default.GetString(data, 0, len);
                AppendTextToTxtLog(String.Format("接收到服务端：{0}的消息是：{1}", proxSocket.RemoteEndPoint.ToString(), str));

            }
        }
        //关闭客户端连接
        public void StopContent()
        {
            try
            {
                if (ClientSocket.Connected)
                {
                    ClientSocket.Shutdown(SocketShutdown.Both);
                    ClientSocket.Close();
                }
            }catch (Exception ex)
            {

            }
        }
        //往日志的文本框上追加数据
        public void AppendTextToTxtLog(string txt)
        {
            if (txtLog.InvokeRequired) //考虑跨线程
            {
                txtLog.BeginInvoke(new Action<string>(s =>
                {
                    txtLog.Text = string.Format("{0}\r\n{1}", s, txtLog.Text);
                }), txt);

                //txtLog.Invoke(new Action<string>(s =>
                //{
                //    txtLog.Text = string.Format("{0}\r\n{1}", s, txtLog.Text);
                //}), txt);
            }
            else
            {
                this.txtLog.Text = string.Format("{0}\r\n{1}", txt, txtLog.Text);
            }

        }
        private void btnSendMsg_Click(object sender, EventArgs e)
        {
            if (ClientSocket.Connected)
            {
                byte[] data = Encoding.Default.GetBytes(txtMsg.Text);
                
                txtLog.Text = string.Format("发送给服务端的消息是：{0}\r\n{1}",txtMsg.Text, txtLog.Text);
                ClientSocket.Send(data, 0, data.Length, SocketFlags.None);
            }
        }
    }

    internal class Tread
    {
    }
}
