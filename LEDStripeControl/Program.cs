using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

using MFCSofware;

namespace LEDStripeControl
{
	public class Program
	{
		public struct blinkingPorts
		{
			public static bool[] port = new bool[5];
		}

		//static blinkingPorts bp = new blinkingPorts();

		static OutputPort[] ports = new OutputPort[6];

		public static void Main()
		{
			Logging.log_entry("starting");

			Debug.EnableGCMessages(false);

			// >>> inicializace site
			Microsoft.SPOT.Net.NetworkInformation.NetworkInterface[] interfaces = Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
			string dhcp = FileUtilities.file_get_contents(@"\SD\setup\dhcp");
			if (dhcp.Trim().Equals("0"))
			{
				interfaces[0].EnableStaticIP(
					FileUtilities.file_get_contents(@"\SD\setup\ip_address").Trim(), 
					FileUtilities.file_get_contents(@"\SD\setup\netmask").Trim(), 
					FileUtilities.file_get_contents(@"\SD\setup\gateway").Trim()
				);
			}
			else
			{
				Logging.log_entry("default dhcp");
				interfaces[0].EnableDhcp();
			}
			Logging.log_entry("init networking done, ip: " + interfaces[0].IPAddress);
			// <<<

			// >>> datum a cas z ntp
			int timezone;
			try
			{
				timezone = int.Parse(FileUtilities.file_get_contents(@"\SD\setup\timezone").Trim());
			}
			catch (Exception)
			{
				timezone = 0;
			}
			string ntp_servers = FileUtilities.file_get_contents(@"\SD\setup\ntp_server").Trim();
			DateTime time = new DateTime();
			if (ntp_servers.Length > 0)
			{
				// je k dispozici nastaveni, vyzkousim
				string[] ntp_server = ntp_servers.Split('\n');
				if (ntp_server.Length > 0)
				{
					for (int i = 0; i < ntp_server.Length; i++)
					{
						try
						{
							time = NTP.NTPTime(ntp_server[i].Trim(), timezone).ToUniversalTime();
						}
						catch (Exception) 
						{
						}
						if (time.Year > 2000)
						{
							Logging.log_entry("got time from " + ntp_server[i].Trim());
							break;
						}
					}
				}
				else
				{
					time = new DateTime();
				}
			}
			else
			{
				Logging.log_entry("using default ntp");
				time = NTP.NTPTime("time-a.nist.gov", timezone).ToUniversalTime();
			}
			if (time.ToString().Length > 0 && time.Year > 2000)
			{
				Utility.SetLocalTime(time);
				Logging.log_entry("init time, local time set to " + time.ToString());
			}
			else
			{
				Logging.log_entry("init time, cannot set local time, rebooting");
				PowerState.RebootDevice(false);
			}
			// <<<

			// >>> spusteni webserveru
			Logging.log_entry("starting webserver");
			new Thread(startWebServer).Start();
			// <<<

			new Thread(blinkerThread).Start();

		}

		public static void blinkerThread()
		{
			bool blinkState = false;
			while (true)
			{
				for (int i = 0; i < 5; i++)
				{
					if (blinkingPorts.port[i] == true)
					{
						if (blinkState)
						{
							ports[i + 1].Write(false);
						}
						else
						{
							ports[i + 1].Write(true);
						}
					}
				}
				if (blinkState)
				{
					blinkState = false;
				}
				else
				{
					blinkState = true;
				}
				Thread.Sleep(500);
			}
		}

		public static void startWebServer()
		{
			// priprava portu
			ports[0] = new OutputPort(Pins.ONBOARD_LED, false);
			ports[1] = new OutputPort(Pins.GPIO_PIN_D8, false);
			ports[2] = new OutputPort(Pins.GPIO_PIN_D9, false);
			ports[3] = new OutputPort(Pins.GPIO_PIN_D10, false);
			ports[4] = new OutputPort(Pins.GPIO_PIN_D11, false);
			ports[5] = new OutputPort(Pins.GPIO_PIN_D12, false);

			// povypinani
			for (int i = 1; i <= 5; i++)
			{
				ports[i].Write(false);
				blinkingPorts.port[i - 1] = false;
			}

			// webserver, predame porty
			WebServer ws = new WebServer(ports);
			ws.waitForRequest();
		}

	}
}
