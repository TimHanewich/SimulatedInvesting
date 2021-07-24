using System;
using SimulatedInvesting;
using Newtonsoft.Json;

namespace testing
{
    class Program
    {
        static void Main(string[] args)
        {
            SimulatedPortfolio sp = SimulatedPortfolio.Create("TimHanewich");
            sp.TradeCost = 7;

            sp.EditCash(500000);
            sp.TradeEquityAsync("BTC-USD", 1, TransactionType.Buy).Wait();

            Console.Write("Waiting... ");
            System.Threading.Tasks.Task.Delay(60000).Wait();

            Console.WriteLine(JsonConvert.SerializeObject(sp));
            
            float f = sp.CalculateNetProfitAsync().Result;
            Console.WriteLine(f);
        }
    }
}
