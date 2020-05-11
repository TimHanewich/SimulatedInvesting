using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Yahoo.Finance;

namespace SimulatedInvesting
{
    public class SimulatedPortfolio
    {
        public Guid Id { get; set;  }
        public DateTimeOffset CreatedOn { get; set;  }
        public string Owner { get; set; }
        public float Cash { get; set; }
        public List<EquityHolding> EquityHoldings { get; set; }
        public List<EquityTransaction> EquityTransactionLog { get; set; }
        public List<CashTransaction> CashTransactionLog { get; set; }

        public static SimulatedPortfolio Create(string ownername = "")
        {
            SimulatedPortfolio ReturnInstance = new SimulatedPortfolio();

            ReturnInstance.Id = Guid.NewGuid();
            ReturnInstance.EquityHoldings = new List<EquityHolding>();
            ReturnInstance.EquityTransactionLog = new List<EquityTransaction>();
            ReturnInstance.CashTransactionLog = new List<CashTransaction>();
            ReturnInstance.CreatedOn = DateTimeOffset.Now;
            ReturnInstance.Owner = ownername;

            return ReturnInstance;
        }

        public void EditCash(float cash_edit)
        {
            if(cash_edit > 0)
            {
                Cash = Cash + cash_edit;
                CashTransaction ct = new CashTransaction();
                ct.UpdateTransactionTime();
                ct.CashChange = cash_edit;
                CashTransactionLog.Add(ct);
            }
            else if (cash_edit < 0)
            {
                if (Math.Abs(cash_edit) <= Cash) //They have enough to back it up (more cash in the bank than they are requesting)
                {
                    Cash = Cash + cash_edit;
                    CashTransaction ct = new CashTransaction();
                    ct.UpdateTransactionTime();
                    ct.CashChange = cash_edit;
                    CashTransactionLog.Add(ct);
                }
                else
                {
                    throw new Exception("Trying to withdraw more cash than portfolio has.  Trying to withdraw: $" + cash_edit.ToString("#,##0.00") + ", portfolio has $" + Cash.ToString("#,##0.00"));
                }
            }
        }

        public async Task TradeEquityAsync(string symbol, int quantity, TransactionType order_type)
        {
            Equity e = Equity.Create(symbol);
            try
            {
                await e.DownloadSummaryAsync();
            }
            catch
            {
                throw new Exception("Critical error while fetching equity '" + symbol + "'.  Does this equity exist?");
            }
            

            if (order_type == TransactionType.Buy)
            {
                //Be sure we have enough cash to buy
                float cash_needed = e.Summary.Price * quantity;
                if (Cash < cash_needed)
                {
                    throw new Exception("You do not have enough cash to execute this buy order of " + symbol.ToUpper() + ".  Cash needed: $" + cash_needed.ToString("#,##0.00") + ".  Cash balance: $" + Cash.ToString("#,##0.00"));
                }

                //Check if we already have a holding like this
                EquityHolding nh = null;
                foreach (EquityHolding h in EquityHoldings)
                {
                    if (h.Symbol.ToUpper() == symbol.ToUpper())
                    {
                        nh = h;
                    }
                }

                //Create a new one if we couldn't find this holding that already exists.
                if (nh == null)
                {
                    nh = new EquityHolding();
                    nh.Symbol = symbol.ToUpper();
                    nh.Quantity = 0;
                    EquityHoldings.Add(nh);
                }

                //Make the purchase
                Cash = Cash - cash_needed;
                nh.Quantity = nh.Quantity + quantity;

                //Log the transaction
                EquityTransaction et = new EquityTransaction();
                et.UpdateTransactionTime();
                et.Quantity = quantity;
                et.StockSymbol = symbol.ToUpper().Trim();
                et.OrderType = order_type;
                EquityTransactionLog.Add(et);
            }
            else if (order_type == TransactionType.Sell)
            {
                //Find our holding
                EquityHolding eh = null;
                foreach (EquityHolding ceh in EquityHoldings)
                {
                    if (ceh.Symbol.ToUpper() == symbol.ToUpper())
                    {
                        eh = ceh;
                    }
                }

                //Throw an error if we do not have any of those shares.
                if (eh == null)
                {
                    throw new Exception("You do not have any shares of " + symbol.ToUpper() + " to sell.");
                }

                //Throw an error if we do not have enough shares
                if (eh.Quantity < quantity)
                {
                    throw new Exception("You do not have " + quantity.ToString() + " shares to sell!  You only have " + eh.Quantity.ToString() + " shares.");
                }

                //Execute the transaction
                Cash = Cash + (quantity * e.Summary.Price);
                eh.Quantity = eh.Quantity - quantity;

                //Save the transaction log
                EquityTransaction et = new EquityTransaction();
                et.UpdateTransactionTime();
                et.StockSymbol = symbol.ToUpper().Trim();
                et.OrderType = TransactionType.Sell;
                et.Quantity = quantity;
                EquityTransactionLog.Add(et);

                //Remove the holding if it now 0
                if (eh.Quantity == 0)
                {
                    EquityHoldings.Remove(eh);
                }
            }

        }

        public async Task<float> CalculateNetProfitAsync()
        {
            //Get value of full portfolio
            float PortfolioValue = 0;
            foreach (EquityHolding eh in EquityHoldings)
            {
                Equity e = Equity.Create(eh.Symbol);
                await e.DownloadSummaryAsync();
                PortfolioValue = PortfolioValue + (e.Summary.Price * eh.Quantity);
            }


            //Add cash to the value of the portfolio
            PortfolioValue = PortfolioValue + Cash;


            //Get net cash received
            float CashReceived = 0;
            foreach (CashTransaction ct in CashTransactionLog)
            {
                CashReceived = CashReceived + ct.CashChange;
            }

            return PortfolioValue - CashReceived;
        }

    }
}