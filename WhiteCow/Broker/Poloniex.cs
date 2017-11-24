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
            Logger.Instance.LogInfo($"Poloniex post data : {PostData}");
            WebClient client = new WebClient();
            client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            client.Headers["key"] = _Key;
            client.Headers["Sign"] = EncryptPost(PostData, new HMACSHA512());

            Int16 PostTry = 0;
            while (PostTry < 3)
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
                    Thread.Sleep(5000);
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
        protected override Ticker GetTick()
        {
            String address = _GetUrl + "/public?command=returnTicker";
            WebClient client = new WebClient();

            var content = client.DownloadString(address);

            if (String.IsNullOrEmpty(content))
            {
                Logger.Instance.LogWarning("Poloniex : ticker time out");
                return null;
            }
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
            Logger.Instance.LogInfo("Poloniex Average Yield started");
            String address = _GetUrl + $"/public?command=returnLoanOrders&currency={BaseWallet.currency}";
            WebClient client = new WebClient();
            var lend = PoloniexLendInfo.FromJson(client.DownloadString(address));
            Logger.Instance.LogInfo("Poloniex Average Yield end");
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
                
				Logger.Instance.LogInfo("Poloniex Call WithDraw fees started");
                do
                {
                    String url = String.Concat(_GetUrl, "/public?command=returnCurrencies");

                    WebClient client = new WebClient();

                    var content = client.DownloadString(url);

                    if (String.IsNullOrEmpty(content))
                    {
                        IsInError = true;
                        Logger.Instance.LogWarning("Poloniex Call WithDraw fee failed");
                        continue;
                    }
                    var currencies = PoloniexCurrencyInfos.FromJson(content);

                    Fees = new Dictionary<String, Double>();

                    foreach (String key in currencies.Keys)
                    {
                        Fees.Add(key, currencies[key].TxFee);
                    }
					IsInError = false;
                } while (IsInError);
			}
        
			Logger.Instance.LogInfo("Poloniex Call WithDraw fees ended");

			return Fees[BaseWallet.currency];

        }
        #endregion


        #region http post
        public override bool MarginBuy()
        {
			Logger.Instance.LogInfo("Poloniex Margin buy started");
			var orderbook = returnMarketOrderBook(20);

            //convert to the target currency because this is amount required in target currency for all exchange
            Double amount = BaseWallet.amount > _MaximumSize ? _MaximumSize : BaseWallet.amount;
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
                Logger.Instance.LogInfo($"Poloniex margin amount is {amountToLoad}");

				String PostData = String.Concat("command=marginSell&nonce=", DateTime.Now.getUnixMilliTime()
                                                , "&currencyPair=", _Pair
                                                , "&rate=", String.Format(CultureInfo.InvariantCulture, "{0:F20}", rate).TrimEnd('0')
                                                , "&amount=", String.Format(CultureInfo.InvariantCulture, "{0:F20}", amountToLoad / rate).TrimEnd('0')
                 );

                Post(PostData);
                if (!IsInError)
                    amount = amount - amountToLoad;
                else
					Logger.Instance.LogWarning("Poloniex Margin Buy has failed");

                if (i==20)
                {
					Logger.Instance.LogError("Poloniex Margin buy failed");
                    IsInError = true;
                    return false;

				}
			}
			Logger.Instance.LogInfo("Bitfinex Margin buy ended");

			return true;
        }

        public override bool MarginSell()
        {
			Logger.Instance.LogInfo("Poloniex Margin sell started");

			var orderbook = returnMarketOrderBook(20);

            //convert to the target currency because this is amount required in target currency for all exchange
            Double amount = BaseWallet.amount > _MaximumSize ? _MaximumSize : BaseWallet.amount;
            int i = -1;
            while (amount > 0.02)
            {
                i++;
                Double rate = (orderbook.Raw_bids[i])[0];
                Double amountToLoad;

                //need to ckeck if the current ask cover the entire available amount
                if ((orderbook.Raw_bids[i])[1] < _MinimumSize)
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

				if (!IsInError)
					amount = amount - amountToLoad;
				else
					Logger.Instance.LogWarning("Poloniex Margin Sell has failed");
				if (i == 20)
				{
					Logger.Instance.LogError("Poloniex Margin Sell failed");
					IsInError = true;
					return false;

				}
            }
			Logger.Instance.LogInfo("Bitfinex Margin sell ended");

			return true;
        }

        public void GetOpenPosition()
        {
            Logger.Instance.LogInfo("Poloniex get open position start");

			String PostData = "command=getMarginPosition&currencyPair=BTC_ETH&nonce=" + DateTime.Now.getUnixMilliTime();
            string res = Post(PostData);
            Logger.Instance.LogInfo("Poloniex get open position end");

        }
        public override Boolean ClosePosition()
        {
            Logger.Instance.LogInfo("Poloniex get close position started");
            String PostData = "command=closeMarginPosition&currencyPair=BTC_ETH&nonce=" + DateTime.Now.getUnixMilliTime();
            string res = Post(PostData);
            Logger.Instance.LogInfo("Poloniex get close position ended");
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

            var balances = PoloniexAvailableAccountBalance.FromJson(Post(PostData));


            if (IsInError)
            {
                Logger.Instance.LogError("Poloniex Refresh wallet has failed");
                return false;
            }


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

            Logger.Instance.LogInfo($"Base wallet amount : {BaseWallet.amount}{Environment.NewLine} Quote wallet amount {QuoteWallet.amount}{Environment.NewLine} Exchange base wallet : {ExchangeBaseWallet.amount}");
            return true;
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

    }


}


