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
            QuoteWallet = new Wallet { currency = _Pair.Split('_')[1] };
            BaseWallet = new Wallet { currency = _Pair.Split('_')[0] };
            ExchangeBaseWallet = new Wallet { currency = _Pair.Split('_')[0] };
           
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
            WebClient client = new WebClient();
            client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            client.Headers["key"] = _Key;
            client.Headers["Sign"] = EncryptPost(PostData);

            Int16 PostTry = 0;
            while (PostTry < 3)
            {
                try
                {
                    AuthorizePost();
                    String content = client.UploadString(_PostUrl, "POST", PostData);
                    Console.WriteLine(content);
                    return content;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Poloniex Post exception occured :");
                    Console.WriteLine(ex.ToString());
                    if (PostTry >= 3)
                    {
                        IsInError = true;
                        return "error";
                    }
                    PostTry++;
                    //wait 5 secondes before retrying
                    Thread.Sleep(5000);
                }
            }
            return String.Empty;
        }

        /// <summary>
        /// Encrypts the post with poloniex pre requisite.
        /// </summary>
        /// <returns>The post.</returns>
        /// <param name="PostData">Post data.</param>
        private String EncryptPost(string PostData)
        {
            var keyByte = Encoding.UTF8.GetBytes(_Secret);
            using (var hmacsha512 = new HMACSHA512(keyByte))
            {
                hmacsha512.ComputeHash(Encoding.UTF8.GetBytes(PostData));
                return BitConverter.ToString(hmacsha512.Hash).Replace("-", "").ToLower();
            }
        }
        #endregion

        #region http get
        /// <summary>
        /// Gets the tick for a specified pair
        /// </summary>
        /// <returns>The tick.</returns>
        protected override Ticker GetTick()
        {
            String address = _GetUrl + "/public?command=returnTicker";
            WebClient client = new WebClient();

            var content = client.DownloadString(address);

            if (String.IsNullOrEmpty(content))
                return null;
            var poloticker = PoloniexTicker.FromJson(content);

            Ticker otick = new Ticker();
            otick.Ask = poloticker[_Pair].LowestAsk;
            otick.Bid = poloticker[_Pair].HighestBid;
            otick.Last = poloticker[_Pair].Last;
            otick.Low = poloticker[_Pair].Low24hr;
            otick.High = poloticker[_Pair].High24hr;
            otick.Volume = poloticker[_Pair].BaseVolume;

            return otick;

        }
        /// <summary>
        /// compute the lending rate
        /// </summary>
        /// <returns>The average yield loan.</returns>
		protected override double GetAverageYieldLoan()
        {
            String address = _GetUrl + $"/public?command=returnLoanOrders&currency={BaseWallet.currency}";
            WebClient client = new WebClient();
            var lend = PoloniexLendInfo.FromJson(client.DownloadString(address));
            return lend.Offers.Average(p => p.Rate);
        }

        private PoloniexMarketOrderBook returnMarketOrderBook(Int32 depth)
		{
			String url = String.Concat(_GetUrl
				, "/public?command=returnOrderBook&currencyPair="
                , _Pair
				, "&depth="
				, depth);

			WebClient client = new WebClient();

			var content = client.DownloadString(url);
            return PoloniexMarketOrderBook.FromJson(content);

			
		}

        public override Double GetWithDrawFees()
        {
            if (Fees == null)
            {

                String url = String.Concat(_GetUrl, "/public?command=returnCurrencies");

                WebClient client = new WebClient();

                var content = client.DownloadString(url);
                var currencies = PoloniexCurrencyInfos.FromJson(content);

                Fees = new Dictionary<String, Double>();

                foreach (String key in currencies.Keys)
                {
                    Fees.Add(key,currencies[key].TxFee);
                }
            }
            return Fees[BaseWallet.currency];

        }
        #endregion


        #region http post
        public override bool MarginBuy()
        {
			var orderbook = returnMarketOrderBook(20);

			//convert to the target currency because this is amount required in target currency for all exchange
			Double amount = BaseWallet.amount;
			int i = -1;
			while (amount > 0.02)
			{
				i++;
				Double rate = (orderbook.Raw_asks[i])[0];
				Double amountToLoad;

				//need to ckeck if the current ask cover the entire available amount
				if ((orderbook.Raw_asks[i])[1] < _MinimumSize)
					continue;
				else if (amount > (orderbook.Raw_asks[i])[1])
					amountToLoad = (orderbook.Raw_asks[i])[1];
				else
					amountToLoad = amount;

				String PostData = String.Concat("command=marginSell&nonce=", DateTime.Now.getUnixMilliTime()
												, "&currencyPair=", _Pair
												, "&rate=", String.Format(CultureInfo.InvariantCulture, "{0:F20}", rate).TrimEnd('0')
												, "&amount=", String.Format(CultureInfo.InvariantCulture, "{0:F20}", amountToLoad / rate).TrimEnd('0')
				 );
                				
				Post(PostData);

				amount = amount - amountToLoad;
			}
			return true;
        }

        public override bool MarginSell()
        {
			var orderbook = returnMarketOrderBook(20);

			//convert to the target currency because this is amount required in target currency for all exchange
			Double amount = BaseWallet.amount;
			int i = -1;
			while (amount > 0.02)
			{
                i++;
				Double rate = (orderbook.Raw_bids[i])[0];
				Double amountToLoad;

                //need to ckeck if the current ask cover the entire available amount
                if ((orderbook.Raw_bids[i])[1]<_MinimumSize)
                    continue;
                else if (amount > (orderbook.Raw_bids[i])[1])
					amountToLoad = (orderbook.Raw_bids[i])[1];
				else
					amountToLoad = amount;

				String PostData = String.Concat("command=marginSell&nonce=", DateTime.Now.getUnixMilliTime()
												, "&currencyPair=", _Pair
												, "&rate=", String.Format(CultureInfo.InvariantCulture, "{0:F20}", rate).TrimEnd('0')
												, "&amount=", String.Format(CultureInfo.InvariantCulture, "{0:F20}", amountToLoad / rate).TrimEnd('0')
				 );

                Post(PostData);
               
                amount = amount - amountToLoad;
			}
			return true;
        }

        public void GetOpenPosition()
        {
            String PostData = "command=getMarginPosition&currencyPair=BTC_ETH&nonce=" + DateTime.Now.getUnixMilliTime();
            string res = Post(PostData);


        }
        public override Boolean ClosePosition()
        {
            String PostData = "command=closeMarginPosition&currencyPair=BTC_ETH&nonce=" + DateTime.Now.getUnixMilliTime();
            string res = Post(PostData);
            return true;
        }


		/// <summary>
		/// Refreshs the amount wallet.
		/// </summary>
		/// <returns><c>true</c>, if wallet was refreshed, <c>false</c> error.</returns>
		public override bool RefreshWallet()
        {
            String PostData = "command=returnAvailableAccountBalances&nonce=" + DateTime.Now.getUnixMilliTime();

            var balances = PoloniexAvailableAccountBalance.FromJson(Post(PostData));


            if (IsInError)
                return false;


            //spolit to retrieve the different balance
            if (balances.margin != null && balances.margin.ContainsKey(BaseWallet.currency))
                BaseWallet.amount = Convert.ToDouble(balances.margin[BaseWallet.currency]);
            else
                BaseWallet.amount = 0.0;

            if (balances.margin != null && balances.margin.ContainsKey(QuoteWallet.currency))
                QuoteWallet.amount = Convert.ToDouble(balances.margin[QuoteWallet.currency]);
            else
                QuoteWallet.amount = 0.0;

            if (balances.exchange != null && balances.exchange.ContainsKey(BaseWallet.currency))
                ExchangeBaseWallet.amount = Convert.ToDouble(balances.exchange[BaseWallet.currency]);
            else
                ExchangeBaseWallet.amount = 0.0;

            Console.WriteLine($"Base wallet amount : {BaseWallet.amount}{Environment.NewLine} Quote wallet amount {QuoteWallet.amount}{Environment.NewLine} Exchange base wallet : {ExchangeBaseWallet.amount}");
            return true;
        }

        public override bool Send(string DestinationAddress, double Amount)
        {
            //RefreshWallet();
            //Double originalExchangeAmount = ExchangeBaseWallet.amount;

            //TransferFund( PoloniexAccountType.margin,PoloniexAccountType.exchange,Amount);

            //while (ExchangeBaseWallet.amount==originalExchangeAmount)
            //{
            //    Thread.Sleep(3000);
            //    RefreshWallet();
            //}
            long nonce = DateTime.Now.getUnixMilliTime();
            String PostData = String.Concat("command=withdraw"
                                            ,$"&currency={BaseWallet.currency}"
                                            , $"&nonce={nonce}"
                                            , $"&amount={Amount}"
                                            , $"&address={DestinationAddress}");
            Post(PostData);

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
        #endregion

    }


}


