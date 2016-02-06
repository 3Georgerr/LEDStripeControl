using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.Net;

namespace LEDStripeControl
{
	public class WebServer
	{
		private Socket server = null;
		private OutputPort[] m_ports;

		public WebServer(OutputPort[] ports)
		{
			m_ports = ports;
			server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 80);
			server.Bind(localEndPoint);
			server.Listen(Int32.MaxValue);
		}

		public void waitForRequest()
		{
			while (true)
			{
				Socket clientSocket = server.Accept();
				new ProcessClientRequest(clientSocket, true, m_ports);
			}
		}

		~WebServer()
		{
			if (server != null)
			{
				server.Close();
			}
		}
	}
}
