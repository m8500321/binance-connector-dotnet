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

namespace Binance.Common.Tests
{


    public class MyTools
    {

        static string apiKey = "Sud7YtqxuBnwKDJZ7zgnGlZuOxssZ5QzrtvhkL7CfHMfP0fWglYzMScttIDsJ42v";
        static string apiSecret = "QnU7QZwESqUnsuwYrs8KESVBXo4W8zgERMukgUhj9DR8phoY43WQZ0TjZgbbYbs9";

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
        static public void LogMsg(params string[] msg)
        {
            string output = "";
            foreach (var item in msg)
            {
                output += (item + "\n");
            }
            output += new StackTrace(new StackFrame(1, true)).ToString();
            logger.LogInformation(output);
            lock (LOCK)
            {
                var s = "";
                using (StreamReader sr = new StreamReader(dataDir + "log.txt"))
                {
                    s = sr.ReadToEnd();
                }
                output = "\n\n[" + (logCount++) + "]:" + DateTime.Now + "\n" + output;
                if (s.Length > 100000)
                {
                    using (StreamWriter sw = new StreamWriter(dataDir + "log.txt"))
                    {
                        sw.Write(s.Substring(s.Length - 30000) + output);
                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(dataDir + "log.txt", true))
                    {
                        sw.Write(output);
                    }
                }
            }

        }


        static public async Task KlineCandlestickData_Response(long startTime, string symbol, string type = "kline")
        {

            var url = "https://api.binance.com";
            if (symbol.EndsWith("_F")) ;
            {
                url = "https://fapi.binance.com";
            }
            Market market = new Market(baseUrl: url, apiKey: apiKey, apiSecret: apiSecret);


            market.TEST_CONNECTIVITY = "/api/v3/ping";
            market.CHECK_SERVER_TIME = "/api/v3/time";
            market.EXCHANGE_INFORMATION = "/api/v3/exchangeInfo";
            market.ORDER_BOOK = "/api/v3/depth";
            market.RECENT_TRADES_LIST = "/api/v3/trades";
            market.OLD_TRADE_LOOKUP = "/api/v3/historicalTrades";
            market.COMPRESSED_AGGREGATE_TRADES_LIST = "/api/v3/aggTrades";
            market.KLINE_CANDLESTICK_DATA = "/api/v3/klines";
            market.CURRENT_AVERAGE_PRICE = "/api/v3/avgPrice";
            market.TWENTY_FOUR_HR_TICKER_PRICE_CHANGE_STATISTICS = "/api/v3/ticker/24hr";
            market.SYMBOL_PRICE_TICKER = "/api/v3/ticker/price";
            market.SYMBOL_ORDER_BOOK_TICKER = "/api/v3/ticker/bookTicker";

            if (symbol.EndsWith("_F")) ;
            {
                market.KLINE_CANDLESTICK_DATA = "/fapi/v1/klines";
            }
            var tag = symbol.Replace("_F", "");

            JArray karray = null;
            if (type == "kline")
            {
                var result = await market.KlineCandlestickData(tag, Interval.FIVE_MINUTE, limit: 1000, startTime: startTime);
                karray = JArray.Parse(result);
            }
            else
            {
                string typeAPI = "/futures/data/" + type;
                var count = 500;
                var msPerMin = 60 * 1000;
                var intervalMin = 5;
                var diffMS = intervalMin * msPerMin * count;
                var result = await market.OpenInterestHistData(tag, Interval.FIVE_MINUTE, limit: count, startTime: startTime, endTime: startTime + diffMS, typeAPI: typeAPI);
                karray = JArray.Parse(result);
            }
            var fileName = symbol + (type == "kline" ? "" : "_" + type);
            using (StreamWriter sw = new StreamWriter(dataDir + fileName + ".txt", true))
            {
                for (int i = 0; i < karray.Count; i++)
                {
                    var item = karray[karray.Count - i - 1];
                    sw.WriteLine(item.ToString().Replace("\r\n", "").Replace(" ", ""));
                }
            }
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

        static public async Task RequestData(string symbol, string type = "kline")
        {
            // request klines
            var fileName = symbol + (type == "kline" ? "" : "_" + type);
            File.Delete(dataDir + fileName + ".txt"); //删除指定文件;
            long startMS = MsFromUTC0(DateTime.UtcNow);
            // long startMS = 1644339900000;
            var maxCount = 1000;
            var msPerMin = 60 * 1000;
            var minPerDay = 60 * 24;
            var intervalMin = 5;

            var days = 365f;
            if (type == "kline")
            {
                days = 365 * 1.5f;
            }
            else
            {
                days = 30;
                maxCount = 500;
            }
            var diffMS = intervalMin * msPerMin * maxCount;
            var iteCount = (minPerDay * days) / (intervalMin * maxCount);
            // 每天多少个n分钟*一年多少天
            for (int i = 0; i < iteCount; i++)
            {
                // 5分钟*每分钟多少秒*每次多少条
                startMS -= diffMS;
                // LogMsg(UTC_START.AddMilliseconds(startMS).ToString());
                try
                {
                    await KlineCandlestickData_Response(startMS, symbol, type);
                }
                catch (Exception e)
                {
                    LogMsg("Error!", e.ToString());
                    startMS += diffMS;
                }
                var waitTm = symbol.EndsWith("_F") ? 100 : 30;
                Thread.Sleep(waitTm);
            }
        }

        static public string Text2Serializable(string symbol)
        {
            // symbol = symbol + (MyTools.IsFuture ? "_future" : "");
            StreamReader sr = new StreamReader(dataDir + symbol + ".txt");
            var sList = new KlineList();
            //判断文件中是否有字符
            FileStream fileStream = new FileStream(dataDir + symbol + ".data", FileMode.Create);
            BinaryFormatter bf = new BinaryFormatter();
            while (sr.Peek() != -1)
            {
                var obj = new JsonKline(sr.ReadLine());
                var myobj = new MyKline(obj);
                sList.myKlines.Add(myobj);
            }
            sList.myKlines.Reverse();
            var MAX_LEN = 40;
            for (int i = MAX_LEN; i < sList.myKlines.Count - MAX_LEN; i++)
            {
                var itemI = sList.myKlines[i];
                var sumClose = 0f;
                var sumPrice = 0f;
                var sumVolume = 0f;
                // var sumVolume = 0f;
                for (int j = 1; j <= MAX_LEN; j++)
                {
                    // 前面40条的平均价格
                    var itemJ = sList.myKlines[i - j + 1];
                    sumClose += itemJ.closePrice;
                    sumPrice += itemJ.volumePrice;
                    sumVolume += itemJ.volume;
                    var idx = -1;
                    if (j == MAX_LEN / 8)
                    {
                        idx = 0;
                    }
                    else if (j == MAX_LEN / 4)
                    {
                        idx = 1;

                    }
                    else if (j == MAX_LEN / 2)
                    {
                        idx = 2;

                    }
                    else if (j == MAX_LEN)
                    {
                        idx = 3;

                    }
                    if (idx > -1)
                    {
                        // itemI.prevAveVolumeList[idx] = sumVolume / j;
                        itemI.prevAvePriceList[idx] = sumPrice / sumVolume;
                        // itemI.prevMaxList[idx] = Math.Max(itemI.prevMaxList[idx], itemJ.maxPrice);
                        // itemI.prevMinList[idx] = Math.Min(itemI.prevMinList[idx], itemJ.minPrice);
                        itemI.prevAveCloseList[idx] = sumClose / j;
                    }
                }
                var aveVolume = sumVolume / MAX_LEN;
                var curSumVolume = 0f;
                var curSumPrice = 0f;
                // for (int j = 1; j <= MAX_LEN; j++)
                // {
                //         itemI.prevVolumePriceList[idx] = curSumPrice / curSumVolume;

                // }
                for (int j = 1; j <= MAX_LEN; j++)
                {
                    // 单位成交量的平均价格
                    var itemJ = sList.myKlines[i - j + 1];
                    curSumPrice += itemJ.volumePrice;
                    curSumVolume += itemJ.volume;
                    var rate = curSumVolume / sumVolume;
                    var idx = -1;
                    if (rate >= 1)
                    {
                        idx = 3;
                    }
                    else if (rate >= 1 / 2f)
                    {
                        idx = 2;
                    }
                    else if (rate >= 1 / 4f)
                    {
                        idx = 1;
                    }
                    else if (rate >= 1 / 8f)
                    {
                        idx = 0;
                    }
                    if (idx > -1)
                    {
                        itemI.prevVolumePriceList[idx] = curSumPrice / curSumVolume;
                    }
                }
                for (int j = 0; j < 4; j++)
                {
                    var prevVal = j > 0 ? itemI.nextSumIncList[j - 1] : 0;
                    itemI.nextSumIncList[j] = prevVal + sList.myKlines[i + j + 1].incPercent;
                }

            }
            bf.Serialize(fileStream, sList);
            fileStream.Flush();
            fileStream.Close();
            sr.Close();
            // sw.Close();
            return "";
        }

        static public KlineList LoadFileData(string symbol)
        {
            lock (klCache)
            {
                if (!klCache.ContainsKey(symbol))
                {
                    // symbol = symbol + (MyTools.IsFuture ? "_future" : "");
                    FileStream fileStream = new FileStream(dataDir + symbol + ".data", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    BinaryFormatter bf = new BinaryFormatter();
                    var kList = bf.Deserialize(fileStream) as KlineList;
                    klCache.Add(symbol, kList);
                    fileStream.Flush();
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
            return SimilarRate((a1 - a2) / a1, (b1 - b2) / b1, percent, val, extraRange);
        }

        static public float SimilarRate(float r1, float r2, float percent = 0.1f, float val = 0.001f, float extraRange = 1f)
        {
            // var a = new System.Diagnostics.CodeAnalysis.ObjectPool();
            var maxDiff = Math.Abs(r1) * percent + val;
            var curDiff = Math.Abs(r1 - r2);
            var s = 1 - curDiff / maxDiff;
            // s = (float)((s > 0 ? 1 : -1) * s * s);
            if (s < -0f)
            {
                return -99999f;
            }
            return s;
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