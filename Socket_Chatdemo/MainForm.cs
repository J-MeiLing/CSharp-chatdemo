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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Socket_Chatdemo
{
    public partial class MainForm : Form
    {
        List<Socket> ClientProxSocketList = new List<Socket>();
        public MainForm()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //1.创建Socket
            //使用指定的地址族、套接字类型和协议初始化 Socket 类的新实例
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            //2.绑定端口IP
            socket.Bind(new IPEndPoint(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text)));

            //3.开启监听
            socket.Listen(10); //队列只能处理一个连接，最大允许等待10个连接，超过个数返回错误消息 

            //4.开始接受客户端的连接
            ThreadPool.QueueUserWorkItem(new WaitCallback
                (this.AcceptClientConnect),socket); 
        }
        public void AcceptClientConnect(object socket)
        {
            var serverSocket = socket as Socket;
            this.AppendTextToTxtLog("服务器端开始接受客户端的连接。");
            while (true)
            {
                var proxSocket = serverSocket.Accept(); //返回一个代理socket对象，这需要不停的连接否则会堵塞，所以需要放在一个while循环内
                this.AppendTextToTxtLog(string.Format("客户端:{0}连接上了", proxSocket.RemoteEndPoint.ToString()));
                ClientProxSocketList.Add(proxSocket);
                //不停地接受当前连接地客户端发来的消息
                // proxSocket.Receive(); 同样会阻塞，放在线程池内
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.Receivedata),proxSocket);
            }
            
        }

        //接收客户端的消息
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
                }catch(Exception e)
                {
                    AppendTextToTxtLog(String.Format("客户端：{0}异常退出！", proxSocket.RemoteEndPoint.ToString()));
                    ClientProxSocketList.Remove(proxSocket);
                    StopContent(proxSocket);                  
                    return;//结束当前接受客户端数据的异步线程
                }
                
                
                if(len <= 0)
                {
                    //客户端正常退出
                    AppendTextToTxtLog(String.Format("客户端：{0}正常退出！", proxSocket.RemoteEndPoint.ToString()));
                    ClientProxSocketList.Remove(proxSocket);
                    StopContent(proxSocket);
                    return;//结束当前接受客户端数据的异步线程
                }
                //把接收到的数据放在文本框上
               string str =  Encoding.Default.GetString(data, 0, len);
                AppendTextToTxtLog(String.Format("接收到客户端：{0}的消息是：{1}",proxSocket.RemoteEndPoint.ToString(),str));

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
                // }), txt);
            }
            else
            {
                this.txtLog.Text = string.Format("{0}\r\n{1}", txt, txtLog.Text);
            }
            
        }
        public void StopContent(Socket socket)
        {
            try
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }

        //发送消息
        private void btnSendMsg_Click(object sender, EventArgs e)
        {
            foreach(var proxSocket in ClientProxSocketList)
            {
                if (proxSocket.Connected) //确认仍在连接
                {
                    byte[] data = Encoding.Default.GetBytes(txtMsg.Text);
                    txtLog.Text = string.Format("发送给客户端的消息是：{0}\r\n{1}", txtMsg.Text, txtLog.Text);
                    proxSocket.Send(data,0,data.Length,SocketFlags.None);
                }
            }
        }
    }
}
