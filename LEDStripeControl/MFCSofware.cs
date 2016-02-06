using System.IO;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Microsoft.SPOT;

namespace MFCSofware
{
	public class FileUtilities
	{
		public static byte[] file_get_contents_byte(string filename)
		{
			byte[] buffer = new byte[0];
			try
			{
				FileStream file = new FileStream(filename, FileMode.Open);
				try
				{
					buffer = new byte[file.Length];
					if (file.CanRead)
					{
						file.Read(buffer, 0, (int)file.Length);
					}
				}
				catch (Exception e)
				{
					// odchyceni problemu s pameti nebo ctenim
					Logging.log_entry(e.ToString());
				}
				file.Close();
				file.Dispose();
			}
			catch (Exception e)
			{
				Logging.log_entry(e.ToString());
			}
			return (buffer);
		}

		public static string file_get_contents(string filename)
		{
			string file_content = string.Empty;
			try
			{
				using (var filestream = new FileStream(filename, FileMode.Open))
				{
					StreamReader reader = new StreamReader(filestream);
					file_content = reader.ReadToEnd();
					reader.Close();
				}
			}
			catch (Exception e)
			{
				Debug.Print(e.Message);
			}
			return (file_content);
			//return (new string(Encoding.UTF8.GetChars(file_get_contents_byte(filename))).Trim());
		}

		public static void file_put_contents(string filename, string data)
		{
		}

	}

	public class NTP {

		/**
		 * 
		 * http://forums.netduino.com/index.php?/topic/475-still-learning-internet-way-to-grab-date-and-time-on-startup/
		 * 
		 * */
		public static DateTime NTPTime(String TimeServer, int UTC_offset = 0)
		{
			// Find endpoint for timeserver
			IPEndPoint ep = new IPEndPoint(Dns.GetHostEntry(TimeServer).AddressList[0], 123);

			// Connect to timeserver
			Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			//s.SetSocketOption
			s.ReceiveTimeout = 5000;
			s.SendTimeout = 5000;
			s.Connect(ep);

			// Make send/receive buffer
			byte[] ntpData = new byte[48];
			Array.Clear(ntpData, 0, 48);

			// Set protocol version
			ntpData[0] = 0x1B;

			try
			{
				// Send Request
				s.Send(ntpData);

				// Receive Time
				s.Receive(ntpData);
			}
			catch (Exception e)
			{
				// nepovedlo se poslat nebo nacist
				Logging.log_entry(e.ToString());
				return (new DateTime());
			}

			byte offsetTransmitTime = 40;

			ulong intpart = 0;
			ulong fractpart = 0;

			for (int i = 0; i <= 3; i++)
			{
				intpart = (intpart << 8) | ntpData[offsetTransmitTime + i];
			}

			for (int i = 4; i <= 7; i++)
			{
				fractpart = (fractpart << 8) | ntpData[offsetTransmitTime + i];
			}

			ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);

			s.Close();

			TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
			DateTime dateTime = new DateTime(1900, 1, 1);
			dateTime += timeSpan;

			//TimeSpan offsetAmount = TimeZone.CurrentTimeZone.GetUtcOffset(dateTime);
			TimeSpan offsetAmount = new TimeSpan(0, UTC_offset, 0, 0, 0);
			DateTime networkDateTime = (dateTime + offsetAmount);

			return (networkDateTime);
		}

	}

	public class Logging
	{
		public static void log_entry(string entry)
		{
			// @todo: smazani starych souboru
			entry = "[" + DateTime.Now + "] " + entry + "\r\n";
			Debug.Print(entry);
			return;
			/*
			try
			{
				using (var logFile = new FileStream(@"\SD\log-" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + ".txt", FileMode.Append))
				{
					byte[] buffer = new byte[entry.Length];
					Array.Clear(buffer, 0, buffer.Length);
					buffer = Encoding.UTF8.GetBytes(entry);
					logFile.Write(buffer, 0, buffer.Length);
				}
			}
			catch (Exception e)
			{
				Debug.Print(e.Message);
			}
			*/
		}
	}

}
