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

namespace WhiteCow.Broker
{
    public class Poloniex : Broker
    {
        public Poloniex() : base(Plateform.Poloniex.ToString())
        {
            QuoteWallet = new Wallet { currency = _Pair.Split('_')[1] };
			BaseWallet = new Wallet { currency = _Pair.Split('_')[0] };
            RefreshWallet();
        }

		#region private

		

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
        public override Ticker GetTick()
        {
            String address = _GetUrl + "/public?command=returnTicker";
            WebClient client = new WebClient();


            var poloticker = PoloniexTicker.FromJson(client.DownloadString(address));

            Ticker otick = new Ticker();
            otick.Ask = poloticker[_Pair].LowestAsk;
            otick.Bid = poloticker[_Pair].HighestBid;
            otick.Last = poloticker[_Pair].Last;
            otick.Low = poloticker[_Pair].Low24hr;
            otick.High = poloticker[_Pair].High24hr;
            otick.Volume = poloticker[_Pair].BaseVolume;

            return otick;

        }
        #endregion


        #region http post
        public override bool MarginBuy()
        {
            throw new NotImplementedException();
        }

        public override bool MarginSell()
        {
            throw new NotImplementedException();
        }

        public override bool RefreshWallet()
        {
            String PostData = "command=returnBalances&nonce=" + DateTime.Now.getUnixMilliTime();

			String content = Post(PostData);

            if (IsInError)
                return false;
            
            //retrieve the right wallet
            var allWallet = content.Split(',').ToList();

            try
            {
                BaseWallet.amount = Convert.ToDouble(allWallet.First(p => p.Contains(BaseWallet.currency)).Split(':')[1].Replace('\"', '0'), CultureInfo.InvariantCulture);
                QuoteWallet.amount = Convert.ToDouble(allWallet.First(p => p.Contains(QuoteWallet.currency)).Split(':')[1].Replace('\"', '0'), CultureInfo.InvariantCulture);
                                                     
            }
            catch
            {
                return false;
            }

            Console.WriteLine($"Base wallet amount : {BaseWallet.amount}{Environment.NewLine} Quote wallet amount {QuoteWallet.amount}");
            return true;
		}

        public override bool Send(string DestinationAddress, double Amount)
        {
            throw new NotImplementedException();
        }

        protected override double GetAverageYieldLoan()
        {
            throw new NotImplementedException();
        }
        #endregion

    } 

	
}


