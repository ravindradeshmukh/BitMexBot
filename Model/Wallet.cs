using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitMexBot.Model
{
    public class Wallet
    {
        public decimal? account { get; set; }
        public string currency { get; set; }
        public decimal? prevDeposited { get; set; }
        public decimal? prevWithdrawn { get; set; }
        public decimal? prevTransferIn { get; set; }
        public decimal? prevTransferOut { get; set; }
        public decimal? prevAmount { get; set; }
        public DateTime? prevTimestamp { get; set; }
        public decimal? deltaDeposited { get; set; }
        public decimal? deltaWithdrawn { get; set; }
        public decimal? deltaTransferIn { get; set; }
        public decimal? deltaTransferOut { get; set; }
        public decimal? deltaAmount { get; set; }
        public decimal? deposited { get; set; }
        public decimal? withdrawn { get; set; }
        public decimal? transferIn { get; set; }
        public decimal? transferOut { get; set; }
        public decimal? amount { get; set; }
        public decimal? pendingCredit { get; set; }
        public decimal? pendingDebit { get; set; }
        public decimal? confirmedDebit { get; set; }
        public DateTime? timestamp { get; set; }
        public string addr { get; set; }
        public string script { get; set; }
        public List<string> withdrawalLock { get; set; }

        public static implicit operator Wallet(JObject json)
        {
            var wallet = json.ToObject<Wallet>();
            return wallet;
        }
    }
}