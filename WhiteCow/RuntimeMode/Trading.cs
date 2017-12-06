using System;
using System.Configuration;
using System.Threading;
using WhiteCow.Broker;
using WhiteCow.Interface;
using WhiteCow.Entities.Trading;
using WhiteCow.Log;

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
        public Trading()
        {
            ThresholdGap=Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.ThresholdGap"]);

        }
        /// <summary>
        /// start trading engine
        /// </summary>
        public void StartToMooh()
		{
            
            //while(true)
            TickGapAnalisys();
		}

		/// <summary>
		/// Analyse if the gap is enough to start trading
		/// if not then wait
		/// </summary>
		private void TickGapAnalisys()
		{
            Logger.Instance.LogInfo("Tick analysis");
            Step = TradingStep.Tick;
			Poloniex polo = new Poloniex();
			BitFinex btx = new BitFinex();
           
			//tick analisys
			Double gap = 0.0;
			do
			{
				Logger.Instance.LogInfo("Gap is not enough wait 10sec");
				Thread.Sleep(10000);

				if (polo.LastTick == null)
					continue;


				if (btx.LastTick == null)
					continue;

				if (polo.LastTick.Last > btx.LastTick.Last)
					gap =100 * polo.LastTick.Last / btx.LastTick.Last - 100.0;
				else
					gap = 100* btx.LastTick.Last / polo.LastTick.Last - 100.0;

                Logger.Instance.LogInfo($"Gap is {gap}");
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
		/// <param name="Brlow">low broker ticker</param>
		/// <param name="BrHigh">low broker ticker</param>
		private void SetPosition(Broker.Broker Brlow, Broker.Broker BrHigh)
		{
            Logger.Instance.LogInfo("taking position..");
            Step = TradingStep.OpenPosition;
			Brlow.MarginBuy();
			BrHigh.MarginSell();
            Logger.Instance.LogInfo("Position done");
			ClosePosition(Brlow, BrHigh);
		}
		/// <summary>
		/// wait for the cross or nearly the cross for closing position
		/// </summary>
		/// <param name="Brlow">low broker ticker</param>
		/// <param name="BrHigh">low broker ticker</param>
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

                gap = 100 * BrHigh.LastTick.Last / Brlow.LastTick.Last - 100;
				Logger.Instance.LogInfo($"Gap is {gap}");

			} while (gap > Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.Closegap"]));

			BrHigh.ClosePosition();
			Brlow.ClosePosition();

            Logger.Instance.LogInfo("position closed"); 
             
            EquilibrateFund(Brlow,BrHigh);
		}
		/// <summary>
		/// Reequilibrate all broker
		/// </summary>
		/// <param name="Brlow">low broker ticker</param>
		/// <param name="BrHigh">low broker ticker</param>
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

        public void Dispose()
        {
            
        }
    }
}
