using System;
using System.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using WhiteCow.Entities;

namespace WhiteCow.Broker
{
    public abstract class Broker
    {
        protected readonly String _Key;
        protected readonly String _Secret;
        protected readonly String _GetUrl;
        protected readonly String _PostUrl;
        protected readonly String _Pair;

        protected Wallet BaseWallet;
        protected Wallet QuoteWallet;

        public Double Last { get; protected set; }
      
        public Broker(String Platform)
        {
            _Key = ConfigurationManager.AppSettings[$"{Platform}.key"];
            _Secret = ConfigurationManager.AppSettings[$"{Platform}.secret"];
            _GetUrl = ConfigurationManager.AppSettings[$"{Platform}.geturl"];
            _PostUrl = ConfigurationManager.AppSettings[$"{Platform}.posturl"];
            _Pair = ConfigurationManager.AppSettings[$"{Platform}.pair"];
        }

		protected string Base64Encode(string plainText)
		{
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
			return System.Convert.ToBase64String(plainTextBytes);
		}

		protected String EncryptPost(string PostData, HMAC hm)
		{
            try
            {
                var keyByte = Encoding.UTF8.GetBytes(_Secret);
                hm.Key = keyByte;
                hm.ComputeHash(Encoding.UTF8.GetBytes(PostData));
                return BitConverter.ToString(hm.Hash).Replace("-", "").ToLower();
            }
            finally
            {
                hm.Dispose();
            }
		}

        public abstract Ticker GetTick();
      
        public abstract Boolean RefreshWallet();
        public abstract Boolean MarginBuy();
        public abstract Boolean MarginSell();
     }
}
