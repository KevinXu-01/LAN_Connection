# LAN_Connection
This is a dynamic link library project that enables you to connect to another computer (or your own computer) inside the same LAN network via Socket support.

Here are some tips for using this:
1) Listen: Use this code block

        Thread ListenThread = new Thread(new ThreadStart(LAN_Connection.Lan_connection.Listen_Operation));
        ListenThread.Start();
   to start listening from another client's connection request.

2) Connect: Use this code block

        LAN_Connection.Lan_connection.Connect_Operation(*parameter*); //Note: Here, *parameter*(byte) is the target's IP address.
   to start connecting to another client that has started listening.

3) Send: Use this code block

        LAN_Connection.Lan_connection.SendMsg_Operation(*parameter1*, *parameter2*, *parameter3*); //Note: Here, *parameter1*(byte), *parameter1*(byte) and *parameter3*(string) are parameters whose meanings and values need to be defined by user.
        
   to send messages after a connection is established.

4) Receive: 
   Data is presented via gameEvent(of three parameters), whose meanings and values are also to be defined by user.
   
   Here, we present you with *received*(bool). Each time we receive a message, *received* will be set to be true, and we'll present you all the information you need with gameEvent; after you process these information, you need to set *received* to be false and detect when it becomes true again, so that next time a message is received, you can process it just in time.

5) Note: this code block

         IPAddress[] ip_list = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
         IPAddress myIP = ip_list[ip_list.Length - 1];
   can be used to get local IP address.
