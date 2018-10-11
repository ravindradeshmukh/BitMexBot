//using ServiceStack.Text;
using BitMexBot;
using BitMexBot.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace BitMEX
{

    public class BitMEXApi
    {
        private string domain = "https://testnet.bitmex.com";
        private string apiKey;
        private string apiSecret;
        private int rateLimit;

        public BitMEXApi(ApiKey apiKey)
        {
            this.apiKey = apiKey.key;
            this.apiSecret = apiKey.secretKey;
            this.rateLimit = apiKey.rateLimit;
            this.domain = apiKey.domain;
        }

        #region API Connector - Don't touch
        private string BuildQueryData(Dictionary<string, string> param)
        {
            if (param == null)
                return "";

            StringBuilder b = new StringBuilder();
            foreach (var item in param)
                b.Append(string.Format("&{0}={1}", item.Key, WebUtility.UrlEncode(item.Value)));

            try { return b.ToString().Substring(1); }
            catch (Exception) { return ""; }
        }

        private string BuildJSON(Dictionary<string, string> param)
        {
            if (param == null)
                return "";

            var entries = new List<string>();
            foreach (var item in param)
                entries.Add(string.Format("\"{0}\":\"{1}\"", item.Key, item.Value));

            return "{" + string.Join(",", entries) + "}";
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private long GetNonce()
        {
            DateTime yearBegin = new DateTime(2018, 1, 1);
            return DateTime.UtcNow.Ticks - yearBegin.Ticks;
        }

        private byte[] hmacsha256(byte[] keyByte, byte[] messageBytes)
        {
            using (var hash = new HMACSHA256(keyByte))
            {
                return hash.ComputeHash(messageBytes);
            }
        }

        private string Query(string method, string function, Dictionary<string, string> param = null, bool auth = false, bool json = false)
        {
            string paramData = json ? BuildJSON(param) : BuildQueryData(param);
            string url = "/api/v1" + function + ((method == "GET" && paramData != "") ? "?" + paramData : "");
            string postData = (method != "GET") ? paramData : "";

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(domain + url);
            webRequest.Method = method;

            if (auth)
            {
                string nonce = GetNonce().ToString();
                string message = method + url + nonce + postData;
                byte[] signatureBytes = hmacsha256(Encoding.UTF8.GetBytes(apiSecret), Encoding.UTF8.GetBytes(message));
                string signatureString = ByteArrayToString(signatureBytes);

                webRequest.Headers.Add("api-nonce", nonce);
                webRequest.Headers.Add("api-key", apiKey);
                webRequest.Headers.Add("api-signature", signatureString);

                //string expires = GetExpiresArg();
                //string message = method + url + expires + postData;
                //byte[] signatureBytes = hmacsha256(Encoding.UTF8.GetBytes(apiSecret), Encoding.UTF8.GetBytes(message));
                //string signatureString = ByteArrayToString(signatureBytes);

                ////webRequest.Headers.Add("api-nonce", nonce);
                //webRequest.Headers.Add("api-expires", expires);
                //webRequest.Headers.Add("api-key", apiKey);
                //webRequest.Headers.Add("api-signature", signatureString);
            }

            try
            {
                if (postData != "")
                {
                    webRequest.ContentType = json ? "application/json" : "application/x-www-form-urlencoded";
                    var data = Encoding.UTF8.GetBytes(postData);
                    using (var stream = webRequest.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }

                using (WebResponse webResponse = webRequest.GetResponse())
                using (Stream str = webResponse.GetResponseStream())
                using (StreamReader sr = new StreamReader(str))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (WebException wex)
            {
                using (HttpWebResponse response = (HttpWebResponse)wex.Response)
                {
                    if (response == null)
                        throw;

                    using (Stream str = response.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(str))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
        #endregion

        #region Examples from BitMex
        //public List<OrderBookItem> GetOrderBook(string symbol, int depth)
        //{
        //    var param = new Dictionary<string, string>();
        //    param["symbol"] = symbol;
        //    param["depth"] = depth.ToString();
        //    string res = Query("GET", "/orderBook", param);
        //    return JsonSerializer.DeserializeFromString<List<OrderBookItem>>(res);
        //}

        public string GetOrders(string Symbol)
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = Symbol;
            //param["filter"] = "{\"open\":true}";
            //param["columns"] = "";
            //param["count"] = 100.ToString();
            //param["start"] = 0.ToString();
            //param["reverse"] = false.ToString();
            //param["startTime"] = "";
            //param["endTime"] = "";
            return Query("GET", "/order", param, true);
        }

        public string PostOrders()
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = "XBTUSD";
            param["side"] = "Buy";
            param["orderQty"] = "1";
            param["ordType"] = "Market";
            return Query("POST", "/order", param, true);
        }

        public string DeleteOrders()
        {
            var param = new Dictionary<string, string>();
            param["orderID"] = "de709f12-2f24-9a36-b047-ab0ff090f0bb";
            param["text"] = "cancel order by ID";
            return Query("DELETE", "/order", param, true, true);
        }
        #endregion

        #region Our Calls
        public List<OrderBook> GetOrderBook(string symbol, int depth)
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = symbol;
            param["depth"] = depth.ToString();
            string res = Query("GET", "/orderBook/L2", param);
            return JsonConvert.DeserializeObject<List<OrderBook>>(res);
        }

        public string PostOrderPostOnly(string Symbol, string Side, double Price, int Quantity, bool hideQuantity = false)
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = Symbol;
            param["side"] = Side;
            param["ordType"] = "Limit";
            param["execInst"] = "ParticipateDoNotInitiate";
            param["price"] = Price.ToString();
            param["displayQty"] = hideQuantity ? 0.ToString() : Quantity.ToString();    // Shows the order as hidden, keeps us from moving price away from our own orders
            return Query("POST", "/order", param, true);
        }

        public string MarketOrder(string Symbol, string Side, int Quantity)
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = Symbol;
            param["side"] = Side;
            param["orderQty"] = Quantity.ToString();
            param["ordType"] = "Market";
            return Query("POST", "/order", param, true);
        }

        public string CancelAllOpenOrders(string symbol, string Note = "")
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = symbol;
            param["text"] = Note;
            return Query("DELETE", "/order/all", param, true, true);
        }

        public List<Instrument> GetActiveInstruments()
        {
            string res = Query("GET", "/instrument/active");
            return JsonConvert.DeserializeObject<List<Instrument>>(res);
        }

        public List<Instrument> GetInstrument(string symbol)
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = symbol;
            param["count"] = 2.ToString();
            string res = Query("GET", "/instrument", param);
            return JsonConvert.DeserializeObject<List<Instrument>>(res);
        }

        public List<Candle> GetCandleHistory(string symbol, int count, string size)
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = symbol;
            param["count"] = count.ToString();
            param["reverse"] = true.ToString();
            param["partial"] = false.ToString();
            param["binSize"] = size;
            string res = Query("GET", "/trade/bucketed", param);
            return JsonConvert.DeserializeObject<List<Candle>>(res).OrderByDescending(a => a.TimeStamp).ToList();
        }

        public List<Position> GetOpenPositions(string symbol)
        {
            var param = new Dictionary<string, string>();
            string res = Query("GET", "/position", param, true);
            return JsonConvert.DeserializeObject<List<Position>>(res).Where(a => a.Symbol == symbol && a.IsOpen == true).OrderByDescending(a => a.TimeStamp).ToList();
        }

        public List<Order> GetOpenOrders(string symbol)
        {
            var param = new Dictionary<string, string>();
            param["symbol"] = symbol;
            param["reverse"] = true.ToString();
            string res = Query("GET", "/order", param, true);
            return JsonConvert.DeserializeObject<List<Order>>(res).Where(a => a.OrdStatus == "New" || a.OrdStatus == "PartiallyFilled").OrderByDescending(a => a.TimeStamp).ToList();
        }

        public string EditOrderPrice(string OrderId, double Price)
        {
            var param = new Dictionary<string, string>();
            param["orderID"] = OrderId;
            param["price"] = Price.ToString();
            return Query("PUT", "/order", param, true, true);
        }

        public Wallet GetWallet()
        {
            var param = new Dictionary<string, string>();
            param["currency"] = "XBt";
            var res = Query("GET", "/user/margin", param, true);
            Wallet wallet = JsonConvert.DeserializeObject<Wallet>(res);
            return wallet;
        }

        public string GetExpiresArg()
        {
            long timestamp = (long)((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);

            string expires = (timestamp + 60).ToString();

            return (expires);
        }

        #endregion



        #region RateLimiter

        private long lastTicks = 0;
        private object thisLock = new object();

        private void RateLimit()
        {
            lock (thisLock)
            {
                long elapsedTicks = DateTime.Now.Ticks - lastTicks;
                var timespan = new TimeSpan(elapsedTicks);
                if (timespan.TotalMilliseconds < rateLimit)
                    Thread.Sleep(rateLimit - (int)timespan.TotalMilliseconds);
                lastTicks = DateTime.Now.Ticks;
            }
        }

        #endregion RateLimiter
    }
}