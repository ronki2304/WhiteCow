using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using WhiteCow.Entities;
using WhiteCow.Entities.Trading;

namespace WhiteCow.Broker
{
    public abstract class Broker
    {
        protected readonly String _Key;
        protected readonly String _Secret;
        protected readonly String _GetUrl;
        protected readonly String _PostUrl;
        protected readonly String _Pair;
        protected readonly Double _Leverage;
        protected readonly Int32 _NbCallPost;
        protected readonly Int32 _CallPostMaxInterval;

        public readonly String _PublicAddress;

        protected Wallet BaseWallet;
        protected Wallet QuoteWallet;
        protected Ticker LastTick { get; set; }

        /// <summary>
        /// use to constraint call number to avoid black listing
        /// </summary>
        protected volatile SynchronizedCollection<DateTime> NbPostCall;


        public PositionTypeEnum Position { get; protected set; }
        public Boolean IsInError;
        /// <summary>
        /// return an avarage rate lend
        /// </summary>
        /// <value>The average yield loan.</value>
        public Double AverageYieldLoan
        {
            get
            {
                return GetAverageYieldLoan();
            }
        }
        /// <summary>
        /// store the amount of the currency bought or sold
        /// </summary>
        /// <value>The quote currency quantity.</value>
        public Double QuoteAmount { get; protected set; }

        public Broker(String Platform)
        {
            _Key = ConfigurationManager.AppSettings[$"{Platform}.key"];
            _Secret = ConfigurationManager.AppSettings[$"{Platform}.secret"];
            _GetUrl = ConfigurationManager.AppSettings[$"{Platform}.geturl"];
            _PostUrl = ConfigurationManager.AppSettings[$"{Platform}.posturl"];
            _Pair = ConfigurationManager.AppSettings[$"{Platform}.pair"];
            _PublicAddress = ConfigurationManager.AppSettings[$"{Platform}.PublicAddress"];
            _NbCallPost = Convert.ToInt32(ConfigurationManager.AppSettings[$"{Platform}.NbCallPost"]); 
            _CallPostMaxInterval = Convert.ToInt32(ConfigurationManager.AppSettings[$"{Platform}.PostInterval"]);



			Position = PositionTypeEnum.Out;
            IsInError = false;

            _Leverage = Convert.ToDouble(ConfigurationManager.AppSettings["Leverage"]);
            if (_Leverage < 1.0)
                _Leverage = 1.0;
            else if (_Leverage > 2.5)
                _Leverage = 2.5;
         }

        protected string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Used in the authenticated rest service to sign the transaction
        /// </summary>
        /// <returns>The post.</returns>
        /// <param name="PostData">Post data.</param>
        /// <param name="hm">Hm.</param>
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

        protected void UpdateMarketPosition(PositionTypeEnum pos)
        {
            if (Position == PositionTypeEnum.Out)
                Position = pos;
            else
                Position = PositionTypeEnum.Out;

        }

		/// <summary>
		/// /// this method check if we are under the calling quota
		/// if yes the method return
		/// if no the method wait for 1 second before return
        /// </summary>
		protected void AuthorizePost()
		{
			Boolean isOK = false;

			while (!isOK)
			{
				for (int i = 0; i < NbPostCall.Count; i++)
				{
					if (NbPostCall[i] < DateTime.Now.AddSeconds(_CallPostMaxInterval*-1))
						NbPostCall.RemoveAt(i);
				}
                if (NbPostCall.Count <= _NbCallPost)
				{
					isOK = true;
					NbPostCall.Add(DateTime.Now);
				}
				else
					Thread.Sleep(1000);
			}
		}

		/// <summary>
		/// return an avarage rate lend
		/// </summary>
		/// <value>The average yield loan.</value>
		protected abstract Double GetAverageYieldLoan();


        /// <summary>
        /// Gets the tick.
        /// </summary>
        /// <returns>The tick.</returns>
        public abstract Ticker GetTick();

        /// <summary>
        /// Refreshs the wallet.
        /// </summary>
        /// <returns><c>true</c>, if wallet was refreshed, <c>false</c> otherwise.</returns>
        public abstract Boolean RefreshWallet();
       
        /// <summary>
        /// Take a long position in margin market
        /// </summary>
        /// <returns><c>true</c>, if buy was margined, <c>false</c> otherwise.</returns>
        public abstract Boolean MarginBuy();

        /// <summary>
        ///Take a short position in margin market        /// </summary>
        /// <returns><c>true</c>, if sell was margined, <c>false</c> otherwise.</returns>
        public abstract Boolean MarginSell();

        /// <summary>
        /// Send coin to a specific address
        /// </summary>
        /// <returns>The send.</returns>
        /// <param name="DestinationAddress">Destination address.</param>
        /// <param name="Amount">Amount.</param>
        public abstract Boolean Send(String DestinationAddress, double Amount);

    }
}
