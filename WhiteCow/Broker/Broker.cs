using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using WhiteCow.Entities;
using WhiteCow.Entities.Trading;
using WhiteCow.Extension;
using System.Linq;
namespace WhiteCow.Broker
{
	public abstract class Broker
	{
		protected readonly String _Key;
		protected readonly String _Secret;
		protected readonly String _GetUrl;
		protected readonly String _PostUrl;
		protected readonly Double _Leverage;
		protected readonly Int32 _NbCallPost;
		protected readonly Int32 _CallPostMaxInterval;
		protected readonly Double _MinimumSize;
		protected readonly Double _MaximumSize;
		protected readonly String _BaseCurrency;

		protected List<String> _QuoteCurrencies;
		public Wallet BaseWallet { get; protected set; }




		/// <summary>
		/// use to constraint call number to avoid black listing
		/// </summary>
		protected volatile SynchronizedCollection<DateTime> NbPostCall;

		
		public readonly Plateform Name;
		#region Tick
		/// <summary>
		/// return the last tick
		/// to prevent overload all call to the get service is called every seconde
		/// </summary>
		/// <value>The last tick.</value>
		/// 
		public Dictionary<String, Ticker> LastTicks
		{
			get
			{
				if (_LastTicks == null || DateTime.Now.AddSeconds(-1).getUnixMilliTime() - _LastTicks.First().Value.Timestamp >= 1000)
				{                  
					do
					{
						IsInError = false;
						_LastTicks = GetTicks();
						if (_LastTicks == null)
						{
							Thread.Sleep(100);
							IsInError = true;
						}
					}
					while (IsInError);
				}
				return _LastTicks;
			}
		}


		Dictionary<String, Ticker> _LastTicks;
		#endregion

		public PositionTypeEnum Position { get; protected set; }
		public Boolean IsInError;
		
		public Broker(String Platform)
		{
			_Key = ConfigurationManager.AppSettings[$"{Platform}.key"];
			_Secret = ConfigurationManager.AppSettings[$"{Platform}.secret"];
			_GetUrl = ConfigurationManager.AppSettings[$"{Platform}.geturl"];
			_PostUrl = ConfigurationManager.AppSettings[$"{Platform}.posturl"];

			_NbCallPost = Convert.ToInt32(ConfigurationManager.AppSettings[$"{Platform}.NbCallPost"]);
			_CallPostMaxInterval = Convert.ToInt32(ConfigurationManager.AppSettings[$"{Platform}.PostInterval"]);
			_MinimumSize = Convert.ToDouble(ConfigurationManager.AppSettings[$"{Platform}.MinimumSize"]);
			_MaximumSize = Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.MaxPositionSize"]);
			_Leverage = Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.Leverage"]);
			_BaseCurrency = ConfigurationManager.AppSettings["Runtime.BaseCurrency"];
			_QuoteCurrencies = ConfigurationManager.AppSettings[$"{Platform}.QuoteCurrency"].Split(',').ToList();

			Name = (Plateform)Enum.Parse(typeof(Plateform), Platform);
			Position = PositionTypeEnum.Out;
			IsInError = false;
			NbPostCall = new SynchronizedCollection<DateTime>();

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
					if (NbPostCall[i] < DateTime.Now.AddSeconds(_CallPostMaxInterval * -1))
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

		protected String HttpGet(String address)
		{
			IsInError = false;
			String Response = String.Empty;
			using (WebClient client = new WebClient())
			{
				while (String.IsNullOrEmpty(Response))
				{
					try
					{
						Response = client.DownloadString(address);
						IsInError = false;
					}
					catch (TimeoutException tex)
					{
						Log.Logger.Instance.LogWarning($"Warning hhtp get for {address} has timed out  : {tex.Message}");
						Thread.Sleep(500);
					}
					catch (Exception ex)
					{
						Log.Logger.Instance.LogError($"Http get error : {address}");
						Log.Logger.Instance.LogError(ex);
						IsInError = true;
						Thread.Sleep(500);
					}
				}
			}
			return Response;
		}
        /// <summary>
		/// Gets the tick for a specific pair.
		/// </summary>
		/// <returns>The tick.</returns>
		protected abstract Ticker GetTick(String currency);

		/// <summary>
		/// Gets the tick for all specified pair.
		/// </summary>
		/// <returns>The tick.</returns>
		protected abstract Dictionary<String, Ticker> GetTicks();

		/// <summary>
		/// format the selected pair to the broker format requirement
		/// </summary>
		/// <returns>The pair well format.</returns>
		/// <param name="Currency">Currency wanted</param>
		protected abstract String Pair(String Currency);

		/// <summary>
		/// Refreshs the wallet.
		/// </summary>
		/// <returns><c>true</c>, if wallet was refreshed, <c>false</c> otherwise.</returns>
		public abstract Boolean RefreshWallet();

		/// <summary>
		/// Take a long position in margin market with all available fund on base wallet
		/// </summary>
		/// <returns><c>true</c>, if buy was margined, <c>false</c> otherwise.</returns>

		public abstract Double MarginBuy(String currency);
		/// <summary>
		/// Take a long position in margin market
		/// </summary>
		/// <returns><c>true</c>, if buy was margined, <c>false</c> otherwise.</returns>
		/// <param name="Amount">Amount.</param>
		/// <param name="unit"><c>represent the amount currency </c></param>
		public abstract Double MarginBuy(String currency, Double Amount, String unit);

		/// <summary>
		///Take a short position in margin market with all available fund    on base wallet    /// </summary>
		/// <returns><c>true</c>, if sell was margined, <c>false</c> otherwise.</returns>
		/// <param name="currency"><c>the quoted currency</c></param>       
		public abstract Double MarginSell(String currency);

		/// <summary>
		///Take a short position in margin market        /// </summary>
		/// <returns><c>true</c>, if sell was margined, <c>false</c> otherwise.</returns>
		/// <param name="Amount">Amount.</param>
		/// <param name="unit"><c>represent the amount currency </c></param>
		/// <param name="currency"><c>the quoted currency</c></param>
		public abstract Double MarginSell(String currency, Double Amount, String unit);

		/// <summary>
		/// Closes the position.
		/// </summary>
		/// <returns><c>true</c>, if position was closed, <c>false</c> otherwise.</returns>
		/// <param name="currency">Currency.</param>
		public abstract Boolean ClosePosition(String currency);

        /// <summary>
        /// retrieve all open orders with the order id and the amount in the quote currency.
        /// </summary>
        /// <returns>list of tupple (orderid then amount)</returns>
        /// <param name="currency">Currency.</param>
        public abstract List<Tuple<String, Double>> GetOpenOrders(String currency);

        /// <summary>
        /// Cancels order that are not traded.
        /// </summary>
        /// <returns><c>true</c>, if open order was canceled, <c>false</c> otherwise.</returns>
        /// <param name="OrderId">Order identifier.</param>
        public abstract Boolean CancelOpenOrder(String OrderId);

	}
}