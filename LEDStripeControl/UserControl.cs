using System;
using Microsoft.SPOT;
using MFCSofware;
using System.Text;

namespace LEDStripeControl
{
	public class UserControl
	{

		public static bool authenticate(string username, string password)
		{
			byte[] t_sha1;
			t_sha1 = System.Security.Cryptography.SHA1CryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(password));
			string sha1 = BitConverter.ToString(t_sha1);

			string[] accounts = FileUtilities.file_get_contents(@"\SD\data\users").Split('\n');
			if (accounts.Length > 0)
			{
				for (int i = 0; i < accounts.Length; i++)
				{
					if (accounts[i].Trim().Equals(username + ":" + sha1))
					{
						return (true);
					}
				}
			}
			return (false);
		}

	}
}
