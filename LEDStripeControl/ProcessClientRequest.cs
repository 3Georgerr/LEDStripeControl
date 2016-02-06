using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using Microsoft.SPOT.Hardware;
using System.Threading;
using System.Text;
using MFCSofware;

namespace LEDStripeControl
{
	internal sealed class ProcessClientRequest
	{
		private Socket m_clientSocket;
		OutputPort[] m_ports;

		public ProcessClientRequest(Socket clientSocket, Boolean asynchronously, OutputPort[] ports)
		{
			m_clientSocket = clientSocket;
			m_ports = ports;

			if (asynchronously)
			{
				new Thread(ProcessRequest).Start();
			}
			else
			{
				ProcessRequest();
			}

		}

		private void SendResponse(string response, string http_status = "")
		{
			string header = string.Empty;
			if (http_status == "")
			{
				http_status = response;
			}
			header = "HTTP/1.0 " + http_status + "\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: " + response.Length.ToString() + "\r\nConnection: close\r\n\r\n";
			try
			{
				m_clientSocket.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);
				m_clientSocket.Send(Encoding.UTF8.GetBytes(response), response.Length, SocketFlags.None);
			}
			catch (Exception)
			{
				m_clientSocket.Close();
			}
		}

		private void ProcessRequest()
		{
			const Int32 c_microsecondsPerSecond = 1000000;

			try
			{
				using (m_clientSocket)
				{
					if (m_clientSocket.Poll(5 * c_microsecondsPerSecond,
						SelectMode.SelectRead))
					{
						if (m_clientSocket.Available == 0)
						{
							return;
						}

						int bytesReceived = m_clientSocket.Available;
						if (bytesReceived > 0)
						{
							byte[] buffer = new byte[bytesReceived];
							int byteCount = m_clientSocket.Receive(buffer, bytesReceived, SocketFlags.None);
							string request = new string(Encoding.UTF8.GetChars(buffer));
							string firstLine = request.Substring(0, request.IndexOf('\n'));
							string[] words = firstLine.Split(' ');

							string command = string.Empty;
							string response = string.Empty;
							string header = string.Empty;

							Logging.log_entry("ACCESS " + m_clientSocket.RemoteEndPoint.ToString() + " - " + '"' + firstLine.Trim() + '"');

							//Debug.Print("Memory: " + Microsoft.SPOT.Debug.GC(true).ToString());

							command = words[1].Substring(1).Trim();
							if (command[0] == 63) // '?'
							{
								// pokud nam zustal otaznik na zacatku, tak oriznout o dva
								command = words[1].Substring(2).Trim();
							}

							// bliknuti
							m_ports[0].Write(true);
							Thread.Sleep(10);
							m_ports[0].Write(false);

                            // ocekavane url
                            // GET /?login=admin:admin&led=

							if (command.Equals("favicon.ico"))
							{
								byte[] favicon = FileUtilities.file_get_contents_byte(@"\SD\setup\favicon.ico");
								header = "HTTP/1.0 200 OK\r\nContent-Type: image/x-icon\r\nContent-Length: " + favicon.Length.ToString() + "\r\nConnection: close\r\n\r\n";
								m_clientSocket.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);
								m_clientSocket.Send(favicon, favicon.Length, SocketFlags.None);
							}
							else
							{
								if (command.Length > 0)
								{
									bool authenticated = false;

									try
									{
										string[] cmds = command.Split('&');
										if (cmds.Length > 0)
										{
											for (int i = 0; i < cmds.Length; i++)
											{
												// zpracovani jednotlivych prikazu
												string[] cmd_parsed = cmds[i].Split('=');
												switch (cmd_parsed[0])
												{
													// login - prihlaseni ke sluzbam
													// l=user:password
													case "login":
														string[] auth_data = cmd_parsed[1].Split(':');
														authenticated = UserControl.authenticate(auth_data[0], auth_data[1]);
														break;

													// ports
													case "led":
														if (authenticated)
														{
															if (cmd_parsed[1].Length == 5)
															{
																controlLedStripe(cmd_parsed[1]);
															}
															else
															{
																SendResponse("500 Bad command params", "500 Internal Server Error");
															}
														}
														else
														{
															SendResponse("401 Unauthorized");
														}
														break;

													case "restart":
														if (authenticated)
														{
															SendResponse("200 OK");
															m_clientSocket.Close();
															PowerState.RebootDevice(false);
														}
														break;

													default:
														SendResponse("500 Unknown command", "500 Internal Server Error");

														break;
												}
											}

										}
										else
										{
											// stranka pokud nebyly predany parametry
											SendResponse("200 HELLO * GIVE ME COMMAND :-)", "200 OK");
										}
									}
									catch (Exception e)
									{
										Logging.log_entry(e.ToString());
									}
								}
								else
								{
									// stranka pokud nebyl zadny request string
									SendResponse("200 HELLO * GIVE ME COMMAND :-)", "200 OK");
								}

							}

							m_clientSocket.Close();
						}
					}
				}
			}
			catch (Exception e)
			{
				Logging.log_entry(e.ToString());
			}
		}

		private void controlLedStripe(string status)
		{
			//Debug.Print("setting LEDs: " + cmd_parsed[1]);

			for (int port = 0; port < 5; port++)
			{
				if (status[port] == '0')
				{
					Program.blinkingPorts.port[port] = false;
					m_ports[port + 1].Write(false);
				}
				else if (status[port] == '1')
				{
					Program.blinkingPorts.port[port] = false;
					m_ports[port + 1].Write(true);
				}
				else if (status[port] == 'B')
				{
					//blinkingPorts.port1 = blinkingPorts.port2 = blinkingPorts.port3 = blinkingPorts.port4 = blinkingPorts.port5 = true;
					m_ports[port + 1].Write(true);
					Program.blinkingPorts.port[port] = true;
				}
				else
				{
					// cokoliv jineho krome 0,1 znamena ignorovat port
				}
			}

			SendResponse("200 OK");
		}
	}
}
