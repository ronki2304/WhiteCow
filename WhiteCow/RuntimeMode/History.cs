﻿using System;
using System.Configuration;
using System.IO;
using System.Threading;
using WhiteCow.Broker;
using WhiteCow.Interface;

namespace WhiteCow.RuntimeMode
{
    /// <summary>
    /// retrieving data for analysis before go to trading
    /// </summary>
    public class History: IRuntimeMode
    {
		Poloniex polo ;
		BitFinex btx ;
		string fileName = "history.csv";
		Timer tm;

        public void StartToMooh()
        {
            
			polo = new Poloniex();
			btx = new BitFinex();

			Console.WriteLine("History mode enable");
			String Header = "Date;Bitfinex;Cex.io;Poloniex";
			if (!File.Exists(fileName))
				File.AppendAllText(fileName, Header + Environment.NewLine);

			tm = new Timer(GenerateHistory, null, 0, Convert.ToInt32(ConfigurationManager.AppSettings["History.Interval"]) * 1000);

		}

        public void GenerateHistory(object state)
		{
			
			String bitfinextick, cextick, polotick;

			    var btfTicker = btx.LastTick;
			if (btfTicker != null)
			    bitfinextick = btfTicker.Last.ToString();
			else
			    bitfinextick = String.Empty;
			try
			{
			    var cexTicker = CexIO.GetTick(ConfigurationManager.AppSettings["Cex.io.Pair"]);
			    cextick = cexTicker.Last.ToString();
			}
			catch
			{ cextick = String.Empty; }


			    var PoloTicker = polo.LastTick;
			    if (PoloTicker != null)
			        polotick = PoloTicker.Last.ToString();
			    else
			        polotick = String.Empty;

			String content = $"{DateTime.Now.ToString()};{bitfinextick};{cextick};{polotick}";
			Console.WriteLine(content);

			File.AppendAllText(fileName, content + Environment.NewLine);


		}

        public void Dispose()
        {
            tm.Dispose();
        }
    }
}