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
        public static string dataDir;
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
                output += (item + "\n");
            }
            logger.LogInformation(output);
            logger.LogInformation(new StackTrace(new StackFrame(1, true)).ToString());

        }
        string apiKey = "Sud7YtqxuBnwKDJZ7zgnGlZuOxssZ5QzrtvhkL7CfHMfP0fWglYzMScttIDsJ42v";
        string apiSecret = "QnU7QZwESqUnsuwYrs8KESVBXo4W8zgERMukgUhj9DR8phoY43WQZ0TjZgbbYbs9";
        public static async Task Main(string[] args)
        {
            dataDir = System.Environment.CurrentDirectory.Replace("\\", "/").Replace("Tests/Spot.Tests", "datas/");
            Console.WriteLine("Started 1111111");
            var thisobj = new MyTest();


            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            logger = loggerFactory.CreateLogger("");

            // "BTCUSDT" "ETHUSDT" "BNBUSDT"
            var symbols = new List<string> { "BTCUSDT", "ETHUSDT", "BNBUSDT" };
            foreach (var name in symbols)
            {
                // await thisobj.FetchKlineData(name);
                // thisobj.Data2Readable(name);
                // thisobj.Data2Serializable(name);
                // thisobj.AnalyseTime(name);
                // thisobj.AnalysePrevKline(name);
                thisobj.AnalyseBigVolume(name);

            }
            Console.WriteLine("End 2222222");
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
        public async Task KlineCandlestickData_Response(long startTime, string symbol)
        {
            Market market = new Market(apiKey: apiKey, apiSecret: apiSecret);
            var result = await market.KlineCandlestickData(symbol, Interval.FIVE_MINUTE, limit: 1000, startTime: startTime);

            var karray = JArray.Parse(result);

            using (StreamWriter sw = new StreamWriter(dataDir + symbol + ".txt", true))
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
        // public void Data2Readable()
        // {
        //     // List<MyKlines> klines = new List<MyKlines>();
        //     StreamReader sr = new StreamReader(dataDir + "data_test.txt");
        //     StreamWriter sw = new StreamWriter(dataDir + "data_output.txt", true);
        //     //判断文件中是否有字符
        //     while (sr.Peek() != -1)
        //     {
        //         var obj = new JsonKline(sr.ReadLine());
        //         // klines.Add(new MyKlines(sr.ReadLine()));
        //         var readTm = UTC_START.AddMilliseconds(obj.openTime + HOOUR_8).ToString("yy-M-d HH:mm:ss");
        //         var avePrice = obj.volumePrice / obj.volume;
        //         var line = $"{readTm}, {obj.openTime}, {obj.openPrice:F3}, {obj.closePrice:F3}, {obj.minPrice:F3}, {obj.maxPrice:F3}, {avePrice:F3}, {obj.volumePrice:F3}";
        //         sw.WriteLine(line);
        //     }
        //     sr.Close();
        //     sw.Close();
        // }

        public async Task FetchKlineData(string symbol)
        {
            // request klines
            File.Delete(dataDir + symbol + ".txt"); //删除指定文件;
            long startMS = TimeToMS(DateTime.UtcNow);
            // long startMS = 1650844500000;
            var maxCount = 1000;
            var msPerMin = 60 * 1000;
            var minPerDay = 60 * 24;
            var intervalMin = 5;
            var diffMS = intervalMin * msPerMin * maxCount;
            var yearday = 365;
            var forNum = (minPerDay * yearday * 1.5) / (intervalMin * maxCount);
            // 每天多少个n分钟*一年多少天
            for (int i = 0; i < forNum; i++)
            {
                // 5分钟*每分钟多少秒*每次多少条
                startMS -= diffMS;
                // LogMsg(UTC_START.AddMilliseconds(startMS).ToString());
                try
                {
                    await KlineCandlestickData_Response(startMS, symbol);
                }
                catch (Exception e)
                {
                    LogMsg("Error!", e.ToString());
                    startMS += diffMS;
                }
                Thread.Sleep(30);
            }
        }

        public void Data2Serializable(string symbol)
        {
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
            bf.Serialize(fileStream, sList);
            fileStream.Flush();
            fileStream.Close();
            sr.Close();
            // sw.Close();
        }

        public KlineList Serializable2Data(string symbol)
        {
            FileStream fileStream = new FileStream(dataDir + symbol + ".data", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            BinaryFormatter bf = new BinaryFormatter();
            var kList = bf.Deserialize(fileStream) as KlineList;
            fileStream.Flush();
            fileStream.Close();
            return kList;
        }

        public void AnalyseTime(string symbol)
        {
            // 每个时刻的涨跌情况
            Dictionary<string, List<float>> timeAdd = new Dictionary<string, List<float>>();
            var kList = Serializable2Data(symbol);
            foreach (var item in kList.myKlines)
            {
                // var key = UTC_START.AddMilliseconds(item.openTime + HOOUR_8).ToString("ddd");
                var key = UTC_START.AddMilliseconds(item.openTime + HOOUR_8).ToString("HH:mm");
                // var key = UTC_START.AddMilliseconds(item.openTime + HOOUR_8).ToString("ddd:HH:mm");
                if (!timeAdd.ContainsKey(key))
                {
                    timeAdd.Add(key, new List<float>());
                }
                var addValue = item.incPercent;
                timeAdd[key].Add(addValue);
            }
            // var timeProb = new Dictionary<string, float>();
            string output = "";
            foreach (var item in timeAdd)
            {
                var incCount = 0;
                var sumAddValue = 0f;
                foreach (var addValue in item.Value)
                {
                    if (addValue > 0)
                    {
                        incCount++;
                    }
                    sumAddValue += addValue;
                }
                var aveValue = sumAddValue / item.Value.Count;
                var probability = (float)incCount / item.Value.Count;
                if (MathF.Abs(aveValue) > 0.04 * 0.01)
                // if (probability > 0.56 || probability < 0.44)
                {
                    // timeProb.Add(kv.Key, probability);
                    output += ("时间点" + item.Key + "\t上涨概率" + ToPercent(probability) + "\t涨幅" + ToPercent(aveValue) + "\n");
                }
            }
            LogMsg(symbol, output);
        }

        public void AnalysePrevKline(string symbol)
        {
            // 每个时刻的涨跌情况
            Dictionary<string, List<float>> addDict = new Dictionary<string, List<float>>();
            var slist = Serializable2Data(symbol);
            for (int i = 3; i < slist.myKlines.Count; i++)
            {
                var item = slist.myKlines[i];
                var prevItem1 = slist.myKlines[i - 1];
                var prevItem2 = slist.myKlines[i - 2];
                var prevItem3 = slist.myKlines[i - 3];
                var sum1 = prevItem1.incPercent;
                var sum2 = prevItem1.incPercent + prevItem2.incPercent;
                var sum3 = prevItem1.incPercent + prevItem2.incPercent + prevItem3.incPercent;
                var bigger = sum1 * sum1 > sum2 * sum2 ? sum1 : sum2;
                bigger = bigger * bigger > sum2 * sum2 ? bigger : sum2;
                // var maxSum = MathF.Max(Math.Abs(sum1), Math.Abs(sum2), Math.Abs(sum3));
                if (Math.Abs(bigger) < 0.03)
                {
                    continue;
                }
                bigger = Math.Clamp(bigger, -0.06f, 0.06f);
                var key = bigger.ToString("f2");
                if (!addDict.ContainsKey(key))
                {
                    addDict.Add(key, new List<float>());
                }
                addDict[key].Add(item.incPercent);
            }

            addDict = addDict.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            string output = "";
            foreach (var item in addDict)
            {
                var incCount = 0;
                var sumAddValue = 0f;
                foreach (var addValue in item.Value)
                {
                    if (addValue > 0)
                    {
                        incCount++;
                    }
                    sumAddValue += (addValue);
                }
                var aveValue = sumAddValue / item.Value.Count;
                var probability = (float)incCount / item.Value.Count;
                // if (MathF.Abs(float.Parse(item.Key)) > 0.01f)
                // {
                output += ("前一条:" + ToPercent(item.Key) + "\tnum:" + item.Value.Count + "\t上涨概率" + ToPercent(probability) + "\t涨幅" + ToPercent(aveValue) + "\n");
                // }
            }
            LogMsg(symbol, output);
        }

        public float CutDecim(string s, int count)
        {
            var f = float.Parse(s);
            return MathF.Round(f, count);
        }

        public float CutDecim(float f, int count)
        {
            return MathF.Round(f, count);
        }
        public string ToPercent(string s)
        {
            var f = float.Parse(s);
            var sig = f > 0 ? "+" : "";
            return sig + MathF.Round(f * 100, 2) + "%";
        }
        public string ToPercent(float f)
        {
            var sig = f > 0 ? "+" : "";
            return sig + MathF.Round(f * 100, 2) + "%";
        }

        public void AnalyseBigVolume(string symbol)
        {
            Dictionary<string, List<float>> prevVolume = new Dictionary<string, List<float>>();
            var slist = Serializable2Data(symbol);
            for (int i = 3; i < slist.myKlines.Count; i++)
            {
                var item = slist.myKlines[i];
                var prevItem1 = slist.myKlines[i - 1];
                var prevItem2 = slist.myKlines[i - 2];
                var prevItem3 = slist.myKlines[i - 3];
                var multiFactor = prevItem1.volumePerHand * 2 / (prevItem2.volumePerHand + prevItem3.volumePerHand);
                if (multiFactor > 2)
                {
                    var sig = prevItem1.isUp ? "+ x" : "- x";
                    multiFactor = Math.Min(multiFactor, 4);
                    var key = sig + CutDecim(multiFactor, 1);
                    if (!prevVolume.ContainsKey(key))
                    {
                        prevVolume.Add(key, new List<float>());
                    }
                    // var addValue = (item.closePrice - item.openPrice) / item.openPrice;
                    prevVolume[key].Add(item.incPercent);
                }

            }
            prevVolume = prevVolume.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            string output = "";
            foreach (var dictItem in prevVolume)
            {

                var sum = dictItem.Value.Sum()/dictItem.Value.Count;
                // foreach (var listItem in dictItem.Value)
                // {

                // }
                if (Math.Abs(sum) > 0.0005)
                {
                    output += ($"{dictItem.Key} {ToPercent(sum)} count:{dictItem.Value.Count}\n");
                }
            }
            LogMsg(symbol, output);
        }
    }

    [Serializable]
    public class MyKline
    {
        public MyKline(JsonKline jk)
        {
            openTime = jk.openTime;
            openPrice = jk.openPrice;
            maxPrice = jk.maxPrice;
            minPrice = jk.minPrice;
            closePrice = jk.closePrice;
            volume = jk.volume;
            closeTime = jk.closeTime;
            volumePrice = jk.volumePrice;
            volumeCount = jk.volumeCount;
            activeVolume = jk.activeVolume;
            activeVolumePrice = jk.activeVolumePrice;


            incPercent = (closePrice - openPrice) / openPrice;
            maxIncPercent = (maxPrice - openPrice) / openPrice;
            maxDescPercent = (minPrice - openPrice) / openPrice;
            avePrice = volumePrice / volume;
            volumePerHand = volumePrice / volumeCount;
            isUp = closePrice > openPrice ? true : false;

        }
        public long openTime; // 开盘时间
        public float openPrice; // 开盘价
        public float maxPrice; // 最高价
        public float minPrice; // 最低价
        public float closePrice; // 收盘价
        public float volume; // 成交量，手
        public long closeTime; // 收盘时间
        public float volumePrice; // 成交额，价值
        public int volumeCount; // 成交笔数
        public float activeVolume; // 主动成交量
        public float activeVolumePrice; // 主动成交额

        public float incPercent = 0f; // 涨幅
        public float maxIncPercent = 0f; // 最大涨幅
        public float maxDescPercent = 0f; // 最大跌幅
        public float avePrice = 0f; // 平均价
        public float volumePerHand = 0f; // 交易额/手
        public bool isUp = true; // 是否上涨
    }

    [Serializable]
    public class KlineList
    {
        public List<MyKline> myKlines = new List<MyKline>();
    }

    [Serializable]
    public class JsonKline
    {

        public JsonKline(string kJson = "")
        {
            // MyTest.LogMsg(kJson);
            var jObj = JArray.Parse(kJson);
            openTime = (long)jObj[0]; // 开盘时间
            openPrice = (float)jObj[1]; // 开盘价
            maxPrice = (float)jObj[2]; // 最高价
            minPrice = (float)jObj[3]; // 最低价
            closePrice = (float)jObj[4]; // 收盘价
            volume = (float)jObj[5]; // 成交量，手
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