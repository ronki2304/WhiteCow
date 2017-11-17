using System;
using System.Configuration;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WhiteCow.Entities;

namespace WhiteCow.Broker
{
    public static class CexIO
    {
        static readonly String _Key;
        static readonly String _Secret;
        static readonly String _Url;
        static CexIO()
        {
            _Key = ConfigurationManager.AppSettings["Cex.io.key"];
           _Secret=ConfigurationManager.AppSettings["Cex.io.secret"];
            _Url = ConfigurationManager.AppSettings["Cex.io.url"];
        }

        public static Ticker GetTick(String Pair)
        {
            System.Security.Cryptography.AesCryptoServiceProvider b = new System.Security.Cryptography.AesCryptoServiceProvider();
			System.Net.ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;

			String address = _Url  +"ticker/"+ Pair;
			WebClient client = new WebClient();

			
			return Ticker.FromJson(client.DownloadString(address));
        }

		private static String EncryptPost(string PostData)
		{
			var keyByte = Encoding.UTF8.GetBytes(_Secret);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(PostData));
                return BitConverter.ToString(hmacsha256.Hash).Replace("-", "").ToLower();
            }
		}

	
		
    }
}
