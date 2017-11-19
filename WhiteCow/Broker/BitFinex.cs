﻿using System;
using System.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using WhiteCow.Entities;
using WhiteCow.Extension;
using System.Linq;
using WhiteCow.Entities.Bitfinex.PostRequest.V1;
using System.Diagnostics;

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
            String body64 = Base64Encode(request.serialize());
            String address = _PostUrl + apiPath;
            WebClient client = new WebClient();
            client.Headers["X-BFX-APIKEY"] = _Key;
            client.Headers["X-BFX-PAYLOAD"] = body64;
            client.Headers[HttpRequestHeader.ContentType] = "application/json";
            client.Headers["X-BFX-SIGNATURE"] = EncryptPost(body64, new HMACSHA384());
            String response = client.UploadString(address, request.serialize());
            Debug.WriteLine(response);
            return response;

        }
        private String PostV2(string apiPath, String body, long nonce)
        {
            String address = _PostUrl + apiPath;

            WebClient client = new WebClient();

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
                    output = client.UploadString(address, body);
                    break;

                }
                catch (Exception ex)
                {
                    Console.WriteLine("refresh Wallet post exception occured :");
                    Console.WriteLine(ex.ToString());
                    if (PostTry >= 3)
                        return "error";
                    PostTry++;

                }
            }
            
            Console.WriteLine(output);
            return output;
        }
        #endregion

        #region http get
        /// <summary>
        /// Gets the tick.
        /// </summary>
        /// <returns>The tick.</returns>
        public override Ticker GetTick()
        {
            //System.Security.Cryptography.AesCryptoServiceProvider b = new System.Security.Cryptography.AesCryptoServiceProvider();
            String address = _GetUrl + "/v2/ticker/t" + _Pair;
            WebClient client = new WebClient();
            var content = client.DownloadString(address);
            Ticker tick = new Ticker();
            var listParam = content.Split(',');
            tick.Bid = Convert.ToDouble(listParam[0].Substring(1));
            tick.Ask = Convert.ToDouble(listParam[2]);
            tick.Last = Convert.ToDouble(listParam[6]);
            tick.Low = Convert.ToDouble(listParam[9].Replace("]", String.Empty));
            tick.High = Convert.ToDouble(listParam[8]);
            tick.Volume = Convert.ToDouble(listParam[7]);
            tick.Timestamp = DateTime.Now.getUnixTime();

            Last = tick.Last;
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

            const String apiPath = "/v2/auth/r/wallets";
            const String body = "{}";
            String output = PostV2(apiPath, body, DateTime.Now.getUnixTime());

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
            Console.WriteLine($"Base wallet new amount : {BaseWallet.amount}");
            //quote 
            wal = Wallets.FirstOrDefault(p => p.StartsWith($"margin,{QuoteWallet.currency}", StringComparison.InvariantCulture));

            if (String.IsNullOrEmpty(wal))
                QuoteWallet.amount = 0.0;
            else
                QuoteWallet.amount = Convert.ToDouble(wal.Split(',')[2]);
            Console.WriteLine($"Quote wallet new amount : {QuoteWallet.amount}");
            return true;
        }

        public String ListOpenPositions()
        {

            const String apiPath = "/v2/auth/r/positions";
            var nonce = DateTime.Now.getUnixTime();
            const String body = "{}";
            String res = PostV2(apiPath, body, nonce);
            return res;

        }

        protected override Double GetAverageYieldLoan()
		{

			const String apiPath = "/v2/auth/r/info/funding/";
			var nonce = DateTime.Now.getUnixTime();
			const String body = "{}";
            String res = PostV2(apiPath+"f"+BaseWallet.currency, body, nonce);

            return Convert.ToDouble(res.Split(',')[3]);
		}

        public bool Account_info()
        {
            long nonce = DateTime.Now.getUnixTime();
            const String apiPath = "/v1/account_infos";
            BitfinexPostBase request = new BitfinexPostBase();
            request.Request = apiPath;
            request.Nonce = nonce.ToString();

            Console.WriteLine(PostV1(apiPath, request));

            return true;
        }


        public override bool MarginBuy()
        {
            long nonce = DateTime.Now.getUnixTime();
            const String apiPath = "/v1/order/new";

            BitfinexNewOrder request = new BitfinexNewOrder();
            request.Request = apiPath;
            request.Nonce = nonce.ToString();
            request.Symbol = _Pair.ToLower();
            if (Position == Entities.Trading.PositionTypeEnum.Out)
            {
                request.Amount = (BaseWallet.amount / Last).ToString();
                Position = Entities.Trading.PositionTypeEnum.Long;
            }
            else
            {
                request.Amount = QuoteAmount.ToString();
                Position = Entities.Trading.PositionTypeEnum.Out;
            }

            request.Price = Last.ToString();
            request.Side = "buy";
            request.Type = "market";
            request.use_all_available = "1";
            try
            {
                BitfinexNewOrderResponse response = BitfinexNewOrderResponse.FromJson(PostV1(apiPath, request));
                QuoteAmount = Convert.ToDouble(response.OriginalAmount);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }

        }

        public override Boolean MarginSell()
        {
            long nonce = DateTime.Now.getUnixTime();
            const String apiPath = "/v1/order/new";

            BitfinexNewOrder request = new BitfinexNewOrder();
            request.Request = apiPath;
            request.Nonce = nonce.ToString();
            request.Symbol = _Pair.ToLower();

            if (Position == Entities.Trading.PositionTypeEnum.Out)
            {
                request.Amount = (BaseWallet.amount / Last).ToString();
                Position = Entities.Trading.PositionTypeEnum.Short;
            }
            else
            {
                request.Amount = QuoteAmount.ToString();
                Position = Entities.Trading.PositionTypeEnum.Out;
            }
            request.Price = Last.ToString();
            request.Side = "sell";
            request.Type = "market";
            request.use_all_available = "1";

            try
            {
                BitfinexNewOrderResponse response = BitfinexNewOrderResponse.FromJson(PostV1(apiPath, request));
                QuoteAmount = Convert.ToDouble(response.OriginalAmount);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public Boolean CancelOrder(Int64 orderId)
        {
            long nonce = DateTime.Now.getUnixTime();
            const String apiPath = "/v1/order/cancel";

            var request = new BitfinexOrderStatus();
            request.Request = apiPath;
            request.Nonce = nonce.ToString();
            request.OrderId = orderId;
            try
            {
                PostV1(apiPath, request);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }


        public override bool Send(string DestinationAddress, double Amount)
        {
            long nonce = DateTime.Now.getUnixTime();
            const String apiPath = "/v1/withdraw";

            BitFinexWithDrawal request = new BitFinexWithDrawal();
            request.Nonce = nonce.ToString();
            request.Request = apiPath;
            request.WithDrawType = "bitcoin";
            request.WalletSelected = "trading";
            request.Amount = Amount.ToString();
            request.Address = DestinationAddress;
            try
            {
                String response = PostV1(apiPath, request);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        #endregion
    }
}
