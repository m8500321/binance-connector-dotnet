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
        // static ILogger logger;
        static public string dataDir = System.Environment.CurrentDirectory.Replace("\\", "/").Replace("Tests/Spot.Tests", "datas/");
        static private DateTime UTC_START = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        readonly int HOOUR8_MS = 8 * 60 * 60 * 1000;
        string apiKey = "Sud7YtqxuBnwKDJZ7zgnGlZuOxssZ5QzrtvhkL7CfHMfP0fWglYzMScttIDsJ42v";
        string apiSecret = "QnU7QZwESqUnsuwYrs8KESVBXo4W8zgERMukgUhj9DR8phoY43WQZ0TjZgbbYbs9";
        static public async Task Main(string[] args)
        {
            // dataDir = System.Environment.CurrentDirectory.Replace("\\", "/").Replace("Tests/Spot.Tests", "datas/");
            Console.WriteLine("Started 1111111");
            var thisobj = new MyTest();

            long startMS = MyTools.MsFromUTC0(DateTime.UtcNow);

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            MyTools.logger = loggerFactory.CreateLogger("");

            // "BTCUSDT" "ETHUSDT" "BNBUSDT"
            var symbols = new List<string> { "BTCUSDT" };
            // var symbols = new List<string> { "BTCUSDT", "ETHUSDT", "BNBUSDT" };
            foreach (var name in symbols)
            {
                // await thisobj.FetchKlineData(name);
                // thisobj.Data2Readable(name);
                // thisobj.Data2Serializable(name);

                // thisobj.AnalyseTime(name);
                // thisobj.AnalysePrevKline(name);

                // thisobj.AnalyseBigVolume(name);
                // thisobj.AnalyseTend(name);
                thisobj.AnalyseCurveMatch(name);

            }
            MyTools.LogMsg($"运行耗时：{MyTools.MsFromUTC0(DateTime.UtcNow) - startMS / 1000}");

            Console.WriteLine("End 2222222");
        }



        public void AnalyseTime(string symbol)
        {
            // 每个时刻的涨跌情况
            Dictionary<string, List<float>> timeAdd = new Dictionary<string, List<float>>();
            var kList = MyTools.Serializable2Data(symbol);
            foreach (var item in kList.myKlines)
            {
                // var key = UTC_START.AddMilliseconds(item.openTime + HOOUR8_MS).ToString("ddd");
                // var key = UTC_START.AddMilliseconds(item.openTime + HOOUR8_MS).ToString("d");
                var key = UTC_START.AddMilliseconds(item.openTime + HOOUR8_MS).ToString("HH:mm");
                // var key = UTC_START.AddMilliseconds(item.openTime + HOOUR8_MS).ToString("dd");
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
                if (MathF.Abs(aveValue) > 0.03 * 0.01)
                // if (probability > 0.56 || probability < 0.44)
                {
                    // timeProb.Add(kv.Key, probability);
                    output += ("时间点" + item.Key + "\t上涨概率" + MyTools.ToPercent(probability) + "\t涨幅" + MyTools.ToPercent(aveValue) + "\tcount:" + item.Value.Count + "\n");
                }
            }
            MyTools.LogMsg(symbol, output);
        }

        public void AnalysePrevKline(string symbol)
        {
            // 每个时刻的涨跌情况
            Dictionary<string, List<float>> addDict = new Dictionary<string, List<float>>();
            var slist = MyTools.Serializable2Data(symbol);
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
                if (Math.Abs(bigger) < 0.02)
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
                output += ("前一条:" + MyTools.ToPercent(item.Key) + "\tnum:" + item.Value.Count + "\t上涨概率" + MyTools.ToPercent(probability) + "\t涨幅" + MyTools.ToPercent(aveValue) + "\n");
                // }
            }
            MyTools.LogMsg(symbol, output);
        }

        public void AnalyseBigVolume(string symbol)
        {
            Dictionary<string, List<float>> prevVolume = new Dictionary<string, List<float>>();
            var slist = MyTools.Serializable2Data(symbol);
            for (int i = 3; i < slist.myKlines.Count; i++)
            {
                var item = slist.myKlines[i];
                var prevItem1 = slist.myKlines[i - 1];
                var prevItem2 = slist.myKlines[i - 2];
                var prevItem3 = slist.myKlines[i - 3];
                var multiFactor = prevItem1.volume * 2 / (prevItem2.volume + prevItem3.volume);
                // 交易量为2倍，涨幅超过0.5%
                if (multiFactor > 2 && Math.Abs(prevItem1.incPercent) > 0.005)
                {
                    var sig = prevItem1.isUp ? "+ x" : "- x";
                    multiFactor = Math.Min(multiFactor, 4);
                    var key = sig + MyTools.CutDecim(multiFactor, 1);
                    if (!prevVolume.ContainsKey(key))
                    {
                        prevVolume.Add(key, new List<float>());
                    }
                    prevVolume[key].Add(item.incPercent);
                }
            }
            prevVolume = prevVolume.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            string output = "";
            foreach (var dictItem in prevVolume)
            {

                var sum = dictItem.Value.Sum() / dictItem.Value.Count;
                if (Math.Abs(sum) > 0.0005)
                {
                    output += ($"{dictItem.Key} {MyTools.ToPercent(sum)} count:{dictItem.Value.Count}\n");
                }
            }
            MyTools.LogMsg(symbol, output);
        }

        // 连续N连趋势后预测
        public void AnalyseTend(string symbol)
        {
            Dictionary<string, List<float>> count2inc = new Dictionary<string, List<float>>();
            var slist = MyTools.Serializable2Data(symbol);
            for (int i = 10; i < slist.myKlines.Count; i++)
            {
                var item = slist.myKlines[i];
                for (int count = 3; count <= 6; count++)
                {
                    var isContinuous = false;
                    var firstSign = slist.myKlines[i - 1].incPercent;
                    for (int j = 1; j <= count; j++)
                    {
                        var prev = slist.myKlines[i - j];
                        if (firstSign * prev.incPercent < 0)
                        {
                            isContinuous = false;
                            break;
                        }
                        else
                        {
                            firstSign += prev.incPercent;
                        }
                        if (j == count)
                        {
                            isContinuous = true;
                        }
                    }
                    if (!isContinuous)
                    {
                        break;
                    }
                    else
                    {

                        // if (Math.Abs(firstSign) > 0.008)
                        // {
                        var key = (firstSign < 0 ? "-" : "+") + count;
                        if (!count2inc.ContainsKey(key))
                        {
                            count2inc.Add(key, new List<float>());
                        }
                        count2inc[key].Add(item.incPercent);
                        // }
                    }

                }

            }
            count2inc = count2inc.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            string output = "";
            foreach (var dictItem in count2inc)
            {

                var sum = dictItem.Value.Sum() / dictItem.Value.Count;
                // if (Math.Abs(sum) > 0.0001)
                // {
                output += ($"{dictItem.Key} {MyTools.ToPercent(sum)} count:{dictItem.Value.Count}\n");
                // }
            }
            MyTools.LogMsg(symbol, output);
        }

        // 0.01-0.30
        // 最低0.01，最高10%
        // 曲线匹配
        public void AnalyseCurveMatch(string symbol)
        {
            // 缓存前两条特征

            Dictionary<int, List<int>> cachePrev2 = new Dictionary<int, List<int>>();
            var slist = MyTools.Serializable2Data(symbol);
            for (int i = 100; i < slist.myKlines.Count / 2; i++)
            {
                // 被匹配
                for (int j = i + 20; j < slist.myKlines.Count; j++)
                {
                    // 匹配项
                    var isMatch = true;
                    for (int idx = 0; idx < 5; idx++)
                    {
                        // 往前5条
                        var itemI = slist.myKlines[i - idx];
                        var itemJ = slist.myKlines[j - idx];
                        var diff = Math.Max(Math.Abs(itemI.incPercent) * 0.15, 0.015 * 0.01);
                        if (itemJ.incPercent < itemI.incPercent - diff || itemJ.incPercent > itemI.incPercent + diff)
                        {
                            isMatch = false;
                            break;
                        }
                    }
                    if (isMatch)
                    {
                        if (!cachePrev2.ContainsKey(i))
                        {
                            cachePrev2.Add(i, new List<int>());
                            cachePrev2[i].Add(i);
                        }
                        cachePrev2[i].Add(j);
                    }
                }
            }
            foreach (var item in cachePrev2)
            {
                if (item.Value.Count < 6)
                {
                    cachePrev2[item.Key] = null;
                }
            }
            // Dictionary<float, List<int>> similarIdx = new Dictionary<float, List<int>>();


            // for (int i = 100; i < slist.myKlines.Count; i++)
            // {
            //     var prevItem1 = slist.myKlines[i - 1];
            //     var prevItem2 = slist.myKlines[i - 2];
            //     var key = MyTools.CutDecim(prevItem1.incPercent, 2) * 100 + MyTools.CutDecim(prevItem2.incPercent, 2);
            //     if (!cachePrev2.ContainsKey(key))
            //     {
            //         cachePrev2.Add(key, new List<int>());
            //     }
            //     cachePrev2[key].Add(i);
            // }
            // inc2inc = inc2inc.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            string output = "";
            foreach (var kvItem in cachePrev2)
            {
                if (kvItem.Value == null)
                {
                    continue;
                }
                foreach (var listItem in kvItem.Value)
                {

                    var next0 = slist.myKlines[listItem + 0];
                    var next1 = slist.myKlines[listItem + 1];
                    var next2 = slist.myKlines[listItem + 2];
                    output += ($"{listItem} \t0:{MyTools.ToPercent(next0.incPercent)} \t1:{MyTools.ToPercent(next1.incPercent)} \t2:{MyTools.ToPercent(next2.incPercent)} \tave:{MyTools.ToPercent(next2.incPercent + next1.incPercent)}\n");
                }
                output += kvItem.Value.Count + "\n------------------------------\n\n";

            }
            // foreach (var dictItem in inc2inc)
            // {

            //     var sum = dictItem.Value.Sum() / dictItem.Value.Count;

            //     if (Math.Abs(sum) > 0.0005)
            //     {
            //         output += ($"{dictItem.Key} {MyTools.ToPercent(sum)} count:{dictItem.Value.Count}\n");
            //     }
            // }
            MyTools.LogMsg(symbol, output);
        }
    }
}