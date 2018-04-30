using System;
using System.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using WhiteCow.Entities;
using WhiteCow.Extension;
using System.Linq;
using WhiteCow.Entities.Bitfinex.PostRequest.V1;
using System.Diagnostics;
using System.Threading;
using WhiteCow.Log;
using System.Collections.Generic;
using WhiteCow.Entities.Trading;

namespace WhiteCow.Broker
{
    public class BitFinex : Broker
    {

        //all bitfinex pair are available here https://api.bitfinex.com/v1/symbols
        public BitFinex() : base(Plateform.BitFinex.ToString())
        {
            BaseWallet = new Wallet { currency = _BaseCurrency };

            RefreshWallet();
        }


        #region private 
        /// <summary>
        /// store the amount of the currency bought or sold
        /// </summary>
        /// <value>The quote currency quantity.</value>
        private Double QuoteAmount { get; set; }

        private String PostV1(string apiPath, BitfinexPostBase request)
        {

            Logger.Instance.LogInfo($"Bitfinex Post V1 selected for the method {apiPath}");
            String body64 = Base64Encode(request.serialize());
            String address = _PostUrl + apiPath;
            using (WebClient client = new WebClient())
            {
                client.Headers["X-BFX-APIKEY"] = _Key;
                client.Headers["X-BFX-PAYLOAD"] = body64;
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.Headers["X-BFX-SIGNATURE"] = EncryptPost(body64, new HMACSHA384());

                IsInError = true;
                Int16 PostTry = 0;
                String output = String.Empty;
                while (PostTry < 5)
                {
                    try
                    {
                        AuthorizePost();
                        String response = client.UploadString(address, request.serialize());
                        Logger.Instance.LogInfo(String.Concat("Response is : ", response));
                        IsInError = false;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogWarning($"Post V1 for {apiPath} exception occured :");
                        Logger.Instance.LogWarning(ex.ToString());
                    }
                    Thread.Sleep(500);
                }
            }

            return String.Empty;

        }
        private String PostV2(string apiPath, String body, long nonce)
        {
            IsInError = false;
            Logger.Instance.LogInfo($"Bitfinex Post V2 selected for the method {apiPath}");
            String address = _PostUrl + apiPath;

            using (WebClient client = new WebClient())
            {

                client.Headers["bfx-apikey"] = _Key;
                client.Headers["bfx-nonce"] = nonce.ToString();
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.Headers["bfx-signature"] = EncryptPost($"/api{apiPath}{nonce}{body}", new HMACSHA384());

                Int16 PostTry = 0;
                String output = String.Empty;
                while (PostTry < 5)
                {
                    try
                    {
                        AuthorizePost();
                        output = client.UploadString(address, body);
                        Logger.Instance.LogInfo(String.Concat("Response is : ", output));
                        break;

                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogWarning($"Post V2 for {apiPath} exception occured :");
                        Logger.Instance.LogWarning(ex.ToString());
                        if (PostTry >= 3)
                        {
                            IsInError = true;
                            Logger.Instance.LogError($"Post V2 for {apiPath} exception occured :");
                            Logger.Instance.LogError(ex);
                            return "error";
                        }
                        PostTry++;
                        //wait 0.5 secondes before retrying
                        Thread.Sleep(500);
                    }
                }

                if (String.IsNullOrEmpty(output))
                    IsInError = true;

                return output;
            }
        }
        #endregion

        #region http get
        /// <summary>
        /// Gets the tick.
        /// </summary>
        /// <returns>The tick.</returns>
        protected override Ticker GetTick(String currency)
        {
            Logger.Instance.LogInfo("BitFinex Get Tick start");

            String address = _GetUrl + "/v2/ticker/" + Pair(currency);

            var content = HttpGet(address);

            if (IsInError)
            {
                Logger.Instance.LogInfo("BitFinex Get Tick end with error");

                return null;
            }

            Ticker tick = new Ticker();
            var listParam = content.Split(',');
            tick.Bid = Convert.ToDouble(listParam[0].Substring(1));
            tick.Ask = Convert.ToDouble(listParam[2]);
            tick.Last = Convert.ToDouble(listParam[6]);
            tick.Low = Convert.ToDouble(listParam[9].Replace("]", String.Empty));
            tick.High = Convert.ToDouble(listParam[8]);
            tick.Volume = Convert.ToDouble(listParam[7]);
            tick.Timestamp = DateTime.Now.getUnixMilliTime();
            Logger.Instance.LogInfo($"BitFinex last Tick is {tick.Last}, last bid is : {tick.Bid}, last ask is : {tick.Ask}");
            Logger.Instance.LogInfo("BitFinex Get Tick end");

            return tick;
        }
        public override Dictionary<String, Ticker> GetTicks()
        {
            Logger.Instance.LogInfo("BitFinex Get all Ticks start");

            String currenciesinLine = String.Empty;
            foreach (var currency in _QuoteCurrencies)
            {
                currenciesinLine += "t" + Pair(currency) + ",";
            }
            currenciesinLine = currenciesinLine.Remove(currenciesinLine.Length - 1);
            String address = _GetUrl + "/v2/tickers?symbols=" + currenciesinLine;

            var content = HttpGet(address);

            if (IsInError)
            {
                Logger.Instance.LogInfo("BitFinex Get Tick end with error");

                return null;
            }
            //remove the first and last []
            content = content.Replace("[[", String.Empty).Replace("]]", String.Empty);

            var listtick = content.Split(new string[] { "],[" }, StringSplitOptions.None).ToList();

            Dictionary<String, Ticker> oticks = new Dictionary<String, Ticker>();

            foreach (var onetick in listtick)
            {
                Ticker tick = new Ticker();
                var listParam = onetick.Split(',');
                tick.Bid = Convert.ToDouble(listParam[1]);
                tick.Ask = Convert.ToDouble(listParam[3]);
                tick.Last = Convert.ToDouble(listParam[7]);
                tick.Low = Convert.ToDouble(listParam[10].Replace("]", String.Empty));
                tick.High = Convert.ToDouble(listParam[9]);
                tick.Volume = Convert.ToDouble(listParam[8]);
                tick.Timestamp = DateTime.Now.getUnixMilliTime();
                String currencyName = listParam[0].Substring(2, listParam[0].Length - listParam[0].IndexOf(_BaseCurrency) - 1);//format currency  name based the base currency to manage 4 letters currency
                switch (currencyName)
                {
                    case "DSH":
                        currencyName = "DASH";
                        break;
                    case "IOT":
                        currencyName = "IOTA";
                        break;
                    case "QTM":
                        currencyName = "QTUM";
                        break;
                    case "DAT":
                        currencyName = "DATA";
                        break;
                    case "QSH":
                        currencyName = "QASH";
                        break;
                    case "AIO":
                        currencyName = "AION";
                        break;
                    case "IOS":
                        currencyName = "IOST";
                        break;
                    case "ODE":
                        currencyName = "ODEM";
                        break;
                    default:
                        break;
                }
                Logger.Instance.LogInfo($"BitFinex last Tick for {currencyName} is {tick.Last} , last bid is : {tick.Bid}, last ask is : {tick.Ask}");

                oticks.Add(currencyName, tick);
            }

            Logger.Instance.LogInfo($"BitFinex last all Ticks retrieved, found {oticks.Count()} ticks");
            Logger.Instance.LogInfo("BitFinex Get all Ticks end");
            return oticks;

        }
		#endregion

		#region http post
		/// <summary>
		/// Refreshs the wallet amount.
		/// bitfinex api v2
		/// </summary>
		public override Boolean RefreshWallet()
        {
            Logger.Instance.LogInfo("Bitfinex Refresh Wallet started");
            const String apiPath = "/v2/auth/r/wallets";
            const String body = "{}";
            String output = PostV2(apiPath, body, DateTime.Now.getUnixTime());

            //call post failed three times then stop process
            if (IsInError)
            {
                Logger.Instance.LogWarning("Bitfinex Refresh wallet failed");
                return false;
            }

            if (output == "[]")
            {
                //when poor people run WhiteCow whitout fund like Mick
                BaseWallet.amount = 0.0;
                return true;
            }
            else
            {
                output = output.Substring(1, output.Length - 2);

                //split by array
                var Wallets = output.Split(new[] { "],[" }, StringSplitOptions.None).ToList();
                Wallets[0] = Wallets[0].Substring(1);
                Wallets[Wallets.Count - 1] = Wallets[Wallets.Count - 1].Substring(0, Wallets[Wallets.Count - 1].Length - 2);

                //clean data
                for (int i = 0; i < Wallets.Count; i++)
                {
                    Wallets[i] = Wallets[i].Replace("\"", "");
                }
                //refresh amount
                //base
                var wal = Wallets.FirstOrDefault(p => p.StartsWith($"margin,{BaseWallet.currency}", StringComparison.InvariantCulture));
                if (String.IsNullOrEmpty(wal))
                    BaseWallet.amount = 0.0;
                else
                    BaseWallet.amount = Convert.ToDouble(wal.Split(',')[2]);
                Logger.Instance.LogInfo($"Bitfinex Base wallet new amount : {BaseWallet.amount}");

                IsInError = false;
                Logger.Instance.LogInfo("Bitfinex Refresh wallet succeeded");
                return true;
            }
        }

        public override Double MarginBuy(String currency)
        {
            return MarginBuy(currency, BaseWallet.amount, BaseWallet.currency);
        }
        public override Double MarginBuy(String currency, Double Amount, String unit)
        {
            int nbtry = 0; //compute how many try to access to the API call

            while (nbtry < 3)
            {
                Logger.Instance.LogInfo("Bitfinex Margin buy started");
                long nonce = DateTime.Now.getUnixTime();
                const String apiPath = "/v1/order/new";

                BitfinexNewOrder request = new BitfinexNewOrder();
                PositionTypeEnum tempPos; //use to store the actual position if rollback we don't update the real position if it is ok we update the real position

                if (BaseWallet.currency == unit)
                    request.Amount = ((_Leverage * Amount > _MaximumSize ? _MaximumSize : _Leverage * Amount) / LastTicks[currency].Ask).ToString();
                else
                    request.Amount = Amount.ToString();

                tempPos = Entities.Trading.PositionTypeEnum.Long;
                request.Request = apiPath;
                request.Nonce = nonce.ToString();
                request.Symbol = Pair(currency);
                request.Price = LastTicks[currency].Ask.ToString("F99").TrimEnd('0');
                request.Side = "buy";
                request.Type = "market";
                request.use_all_available = "0";

                Logger.Instance.LogInfo($"Bitfinex margin amount is {request.Amount}");

                BitfinexNewOrderResponse response = BitfinexNewOrderResponse.FromJson(PostV1(apiPath, request));
                //call post failed three times then stop process
                if (IsInError)
                {
                    nbtry++;
                    Thread.Sleep(500); //wait for 500ms before retry
                    continue;
                }
                IsInError = false;

                QuoteAmount = Convert.ToDouble(response.OriginalAmount);
                Logger.Instance.LogInfo("Bitfinex Margin buy ended");
                Position = tempPos;
                return QuoteAmount;
            }
            return Double.NaN;
        }

        public override Double MarginSell(String currency)
        {
            return MarginSell(currency, BaseWallet.amount, BaseWallet.currency);
        }
        public override Double MarginSell(String currency, Double Amount, String unit)
        {
            int nbtry = 0; //compute how many try to access to the API call
            while (nbtry < 3)
            {
                Logger.Instance.LogInfo("Bitfinex Margin sell started");
                long nonce = DateTime.Now.getUnixTime();
                const String apiPath = "/v1/order/new";

                PositionTypeEnum tempPos; //use to store the actual position if rollback we don't update the real position if it is ok we update the real position


                BitfinexNewOrder request = new BitfinexNewOrder();
                request.Request = apiPath;
                request.Nonce = nonce.ToString();
                request.Symbol = Pair(currency);

                if (BaseWallet.currency == unit)
                    request.Amount = ((_Leverage * Amount > _MaximumSize ? _MaximumSize : _Leverage * Amount) / LastTicks[currency].Ask).ToString();
                else
                    request.Amount = Amount.ToString();

                tempPos = Entities.Trading.PositionTypeEnum.Short;

                request.Price = LastTicks[currency].Bid.ToString("F99").TrimEnd('0');
                request.Side = "sell";
                request.Type = "market";
                request.use_all_available = "0";
                Logger.Instance.LogInfo($"Bitfinex margin quoted amount is {request.Amount}");

                BitfinexNewOrderResponse response = BitfinexNewOrderResponse.FromJson(PostV1(apiPath, request));
                //call post failed three times then stop process
                if (IsInError)
                {
                    nbtry++;
                    Thread.Sleep(500); //wait for 500ms before retry
                    continue;
                }

                QuoteAmount = Convert.ToDouble(response.OriginalAmount);
                Logger.Instance.LogInfo("Bitfinex Margin sell ended");
                Position = tempPos;
                return QuoteAmount;

            }
            return Double.NaN;
        }






        public override Boolean ClosePosition(String currency)
        {
            Double ClosedAmount;
            switch (Position)
            {
                case Entities.Trading.PositionTypeEnum.Long:
                    Logger.Instance.LogInfo("Bitfinex Close position call margin sell");
                    ClosedAmount = MarginSell(currency, QuoteAmount, currency);
                    break;
                case Entities.Trading.PositionTypeEnum.Short:
                    Logger.Instance.LogInfo("Bitfinex Close position call margin buy");
                    ClosedAmount = MarginBuy(currency, QuoteAmount, currency);
                    break;
                default:
                    return true;
            }

            if (!Double.IsNaN(ClosedAmount))
            {
                Position = Entities.Trading.PositionTypeEnum.Out;
                return true;
            }
            else
                return false;

        }

        public override List<Tuple<String, Double>> GetOpenOrders(String currency)
        {
            //nothing todo bitfinex has a function to operate at market price so it is not possile to have an open orders
            return null;
        }

        public override Boolean CancelOpenOrder(String OrderId)
        {
	   
			Logger.Instance.LogInfo("Bitfinex Cancel Order started");
			long nonce = DateTime.Now.getUnixTime();
			const String apiPath = "/v1/order/cancel";

			var request = new BitfinexOrderStatus();
			request.Request = apiPath;
			request.Nonce = nonce.ToString();
            request.OrderId = Convert.ToInt64(OrderId);
			try
			{
				PostV1(apiPath, request);
				Logger.Instance.LogInfo("Bitfinex Cancel Order ended");
				return true;
			}
			catch (Exception ex)
			{
				Logger.Instance.LogError("bitfinex Cancel Order Failed");
				Logger.Instance.LogError(ex);
				return false;
			}
		}
        #endregion


        protected override string Pair(string Currency)
        {
            String formatedCurrency;
            switch (Currency)
            {
                case "DASH":
                    formatedCurrency = "DSH";
                    break;
                case "IOTA":
                    formatedCurrency = "IOT";
                    break;
                case "QTUM":
                    formatedCurrency = "QTM";
                    break;
                case "DATA":
                    formatedCurrency = "DAT";
                    break;
                case "QASH":
                    formatedCurrency = "QSH";
                    break;
                case "AION":
                    formatedCurrency = "AIO";
                    break;
                case "IOST":
                    formatedCurrency = "IOS";
                    break;
                case "ODEM":
                    formatedCurrency = "ODE";
                    break;
                default:
                    formatedCurrency = Currency;
                    break;
            }

            return formatedCurrency + _BaseCurrency;
        }


    }
}
