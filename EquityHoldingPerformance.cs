using System;

namespace SimulatedInvesting
{
    public class EquityHoldingPerformance
    {
        public string Symbol {get; set;}
        public float DollarsInvested {get; set;}
        public float HoldingValue {get; set;}
        public float DollarProfit {get; set;}
        public float PercentProfit {get; set;}
    }
}