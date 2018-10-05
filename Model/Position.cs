using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitMexBot.Model
{
    public class Position
    {
        public DateTime TimeStamp { get; set; }
        public double? Leverage { get; set; }
        public int? CurrentQty { get; set; }
        public double? CurrentCost { get; set; }
        public bool IsOpen { get; set; }
        public double? MarkPrice { get; set; }
        public double? MarkValue { get; set; }
        public double? UnrealisedPnl { get; set; }
        public double? UnrealisedPnlPcnt { get; set; }
        public double? AvgEntryPrice { get; set; }
        public double? BreakEvenPrice { get; set; }
        public double? LiquidationPrice { get; set; }
        public string Symbol { get; set; }
    }
}
