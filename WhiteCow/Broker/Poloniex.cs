using System;
using System.Configuration;
using System.Net;
using WhiteCow.Entities;
using WhiteCow.Entities.Poloniex.Ticker;

namespace WhiteCow.Broker
{
    public static class Poloniex
    {
        static readonly String _Key;
        static readonly String _Secret;
        static readonly String _Url;
        static Poloniex()
        {
            _Key = ConfigurationManager.AppSettings["Poloniex.key"];
            _Secret = ConfigurationManager.AppSettings["Poloniex.secret"];
            _Url = ConfigurationManager.AppSettings["Poloniex.url"];
        }

        #region http get
        public static Ticker GetTick(String Pair)
        {
            System.Security.Cryptography.AesCryptoServiceProvider b = new System.Security.Cryptography.AesCryptoServiceProvider();
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;

            String address = _Url + "/public?command=returnTicker";
            WebClient client = new WebClient();


            var poloticker = PoloniexTicker.FromJson(client.DownloadString(address));

            Ticker otick = new Ticker();
            otick.Ask = poloticker[Pair].LowestAsk;
            otick.Bid = poloticker[Pair].HighestBid;
            otick.Last = poloticker[Pair].Last;
            otick.Low = poloticker[Pair].Low24hr;
            otick.High = poloticker[Pair].High24hr;
            otick.Volume = poloticker[Pair].BaseVolume;

            return otick;

        }
        #endregion

    }

	
}


