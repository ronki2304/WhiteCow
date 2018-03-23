using System;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using WhiteCow.Entities;
using WhiteCow.Entities.Poloniex.Ticker;
using WhiteCow.Extension;
using System.Linq;
using System.Threading;
using WhiteCow.Entities.Poloniex;
using System.Collections.Generic;
using WhiteCow.Log;
using WhiteCow.Entities.Poloniex.Close;

namespace WhiteCow.Broker
{
	public class Poloniex : Broker
	{
		/// <summary>
		/// poloniex doesn't permit to send directly money to margin part
		/// all income come from exchange
		/// </summary>
		private Wallet ExchangeBaseWallet;


		public Poloniex() : base(Plateform.Poloniex.ToString())
		{

			BaseWallet = new Wallet { currency = _BaseCurrency };
			ExchangeBaseWallet = new Wallet { currency = _BaseCurrency };

			RefreshWallet();
		}

		#region private


		/// <summary>
		/// operate the post, and limit it to 6 per seconds
		/// </summary>
		/// <returns>The post.</returns>
		/// <param name="PostData">Post data.</param>
		private String Post(String PostData)
		{
			IsInError = false;
			Logger.Instance.LogInfo($"Poloniex post data : {PostData}");
			using (WebClient client = new WebClient())
			{
				client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
				client.Headers["key"] = _Key;
				client.Headers["Sign"] = EncryptPost(PostData, new HMACSHA512());

				Int16 PostTry = 0;
				while (PostTry < 5)
				{
					try
					{
						AuthorizePost();
						String content = client.UploadString(_PostUrl, "POST", PostData);
						Logger.Instance.LogInfo("Poloniex post result : " + content);
						return content;
					}
					catch (Exception ex)
					{
						Logger.Instance.LogWarning("Poloniex Post exception occured :");
						Logger.Instance.LogWarning(ex.ToString());
						if (PostTry >= 3)
						{
							IsInError = true;
							Logger.Instance.LogError("Poloniex Post exception occured :");
							Logger.Instance.LogError(ex.ToString());
							return "error";
						}
						PostTry++;
						//wait 5 secondes before retrying
						Thread.Sleep(500);
					}
				}
			}
			return String.Empty;
		}


		#endregion

		#region http get
		/// <summary>
		/// Gets the tick for a specified pair
		/// </summary>
		/// <returns>The tick.</returns>
		protected override Ticker GetTick(String currency)
		{
			Logger.Instance.LogInfo("Poloniex Get Tick start");


			var poloticker = GetAllTicks();
			Ticker otick = new Ticker();
			otick.Ask = poloticker[Pair(currency)].LowestAsk;
			otick.Bid = poloticker[Pair(currency)].HighestBid;
			otick.Last = poloticker[Pair(currency)].Last;
			otick.Low = poloticker[Pair(currency)].Low24hr;
			otick.High = poloticker[Pair(currency)].High24hr;
			otick.Volume = poloticker[Pair(currency)].BaseVolume;
			otick.Timestamp = DateTime.Now.getUnixMilliTime();
			Logger.Instance.LogInfo($"Poloniex last Tick is {otick.Last} , last bid is : {otick.Bid}, last ask is : {otick.Ask}");
			Logger.Instance.LogInfo("Poloniex Get Tick end");

			return otick;

		}

		protected override Dictionary<String, Ticker> GetTicks()
		{
			Logger.Instance.LogInfo("Poloniex Get Tick start");
			Dictionary<String, Ticker> ret = new Dictionary<string, Ticker>();

			var poloticker = GetAllTicks();

			foreach (var currency in _QuoteCurrencies)
			{
				Ticker otick = new Ticker();
				otick.Ask = poloticker[Pair(currency)].LowestAsk;
				otick.Bid = poloticker[Pair(currency)].HighestBid;
				otick.Last = poloticker[Pair(currency)].Last;
				otick.Low = poloticker[Pair(currency)].Low24hr;
				otick.High = poloticker[Pair(currency)].High24hr;
				otick.Volume = poloticker[Pair(currency)].BaseVolume;
				otick.Timestamp = DateTime.Now.getUnixMilliTime();
				Logger.Instance.LogInfo($"Poloniex last Tick for {currency} is {otick.Last} , last bid is : {otick.Bid}, last ask is : {otick.Ask}");

				ret.Add(currency, otick);
			}
			Logger.Instance.LogInfo("Poloniex Get Tick end");
			return ret;
		}

		/// <summary>
		/// this method is called for all get tick situation refactorisation was required
		/// only the gettick and the getticks must call it
		/// </summary>
		/// <returns>all ticks poloniex format</returns>
		private Dictionary<String, PoloniexTicker> GetAllTicks()
		{
			String address = _GetUrl + "/public?command=returnTicker";
			WebClient client = new WebClient();

			var content = HttpGet(address);

			if (IsInError)
			{
				Logger.Instance.LogInfo("Poloniex Get Tick end with error");

				return null;
			}
			return PoloniexTicker.FromJson(content);
		}
		/// <summary>
		/// compute the lending rate
		/// </summary>
		/// <returns>The average yield loan.</returns>
		protected override double GetAverageYieldLoan()
		{
			Logger.Instance.LogInfo("Poloniex Average Yield started");
			String address = _GetUrl + $"/public?command=returnLoanOrders&currency={BaseWallet.currency}";
			var content = HttpGet(address);
			if (IsInError)
			{
				Logger.Instance.LogInfo("Poloniex Average Yield end with error");

				return Double.NaN;
			}
			var lend = PoloniexLendInfo.FromJson(content);
			Logger.Instance.LogInfo("Poloniex Average Yield end");
			return lend.Offers.Average(p => p.Rate);

		}

		private PoloniexMarketOrderBook returnMarketOrderBook(Int32 depth, String currency)
		{
			Logger.Instance.LogInfo("Poloniex return Market Order Book start");

			String url = String.Concat(_GetUrl
				, "/public?command=returnOrderBook&currencyPair="
				, Pair(currency)
				, "&depth="
				, depth);



			var content = HttpGet(url);
			if (IsInError)
			{
				Logger.Instance.LogInfo("Poloniex return Market Order Book end with errors");

				return null;
			}
			Logger.Instance.LogInfo("Poloniex return Market Order Book end");

			return PoloniexMarketOrderBook.FromJson(content);


		}

		public override Double GetWithDrawFees()
		{
			if (Fees == null)
			{

				Logger.Instance.LogInfo("Poloniex Call WithDraw fees started");

				String url = String.Concat(_GetUrl, "/public?command=returnCurrencies");


				var content = HttpGet(url);

				if (IsInError)
				{
					Logger.Instance.LogInfo("Poloniex Call WithDraw fees ended with error");
					return Double.NaN;

				}
				var currencies = PoloniexCurrencyInfos.FromJson(content);

				Fees = new Dictionary<String, Double>();

				foreach (String key in currencies.Keys)
				{
					Fees.Add(key, currencies[key].TxFee);
				}

			}

			Logger.Instance.LogInfo("Poloniex Call WithDraw fees ended");

			return Fees[BaseWallet.currency];

		}
		#endregion


		#region http post
		public override Double MarginBuy(String currency)
		{
			return MarginBuy(currency, BaseWallet.amount, _BaseCurrency);
		}

		public override Double MarginBuy(String currency, Double Amount, String unit)
		{
			Logger.Instance.LogInfo("Poloniex Margin buy started");
			var orderbook = returnMarketOrderBook(20, currency);
			Double OrderedAmount;
			Double finalAmount = 0.0; //represent the final bought quantity of currency bought

            if (unit == _BaseCurrency)
                //convert to the target currency because this is amount required in target currency for all exchange
                OrderedAmount = Amount * _Leverage > _MaximumSize ? _MaximumSize : Amount * _Leverage;
            else
            {
                //include the fee
                OrderedAmount = (1+0.005)*Amount;
            }
			
            int i = -1;

			//be careful OrderedAmount unit can be the base one or the quote one
			while (OrderedAmount >= _MinimumSize)
			{
				i++;
				Double rate = (orderbook.Raw_asks[i])[0];
				Double amountToLoad; //quantity of quoted currency

				//need to ckeck if the current ask cover the entire available amount
				if ((orderbook.Raw_asks[i])[1] < _MinimumSize)
					continue;

                if (unit == BaseWallet.currency)
                {
                    if (OrderedAmount / (orderbook.Raw_asks[i])[0] > (orderbook.Raw_asks[i])[1])
                        amountToLoad = (orderbook.Raw_asks[i])[1];
                    else
                        amountToLoad = OrderedAmount / (orderbook.Raw_asks[i])[0];
                }
                else
                {
                    if (OrderedAmount > (orderbook.Raw_asks[i])[1])
                        amountToLoad = (orderbook.Raw_asks[i])[1];
                    else
                        amountToLoad = OrderedAmount;
                }
				Logger.Instance.LogInfo($"Poloniex margin amount is {amountToLoad}");

				String PostData = String.Concat("command=marginBuy&nonce=", DateTime.Now.getUnixMilliTime()
												, "&currencyPair=", Pair(currency)
												, "&rate=", String.Format(CultureInfo.InvariantCulture, "{0:F20}", rate).TrimEnd('0')
												, "&amount=", String.Format(CultureInfo.InvariantCulture, "{0:F20}", amountToLoad ).TrimEnd('0')
				 );

				var response = Post(PostData);



				if (IsInError)
				{
					Logger.Instance.LogWarning("Poloniex Margin Buy has failed");
					if (i == 20)
					{
						Logger.Instance.LogError("Poloniex Margin Buy failed");
						IsInError = true;
						return Double.NaN;

					}
					else
						continue;
				}

                if (BaseWallet.currency == unit)
                    OrderedAmount = OrderedAmount - amountToLoad * (orderbook.Raw_asks[i])[0];
                else
                    OrderedAmount = OrderedAmount - amountToLoad;
                
				finalAmount += amountToLoad;

			}
			Logger.Instance.LogInfo("Poloniex Margin buy ended");

			return finalAmount;
		}

		public override Double MarginSell(String currency)
		{
			return MarginSell(currency, BaseWallet.amount, _BaseCurrency);
		}

		public override Double MarginSell(String currency, Double Amount, String unit)
		{
			Logger.Instance.LogInfo("Poloniex Margin sell started");

			var orderbook = returnMarketOrderBook(20, currency);
			Double OrderedAmount;
			Double finalAmount = 0.0; //represent the final bought quantity of currency bought


			if (unit == _BaseCurrency)
				OrderedAmount = Amount * _Leverage > _MaximumSize ? _MaximumSize : Amount * _Leverage;
			else
				OrderedAmount = Amount;
			int i = -1;
			
            //be careful OrderedAmount unit can be the base one or the quote one
            while (OrderedAmount >= _MinimumSize)
			{
				i++;
				Double rate = (orderbook.Raw_bids[i])[0];
				Double amountToLoad; //amount only in the quoted currency

				//need to ckeck if the current ask cover the entire available amount
				if ((orderbook.Raw_bids[i])[1] < _MinimumSize)
					continue;

                if (unit == BaseWallet.currency)
                {

                    if (OrderedAmount/(orderbook.Raw_bids[i])[0] > (orderbook.Raw_bids[i])[1])
                        amountToLoad = (orderbook.Raw_bids[i])[1];
                    else
                        amountToLoad = OrderedAmount/(orderbook.Raw_bids[i])[0];
                }
                else
                {
					if (OrderedAmount  > (orderbook.Raw_bids[i])[1])
						amountToLoad = (orderbook.Raw_bids[i])[1];
					else
						amountToLoad = OrderedAmount;
                }

				String PostData = String.Concat("command=marginSell&nonce=", DateTime.Now.getUnixMilliTime()
												, "&currencyPair=", Pair(currency)
												, "&rate=", String.Format(CultureInfo.InvariantCulture, "{0:F20}", rate).TrimEnd('0')
												, "&amount=", String.Format(CultureInfo.InvariantCulture, "{0:F20}", amountToLoad).TrimEnd('0')
				 );

				var response = Post(PostData);



				if (IsInError)
				{
					Logger.Instance.LogWarning("Poloniex Margin Sell has failed");
					if (i == 20)
					{
						Logger.Instance.LogError("Poloniex Margin Sell failed");
						IsInError = true;
						return Double.NaN;
					}
					else
						continue;
				}

                if (BaseWallet.currency == unit)
                    OrderedAmount = OrderedAmount - amountToLoad * (orderbook.Raw_bids[i])[0];
                else
                    OrderedAmount = OrderedAmount - amountToLoad;
				finalAmount += amountToLoad;


			}
			Logger.Instance.LogInfo("Poloniex Margin sell ended");

			return finalAmount;
		}

		public void GetOpenPosition(String currency)
		{
			Logger.Instance.LogInfo("Poloniex get open position start");

			String PostData = $"command=getMarginPosition&currencyPair={Pair(currency)}&nonce=" + DateTime.Now.getUnixMilliTime();
			string res = Post(PostData);
			Logger.Instance.LogInfo("Poloniex get open position end");

		}
		public override Boolean ClosePosition(String currency)
		{
			Logger.Instance.LogInfo("Poloniex get close position started");
			String PostData = $"command=closeMarginPosition&currencyPair={Pair(currency)}&nonce=" + DateTime.Now.getUnixMilliTime();
			PoloniexCloseResult close = PoloniexCloseResult.FromJson(Post(PostData));
			Logger.Instance.LogInfo("Poloniex get close position ended");
			if (close.ResultingTrades == null || close.ResultingTrades.Count == 0)
				return false;

			return true;
		}
		


		/// <summary>
		/// Refreshs the amount wallet.
		/// </summary>
		/// <returns><c>true</c>, if wallet was refreshed, <c>false</c> error.</returns>
		public override bool RefreshWallet()
		{
			Logger.Instance.LogInfo("Poloniex refreshwallet started");

			String PostData = "command=returnAvailableAccountBalances&nonce=" + DateTime.Now.getUnixMilliTime();

			var response = Post(PostData);


			if (IsInError)
			{
				Logger.Instance.LogError("Poloniex Refresh wallet has failed");
				return false;
			}

			if (response == "[]")
			{
				//when people run WhiteCow whitout fund
				BaseWallet.amount = 0.0;
				ExchangeBaseWallet.amount = 0.0;
				return true;
			}
			else
			{
				var balances = PoloniexAvailableAccountBalance.FromJson(response);
				//split to retrieve the different balance
				if (balances.margin != null && balances.margin.ContainsKey(BaseWallet.currency))
					BaseWallet.amount = Convert.ToDouble(balances.margin[BaseWallet.currency]);
				else
					BaseWallet.amount = 0.0;


				if (balances.exchange != null && balances.exchange.ContainsKey(BaseWallet.currency))
					ExchangeBaseWallet.amount = Convert.ToDouble(balances.exchange[BaseWallet.currency]);
				else
					ExchangeBaseWallet.amount = 0.0;

				Logger.Instance.LogInfo($"Base wallet amount : {BaseWallet.amount}{Environment.NewLine} Exchange base wallet : {ExchangeBaseWallet.amount}");
				return true;
			}
		}

		public override bool Send(string DestinationAddress, double Amount)
		{
			Logger.Instance.LogInfo("Poloniex Send money started");

			long nonce = DateTime.Now.getUnixMilliTime();
			String PostData = String.Concat("command=withdraw"
											, $"&currency={BaseWallet.currency}"
											, $"&nonce={nonce}"
											, $"&amount={Amount}"
											, $"&address={DestinationAddress}");
			Post(PostData);
			Logger.Instance.LogInfo($"Poloniex {Amount} is sent");

			return true;
		}


		/// <summary>
		/// permit to transfer fund between account
		/// </summary>
		/// <returns><c>true</c>, if fund was transfered, <c>false</c> otherwise.</returns>
		/// <param name="Input">Input account</param>
		/// <param name="Output">Output account</param>
		/// <param name="amount">Amount</param>
		private Boolean TransferFund(PoloniexAccountType Input, PoloniexAccountType Output, Double amount)
		{
			String PostData = String.Concat("command=transferBalance&nonce="
											, DateTime.Now.getUnixMilliTime()
											, "&currency="
											, BaseWallet.currency
											, "&amount="
											, amount.ToString()
											, "&fromAccount="
											, Input.ToString()
											, "&toAccount="
											, Output.ToString()
										   );

			String res = Post(PostData);
			if (IsInError)
				return false;
			RefreshWallet();
			return true;
		}

		public override Boolean CheckReceiveFund(Double amount)
		{
			Logger.Instance.LogInfo("Poloniex check receive fund started");
			Double oldAmount = ExchangeBaseWallet.amount;

			while (ExchangeBaseWallet.amount == oldAmount)
			{
				Logger.Instance.LogInfo("funds not received for wait 3 min again");
				//wait 3 minuts
				Thread.Sleep(180000);
				oldAmount = ExchangeBaseWallet.amount;
				RefreshWallet();

			}
			Logger.Instance.LogInfo("fund received transfer them");
			TransferFund(PoloniexAccountType.exchange, PoloniexAccountType.margin, amount);
			Logger.Instance.LogInfo("Poloniex check receive fund ended");

			return true;

		}
		#endregion


		protected override String Pair(String currency)
		{
			return _BaseCurrency + "_" + currency;
		}
	}


}

