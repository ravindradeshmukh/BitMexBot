using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitMexBot.Model
{
    public class Instrument
    {
        public string Symbol { get; set; }
        public double TickSize { get; set; }
        public double Volume24H { get; set; }
    }
}
