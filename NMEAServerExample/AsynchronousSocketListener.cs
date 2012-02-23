using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NMEAServer
{
	public class AsynchronousSocketListener
	{
		// Server variables
		public static ManualResetEvent allDone = new ManualResetEvent(false);
		private const int portNum = 10116;

        private static string response;

		public AsynchronousSocketListener()
		{
		}

		public static void StartListening()
		{
			// Data buffer for incoming data.
			byte[] bytes = new Byte[1024];

			// Establish the local endpoint for the socket.
			// The DNS name of the computer
			// running the listener is "host.contoso.com".
			//IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
			IPHostEntry ipHostInfo = new IPHostEntry();
			ipHostInfo.AddressList = new IPAddress[] { new IPAddress(new Byte[] { 127, 0, 0, 1 }) };
			IPAddress ipAddress = ipHostInfo.AddressList[0];
			IPEndPoint localEndPoint = new IPEndPoint(ipAddress, portNum);
			// Create a TCP/IP socket.
			Socket listener = new Socket(AddressFamily.InterNetwork,
				SocketType.Stream, ProtocolType.Tcp);

            response = "Message from server : 0";

			// Bind the socket to the local endpoint and listen for incoming connections.
			try
			{
				listener.Bind(localEndPoint);
				listener.Listen(100);

				while (true)
				{
					// Set the event to nonsignaled state.
					allDone.Reset();

					// Start an asynchronous socket to listen for connections.
					Console.WriteLine("Waiting for a connection...");
					listener.BeginAccept(
						new AsyncCallback(ServerAcceptCallback),
						listener);

					// Wait until a connection is made before continuing.
					allDone.WaitOne();
				}

			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			Console.WriteLine("\nPress ENTER to continue...");
			Console.Read();

		}

		public static void ServerAcceptCallback(IAsyncResult ar)
		{
			// Signal the main thread to continue.
			allDone.Set();

			// Get the socket that handles the client request.
			Socket listener = (Socket)ar.AsyncState;
			Socket handler = listener.EndAccept(ar);

			// Create the state object.
			StateObject state = new StateObject();
			state.workSocket = handler;
			handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
				new AsyncCallback(ServerReadCallback), state);
		}

		public static void ServerReadCallback(IAsyncResult ar)
		{
			String content = String.Empty;

			// Retrieve the state object and the handler socket
			// from the asynchronous state object.
			StateObject state = (StateObject)ar.AsyncState;
			Socket handler = state.workSocket;

			// Read data from the client socket. 
			int bytesRead = handler.EndReceive(ar);

			if (bytesRead > 0)
			{
				// There  might be more data, so store the data received so far.
				state.sb.Append(Encoding.ASCII.GetString(
					state.buffer, 0, bytesRead));

				// Check for end-of-file tag. If it is not there, read 
				// more data.
				content = state.sb.ToString();
				if (true)
				{
					// All the data has been read from the 
					// client. Display it on the console.
					Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
						content.Length, content);
					// Echo the data back to the client.
						ServerSend(handler, response);
				}
				//else
				//{
				//    // Not all data received. Get more.
				//    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
				//    new AsyncCallback(ReadCallback), state);
				//}
			}
		}

		private static void ServerSend(Socket handler, String data)
		{
			// Convert the string data to byte data using ASCII encoding.
			byte[] byteData = Encoding.ASCII.GetBytes(data);

			// Begin sending the data to the remote device.
			handler.BeginSend(byteData, 0, byteData.Length, 0,
				new AsyncCallback(ServerSendCallback), handler);
		}

		private static void ServerSendCallback(IAsyncResult ar)
		{
			try
			{
				// Retrieve the socket from the state object.
				Socket handler = (Socket)ar.AsyncState;

				// Complete sending the data to the remote device.
				int bytesSent = handler.EndSend(ar);
				Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                int counter;
                if (int.TryParse(response.Substring(response.LastIndexOf(':') + 1), out counter))
                {
                    counter++;

                    response = response.Substring(0, response.LastIndexOf(':')) + counter.ToString();
                }

				handler.Shutdown(SocketShutdown.Both);
				handler.Close();

			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}



        //// asynchronous methods for feeds
        //private static void FeedConnectCallback(IAsyncResult ar)
        //{
        //    try
        //    {
        //        // Retrieve the socket from the state object.
        //        Socket client = (Socket)ar.AsyncState;

        //        // Complete the connection.
        //        client.EndConnect(ar);

        //        Console.WriteLine("Socket connected to {0}",
        //            client.RemoteEndPoint.ToString());

        //        // Signal that the connection has been made.
        //        feedConnectDone.Set();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.ToString());
        //    }
        //}

        //private static void FeedReceive(Socket client)
        //{
        //    try
        //    {
        //        // Create the state object.
        //        StateObject state = new StateObject();
        //        state.workSocket = client;

        //        // Begin receiving the data from the remote device.
        //        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
        //            new AsyncCallback(FeedReceiveCallback), state);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.ToString());
        //    }
        //}

        //private static void FeedReceiveCallback(IAsyncResult ar)
        //{
        //    try
        //    {
        //        // Retrieve the state object and the client socket 
        //        // from the asynchronous state object.
        //        StateObject state = (StateObject)ar.AsyncState;
        //        Socket client = state.workSocket;

        //        // Read data from the remote device.
        //        int bytesRead = client.EndReceive(ar);

        //        if (bytesRead > 0)
        //        {
        //            // There might be more data, so store the data received so far.
        //            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

        //            // Get the rest of the data.
        //            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
        //                new AsyncCallback(FeedReceiveCallback), state);
        //        }
        //        else
        //        {
        //            // All the data has arrived; put it in response.
        //            if (state.sb.Length > 1)
        //            {
        //                feedResponse = state.sb.ToString();
        //            }
        //            // Signal that all bytes have been received.
        //            feedReceiveDone.Set();
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.ToString());
        //    }
        //}

        //private static void FeedSend(Socket client, String data)
        //{
        //    // Convert the string data to byte data using ASCII encoding.
        //    byte[] byteData = Encoding.ASCII.GetBytes(data);

        //    // Begin sending the data to the remote device.
        //    client.BeginSend(byteData, 0, byteData.Length, 0,
        //        new AsyncCallback(FeedSendCallback), client);
        //}

        //private static void FeedSendCallback(IAsyncResult ar)
        //{
        //    try
        //    {
        //        // Retrieve the socket from the state object.
        //        Socket client = (Socket)ar.AsyncState;

        //        // Complete sending the data to the remote device.
        //        int bytesSent = client.EndSend(ar);
        //        Console.WriteLine("Sent {0} bytes to server.", bytesSent);

        //        // Signal that all bytes have been sent.
        //        feedSendDone.Set();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.ToString());
        //    }
        //}
	}
}
