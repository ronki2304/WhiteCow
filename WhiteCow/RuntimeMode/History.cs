using System;
using System.Configuration;
using System.IO;
using WhiteCow.Broker;

namespace WhiteCow.RuntimeMode
{
    public class History
    {
		Poloniex polo ;
		BitFinex btx ;
		string fileName = "history.csv";
        public History()
        {
			polo = new Poloniex();
			btx = new BitFinex();

			Console.WriteLine("History mode enable");
			String Header = "Date;Bitfinex;Cex.io;Poloniex";
			if (!File.Exists(fileName))
				File.AppendAllText(fileName, Header + Environment.NewLine);
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
    }
}
