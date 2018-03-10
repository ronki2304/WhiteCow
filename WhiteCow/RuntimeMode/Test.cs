using System;
using WhiteCow.Broker;
using WhiteCow.Log;

namespace WhiteCow.RuntimeMode
{
    public class Test:Interface.IRuntimeMode
    {
        public Test()
        {
        }

        public void Dispose()
        {
         
        }

        public void StartToMooh()
        {
            /*
            Console.Clear();
            Logger.Instance.LogInfo("test mode started");
            Poloniex polo = new Poloniex();
            BitFinex btx = new BitFinex();
           
          
          
			Console.WriteLine("test procedure to check if all thing are ok");
			Console.WriteLine("please note that procedure will put order and withdraw");
			Console.WriteLine("So you will have to pay the fees");
            Console.WriteLine("please check that you have the minimum for argin trading on the plateform");

            Console.WriteLine("Which platform do you want to test (poloniex\\bitfinex) leave empty for both");
            String plat = Console.ReadLine();

            if (String.IsNullOrEmpty(plat))
            {
                Console.WriteLine("Test with bitfinex press enter to start");
                Console.ReadLine();
                procedure("bitfinex", btx);

                Console.WriteLine("Now test with Poloniex press enter to start");
                Console.ReadLine();
                procedure("poloniex", polo);
            }
            else if (plat=="poloniex")
            {
				Console.WriteLine("Now test with Poloniex press enter to start");
				Console.ReadLine();
				procedure("poloniex", polo);
            }
            else if (plat=="bitfinex")
            {
				Console.WriteLine("Test with bitfinex press enter to start");
				Console.ReadLine();
				procedure("bitfinex", btx);
            }*/
                

        }
         
        private void procedure(String Market,Broker.Broker broker)
        {/*
            Console.Clear();
            Console.WriteLine($"{Market} : Check tick data");
            Console.WriteLine($"{Market} : The last tick is {broker.LastTicks.Last}");

            Console.WriteLine("press enter to continue if ok");
            Console.ReadLine();

            Console.Clear();
            Console.WriteLine($"{Market} : Check wallet account");
            broker.RefreshWallet();
            Console.WriteLine($"{Market} : the margin account have : {broker.BaseWallet.amount}");
			Console.ReadLine();

            Console.Clear();
			Console.WriteLine($"{Market} : Put a margin long order");
            broker.MarginBuy();
            Console.WriteLine($"{Market} : long order posted please check on your account if you can see it");
            Console.ReadLine();

            Console.Clear();
            Console.WriteLine($"{Market} : Closing the short order....");
            broker.ClosePosition();
            Console.WriteLine($"{Market} : The short order is now closed please check your account");
            Console.ReadLine();

            Console.Clear();
            Console.WriteLine($"{Market} : Sending coins");
            Console.WriteLine($"{Market} : How any do you want to send ?");
            Double amount = Double.Parse(Console.ReadLine());
            Console.WriteLine($"{Market} : please give an address where you want to send those");
            Console.WriteLine($"{Market} : Be carefull a wrong address ad your coins are lost");

			String address = Console.ReadLine();

            broker.Send(address,amount);

            Console.WriteLine($"{Market} : coin sent now check on younanor plateform it is ok");
            Console.WriteLine($"{Market} check if they are received before continue the test");*/
		}
    }
}
