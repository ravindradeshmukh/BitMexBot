using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitMexBot.Model
{
    public class OrderBook
    {
        public string Side { get; set; }
        public double Price { get; set; }
        public int Size { get; set; }
    }
}
