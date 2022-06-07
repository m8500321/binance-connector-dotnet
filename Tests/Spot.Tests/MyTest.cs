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
    // /api/*接口和 /sapi/*接口采用两套不同的访问限频规则, 两者互相独立。

    // /api/*的接口相关：
    //     按IP和按UID(account)两种模式分别统计, 两者互相独立。
    //     以 /api/*开头的接口按IP限频，且所有接口共用每分钟1200限制。
    //     每个请求将包含一个 X-MBX-USED-WEIGHT-(intervalNum)(intervalLetter)的头，包含当前IP所有请求的已使用权重。
    //     每个成功的下单回报将包含一个X-MBX-ORDER-COUNT-(intervalNum)(intervalLetter)的头，其中包含当前账户已用的下单限制数量。

    // /sapi/*的接口相关：
    //     按IP和按UID(account)两种模式分别统计, 两者互相独立。
    //     以/sapi/*开头的接口采用单接口限频模式。按IP统计的权重单接口权重总额为每分钟12000；按照UID统计的单接口权重总额是每分钟180000。
    //     每个接口会标明是按照IP或者按照UID统计, 以及相应请求一次的权重值。
    //     按照IP统计的接口, 请求返回头里面会包含X-SAPI-USED-IP-WEIGHT-1M=<value>, 包含当前IP所有请求已使用权重。
    //     按照UID统计的接口, 请求返回头里面会包含X-SAPI-USED-UID-WEIGHT-1M=<value>, 包含当前账户所有已用的UID权重。

    //  一年525,600分钟
    // 一秒1000*10，循环60秒
    // 逐行存入文本
    // 取出来的数据，全部放入内存，然后json转为单个字符串，

    public class MyTest
    {
        static ILogger logger;
        public static string dataDir = "E:/projects/binance-connector-dotnet/datas/";
        /// </summary>
        private static DateTime UTC_START = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        long HOOUR_8 = 8l * 60 * 60 * 1000 * 10000;
        public static long TimeToMS(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - UTC_START).TotalMilliseconds;
        }
        public static void LogMsg(params string[] msg)
        {
            string output = "";
            foreach (var item in msg)
            {
                output += ("\n" + item);
            }
            output += ("\n" + new StackTrace(new StackFrame(1, true)).ToString());
            logger.LogInformation(output);

        }
        string apiKey = "Sud7YtqxuBnwKDJZ7zgnGlZuOxssZ5QzrtvhkL7CfHMfP0fWglYzMScttIDsJ42v";
        string apiSecret = "QnU7QZwESqUnsuwYrs8KESVBXo4W8zgERMukgUhj9DR8phoY43WQZ0TjZgbbYbs9";
        public static async Task Main(string[] args)
        {
            var thisobj = new MyTest();
            // string apiKey = "Sud7YtqxuBnwKDJZ7zgnGlZuOxssZ5QzrtvhkL7CfHMfP0fWglYzMScttIDsJ42v";
            // string apiSecret = "QnU7QZwESqUnsuwYrs8KESVBXo4W8zgERMukgUhj9DR8phoY43WQZ0TjZgbbYbs9";
            Console.WriteLine("1 Started 1111111");

            // TestConnectivity_Response();

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            logger = loggerFactory.CreateLogger<MyTest>();


            // // request klines
            // File.Delete(dataDir + "data.txt"); //删除指定文件;
            // long startMS = TimeToMS(DateTime.UtcNow);
            // // long startMS = 1650844500000;
            // var maxCount = 1000;
            // var msPerMin = 60 * 1000;
            // var minPerDay = 60 * 24;
            // var intervalMin = 5;
            // var diffMS = intervalMin * msPerMin * maxCount;
            // var yearday = 365;
            // var forNum = (minPerDay * yearday * 1.5) / (intervalMin * maxCount);
            // // 每天多少个n分钟*一年多少天
            // for (int i = 0; i < forNum; i++)
            // {
            //     // 5分钟*每分钟多少秒*每次多少条
            //     startMS -= diffMS;
            //     LogMsg(UTC_START.AddMilliseconds(startMS).ToString());
            //     try
            //     {
            //         await thisobj.KlineCandlestickData_Response(startMS);
            //     }
            //     catch (Exception e)
            //     {
            //         LogMsg(e.ToString());
            //         startMS += diffMS;
            //     }
            //     Thread.Sleep(50);
            // }


            // thisobj.FetchKlineData();
            // thisobj.Data2Readable();
            // thisobj.Data2Serializable();
            // thisobj.AnalyseTime();
            // thisobj.AnalysePrevKline();
            Console.WriteLine("2 End 2222222");
        }



        // k线
        // [
        //   [
        //     1499040000000,      // 开盘时间
        //     "0.01634790",       // 开盘价
        //     "0.80000000",       // 最高价
        //     "0.01575800",       // 最低价
        //     "0.01577100",       // 收盘价(当前K线未结束的即为最新价)
        //     "148976.11427815",  // 成交量
        //     1499644799999,      // 收盘时间
        //     "2434.19055334",    // 成交额
        //     308,                // 成交笔数
        //     "1756.87402397",    // 主动买入成交量
        //     "28.46694368",      // 主动买入成交额
        //     "17928899.62484339" // 请忽略该参数
        //   ]
        // ]
        public async Task KlineCandlestickData_Response(long startTime = 0)
        {

            Market market = new Market(apiKey: apiKey, apiSecret: apiSecret);

            /// <summary>
            /// Kline/candlestick bars for a symbol.<para />
            /// Klines are uniquely identified by their open time.<para />
            /// - If `startTime` and `endTime` are not sent, the most recent klines are returned.<para />
            /// Weight(IP): 1.
            /// </summary>
            /// <param name="symbol">Trading symbol, e.g. BNBUSDT.</param>
            /// <param name="interval">kline intervals.</param>
            /// <param name="startTime">UTC timestamp in ms.</param>
            /// <param name="startTime">UTC timestamp in ms.</param>
            /// <param name="limit">Default 500; max 1000.</param>
            /// <returns>Kline data.</returns>
            // public async Task<string> KlineCandlestickData(string symbol, Interval interval, long? startTime = null, long? endTime = null, int? limit = null)


            // [[1654355760000,"300.80000000","300.80000000","300.30000000","300.40000000","347.54000000",1654355819999,"104434.53110000",216,"91.51200000","27493.67010000","0"],[1654355820000,"300.50000000","300.50000000","300.20000000","300.30000000","96.30000000",1654355879999,"28924.29190000",100,"33.93300000","10194.97730000","0"],
            if (startTime == 0)
            {
                startTime = TimeToMS(DateTime.UtcNow);
            }
            var result = await market.KlineCandlestickData("BTCUSDT", Interval.FIVE_MINUTE, limit: 1000, startTime: startTime);
            LogMsg(result);
            // List<MyKlines> klines = new List<MyKlines>();
            // JArray.Parse(result);


            var karray = JArray.Parse(result);

            using (StreamWriter sw = new StreamWriter(dataDir + "data.txt", true))
            {
                for (int i = 0; i < karray.Count; i++)
                {
                    var item = karray[karray.Count - i - 1];
                    sw.WriteLine(item.ToString().Replace("\r\n", "").Replace(" ", ""));

                }
                // foreach (var item in karray)
                // {
                //     // klines.Add(new MyKlines(item.ToString()));


                //     // foreach (string s in names)
                //     // {
                //     var rawS = item.ToString();
                //     var repS = rawS.Replace("\t", "").Replace("\r\n", "").Replace(" ", "");
                //     sw.WriteLine(repS);

                //     // }
                //     // Thread.Sleep(70);
                // }
            }
            // LogMsg(klines.ToString());
            // Assert.Equal(responseContent, result);
        }

        // 转化成可读内容
        // [1644546000000,"42886.02000000","42902.96000000","42817.93000000","42897.75000000","141.19340000",1644546299999,"6049947.10973040",3257,"66.40442000","2845337.65305600","0"]
        // 开始时间，开盘价，收盘价，最低价，最高价，平均价格，成交额
        public void Data2Readable()
        {
            // List<MyKlines> klines = new List<MyKlines>();
            StreamReader sr = new StreamReader(dataDir + "data_test.txt");
            StreamWriter sw = new StreamWriter(dataDir + "data_output.txt", true);
            //判断文件中是否有字符
            while (sr.Peek() != -1)
            {
                var obj = new MyKline(sr.ReadLine());
                // klines.Add(new MyKlines(sr.ReadLine()));
                var readTm = UTC_START.AddMilliseconds(obj.openTime + HOOUR_8).ToString("yy-M-d HH:mm:ss");
                var avePrice = obj.volumePrice / obj.volume;
                var line = $"{readTm}, {obj.openTime}, {obj.openPrice:F3}, {obj.closePrice:F3}, {obj.minPrice:F3}, {obj.maxPrice:F3}, {avePrice:F3}, {obj.volumePrice:F3}";
                sw.WriteLine(line);
            }
            sr.Close();
            sw.Close();
        }
        public async Task FetchKlineData()
        {
            // request klines
            File.Delete(dataDir + "data.txt"); //删除指定文件;
            long startMS = TimeToMS(DateTime.UtcNow);
            // long startMS = 1650844500000;
            var maxCount = 1000;
            var msPerMin = 60 * 1000;
            var minPerDay = 60 * 24;
            var intervalMin = 5;
            // 每天多少个n分钟*一年多少天
            var forNum = (minPerDay * 365) / (intervalMin * maxCount);
            for (int i = 0; i < forNum; i++)
            {
                // 5分钟*每分钟多少秒*每次多少条
                startMS -= intervalMin * msPerMin * maxCount;
                LogMsg(UTC_START.AddMilliseconds(startMS).ToString());
                await KlineCandlestickData_Response(startMS);
                Thread.Sleep(50);
            }
        }
        public void Data2Serializable()
        {
            StreamReader sr = new StreamReader(dataDir + "data.txt");
            var sList = new KlineList();
            //判断文件中是否有字符
            FileStream fileStream = new FileStream(dataDir + "serializ.data", FileMode.Create);
            BinaryFormatter bf = new BinaryFormatter();
            while (sr.Peek() != -1)
            {
                var obj = new MyKline(sr.ReadLine());
                sList.myKlines.Add(obj);
            }
            bf.Serialize(fileStream, sList);
            fileStream.Flush();
            fileStream.Close();
            sr.Close();
            // sw.Close();
        }
        public KlineList Serializable2Data()
        {
            FileStream fileStream = new FileStream(dataDir + "serializ.data", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            BinaryFormatter bf = new BinaryFormatter();
            var sList = bf.Deserialize(fileStream) as KlineList;
            fileStream.Flush();
            fileStream.Close();
            return sList;
        }
        public void AnalyseTime()
        {
            // 每个时刻的涨跌情况
            Dictionary<string, List<float>> timeAdd = new Dictionary<string, List<float>>();
            var slist = Serializable2Data();
            foreach (var item in slist.myKlines)
            {
                var key = UTC_START.AddMilliseconds(item.openTime + HOOUR_8).ToString("HH:mm");
                if (!timeAdd.ContainsKey(key))
                {
                    timeAdd.Add(key, new List<float>());
                }
                var addValue = (item.closePrice - item.openPrice) / item.openPrice;
                timeAdd[key].Add(addValue);
            }
            // var timeProb = new Dictionary<string, float>();
            string output = "";
            foreach (var kv in timeAdd)
            {
                var incCount = 0;
                var descCount = 0;
                var sumAddValue = 0f;
                foreach (var addValue in kv.Value)
                {
                    if (addValue > 0)
                    {
                        incCount++;
                    }
                    else
                    {
                        descCount++;
                    }
                    sumAddValue += (addValue * 100);
                }
                var sumPercent = sumAddValue / kv.Value.Count;
                var probability = (float)incCount / (incCount + descCount);
                if (sumPercent > 0.03 || sumPercent < -0.03)
                // if (probability > 0.56 || probability < 0.44)
                {
                    probability *= 100;
                    // timeProb.Add(kv.Key, probability);
                    output += (kv.Key + "\t概率" + decimal.Round((decimal)probability, 3) + "%\t涨幅" + decimal.Round((decimal)sumPercent, 3) + "%\n");
                }
            }
            LogMsg(output);
        }

        public void AnalysePrevKline()
        {
            // 每个时刻的涨跌情况
            Dictionary<string, List<float>> prevAdd = new Dictionary<string, List<float>>();
            var slist = Serializable2Data();
            for (int i = 3; i < slist.myKlines.Count; i++)
            {
                var item = slist.myKlines[i];
                var prevItem1 = slist.myKlines[i - 1];
                // var prevItem2 = slist.myKlines[i-2];
                // var key = UTC_START.AddMilliseconds(item.openTime + HOOUR_8).ToString("HH:mm");
                var add1 = (prevItem1.closePrice - prevItem1.openPrice) / prevItem1.openPrice;
                add1 *= 100;
                add1 = add1 > 0.15f ? 0.15f : add1;
                add1 = add1 < -0.15f ? -0.15f : add1;
                var key = add1.ToString("f2");
                if (!prevAdd.ContainsKey(key))
                {
                    prevAdd.Add(key, new List<float>());
                }
                var addValue = (item.closePrice - item.openPrice) / item.openPrice;
                prevAdd[key].Add(addValue);
            }

            prevAdd = prevAdd.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            string output = "";
            foreach (var kv in prevAdd)
            {
                var incCount = 0;
                var descCount = 0;
                var sumAddValue = 0f;
                foreach (var addValue in kv.Value)
                {
                    if (addValue > 0)
                    {
                        incCount++;
                    }
                    else
                    {
                        descCount++;
                    }
                    sumAddValue += (addValue * 100);
                }
                var sumPercent = sumAddValue / kv.Value.Count;
                var probability = (float)incCount / (incCount + descCount);
                if (sumPercent > 0.01 || sumPercent < -0.01)
                {
                    probability *= 100;
                    output += ("prev:" + float.Parse(kv.Key) * 100 + "\t上涨概率" + decimal.Round((decimal)probability, 3) + "%\t平均涨幅" + decimal.Round((decimal)sumPercent, 3) + "%\n");
                }
            }
            LogMsg(output);
        }

        public void AnalyseBigVolume()
        {
            Dictionary<string, List<float>> prevVolume = new Dictionary<string, List<float>>();
            var slist = Serializable2Data();
            for (int i = 3; i < slist.myKlines.Count; i++)
            {
                var item = slist.myKlines[i];
                var prevItem1 = slist.myKlines[i - 1];
                // var prevItem2 = slist.myKlines[i-2];
                // var key = UTC_START.AddMilliseconds(item.openTime + HOOUR_8).ToString("HH:mm");
                var add1 = (prevItem1.closePrice - prevItem1.openPrice) / prevItem1.openPrice;
                add1 *= 100;
                add1 = add1 > 0.15f ? 0.15f : add1;
                add1 = add1 < -0.15f ? -0.15f : add1;
                var key = add1.ToString("f2");
                // if (!prevAdd.ContainsKey(key))
                // {
                //     prevAdd.Add(key, new List<float>());
                // }
                var addValue = (item.closePrice - item.openPrice) / item.openPrice;
                // prevAdd[key].Add(addValue);
            }

        }
    }
    [Serializable]
    public class KlineList
    {
        public List<MyKline> myKlines = new List<MyKline>();
    }
    [Serializable]
    public class MyKline
    {

        public MyKline(string kJson = "")
        {
            // MyTest.LogMsg(kJson);
            var jObj = JArray.Parse(kJson);
            openTime = (long)jObj[0]; // 开盘时间
            openPrice = (float)jObj[1]; // 开盘价
            maxPrice = (float)jObj[2]; // 最高价
            minPrice = (float)jObj[3]; // 最低价
            closePrice = (float)jObj[4]; // 收盘价
            volume = (float)jObj[5]; // 成交量，一手
            closeTime = (long)jObj[6]; // 收盘时间
            volumePrice = (float)jObj[7]; // 成交额，价值
            volumeCount = (int)jObj[8]; // 成交笔数
            activeVolume = (float)jObj[9]; // 主动成交量
            activeVolumePrice = (float)jObj[10]; // 主动成交额
        }
        public long openTime { get; set; }
        public float openPrice { get; set; }
        public float maxPrice { get; set; }
        public float minPrice { get; set; }
        public float closePrice { get; set; }
        public float volume { get; set; }
        public long closeTime { get; set; }
        public float volumePrice { get; set; }
        public int volumeCount { get; set; }
        public float activeVolume { get; set; }
        public float activeVolumePrice { get; set; }

    }
}