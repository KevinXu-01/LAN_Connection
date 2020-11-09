using System;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace LAN_connection
{
    public class Lan_connection
    {
        public static bool received;
        private static ControlInternet CI = new ControlInternet(); //实例化的联网对战类
        public static GameEvent gameEvent = new GameEvent("", "", "");//实例化的gameEvent
        /// <summary>
        /// 连接函数，用作客户端
        /// </summary>
        /// <param name="ip">IP地址</param>
        public static bool Connect_Operation(string ip)
        {
            try
            {
                CI.OnReceiveMsg += new GameEventHandler(ManageGameEvent);
                CI.Connect(ip);
                Thread ReConnectThread = new Thread(new ParameterizedThreadStart(ReConnect));
                ReConnectThread.Start(ip);
                while (true)
                {
                    if (isConnected(CI.getRec()) && isConnected(CI.getSend()))
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        private static bool isConnected(Socket socket)
        {
            return !((socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0)) || !socket.Connected);
        }

        private static void ReConnect(object obj)
        {
            string ip = (string)obj;
            if (!(isConnected(CI.getRec()) && isConnected(CI.getSend())))
            {
                CI.Close();
                CI.Connect(ip);
            }
        }
        /// <summary>
        /// 监听函数，用作服务器；异常处理是连接失败或使用了与IPv4协议不匹配的IP地址等等
        /// </summary>
        public static void Listen_Operation()
        {
            try
            {
                Thread.CurrentThread.IsBackground = true;
                Control.CheckForIllegalCrossThreadCalls = false;
                CI.Listen();
                CI.OnReceiveMsg += new GameEventHandler(ManageGameEvent);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static bool SendMsg_Operation(byte parameter1, byte parameter2, string parameter3)
        {
            try
            {
                CI.SendMsg(parameter1, parameter2, parameter3);
                return true;
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }
        }

        /// <summary>
        /// 处理接收信息事件的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">自定义棋子事件（即接收的信息）</param>
        private static void ManageGameEvent(object sender, GameEvent e, bool receive)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            gameEvent = e;
            received = receive;
        }
    }
}

/// <summary>
/// 自定义一个事件，该事件继承自预设EventArgs
/// </summary>
public class GameEvent : EventArgs
{
    public string Iclass;
    public string content;
    public string flag;
    public GameEvent(string _class, string _flag, string _content)
    {
        Iclass = _class;
        content = _content;
        flag = _flag;
    }
}

/// <summary>
/// 委托，用于处理GameEvent
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>
public delegate void GameEventHandler(object sender, GameEvent e, bool receive);

/// <summary>
/// 联网对战接口
/// </summary>
public interface ISocket
{
    void Listen();
    void Connect(string ipStr);
    void SendMsg(byte @class, byte flag, string content);
    void ReceiveMsg(object obj);
    void Close();
}

/// <summary>
/// 联网对战类，包含Listen、Connect、SendMsg、ReceiveMsg和Close函数
/// </summary>
public class ControlInternet : ISocket
{
    private Socket skRec = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private IPEndPoint ipeRec;
    private Socket skSend = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private IPEndPoint ipeSend;
    public event GameEventHandler OnReceiveMsg;
    public Socket getRec()
    {
        return skRec;
    }
    public Socket getSend()
    {
        return skSend;
    }
    public ControlInternet()
    {

    }

    public void Listen()
    {
        try
        {
            IPAddress[] ip_list = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            IPAddress myIp = ip_list[ip_list.Length - 1];//获取IP地址
            skRec.Bind(new IPEndPoint(IPAddress.Parse(myIp.ToString()), 8880));
            skRec.Listen(0);
            Socket clientRec = skRec.Accept();
            Thread receiveMsgThread = new Thread(new ParameterizedThreadStart(ReceiveMsg));
            receiveMsgThread.Start(clientRec);
            skSend.Bind(new IPEndPoint(IPAddress.Parse(myIp.ToString()), 8881));
            skSend.Listen(0);
            skSend = skSend.Accept();
        }
        catch (Exception ex)
        {
            MessageBox.Show("listen:" + ex.Message);
        }
    }

    public void Connect(string ipStr)
    {
        ipeSend = new IPEndPoint(IPAddress.Parse(ipStr), 8880);
        skSend.Connect(ipeSend);
        ipeRec = new IPEndPoint(IPAddress.Parse(ipStr), 8881);
        skRec.Connect(ipeRec);
        Thread ReceiveMsgThread = new Thread(new ParameterizedThreadStart(ReceiveMsg));
        ReceiveMsgThread.Start(skRec);
    }

    public void SendMsg(byte @class, byte flag, string content)
    {
        try
        {
            byte[] tmpBytes = Encoding.Default.GetBytes(content);
            MessageControl.Message msg = new MessageControl.Message(@class, flag, tmpBytes);
            byte[] sendBytes = msg.ToBytes();
            skSend.Send(sendBytes);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void ReceiveMsg(object obj)
    {
        Thread.CurrentThread.IsBackground = true;
        Socket clientRec = (Socket)obj;
        MessageControl.Message msg = new MessageControl.Message();
        MessageControl.MessageStream mst = new MessageControl.MessageStream();
        int revb;
        try
        {
            while (clientRec.Connected)
            {
                byte[] recBytes = new byte[512];
                revb = clientRec.Receive(recBytes);
                mst.Write(recBytes, 0, revb);
                if (mst.Read(out msg))
                {
                    bool receive = true;
                    OnReceiveMsg(this, new GameEvent(msg.Class.ToString(), msg.Flag.ToString(), Encoding.Default.GetString(msg.Content)), receive);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    public void Close()
    {
        skRec.Close();
        skSend.Close();
    }

    ~ControlInternet()
    {
        skRec.Close();
        skSend.Close();
    }
}
