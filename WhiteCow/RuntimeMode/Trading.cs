using System;
using System.Configuration;
using System.Threading;
using WhiteCow.Broker;
using WhiteCow.Interface;
using WhiteCow.Entities.Trading;
using WhiteCow.Log;
using System.IO;

namespace WhiteCow.RuntimeMode
{
    /// <summary>
    /// the power of white cow
    /// here is the trading intelligence
    /// </summary>
    public class Trading:IRuntimeMode
    {
        readonly Double ThresholdGap;
        public TradingStep Step;
<<<<<<< HEAD
		
        //trading plateform
        Poloniex polo;
		BitFinex btx;
        public Trading()
        {
            ThresholdGap=Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.ThresholdGap"]);
			polo = new Poloniex();
			btx = new BitFinex();
=======
        readonly Boolean LogToFile;
		readonly string fileName = "Runtime.csv";

        public Trading()
        {
            ThresholdGap = Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.ThresholdGap"]);
            LogToFile = Convert.ToBoolean(ConfigurationManager.AppSettings["Runtime.LogTicks"]);

            if (LogToFile)
            {
                String Header = "Date;BrHigh Name;BrHigh Last;BrHigh Ask;BrHigh Bid;BrLow Name;BrLow Last;BrLow Ask;BrLow Bid;state";
                if (!File.Exists(fileName))
                    File.AppendAllText(fileName, Header + Environment.NewLine);
            }
>>>>>>> 311e4a3... add log to track the close position issue
        }
        /// <summary>
        /// start trading engine
        /// </summary>
        public void StartToMooh()
        {
<<<<<<< HEAD
          while (true)
                TickGapAnalisys();
=======

			Poloniex polo = new Poloniex();
			BitFinex btx = new BitFinex();

            while (true)
                TickGapAnalisys(polo,btx);
>>>>>>> 311e4a3... add log to track the close position issue
            
        }

		/// <summary>
		/// Analyse if the gap is enough to start trading
		/// if not then wait
		/// </summary>
		private void TickGapAnalisys(Poloniex polo,BitFinex btx)
		{
            Logger.Instance.LogInfo("Tick analysis");
            Step = TradingStep.Tick;
<<<<<<< HEAD
			polo.RefreshWallet();
			btx.RefreshWallet();
           
=======
			
>>>>>>> 311e4a3... add log to track the close position issue
			//tick analisys
			Double gap = 0.0;
			do
			{
				Logger.Instance.LogInfo("Gap is not enough wait 10sec");
                Logger.Instance.LogInfo(Environment.NewLine);
				Thread.Sleep(10000);

				if (polo.LastTick == null)
					continue;


				if (btx.LastTick == null)
					continue;

				//check if the broker answer in time to be efficient if not play again
                if (Math.Abs(btx.LastTick.Timestamp - polo.LastTick.Timestamp) >= 4000)
					continue;

				if (polo.LastTick.Last > btx.LastTick.Last)
                    gap = 100.0 * (polo.LastTick.Bid / btx.LastTick.Ask - 1.0);
				else
                    gap = 100.0 * (btx.LastTick.Bid / polo.LastTick.Ask - 1.0);

                Logger.Instance.LogInfo($"Gap is {gap}%");
			} while (gap < ThresholdGap);

            Logger.Instance.LogInfo("Gap is large enough take position");

		
			if (polo.LastTick.Last > btx.LastTick.Last)
				SetPosition(btx, polo);
			else
				SetPosition(polo, btx);


		}

		/// <summary>
		/// put all positions in both platform
		/// </summary>
		/// <param name="Brlow">low broker ticker, the long one</param>
		/// <param name="BrHigh">High broker ticker, the short one</param>
		private void SetPosition(Broker.Broker Brlow, Broker.Broker BrHigh)
		{
            Logger.Instance.LogInfo("taking position..");
            Step = TradingStep.OpenPosition;
            Double amount = Brlow.BaseWallet.amount < BrHigh.BaseWallet.amount ? Brlow.BaseWallet.amount : BrHigh.BaseWallet.amount;
            Logger.Instance.LogInfo($"amount for trading is {amount} {Brlow.BaseWallet.currency}");

            Brlow.MarginBuy(amount);
            BrHigh.MarginSell(amount);

            if (LogToFile)
                LogTicks(BrHigh, Brlow,"init position");

            Logger.Instance.LogInfo("Position done");
			ClosePosition(Brlow, BrHigh);
		}
		/// <summary>
		/// wait for the cross or nearly the cross for closing position
		/// </summary>
		/// <param name="Brlow">low broker ticker, the long one</param>
		/// <param name="BrHigh">High broker ticker, the short one</param>
		private void ClosePosition(Broker.Broker Brlow, Broker.Broker BrHigh)
		{
            Logger.Instance.LogInfo("Now Waiting for the cross");
            Step = TradingStep.ClosePosition;
            double gap = Double.NaN;
			do
			{
				Logger.Instance.LogInfo($"Gap is is too large retry");

				Thread.Sleep(10000);

				if (Brlow.LastTick == null)
					continue;

				if (BrHigh.LastTick == null)
					continue;

<<<<<<< HEAD
                gap = 100 * BrHigh.LastTick.Ask / Brlow.LastTick.Bid - 100;
				Logger.Instance.LogInfo($"Gap is {gap}%");
=======
                //check if the broker answer in time to be efficient if not play again
				if (Math.Abs(BrHigh.LastTick.Timestamp - Brlow.LastTick.Timestamp) >= 4000)
                    continue;

                gap = 100.0 * (BrHigh.LastTick.Ask / Brlow.LastTick.Bid - 1.0);
				Logger.Instance.LogInfo($"Gap is {gap}");
				if (LogToFile)
                    LogTicks(BrHigh, Brlow,"check close");
>>>>>>> 311e4a3... add log to track the close position issue

			} while (gap > Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.Closegap"]));

			BrHigh.ClosePosition();
			Brlow.ClosePosition();

            Logger.Instance.LogInfo("position closed"); 
             
            //EquilibrateFund(Brlow,BrHigh);
		}
		/// <summary>
		/// Reequilibrate all broker
		/// </summary>
		/// <param name="Brlow">low broker ticker, the long one</param>
		/// <param name="BrHigh">High broker ticker, the short one</param>
		private void EquilibrateFund(Broker.Broker Brlow, Broker.Broker BrHigh)
		{
            Logger.Instance.LogInfo("Now transfer extra amount to reequilibrate");
            Step = TradingStep.FundTransfer;
            Brlow.RefreshWallet();
            BrHigh.RefreshWallet();
            Double amountToTransfer;

            //compute the amount to send
            //two differents case similar treatment
            if (Brlow.BaseWallet.amount>BrHigh.BaseWallet.amount)
            {
                amountToTransfer = Brlow.BaseWallet.amount - BrHigh.BaseWallet.amount - Brlow.GetWithDrawFees() / 2;
                Brlow.Send(BrHigh._PublicAddress,amountToTransfer);
                BrHigh.CheckReceiveFund(amountToTransfer);
            }
            else
            {
                amountToTransfer = BrHigh.BaseWallet.amount - Brlow.BaseWallet.amount - Brlow.GetWithDrawFees() / 2;
                BrHigh.Send(Brlow._PublicAddress, amountToTransfer);
                Brlow.CheckReceiveFund(amountToTransfer);
            }
            Logger.Instance.LogInfo($"full loop done {Environment.NewLine}Now go back to ticker analysis");
            
		}

        private void LogTicks(Broker.Broker BrHigh,Broker.Broker BrLow,String state )
        {
            String content = $"{DateTime.Now.ToString()};{BrHigh.Name.ToString()};{BrHigh.LastTick.Last};{BrHigh.LastTick.Ask};{BrHigh.LastTick.Bid};{BrLow.Name.ToString()};{BrLow.LastTick.Last};{BrLow.LastTick.Ask};{BrLow.LastTick.Bid};{state}";

            File.AppendAllText(fileName,content+Environment.NewLine);
        }

        public void Dispose()
        {
            
        }
    }
}
