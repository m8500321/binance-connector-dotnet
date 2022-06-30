using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using Binance.Spot;
using Binance.Common;
using Microsoft.Extensions;
using Binance.Spot.MarketExamples;
using Binance.Spot.Models;
using System.Diagnostics;
using Binance.Spot.Tests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Binance.Common.Tests
{


    public class MyTools
    {

        static string apiKey = "KV12suVvH4WM6JNb3bO7Ev1j6h2pVp2DCxNUJcEn7ACLxbv2Uj50i9zX01r1a8K2";
        static string apiSecret = "rXDNV41PN9Lce56Kmd7lJFN5BFXhzXr0Vr4zeSlZR2SmGwj7QICfaFIcExOhnjqB";
        static string FUTURE_BASE_URL = "https://fapi.binance.com";
        static string TEST_FUTURE_BASE_URL = "https://testnet.binancefuture.com";
        static string SPOT_BASE_URL = "https://api.binance.com";

        // static public bool IsFuture = false;
        static public ILogger logger;
        //E:\projects\binance-connector-dotnet\datas
        private static readonly object LOCK = new object();
        static public string dataDir = "";
        /// </summary>

        static private DateTime UTC_START = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        static readonly int HOOUR8_MS = 8 * 60 * 60 * 1000;
        static public Dictionary<string, KlineList> klCache = new Dictionary<string, KlineList>();
        static public long MsFromUTC0(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - UTC_START).TotalMilliseconds;
        }
        static int logCount = 0;
        static public void LogMsg(params object[] msg)
        {
            string output = "\n\n[" + (logCount++) + "]:" + DateTime.Now + "\n";
            foreach (var item in msg)
            {
                output += (item.ToString() + "\n");
            }
            output += new StackTrace(new StackFrame(1, true)).ToString();
            logger.LogInformation(output);
            lock (LOCK)
            {
                var curStr = "";
                using (StreamReader sr = new StreamReader(dataDir + "log.txt"))
                {
                    curStr = sr.ReadToEnd();
                }
                if (curStr.Length > 1000000)
                {
                    var newFile = "log__" + DateTime.Now.ToString("MM-dd__HH-mm-ss");
                    using (StreamWriter sw = new StreamWriter(dataDir + "backup/" + newFile + ".txt"))
                    {
                        sw.Write(curStr);
                    }
                }
                using (StreamWriter sw = new StreamWriter(dataDir + "log.txt", curStr.Length <= 1000000))
                {
                    sw.Write(output);
                }
            }

        }


        static public async Task<JArray> FetchMarketData(long startTime, string symbol, string type = "kline", int limit = 500, bool isFuture = true, int intervalMin = 5)
        {

            var url = "https://api.binance.com";
            if (isFuture)
            {
                url = "https://fapi.binance.com";
            }
            Market market = new Market(baseUrl: url, apiKey: apiKey, apiSecret: apiSecret);
            // market.TEST_CONNECTIVITY = "/api/v3/ping";
            // market.CHECK_SERVER_TIME = "/api/v3/time";
            // market.EXCHANGE_INFORMATION = "/api/v3/exchangeInfo";
            // market.ORDER_BOOK = "/api/v3/depth";
            // market.RECENT_TRADES_LIST = "/api/v3/trades";
            // market.OLD_TRADE_LOOKUP = "/api/v3/historicalTrades";
            // market.COMPRESSED_AGGREGATE_TRADES_LIST = "/api/v3/aggTrades";
            // market.KLINE_CANDLESTICK_DATA = "/api/v3/klines";
            // market.CURRENT_AVERAGE_PRICE = "/api/v3/avgPrice";
            // market.TWENTY_FOUR_HR_TICKER_PRICE_CHANGE_STATISTICS = "/api/v3/ticker/24hr";
            // market.SYMBOL_PRICE_TICKER = "/api/v3/ticker/price";
            // market.SYMBOL_ORDER_BOOK_TICKER = "/api/v3/ticker/bookTicker";

            if (isFuture)
            {
                market.KLINE_CANDLESTICK_DATA = "/fapi/v1/klines";
            }
            // var tag = symbol.Replace("_F", "");

            JArray karray = null;
            var typeInterval = intervalMin == 5 ? Interval.FIVE_MINUTE : Interval.ONE_MINUTE;
            if (type == "kline")
            {
                var result = await market.KlineCandlestickData(symbol, typeInterval, limit: limit, startTime: startTime);
                // var result = await market.KlineCandlestickData(symbol, Interval.ONE_MINUTE, limit: 5);
                karray = JArray.Parse(result);
            }
            else
            {
                string typeAPI = "/futures/data/" + type;
                // var count = 500;
                var msPerMin = 60 * 1000;
                // var intervalMin = 5;
                var diffMS = intervalMin * msPerMin * limit;
                var result = await market.MyFutureData(symbol, typeInterval, limit: limit, startTime: startTime, endTime: startTime + diffMS, typeAPI: typeAPI);
                karray = JArray.Parse(result);
            }

            return karray;
        }




        // // 转化成可读内容
        // // [1644546000000,"42886.02000000","42902.96000000","42817.93000000","42897.75000000","141.19340000",1644546299999,"6049947.10973040",3257,"66.40442000","2845337.65305600","0"]
        // // 开始时间，开盘价，收盘价，最低价，最高价，平均价格，成交额
        // static public void Data2Readable()
        // {
        //     // List<MyKlines> klines = new List<MyKlines>();
        //     StreamReader sr = new StreamReader(dataDir + "data_test.txt");
        //     StreamWriter sw = new StreamWriter(dataDir + "data_output.txt", true);
        //     //判断文件中是否有字符
        //     while (sr.Peek() != -1)
        //     {
        //         var obj = new JsonKline(sr.ReadLine());                
        //         var readTm = UTC_START.AddMilliseconds(obj.openTime + HOOUR8_MS).ToString("yy-M-d HH:mm:ss");
        //         var avePrice = obj.volumePrice / obj.volume;
        //         var line = $"{readTm}, {obj.openTime}, {obj.openPrice:F3}, {obj.closePrice:F3}, {obj.minPrice:F3}, {obj.maxPrice:F3}, {avePrice:F3}, {obj.volumePrice:F3}";
        //         sw.WriteLine(line);
        //     }
        //     sr.Close();
        //     sw.Close();
        // }

        static public async Task RequestLongData(string symbol, string type, bool isFuture, int intervalMin = 5)
        {
            // request klines
            long curMS = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // long startMS = MsFromUTC0(DateTime.UtcNow);
            var symbolStart = (long)ExchangeInfo.Instance.symbols[symbol]["onboardDate"];
            // long startMS = 1644339900000;
            var maxCount = 1000;
            var msPerMin = 60 * 1000;
            var minPerDay = 60 * 24;
            // var intervalMin = 5;

            var days = 365f;
            if (type == "kline")
            {
                days = 30 * 24f;
            }
            else
            {
                days = 30;
                maxCount = 500;
            }
            long reqStart = curMS - (long)days * minPerDay * msPerMin;
            reqStart = Math.Max(reqStart, symbolStart);
            var diffMS = intervalMin * msPerMin * maxCount;
            var strList = new List<string>();
            var sb = new StringBuilder(100000);
            while (reqStart + intervalMin * msPerMin < curMS)
            {
                try
                {
                    var reqCount = type == "kline" ? 1000 : 500;
                    var karray = await FetchMarketData(reqStart, symbol, type, reqCount, isFuture, intervalMin);
                    for (int j = 0; j < karray.Count; j++)
                    {
                        var item = karray[j];
                        sb.Append(item.ToString().Replace("\r\n", "").Replace(" ", "").Trim() + "\n");
                    }
                    if (karray.Count > 0)
                    {
                        var lastJ = karray[karray.Count - 1];
                        reqStart = (long)lastJ[0] + 100;
                    }
                    else
                    {
                        reqStart += diffMS;
                    }
                    Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    LogMsg("Error!", e.ToString());
                }
            }
            // sb.Remove(sb.Length - 1, 1);
            var fileName = symbol + (type == "kline" ? "" : "_" + type);
            File.Delete(dataDir + fileName + ".txt"); //删除指定文件;
            using (StreamWriter sw = new StreamWriter(dataDir + fileName + ".txt", true))
            {
                sw.Write(sb);
            }
            // MyTools.Text2Serializable(symbol);
        }

        static public string Text2Serializable(string symbol)
        {
            // symbol = symbol + (MyTools.IsFuture ? "_future" : "");
            StreamReader sr = new StreamReader(dataDir + symbol + ".txt");
            var sList = new KlineList();
            //判断文件中是否有字符
            while (sr.Peek() != -1)
            {
                var obj = new JsonKline(sr.ReadLine());
                var myobj = new MyKline(obj);
                sList.myKlines.Add(myobj);
            }
            var lines = sList.myKlines;
            // var MAX_LEN = 9;
            var PrevList = new List<float>();
            var powList = new List<int>() { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };
            var startIdx = 1000;
            var sumVolume = 0f;
            var sumClose = 0f;
            for (int i = 1; i <= 512; i++)
            {
                // sumVolume += (lines[startIdx - i].volumePrice / lines[startIdx - i].volume);
                sumClose += lines[startIdx - i].closePrice;
                var powIdx = powList.IndexOf(i);
                if (powIdx > -1)
                {
                    // lines[startIdx].prevVolumePriceList[powIdx] = sumVolume / i;
                    lines[startIdx].prevClosePriceList[powIdx] = sumClose / i;
                }
            }

            for (int i = startIdx + 1; i < lines.Count; i++)
            {

                var currentItem = lines[i];
                var lastItem = lines[i - 1];
                for (int idx = 0; idx < lastItem.prevClosePriceList.Length; idx++)
                {
                    var count = powList[idx];
                    var lastEnd = lines[i - count];
                    // var lastVolume = lastItem.prevVolumePriceList[idx];
                    var lastClose = lastItem.prevClosePriceList[idx];
                    currentItem.prevClosePriceList[idx] = (lastClose * count - lastEnd.closePrice + currentItem.closePrice) / count;
                }

            }

            FileStream fileStream = new FileStream(dataDir + symbol + ".data", FileMode.Create);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fileStream, sList);
            fileStream.Flush();
            fileStream.Close();
            sr.Close();
            // sw.Close();
            return "";
        }

        // 获取交易信息
        static public async Task<string> GetExchangeInfo()
        {
            // symbol = symbol.Replace("_F", "");
            HttpMessageHandler loggingHandler = new BinanceLoggingHandler(logger: MyTools.logger);
            HttpClient httpClient = new HttpClient(handler: loggingHandler);
            var spotAccountTrade = new SpotAccountTrade(httpClient, FUTURE_BASE_URL, apiKey, apiSecret);
            return await spotAccountTrade.MyFutureExchangeInfo();
            // Console.Read();
        }

        // 查询订单
        static public async Task<string> CheckAccountInfo()
        {
            // symbol = symbol.Replace("_F", "");
            HttpMessageHandler loggingHandler = new BinanceLoggingHandler(logger: MyTools.logger);
            HttpClient httpClient = new HttpClient(handler: loggingHandler);
            var spotAccountTrade = new SpotAccountTrade(httpClient, FUTURE_BASE_URL, apiKey, apiSecret);
            return await spotAccountTrade.MyFutureAccountInfo();
            // Console.Read();
        }

        // 市价单
        static public async Task<string> MarketTrade(string symbol, bool isOpen, Side side, decimal quantity)
        {
            // symbol = symbol.Replace("_F", "");
            HttpMessageHandler loggingHandler = new BinanceLoggingHandler(logger: MyTools.logger);
            HttpClient httpClient = new HttpClient(handler: loggingHandler);
            var spotAccountTrade = new SpotAccountTrade(httpClient, FUTURE_BASE_URL, apiKey, apiSecret);
            var myID = isOpen ? "OPEN_" : "CLOSE_";
            myID += (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 10000000);
            return await spotAccountTrade.MyFutureNewOrder(symbol, side, OrderType.MARKET, quantity: quantity, newClientOrderId: myID, reduceOnly: !isOpen);
            // Console.Read();
        }

        // 限价单
        static public async Task<string> LimitTrade(string symbol, bool isOpen, Side side, decimal quantity, decimal price)
        {
            // symbol = symbol.Replace("_F", "");
            HttpMessageHandler loggingHandler = new BinanceLoggingHandler(logger: MyTools.logger);
            HttpClient httpClient = new HttpClient(handler: loggingHandler);
            var spotAccountTrade = new SpotAccountTrade(httpClient, FUTURE_BASE_URL, apiKey, apiSecret);
            var myID = isOpen ? "OPEN_" : "CLOSE_";
            myID += (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 10000000);
            return await spotAccountTrade.MyFutureNewOrder(symbol, side, OrderType.LIMIT, quantity: quantity, price: price, timeInForce: TimeInForce.GTC, newClientOrderId: myID, reduceOnly: !isOpen);
            // Console.Read();
        }

        // {
        //   "e": "kline",     // 事件类型
        //   "E": 123456789,   // 事件时间
        //   "s": "BNBBTC",    // 交易对
        //   "k": {
        //     "t": 123400000, // 这根K线的起始时间
        //     "T": 123460000, // 这根K线的结束时间
        //     "s": "BNBBTC",  // 交易对
        //     "i": "1m",      // K线间隔
        //     "f": 100,       // 这根K线期间第一笔成交ID
        //     "L": 200,       // 这根K线期间末一笔成交ID
        //     "o": "0.0010",  // 这根K线期间第一笔成交价
        //     "c": "0.0020",  // 这根K线期间末一笔成交价
        //     "h": "0.0025",  // 这根K线期间最高成交价
        //     "l": "0.0015",  // 这根K线期间最低成交价
        //     "v": "1000",    // 这根K线期间成交量
        //     "n": 100,       // 这根K线期间成交笔数
        //     "x": false,     // 这根K线是否完结(是否已经开始下一根K线)
        //     "q": "1.0000",  // 这根K线期间成交额
        //     "V": "500",     // 主动买入的成交量
        //     "Q": "0.500",   // 主动买入的成交额
        //     "B": "123456"   // 忽略此参数
        //   }
        // }
        static public async Task ListenSocket(string name)
        {
            var websocket = new MarketDataWebSocket(name);
            websocket.OnMessageReceived(
                async (data) =>
            {
                data = data.Replace("\0", "");
                MyTools.LogMsg(data);

            }, CancellationToken.None);
            await websocket.ConnectAsync(CancellationToken.None);
            Console.Read();
        }

        static public KlineList LoadFileData(string symbol, bool reload = false)
        {
            lock (klCache)
            {
                if (!klCache.ContainsKey(symbol) || reload)
                {
                    // symbol = symbol + (MyTools.IsFuture ? "_future" : "");
                    FileStream fileStream = new FileStream(dataDir + symbol + ".data", FileMode.Open, FileAccess.Read);
                    BinaryFormatter bf = new BinaryFormatter();
                    var kList = bf.Deserialize(fileStream) as KlineList;
                    klCache.Add(symbol, kList);
                    fileStream.Close();
                }
            }
            return klCache[symbol];
        }

        static public float CutDecim(string s, int count)
        {
            var f = float.Parse(s);
            return MathF.Round(f, count);
        }

        static public float CutDecim(float f, int count)
        {
            return MathF.Round(f, count);
        }
        static public string ToPercent(string s)
        {
            var f = float.Parse(s);
            // var sig = f > 0 ? "+" : "";
            return MathF.Round(f * 100, 3) + "%";
        }
        static public string ToPercent(float f)
        {
            // var sig = f > 0 ? "+" : "";
            return MathF.Round(f * 100, 3) + "%";
        }

        // 百分数+绝对值,在范围内
        static public float DiffRate(float f1, float f2)
        {
            return (f1 - f2) / f1;
        }

        static public float SimilarValue(float a1, float a2, float b1, float b2, float percent = 0.1f, float val = 0.001f, float extraRange = 1f)
        {
            return SimilarRate((a1 - a2) / a2, (b1 - b2) / b2, percent, val, extraRange);
        }

        static public float SimilarRate(float r1, float r2, float percent = 0.1f, float val = 0.001f, float extraRange = 1f)
        {
            // var a = new System.Diagnostics.CodeAnalysis.ObjectPool();
            var maxDiff = Math.Abs(r1) * percent + val;
            var curDiff = Math.Abs(r1 - r2);
            if (curDiff > maxDiff)
            {
                return -99999f;
            }
            return 1 - curDiff / maxDiff;
        }

    }

    public class ListPool<T>
    {

        Stack<List<T>> listPool = new Stack<List<T>>();
        public List<T> PopItem()
        {
            // if (listPool.Count > 0)
            // {
            //     return listPool.Pop();
            // }
            return new List<T>();
        }
        public void PushItem(List<T> l)
        {
            // l.Clear();
            // listPool.Push(l);
        }
    }


}