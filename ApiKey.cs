using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitMexBot
{
    public class ApiKey
    {
        public string key { get; set; }

        public string nonce { get; set; }

        public int rateLimit { get; set; }

        public string secretKey { get; set; }

        public string domain { get; set; }

        public string signature { get; set; }

        public ApiKey(string key, string secretKey, string domain, int rateLimit = 5000)
        {
            this.key = key;
            this.secretKey = secretKey;
            this.domain = domain;
            this.rateLimit = rateLimit;
        }
    }
}
