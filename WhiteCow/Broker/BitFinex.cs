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

namespace WhiteCow.Broker
{
    public class BitFinex : Broker
    {


        public BitFinex() : base(Plateform.BitFinex.ToString())
        {
            QuoteWallet = new Wallet { currency = _Pair.Substring(0, 3) };
            BaseWallet = new Wallet { currency = _Pair.Substring(3, 3) };
            RefreshWallet();
        }
        #region private 
        private String PostV1(string apiPath, BitfinexPostBase request)
        {
            IsInError = false;
            Logger.Instance.LogInfo($"Bitfinex Post V1 selected for the method {apiPath}");
            String body64 = Base64Encode(request.serialize());
            String address = _PostUrl + apiPath;
            using (WebClient client = new WebClient())
            {
                client.Headers["X-BFX-APIKEY"] = _Key;
                client.Headers["X-BFX-PAYLOAD"] = body64;
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.Headers["X-BFX-SIGNATURE"] = EncryptPost(body64, new HMACSHA384());

                Int16 PostTry = 0;
                while (PostTry < 3)
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
                        if (PostTry >= 3)
                        {
                            IsInError = true;
                            Logger.Instance.LogError($"Post V1 for {apiPath} exception occured :");
                            Logger.Instance.LogError(ex);

                            return "error";
                        }
                        PostTry++;
                        //wait 5 secondes before retrying
                        Thread.Sleep(5000);
                    }
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
                while (PostTry < 3)
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
                        //wait 5 secondes before retrying
                        Thread.Sleep(5000);
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
        protected override Ticker GetTick()
        {
			Logger.Instance.LogInfo("BitFinex Get Tick start");

			String address = _GetUrl + "/v2/ticker/t" + _Pair;
         
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

			Logger.Instance.LogInfo("BitFinex Get Tick end");

			return tick;
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
				QuoteWallet.amount = 0.0;
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
                //quote 
                wal = Wallets.FirstOrDefault(p => p.StartsWith($"margin,{QuoteWallet.currency}", StringComparison.InvariantCulture));

                if (String.IsNullOrEmpty(wal))
                    QuoteWallet.amount = 0.0;
                else
                    QuoteWallet.amount = Convert.ToDouble(wal.Split(',')[2]);
                Logger.Instance.LogInfo($"Bitfinex Quote wallet new amount : {QuoteWallet.amount}");
                IsInError = false;
                Logger.Instance.LogInfo("Bitfinex Refresh wallet succeeded");
                return true;
            }
        }

        public String ListOpenPositions()
        {
            Logger.Instance.LogInfo("Bitfinex List Open Position started");
            const String apiPath = "/v2/auth/r/positions";
            var nonce = DateTime.Now.getUnixTime();
            const String body = "{}";
            String res = PostV2(apiPath, body, nonce);

            //call post failed three times then stop process
            if (IsInError)
            {
                Logger.Instance.LogWarning("BitFinex List Open postion failed");
                return String.Empty;
            }
            IsInError = false;

            Logger.Instance.LogInfo("Bitfinex List Open Position end");
            return res;

        }

        protected override Double GetAverageYieldLoan()
        {
            Logger.Instance.LogInfo("Bitfinex Get average Yield started");
            const String apiPath = "/v2/auth/r/info/funding/";
            var nonce = DateTime.Now.getUnixTime();
            const String body = "{}";
            String res = PostV2(apiPath + "f" + BaseWallet.currency, body, nonce);

            //call post failed three times then stop process
            if (IsInError)
            {
                Logger.Instance.LogWarning("BitFinex Get average Yield has failed");

                return Double.NaN;
            }
            IsInError = false;
            Logger.Instance.LogInfo("Bitfinex Get average Yield end");
            return Convert.ToDouble(res.Split(',')[3]);
        }

        public bool Account_info()
        {
            Logger.Instance.LogInfo("Bitfinex Account info started");
            long nonce = DateTime.Now.getUnixTime();
            const String apiPath = "/v1/account_infos";
            BitfinexPostBase request = new BitfinexPostBase();
            request.Request = apiPath;
            request.Nonce = nonce.ToString();

            Console.WriteLine(PostV1(apiPath, request));

            //call post failed three times then stop process
            if (IsInError)
            {
				Logger.Instance.LogWarning("BitFinex Account info has failed");
                return false;
            }
            IsInError = false;
            Logger.Instance.LogInfo("Bitfinex Account info ended");
            return true;
        }


        public override bool MarginBuy()
        {
            Logger.Instance.LogInfo("Bitfinex Margin buy started");
            long nonce = DateTime.Now.getUnixTime();
            const String apiPath = "/v1/order/new";

            BitfinexNewOrder request = new BitfinexNewOrder();
            request.Request = apiPath;
            request.Nonce = nonce.ToString();
            request.Symbol = _Pair.ToLower();
            if (Position == Entities.Trading.PositionTypeEnum.Out)
            {
                request.Amount = (_Leverage* BaseWallet.amount>_MaximumSize?_MaximumSize:BaseWallet.amount / LastTick.Last).ToString();
                Position = Entities.Trading.PositionTypeEnum.Long;
            }
            else
            {
                request.Amount = QuoteAmount.ToString();
                Position = Entities.Trading.PositionTypeEnum.Out;
            }

            request.Price = LastTick.Last.ToString();
            request.Side = "buy";
            request.Type = "market";
            request.use_all_available = "1";

            Logger.Instance.LogInfo($"Bitfinex margin amount is {request.Amount}");
            try
            {
                BitfinexNewOrderResponse response = BitfinexNewOrderResponse.FromJson(PostV1(apiPath, request));
                //call post failed three times then stop process
                if (IsInError)
                {
                    Logger.Instance.LogWarning("BitFinex Margin Buy has failed");
                    return false;
                }
                IsInError = false;

                QuoteAmount = Convert.ToDouble(response.OriginalAmount);
                Logger.Instance.LogInfo("Bitfinex Margin buy ended");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("bitfinex Margin buy failed");
                Logger.Instance.LogError(ex);
                IsInError = true;
                return false;
            }

        }

        public override Boolean MarginSell()
        {
            Logger.Instance.LogInfo("Bitfinex Margin sell started");
            long nonce = DateTime.Now.getUnixTime();
            const String apiPath = "/v1/order/new";

            BitfinexNewOrder request = new BitfinexNewOrder();
            request.Request = apiPath;
            request.Nonce = nonce.ToString();
            request.Symbol = _Pair.ToLower();

            if (Position == Entities.Trading.PositionTypeEnum.Out)
            {
                request.Amount = (_Leverage* BaseWallet.amount > _MaximumSize ? _MaximumSize : BaseWallet.amount / LastTick.Last).ToString();
                Position = Entities.Trading.PositionTypeEnum.Short;
            }
            else
            {
                request.Amount = QuoteAmount.ToString();
                Position = Entities.Trading.PositionTypeEnum.Out;
            }
            request.Price = LastTick.Last.ToString();
            request.Side = "sell";
            request.Type = "market";
            request.use_all_available = "1";
            Logger.Instance.LogInfo($"Bitfinex margin amount is {request.Amount}");
            try
            {
                BitfinexNewOrderResponse response = BitfinexNewOrderResponse.FromJson(PostV1(apiPath, request));
                //call post failed three times then stop process
                if (IsInError)
                {
                    Logger.Instance.LogWarning("BitFinex Margin Buy has failed");
                    return false;
                }

                QuoteAmount = Convert.ToDouble(response.OriginalAmount);
                Logger.Instance.LogInfo("Bitfinex Margin sell ended");
                return true;
            }
            catch (Exception ex)
            {
				Logger.Instance.LogError("bitfinex Margin sell failed");
				Logger.Instance.LogError(ex);
                IsInError = true;
                return false;
            }
        }

        public Boolean CancelOrder(Int64 orderId)
        {
            Logger.Instance.LogInfo("Bitfinex Cancel Order started");
            long nonce = DateTime.Now.getUnixTime();
            const String apiPath = "/v1/order/cancel";

            var request = new BitfinexOrderStatus();
            request.Request = apiPath;
            request.Nonce = nonce.ToString();
            request.OrderId = orderId;
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


        public override bool Send(string DestinationAddress, double Amount)
        {
            Logger.Instance.LogInfo("Bitfinex Send money started");
            long nonce = DateTime.Now.getUnixTime();
            const String apiPath = "/v1/withdraw";

            BitFinexWithDrawal request = new BitFinexWithDrawal();
            request.Nonce = nonce.ToString();
            request.Request = apiPath;
            request.WithDrawType = "bitcoin";
            request.WalletSelected = "trading";
            request.Amount = Amount.ToString();
            request.Address = DestinationAddress;

            String response = PostV1(apiPath, request);

            //call post failed three times then stop process
            if (IsInError)
            {
                Logger.Instance.LogError("Bitfinex Send money has failed");
                return false;
            }
            IsInError = false;

            Logger.Instance.LogInfo("Bitfinex Send money ended");
            return true;

        }

        public override bool ClosePosition()
        {
            switch(Position)
            {
                case Entities.Trading.PositionTypeEnum.Long:
                    Logger.Instance.LogInfo("Bitfinex Close position call margin sell");
                    return MarginSell();
                case Entities.Trading.PositionTypeEnum.Short:
                    Logger.Instance.LogInfo("Bitfinex Close position call margin buy");
                    return MarginBuy();
                default:
                    return true;
            }
        
        }

        public override double GetWithDrawFees()
        {
            if (Fees == null)
            {
                Logger.Instance.LogInfo("BitFinex Call WithDraw fees started");
                do
                {
                    long nonce = DateTime.Now.getUnixTime();
                    const String apiPath = "/v1/account_fees";
                    BitfinexPostBase request = new BitfinexPostBase();
                    request.Nonce = nonce.ToString();
                    request.Request = apiPath;
                    String content = PostV1(apiPath, request);
                    if (IsInError)
                        continue;
                    Fees = BitFinexAccountFees.FromJson(content).Withdraw;
                    Logger.Instance.LogInfo("BitFinex Call WithDraw fees ended");
                } while (IsInError);
            }
            return Fees[BaseWallet.currency];
		}

       
        #endregion
    }
}
