using System;
using System.Configuration;
using System.Net;
using WhiteCow.Entities;
using WhiteCow.Entities.Poloniex.Ticker;

namespace WhiteCow.Broker
{
    public class Poloniex : Broker
    {
        public Poloniex() : base(Plateform.Poloniex.ToString())
        {
        }

        #region private
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
            throw new NotImplementedException();
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


