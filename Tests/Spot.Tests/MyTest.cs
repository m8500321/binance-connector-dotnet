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
using Xunit.Abstractions;

namespace Binance.Common.Tests
{
    // /api/*接口和 /sapi/*接口采用两套不同的访问限频规则, 两者互相独立。

    // /api/*的接口相关:
    //     按IP和按UID(account)两种模式分别统计, 两者互相独立。
    //     以 /api/*开头的接口按IP限频，且所有接口共用每分钟1200限制。
    //     每个请求将包含一个 X-MBX-USED-WEIGHT-(intervalNum)(intervalLetter)的头，包含当前IP所有请求的已使用权重。
    //     每个成功的下单回报将包含一个X-MBX-ORDER-COUNT-(intervalNum)(intervalLetter)的头，其中包含当前账户已用的下单限制数量。

    // /sapi/*的接口相关:
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
        // static public string dataDir = "";
        static private DateTime UTC_START = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        readonly int HOOUR8_MS = 8 * 60 * 60 * 1000;
        string apiKey = "Sud7YtqxuBnwKDJZ7zgnGlZuOxssZ5QzrtvhkL7CfHMfP0fWglYzMScttIDsJ42v";
        string apiSecret = "QnU7QZwESqUnsuwYrs8KESVBXo4W8zgERMukgUhj9DR8phoY43WQZ0TjZgbbYbs9";
        static List<string> symbols = new List<string> { "BTCUSDT", "ETHUSDT", "XRPUSDT", "BNBUSDT" };

        static public async Task Main(string[] args)
        {
            var curDir = System.Environment.CurrentDirectory;
            var arr = curDir.Split("binance-connector-dotnet");

            MyTools.dataDir = arr[0].Replace("\\", "/") + "binance-connector-dotnet/datas/";

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            MyTools.logger = loggerFactory.CreateLogger("");
            var startDt = DateTime.Now;
            MyTools.LogMsg("Start~");
            var thisobj = new MyTest();
            var tasks = new List<Task>();
            foreach (var name in symbols)
            {
                Task t = Task.Run(async () =>
                    {

                        // await MyTools.FetchKlineData(name);
                        // MyTools.Data2Readable(name);
                        // MyTools.Data2Serializable(name);

                        // thisobj.AnalyseTime(name);
                        // thisobj.AnalysePrevKline(name);

                        // thisobj.AnalyseBigVolume(name);
                        // thisobj.AnalyseTend(name);
                        // thisobj.TestRandomInc(name);
                        // thisobj.AnalyseCurveMatch(name);
                        thisobj.AnalyseCurveMatch2(name);
                    });

                tasks.Add(t);
            }
            Task.WaitAll(tasks.ToArray());
            MyTools.LogMsg($"End~ 运行耗时: {DateTime.Now - startDt}");
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
                var key = UTC_START.AddMilliseconds(item.openTime + HOOUR8_MS).ToString("ddd HH");
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
                if (MathF.Abs(aveValue) > 0.03 * 0.01 && (probability > 0.53 || probability < 0.47))
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
            for (int i = 20; i < slist.myKlines.Count; i++)
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

        // 70-25% 100-13% ；100 0.04 60% - 6%
        public void TestRandomInc(string symbol)
        {
            Dictionary<int, List<int>> cachePrev2 = new Dictionary<int, List<int>>();
            var slist = MyTools.Serializable2Data(symbol);

            Random random = new Random();
            var output = symbol;
            var count = 0;
            var checkCount = 100;
            for (int i = 0; i < 1000; i++)
            {
                var nextList = new List<float>();
                var nextList2 = new List<float>();
                var addCount = 0f;
                var addCount2 = 0f;
                int r = random.Next(100, 30000);
                for (int j = 0; j < checkCount * 5; j += 5)
                {
                    var idx = r + i * checkCount + j;
                    var item = slist.myKlines[idx];
                    nextList.Add(item.incPercent);
                    var sum = slist.FollowingInc(idx, 2);
                    nextList2.Add(sum);
                    if (item.incPercent > 0)
                    {
                        addCount++;
                    }
                    if (sum > 0)
                    {
                        addCount2++;
                    }
                }
                var rate = addCount / checkCount;
                var rate2 = addCount2 / checkCount;
                var e = nextList.Sum() / checkCount;
                var e2 = nextList2.Sum() / checkCount;
                if ((e > 0.04f * 0.01 || e < -0.04f * 0.01) && (rate > 0.6 || rate < 0.4))
                {
                    output += $"\n{i} e1:{MyTools.ToPercent(e)} rate:{MyTools.ToPercent(rate)}";
                    count++;
                }
                if ((e2 > 0.04f * 0.01 || e2 < -0.04f * 0.01) && (rate2 > 0.6 || rate2 < 0.4))
                {
                    output += $"\n{i} e2:{MyTools.ToPercent(e2)} rate:{MyTools.ToPercent(rate2)}";
                    count++;
                }
            }
            MyTools.LogMsg(output + "\ncount:" + count);

        }
        // 0.01-0.30
        // 最低0.01，最高10%
        // 曲线匹配
        // public void AnalyseCurveMatch(string symbol)
        // {
        //     Dictionary<int, List<int>> cachePrev2 = new Dictionary<int, List<int>>();
        //     var slist = MyTools.Serializable2Data(symbol);
        //     var args = new Dictionary<string, float>()
        //     {
        //         {"prev_weight",8f},
        //         {"prev_weight_down",1f},
        //         {"max_weight",3f},
        //         {"max_weight_down",3f},
        //         {"price_weight",3f},
        //         {"price_weight_down",1f},
        //         {"close_weight",4f},
        //         {"close_weight_down",1f},
        //         {"volume_weight",2f},

        //         {"need_weight",33f},
        //         {"similar_range",0.1f},
        //         {"similar_val",0.002f},
        //     };
        //     var s_range = args["similar_range"];
        //     var s_val = args["similar_val"];
        //     for (int i = 3000; i < 4000; i = i + 2)
        //     // for (int i = 100; i < slist.myKlines.Count / 2; i++)
        //     {
        //         // 被匹配
        //         for (int j = i + 20; j < slist.myKlines.Count; j++)
        //         {
        //             // 匹配项
        //             // 相似度*权重

        //             var prevValue = 0f;
        //             var itemI = slist.myKlines[i];
        //             var closeI = itemI.closePrice;
        //             var itemJ = slist.myKlines[j];
        //             var closeJ = itemJ.closePrice;
        //             for (int idx = 1; idx < 5; idx++)
        //             {
        //                 // 往前2条
        //                 var itemIPrev = slist.myKlines[i - idx];
        //                 var itemJPrev = slist.myKlines[j - idx];
        //                 var value = (args["prev_weight"] - idx * args["prev_weight_down"]) * MyTools.SimilarRate((itemIPrev.closePrice - closeI) / closeI, (itemJPrev.closePrice - closeJ) / closeJ, s_range, s_val, idx * 0.1f + 1);
        //                 var value2 = (args["prev_weight"] - idx * args["prev_weight_down"]) * MyTools.SimilarRate(itemIPrev.incPercent, itemJPrev.incPercent, s_range, s_val, idx * 0.1f + 1);
        //                 // var value = (args["prev_weight"] - idx * 2f) * MyTools.SimilarRate(itemIPrev.incPercent, itemJPrev.incPercent, s_range, s_val);
        //                 prevValue += (value + value2);
        //             }
        //             var minValue2 = 0f;
        //             var minValue1 = 0f;
        //             var maxValue2 = 0f;
        //             var maxValue1 = 0f;

        //             var volumeRate = 0f;
        //             // volumeRate = (args["volume_weight"] - 0) * MyTools.SimilarRate(itemI.prevAveVolumeList[0] / itemI.prevAveVolumeList[2], itemJ.prevAveVolumeList[0] / itemJ.prevAveVolumeList[2], s_range, s_val, 2f);

        //             minValue1 = (args["max_weight"]) * MyTools.SimilarRate((itemI.prevMinList[1] - closeI) / closeI, (itemJ.prevMinList[1] - closeJ) / closeJ, s_range, s_val, 1.5f);
        //             maxValue1 = (args["max_weight"]) * MyTools.SimilarRate((itemI.prevMaxList[1] - closeI) / closeI, (itemJ.prevMaxList[1] - closeJ) / closeJ, s_range, s_val, 1.5f);

        //             minValue2 = (args["max_weight"] - 0) * MyTools.SimilarRate((itemI.prevMinList[2] - closeI) / closeI, (itemJ.prevMinList[2] - closeJ) / closeJ, s_range, s_val, 1.5f);
        //             maxValue2 = (args["max_weight"] - 0) * MyTools.SimilarRate((itemI.prevMaxList[2] - closeI) / closeI, (itemJ.prevMaxList[2] - closeJ) / closeJ, s_range, s_val, 1.5f);

        //             var avePrice2 = 0f;
        //             var avePrice1 = 0f;
        //             var avePrice0 = 0f;
        //             avePrice2 = (args["price_weight"] - 2 * args["price_weight_down"]) * MyTools.SimilarRate((itemI.prevAvePriceList[2] - closeI) / closeI, (itemJ.prevAvePriceList[2] - closeJ) / closeJ, s_range, s_val);
        //             avePrice1 = (args["price_weight"] - 1 * args["price_weight_down"]) * MyTools.SimilarRate((itemI.prevAvePriceList[1] - closeI) / closeI, (itemJ.prevAvePriceList[1] - closeJ) / closeJ, s_range, s_val);
        //             avePrice0 = (args["price_weight"] - 0 * args["price_weight_down"]) * MyTools.SimilarRate((itemI.prevAvePriceList[0] - closeI) / closeI, (itemJ.prevAvePriceList[0] - closeJ) / closeJ, s_range, s_val);

        //             var aveValue3 = 0f;
        //             aveValue3 = (args["close_weight"] - 3 * args["close_weight_down"]) * MyTools.SimilarRate((itemI.prevAveCloseList[3] - closeI) / closeI, (itemJ.prevAveCloseList[3] - closeJ) / closeJ, s_range, s_val, 1.5f);
        //             var aveValue2 = (args["close_weight"] - 2 * args["close_weight_down"]) * MyTools.SimilarRate((itemI.prevAveCloseList[2] - closeI) / closeI, (itemJ.prevAveCloseList[2] - closeJ) / closeJ, s_range, s_val);
        //             var aveValue1 = (args["close_weight"] - 1 * args["close_weight_down"]) * MyTools.SimilarRate((itemI.prevAveCloseList[1] - closeI) / closeI, (itemJ.prevAveCloseList[1] - closeJ) / closeJ, s_range, s_val);
        //             var aveValue0 = (args["close_weight"] - 0 * args["close_weight_down"]) * MyTools.SimilarRate((itemI.prevAveCloseList[0] - closeI) / closeI, (itemJ.prevAveCloseList[0] - closeJ) / closeJ, s_range, s_val);
        //             var sumValue = volumeRate + prevValue + maxValue1 + minValue1 + maxValue2 + minValue2 + avePrice0 + avePrice1 + avePrice2 + aveValue0 + aveValue1 + aveValue2 + aveValue3;
        //             if (sumValue > 0)
        //             {
        //                 if (!cachePrev2.ContainsKey(i))
        //                 {
        //                     cachePrev2.Add(i, new List<int>());
        //                 }
        //                 cachePrev2[i].Add(j);
        //                 cachePrev2[i].Add((int)sumValue);
        //             }
        //         }
        //         if (cachePrev2.ContainsKey(i) && cachePrev2[i].Count < 200)
        //         {
        //             cachePrev2.Remove(i);
        //         }
        //     }
        //     // MyTools.LogMsg(symbol, $"总匹配量:{cachePrev2.Count}");
        //     string output = "";
        //     var countAll = 0f;
        //     foreach (var kvItem in cachePrev2)
        //     {
        //         var next1List = ListPool<float>.PopItem();
        //         var next2List = ListPool<float>.PopItem();
        //         var winCount1 = 0f;
        //         var winCount2 = 0f;
        //         var loseCount1 = 0f;
        //         var loseCount2 = 0f;
        //         var targetList = kvItem.Value;
        //         // foreach (var item in targetList)
        //         for (int i = 0; i < targetList.Count; i = i + 2)
        //         {
        //             var item = targetList[i];
        //             if (slist.myKlines.Count <= item + 2)
        //             {
        //                 continue;
        //             }
        //             var weight = 1f;
        //             weight = (float)targetList[i + 1] / args["need_weight"] - 0.5f;
        //             // weight = Math.Min(weight, 1f);
        //             next1List.Add(slist.myKlines[item + 1].incPercent * weight);
        //             var ave2 = slist.FollowingInc(item + 1, 2);
        //             next2List.Add(ave2 * weight);
        //             // similar = (float)Math.Pow(similar, 1.5);
        //             winCount1 += (slist.myKlines[item + 1].incPercent > 0 ? weight : 0);
        //             loseCount1 += (slist.myKlines[item + 1].incPercent < 0 ? weight : 0);
        //             winCount2 += (ave2 > 0 ? weight : 0);
        //             loseCount2 += (ave2 < 0 ? weight : 0);
        //         }
        //         var itemCount = targetList.Count / 2;
        //         var weightCount1 = (winCount1 + loseCount1);
        //         var weightCount2 = (winCount2 + loseCount2);
        //         var winRatio1 = (float)winCount1 / weightCount1;
        //         var winDelta = 0.08f;
        //         var eDelta = 0.04f * 0.01;
        //         var highWin1 = winRatio1 > 0.5 + winDelta || winRatio1 < 0.5 - winDelta;
        //         var winRatio2 = (float)winCount2 / weightCount2;
        //         var highWin2 = winRatio2 > 0.5 + winDelta || winRatio2 < 0.5 - winDelta;
        //         var sum1 = next1List.Sum() / weightCount1;
        //         var sum2 = next2List.Sum() / weightCount2;
        //         if ((sum1 > eDelta || sum1 < -eDelta) && highWin1)
        //         {
        //             output += $"{kvItem.Key} count:{itemCount} w:{weightCount1} [1]涨幅:{MyTools.ToPercent(sum1)} 胜率:{MyTools.ToPercent(winRatio1)}\n";
        //             countAll++;
        //         }
        //         if ((sum2 > eDelta || sum2 < -eDelta) && highWin2)
        //         {
        //             output += $"{kvItem.Key} count:{itemCount} w:{weightCount2} [2]涨幅:{MyTools.ToPercent(sum2)} 胜率:{MyTools.ToPercent(winRatio2)}\n";
        //             countAll++;
        //         }
        //     }
        //     var argStr = "";
        //     foreach (var item in args)
        //     {
        //         argStr += (item.Key + ":" + MyTools.CutDecim(item.Value, 3) + ", ");
        //     }
        //     var title = "优质数:" + countAll + "\t优质率:" + MyTools.ToPercent(countAll / cachePrev2.Count);

        //     MyTools.LogMsg(symbol, $"总匹配量:{cachePrev2.Count}", argStr, title, output);
        // }
        // 精准匹配
        public void AnalyseCurveMatch2(string symbol)
        {
            Dictionary<int, List<int>> cachePrev2 = new Dictionary<int, List<int>>();
            var floatPool = new ListPool<float>();
            var intPool = new ListPool<int>();

            var matchArgs = new Dictionary<string, float>()
            {
                {"prev_weight",4f},
                {"prev_weight_down",1f},
                {"max_weight",3f},
                {"max_weight_down",3f},
                {"price_weight",3},
                {"price_weight_down",1f},
                {"close_weight",3f},
                {"close_weight_down",1f},
                {"volume_weight",2f},

                {"need_weight",35f},
                {"similar_range",0.15f},
                {"similar_val",0.0005f},
            };
            var s_range = matchArgs["similar_range"];
            var s_val = matchArgs["similar_val"];
            var symbolAll = new List<string> { "BTCUSDT", "ETHUSDT", "XRPUSDT", "BNBUSDT" };
            List<MyKline> allKlines = new List<MyKline>();
            foreach (var s in symbolAll)
            {
                var slist = MyTools.Serializable2Data(s);
                allKlines.AddRange(slist.myKlines);
            }
            var thisKline = MyTools.Serializable2Data(symbol).myKlines;
            var start = 1000;
            var len = 500;
            for (int i = start; i < start + len; i = i + 1)
            // for (int i = 100; i < allKlines.Count / 2; i++)
            {
                // 被匹配
                for (int j = 100; j < allKlines.Count; j++)
                {
                    // 匹配项
                    // 相似度*权重

                    // var prevValue = 0f;
                    var itemI = thisKline[i];
                    var closeI = itemI.closePrice;
                    var itemJ = allKlines[j];
                    var closeJ = itemJ.closePrice;
                    var prevList = floatPool.PopItem();
                    for (int idx = 1; idx < 4; idx++)
                    {
                        // 往前2条
                        var itemIPrev = thisKline[i - idx];
                        var itemJPrev = allKlines[j - idx];
                        var value1 = 0f;
                        value1 = (matchArgs["prev_weight"]) * MyTools.SimilarValue(closeI, itemIPrev.closePrice, closeJ, itemJPrev.closePrice, s_range, s_val);
                        var value2 = 0f;
                        // value2 = (args["prev_weight"] - idx * args["prev_weight_down"]) * MyTools.SimilarRate(itemIPrev.incPercent, itemJPrev.incPercent, s_range, s_val);
                        // prevValue += (value1 + value2);
                        prevList.Add(value1 + value2);
                    }
                    var volumeRate0 = 0f;
                    var volumeRate1 = 0f;
                    // volumeRate0 = (args["volume_weight"]) * MyTools.SimilarValue(itemI.volume, itemI.prevAveVolumeList[2], itemJ.volume, itemJ.prevAveVolumeList[2], s_range, s_val, 2f);
                    // volumeRate1 = (args["volume_weight"]) * MyTools.SimilarValue(itemI.prevAveVolumeList[1], itemI.prevAveVolumeList[2], itemJ.prevAveVolumeList[1], itemJ.prevAveVolumeList[2], s_range, s_val, 2f);

                    var minValue1 = 0f;
                    var maxValue1 = 0f;
                    var minValue2 = 0f;
                    var maxValue2 = 0f;
                    // minValue1 = (args["max_weight"]) * MyTools.SimilarValue(closeI, itemI.prevMinList[1], closeJ, itemJ.prevMinList[1], s_range, s_val);
                    // maxValue1 = (args["max_weight"]) * MyTools.SimilarValue(closeI, itemI.prevMaxList[1], closeJ, itemJ.prevMaxList[1], s_range, s_val);
                    // minValue2 = (args["max_weight"]) * MyTools.SimilarValue(closeI, itemI.prevMinList[0], closeJ, itemJ.prevMinList[0], s_range, s_val);
                    // maxValue2 = (args["max_weight"]) * MyTools.SimilarValue(closeI, itemI.prevMaxList[0], closeJ, itemJ.prevMaxList[0], s_range, s_val);
                    var aveList = floatPool.PopItem();
                    // var lastidx = 0;
                    // for (int idx = 0; idx < 4; idx++)
                    // {
                    //     // 加权逐条
                    //     lastidx -= idx * 5;
                    //     aveList.Add(args["price_weight"] * MyTools.SimilarValue(closeI, thisKline[i - idx * 5].prevAvePriceList[i], closeJ, allKlines[j - idx * 5].prevAvePriceList[i], s_range, s_val));
                    // }

                    for (int idx = 0; idx < 4; idx++)
                    {
                        // 5 10 20 40
                        aveList.Add(matchArgs["price_weight"] * MyTools.SimilarValue(closeI, itemI.prevAvePriceList[idx], closeJ, itemJ.prevAvePriceList[idx], s_range, s_val));
                    }

                    // for (int idx = 0; idx < 4; idx++)
                    // {
                    // // 每5条比较
                    //     aveList.Add(args["price_weight"] * MyTools.SimilarValue(closeI, thisKline[i - idx * 5].prevAvePriceList[0], closeJ, allKlines[j - idx * 5].prevAvePriceList[0], s_range, s_val));
                    // }

                    // for (int idx = 0; idx < 4; idx++)
                    // {
                    //     // 交易量尺度的比较
                    //     aveList.Add(args["price_weight"] * MyTools.SimilarValue(closeI, itemI.prevVolumePriceList[idx], closeJ, itemJ.prevVolumePriceList[idx], s_range, s_val));
                    // }

                    var aveClose3 = 0f;
                    var aveClose2 = 0f;
                    var aveClose1 = 0f;
                    var aveClose0 = 0f;
                    // aveClose3 = (args["close_weight"]) * MyTools.SimilarValue(closeI, itemI.prevAveCloseList[3], closeJ, itemJ.prevAveCloseList[3], s_range, s_val);
                    // aveClose2 = (args["close_weight"]) * MyTools.SimilarValue(closeI, itemI.prevAveCloseList[2], closeJ, itemJ.prevAveCloseList[2], s_range, s_val);
                    // aveClose1 = (args["close_weight"]) * MyTools.SimilarValue(closeI, itemI.prevAveCloseList[1], closeJ, itemJ.prevAveCloseList[1], s_range, s_val);
                    // aveClose0 = (args["close_weight"]) * MyTools.SimilarValue(closeI, itemI.prevAveCloseList[0], closeJ, itemJ.prevAveCloseList[0], s_range, s_val);

                    var sumValue = volumeRate0 + volumeRate1 + prevList.Sum() + maxValue1 + minValue1 + maxValue2 + minValue2 + aveList.Sum() + aveClose0 + aveClose1 + aveClose2 + aveClose3;
                    if (sumValue > 0)
                    {
                        if (!cachePrev2.ContainsKey(i))
                        {
                            cachePrev2.Add(i, intPool.PopItem());
                        }
                        cachePrev2[i].Add(j);
                        cachePrev2[i].Add((int)sumValue);
                    }
                    floatPool.PushItem(prevList);
                    floatPool.PushItem(aveList);
                }
                if (cachePrev2.ContainsKey(i) && cachePrev2[i].Count < 150)
                {
                    intPool.PushItem(cachePrev2[i]);
                    cachePrev2.Remove(i);
                }
            }
            // MyTools.LogMsg(symbol, $"总匹配量:{cachePrev2.Count}");
            string output = "";
            var usefulCount = 0f;
            var rightCount = 0f;
            var realSum = 0f;

            // var RATE_DELTA = 0.02f;
            // var E_DELTA = 0.06f * 0.01;
            var filterArgs = new Dictionary<string, float>()
            {
                {"RATE_DELTA", 0.02f},
                {"E_DELTA", 0.06f * 0.01f}
            };
            foreach (var kvItem in cachePrev2)
            {
                // 1-3的涨幅
                var incList = floatPool.PopItem();
                var targetList = kvItem.Value;
                var totalWeight = 0f;
                for (int i = 0; i < targetList.Count; i = i + 2)
                {
                    totalWeight += (float)targetList[i + 1];
                }
                var itemCount = targetList.Count / 2;
                var aveWeight = totalWeight / itemCount;
                for (int i = 0; i < targetList.Count; i = i + 2)
                {
                    var itemIdx = targetList[i];
                    if (allKlines.Count <= itemIdx + 3)
                    {
                        continue;
                    }
                    var weight = 1f;
                    weight = (float)targetList[i + 1] / aveWeight;
                    for (int j = 0; j < 3; j++)
                    {
                        incList.Add(allKlines[itemIdx].nextSumIncList[j]);
                    }
                    incList.Add(weight);
                }
                // 每个阶段期望
                var eList = floatPool.PopItem();
                // 每阶段胜率
                var winList = floatPool.PopItem();
                for (int idx = 0; idx < 3; idx++)
                {
                    var sumInc = 0f;
                    var incCount = 0f;
                    for (int i = 0; i < incList.Count; i = i + 4)
                    {
                        if (incList[i + idx] > 0)
                        {
                            incCount += incList[i + 3];
                        }
                        sumInc += incList[i + idx] * incList[i + 3];
                    }
                    var eInc = sumInc / itemCount;
                    var winRate = incCount / itemCount;
                    if ((eInc > filterArgs["E_DELTA"] && winRate > 0.5 + filterArgs["RATE_DELTA"]) || (eInc < -filterArgs["E_DELTA"] && winRate < 0.5 - filterArgs["RATE_DELTA"]))
                    {
                        var nextInc = thisKline[kvItem.Key].nextSumIncList[idx];
                        output += $"{kvItem.Key} num:{itemCount} \t{idx + 1}-涨幅:{MyTools.ToPercent(eInc)} 胜率:{MyTools.ToPercent(winRate)} \treal:{MyTools.ToPercent(nextInc)}\n";
                        usefulCount++;
                        if (nextInc * eInc > 0)
                        {
                            rightCount++;
                            realSum += Math.Abs(nextInc);
                        }
                        else
                        {
                            realSum -= Math.Abs(nextInc);
                        }
                    }
                }
                floatPool.PushItem(incList);
                floatPool.PushItem(eList);
                floatPool.PushItem(winList);

            }
            var argStr = "";
            foreach (var item in matchArgs)
            {
                argStr += (item.Key + ":" + MyTools.CutDecim(item.Value, 3) + ", ");
            }
            var title = "\n优质数:" + usefulCount + "\t优质率:" + MyTools.ToPercent(usefulCount / cachePrev2.Count) + "\t正确率:" + MyTools.ToPercent(rightCount / usefulCount) + "\t交易期望:" + MyTools.ToPercent(realSum / usefulCount);

            MyTools.LogMsg(symbol, $"样本数:{len} 总匹配量:{cachePrev2.Count} 百分比:{(float)cachePrev2.Count / len}\n", argStr, title, output);
        }
    }
}