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
        private static DateTime timeStampStartTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static long TimeToMS(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - timeStampStartTime).TotalMilliseconds;
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
            // // long startMS = TimeToMS(DateTime.UtcNow);
            // long startMS = 1650844500000;
            // var maxCount = 1000;
            // var msPerMin = 60 * 1000;
            // var minPerDay = 60 * 24;
            // var intervalMin = 5;
            // // 每天多少个n分钟*一年多少天
            // for (int i = 0; i < (minPerDay * 365) / (intervalMin * maxCount); i++)
            // {
            //     // 5分钟*每分钟多少秒*每次多少条
            //     startMS -= intervalMin * msPerMin * maxCount;
            //     LogMsg(timeStampStartTime.AddMilliseconds(startMS).ToString());
            //     await thisobj.KlineCandlestickData_Response(startMS);
            //     Thread.Sleep(50);
            // }

            thisobj.Data2Readable();

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
        // 开始时间，开盘价，收盘价，最低价，最高价，成交额，成交笔数，主动成交额
        public void Data2Readable()
        {
            // List<MyKlines> klines = new List<MyKlines>();
            StreamReader sr = new StreamReader(dataDir + "data_test.txt");
            StreamWriter sw = new StreamWriter(dataDir + "output_test.txt", true);

            // 先用列表一条条取出json
            // using (StreamReader sr = new StreamReader(dataDir + "data_test.txt"))
            // {
            //判断文件中是否有字符
            while (sr.Peek() != -1)
            {
                var obj = new MyKlines(sr.ReadLine());
                // klines.Add(new MyKlines(sr.ReadLine()));
                var readTm = timeStampStartTime.AddMilliseconds(obj.openTime).ToString("yy-M-d HH:mm:ss");
                var line = $"{readTm}, {obj.openTime}, {obj.openPrice:F3}, {obj.closePrice:F3}, {obj.minPrice:F3}, {obj.maxPrice:F3}, {obj.volumePrice:F3}, {obj.volumeCount}, {obj.activeVolumePrice:F3}";
                sw.WriteLine(line);
            }
            // }
            // using (StreamWriter sw = new StreamWriter(dataDir + "output_test.txt", true))
            // {
            // foreach (var item in klines)
            // {

            // }
            // }
            sr.Close();
            sw.Close();
        }
    }
    public class MyKlines
    {

        public MyKlines(string kJson = "")
        {
            // MyTest.LogMsg(kJson);

            var jObj = JArray.Parse(kJson);
            openTime = (long)jObj[0];
            openPrice = (float)jObj[1];
            maxPrice = (float)jObj[2];
            minPrice = (float)jObj[3];
            closePrice = (float)jObj[4];
            volume = (float)jObj[5];
            closeTime = (long)jObj[6];
            volumePrice = (float)jObj[7];
            volumeCount = (int)jObj[8];
            activeVolume = (float)jObj[9];
            activeVolumePrice = (float)jObj[10];
        }
        /// <summary>
        /// 开盘时间
        /// </summary>
        public long openTime { get; set; }

        /// <summary>
        /// 开盘价
        /// </summary>
        public float openPrice { get; set; }

        /// <summary>
        /// 最高价
        /// </summary>
        public float maxPrice { get; set; }

        /// <summary>
        /// 最低价
        /// </summary>
        public float minPrice { get; set; }

        /// <summary>
        /// 收盘价
        /// </summary>
        public float closePrice { get; set; }

        /// <summary>
        /// 成交量，一手
        /// </summary>
        public float volume { get; set; }

        /// <summary>
        /// 收盘时间
        /// </summary>
        public long closeTime { get; set; }

        /// <summary>
        /// 成交额，价值
        /// </summary>
        public float volumePrice { get; set; }

        /// <summary>
        /// 成交笔数
        /// </summary>
        public int volumeCount { get; set; }

        /// <summary>
        /// 主动成交量
        /// </summary>
        public float activeVolume { get; set; }

        /// <summary>
        /// 主动成交额
        /// </summary>
        public float activeVolumePrice { get; set; }

    }
}