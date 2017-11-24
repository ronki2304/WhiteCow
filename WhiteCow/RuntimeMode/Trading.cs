using System;
using System.Configuration;
using System.Threading;
using WhiteCow.Broker;
using WhiteCow.Interface;
using WhiteCow.Entities.Trading;

namespace WhiteCow.RuntimeMode
{
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
            Poloniex polo = new Poloniex();
            polo.CheckReceiveFund(0.001);
			//TickGapAnalisys();
		}

		/// <summary>
		/// Analyse if the gap is enough to start trading
		/// if not then wait
		/// </summary>
		private void TickGapAnalisys()
		{
            Step = TradingStep.Tick;
			Poloniex polo = new Poloniex();
			BitFinex btx = new BitFinex();

			//tick analisys
			Double gap = 0.0;
			do
			{
				Thread.Sleep(10000);

				if (polo.LastTick == null)
					continue;


				if (btx.LastTick == null)
					continue;

				if (polo.LastTick.Last > btx.LastTick.Last)
					gap = polo.LastTick.Last / btx.LastTick.Last - 100.0;
				else
					gap = btx.LastTick.Last / polo.LastTick.Last - 100.0;
			} while (gap < ThresholdGap);

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
            Step = TradingStep.OpenPosition;
			Brlow.MarginBuy();
			BrHigh.MarginSell();
			ClosePosition(Brlow, BrHigh);
		}
		/// <summary>
		/// wait for the cross or nearly the cross for closing position
		/// </summary>
		/// <param name="Brlow">low broker ticker</param>
		/// <param name="BrHigh">low broker ticker</param>
		private void ClosePosition(Broker.Broker Brlow, Broker.Broker BrHigh)
		{
            Step = TradingStep.ClosePosition;
			do
			{
				Thread.Sleep(10000);

				if (Brlow.LastTick == null)
					continue;

				if (BrHigh.LastTick == null)
					continue;
			} while (BrHigh.LastTick.Last / Brlow.LastTick.Last - 100 > Convert.ToDouble(ConfigurationManager.AppSettings["Runtime.Closegap"]));

			BrHigh.ClosePosition();
			Brlow.ClosePosition();
		}
		/// <summary>
		/// Reequilibrate all broker
		/// </summary>
		/// <param name="Brlow">low broker ticker</param>
		/// <param name="BrHigh">low broker ticker</param>
		private void EquilibrateFund(Broker.Broker Brlow, Broker.Broker BrHigh)
		{
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
            
		}

        public void Dispose()
        {
            
        }
    }
}
