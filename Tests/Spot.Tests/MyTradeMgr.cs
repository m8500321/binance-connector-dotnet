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

    public class MyTradeMgr
    {

        public static async Task StartRobot()
        {

            // 检查数据时间戳

            // 补齐到最新
            foreach (var symbol in MyTest.symbolAll)
            {
                await MergeLatestData(symbol);
                MyTools.Text2Serializable(symbol);
                MyTools.LoadFileData(symbol);
            }
            // foreach (var symbol in MyTest.symbolAll)
            // {
            // }
            while (true)
            {
                // todo 4:50
                UpdateMyOrders(MyTest.symbolAll);
                // todo 5:00-5:20
                foreach (var symbol in MyTest.symbolAll)
                {
                    var hasNew = await MergeLatestData(symbol);
                    if (hasNew)
                    {
                        if (ActiveOrders.Instance.orders[symbol] != null)
                        {
                            // todo 已有订单

                        }
                        else
                        {
                            MyTools.Text2Serializable(symbol);
                            MyTools.LoadFileData(symbol, true);
                            var tend = CheckTradeNum(symbol);
                            MyTools.LogMsg(symbol, tend.ToString());
                            if (Math.Abs(tend) > 0.0001) ;
                            {
                                await MyTools.MarketTrade(symbol.Replace("_F", ""), false, (tend > 0 ? Side.BUY : Side.SELL), 0.001m);
                            }
                        }
                    }
                }


                // await MyTools.KlineCandlestickData_Response(0, "BTCUSDT_F", "kline");

                // await MyTools.RequestData("BTCUSDT_F", "kline");
                Thread.Sleep(1000);
            }
            // Console.Read();
        }

        // 用最新的K线计算交易值
        public static float CheckTradeNum(string symbol)
        {

            // Dictionary<int, List<int>> cachePrev2 = new Dictionary<int, List<int>>();
            var targetList = new List<int>();
            var floatPool = new ListPool<float>();
            var intPool = new ListPool<int>();

            var matchArgs = new Dictionary<string, float>()
            {
                {"prev_weight",4f},
                // {"prev_weight_down",1f},
                {"max_weight",3f},
                // {"max_weight_down",3f},
                {"price_weight",3},
                // {"price_weight_down",1f},
                {"close_weight",3f},
                // {"close_weight_down",1f},
                {"volume_weight",2f},

                {"need_weight",35f},
                {"similar_range",0.05f},
                {"similar_val",0.001f},

                {"need_count",150},
            };
            var filterArgs = new Dictionary<string, float>()
            {
                {"RATE_DELTA", 0.01f},
                {"E_DELTA", 0.07f * 0.01f}
            };
            var s_range = matchArgs["similar_range"];
            var s_val = matchArgs["similar_val"];
            var prev_weight = matchArgs["prev_weight"];
            var price_weight = matchArgs["price_weight"];
            var need_count = matchArgs["need_count"];
            // var symbolAll = new List<string> { "BTCUSDT", "ETHUSDT", "XRPUSDT", "BNBUSDT" };
            List<MyKline> allKlines = new List<MyKline>();
            foreach (var s in MyTest.symbolAll)
            {
                var slist = MyTools.LoadFileData(s);
                allKlines.AddRange(slist.myKlines);
            }
            var allKlineArray = allKlines.ToArray();
            var thisKline = MyTools.LoadFileData(symbol).myKlines;
            var thisKlineArray = thisKline.ToArray();
            var start = 1000;
            var len = 1000;
            // for (int i = start; i < start + len; i = i + 2)
            // // for (int i = 100; i < klineArray.Length / 2; i++)
            // {
            List<int> iList = null;
            // 被匹配
            var itemI = thisKlineArray[thisKlineArray.Length];
            for (int j = 100; j < allKlineArray.Length; j++)
            {
                // 匹配项
                // 相似度*权重

                // var prevValue = 0f;
                var sumValue = 0f;
                var closeI = itemI.closePrice;
                var itemJ = allKlineArray[j];
                var closeJ = itemJ.closePrice;
                // var prevList = floatPool.PopItem();
                for (int idx = 0; idx < 3; idx++)
                {
                    // 往前2条
                    // var itemIPrev = thisKlineArray[i - idx];
                    // var itemJPrev = allKlineArray[j - idx];
                    sumValue += (prev_weight) * MyTools.SimilarRate(itemI.prevClosePriceRate[idx], itemJ.prevClosePriceRate[idx], s_range, s_val);
                    // sumValue += (prev_weight) * MyTools.SimilarValue(closeI, itemIPrev.closePrice, closeJ, itemJPrev.closePrice, s_range, s_val);
                    // sumValue += (prev_weight) * MyTools.SimilarValue(closeI, itemIPrev.perVolumePrice, closeJ, itemJPrev.perVolumePrice, s_range, s_val);
                    // sumValue += (prev_weight) * MyTools.SimilarRate(itemIPrev.incPercent, itemJPrev.incPercent, s_range, s_val);

                    if (sumValue < 0)
                    {
                        continue;
                    }
                }
                // var volumeRate0 = 0f;
                // var volumeRate1 = 0f;
                // volumeRate0 = (matchArgs["volume_weight"]) * MyTools.SimilarValue(itemI.volume, itemI.prevAveVolumeList[2], itemJ.volume, itemJ.prevAveVolumeList[2], s_range, s_val, 2f);
                // volumeRate1 = (matchArgs["volume_weight"]) * MyTools.SimilarValue(itemI.prevAveVolumeList[1], itemI.prevAveVolumeList[2], itemJ.prevAveVolumeList[1], itemJ.prevAveVolumeList[2], s_range, s_val, 2f);

                // var minValue1 = 0f;
                // var maxValue1 = 0f;
                // var minValue2 = 0f;
                // var maxValue2 = 0f;
                // minValue1 = (args["max_weight"]) * MyTools.SimilarValue(closeI, itemI.prevMinList[1], closeJ, itemJ.prevMinList[1], s_range, s_val);
                // maxValue1 = (args["max_weight"]) * MyTools.SimilarValue(closeI, itemI.prevMaxList[1], closeJ, itemJ.prevMaxList[1], s_range, s_val);
                // minValue2 = (args["max_weight"]) * MyTools.SimilarValue(closeI, itemI.prevMinList[0], closeJ, itemJ.prevMinList[0], s_range, s_val);
                // maxValue2 = (args["max_weight"]) * MyTools.SimilarValue(closeI, itemI.prevMaxList[0], closeJ, itemJ.prevMaxList[0], s_range, s_val);
                // var aveList = floatPool.PopItem();
                // var lastidx = 0;
                // for (int idx = 0; idx < 4; idx++)
                // {
                //     // 加权逐条
                //     lastidx -= idx * 5;
                //     aveList.Add(args["price_weight"] * MyTools.SimilarValue(closeI, thisKlineArray[i - idx * 5].prevAvePriceList[i], closeJ, klineArray[j - idx * 5].prevAvePriceList[i], s_range, s_val));
                // }

                // 5 10 20 40
                for (int idx = 0; idx < 4; idx++)
                {
                    // sumValue += price_weight * MyTools.SimilarValue(closeI, itemI.prevAvePriceList[idx], closeJ, itemJ.prevAvePriceList[idx], s_range, s_val);

                    // sumValue += price_weight * MyTools.SimilarValue(closeI, itemI.prevAveCloseList[idx], closeJ, itemJ.prevAveCloseList[idx], s_range, s_val);

                    // sumValue += price_weight * MyTools.SimilarValue(closeI, itemI.prevVolumePriceList[idx], closeJ, itemJ.prevVolumePriceList[idx], s_range, s_val);
                    sumValue += price_weight * MyTools.SimilarRate(itemI.prevVolumePriceRate[idx], itemJ.prevVolumePriceRate[idx], s_range, s_val);
                    if (sumValue < 0)
                    {
                        continue;
                    }
                }

                // for (int idx = 0; idx < 4; idx++)
                // {
                // // 每5条比较
                //     aveList.Add(args["price_weight"] * MyTools.SimilarValue(closeI, thisKlineArray[i - idx * 5].prevAvePriceList[0], closeJ, klineArray[j - idx * 5].prevAvePriceList[0], s_range, s_val));
                // }

                // for (int idx = 0; idx < 4; idx++)
                // {
                //     // 交易量尺度的比较
                //     aveList.Add(args["price_weight"] * MyTools.SimilarValue(closeI, itemI.prevVolumePriceList[idx], closeJ, itemJ.prevVolumePriceList[idx], s_range, s_val));
                // }

                // var aveClose3 = 0f;
                // var aveClose2 = 0f;
                // var aveClose1 = 0f;
                // var aveClose0 = 0f;
                // aveClose3 = (args["close_weight"]) * MyTools.SimilarValue(closeI, itemI.prevAveCloseList[3], closeJ, itemJ.prevAveCloseList[3], s_range, s_val);
                // aveClose2 = (args["close_weight"]) * MyTools.SimilarValue(closeI, itemI.prevAveCloseList[2], closeJ, itemJ.prevAveCloseList[2], s_range, s_val);
                // aveClose1 = (args["close_weight"]) * MyTools.SimilarValue(closeI, itemI.prevAveCloseList[1], closeJ, itemJ.prevAveCloseList[1], s_range, s_val);
                // aveClose0 = (args["close_weight"]) * MyTools.SimilarValue(closeI, itemI.prevAveCloseList[0], closeJ, itemJ.prevAveCloseList[0], s_range, s_val);

                // sumValue = volumeRate0 + volumeRate1 + prevList.Sum() + maxValue1 + minValue1 + maxValue2 + minValue2 + aveList.Sum() + aveClose0 + aveClose1 + aveClose2 + aveClose3;
                if (sumValue > 0)
                {
                    targetList.Add(j);
                    targetList.Add((int)sumValue);
                }
                // floatPool.PushItem(prevList);
                // floatPool.PushItem(aveList);
            }
            if (targetList.Count < need_count)
            {
                return 0;
            }
            // }
            // MyTools.LogMsg(symbol, $"总匹配量:{cachePrev2.Count}");
            string output = "";
            var usefulCount = 0f;
            var rightCount = 0f;
            var realSum = 0f;

            // var RATE_DELTA = 0.02f;
            // var E_DELTA = 0.06f * 0.01;

            // foreach (var kvItem in cachePrev2)
            // {
            // 1-3的涨幅
            var incList = floatPool.PopItem();
            // var targetList = kvItem.Value;
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
                if (allKlineArray.Length <= itemIdx + 3)
                {
                    continue;
                }
                var weight = 1f;
                weight = (float)targetList[i + 1] / aveWeight;
                // 三个增量和一个权重
                for (int j = 0; j < 3; j++)
                {
                    incList.Add(allKlineArray[itemIdx].nextSumIncList[j]);
                }
                incList.Add(weight);
            }
            // 每个阶段期望
            var eList = floatPool.PopItem();
            // 每阶段胜率
            var winList = floatPool.PopItem();
            var eLast = 0f;
            var rateLast = 0f;
            for (int idx = 0; idx < 3; idx++)
            {
                // 1-3每个增量
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
                if (idx == 0 || (eLast * eInc > 0 && (rateLast - 0.5) * (winRate - 0.5) > 0))
                {
                    // 和前一条相同
                    if ((eInc > filterArgs["E_DELTA"] && winRate > 0.5 + filterArgs["RATE_DELTA"]) || (eInc < -filterArgs["E_DELTA"] && winRate < 0.5 - filterArgs["RATE_DELTA"]))
                    {
                        // var nextInc = thisKlineArray[kvItem.Key].nextSumIncList[idx];
                        // output += $"{kvItem.Key} num:{itemCount} \t{idx + 1}-涨幅:{MyTools.ToPercent(eInc)} 胜率:{MyTools.ToPercent(winRate)} \t{nextInc * eInc > 0}:{MyTools.ToPercent(nextInc)}\n";
                        usefulCount++;
                        return eInc;
                        // if (nextInc * eInc > 0)
                        // {
                        //     rightCount++;
                        //     realSum += Math.Abs(nextInc);
                        // }
                        // else
                        // {
                        //     realSum -= Math.Abs(nextInc);
                        // }
                    }
                }
                eLast = eInc;
                rateLast = winRate;
            }
            floatPool.PushItem(incList);
            floatPool.PushItem(eList);
            floatPool.PushItem(winList);

            // }
            var argStr = "";
            foreach (var item in matchArgs)
            {
                argStr += (item.Key + ":" + MyTools.CutDecim(item.Value, 3) + ", ");
            }
            foreach (var item in filterArgs)
            {
                argStr += (item.Key + ":" + MyTools.CutDecim(item.Value, 5) + ", ");
            }
            var title = "优质数:" + usefulCount + "\t正确率:" + MyTools.ToPercent(rightCount / usefulCount) + "\t交易期望:" + MyTools.ToPercent(realSum / usefulCount) + "\n";

            // MyTools.LogMsg(symbol, $"样本数:{len / 2} 总匹配量:{cachePrev2.Count} 百分比:{(float)cachePrev2.Count / len}", argStr, title, output);


            return 0;
        }
        // 获取到最新
        public static async Task<bool> MergeLatestData(string symbol)
        {
            long startTs = GetTxtEndTs(symbol);

            var current = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var perRequestTime = 500 * 5 * 60 * 1000;
            var endTs = Math.Min(startTs + perRequestTime, current);
            var jarray = new JArray();
            // JArray jarray = new JArray();
            while (startTs < endTs)
            {

                var data = await MyTools.FetchMarketData(startTs, symbol, "kline", 500, false);
                jarray.Merge(data);
                startTs += perRequestTime;
            }
            var hasNew = MergeIntoText(symbol, jarray);
            return hasNew;
        }
        static long GetTxtEndTs(string symbol)
        {
            using (StreamReader sr = new StreamReader(MyTools.dataDir + symbol + ".txt"))
            {

                // var sList = new KlineList();
                //判断文件中是否有字符
                var firstLine = sr.ReadLine();
                if (firstLine != null)
                {
                    // [1655775600000,
                    return long.Parse(firstLine.Substring(1, 13));

                    // var obj = new JsonKline(sr.ReadLine());
                    // var myobj = new MyKline(obj);
                    // sList.myKlines.Add(myobj);
                }
            }
            return 0;
        }
        static bool MergeIntoText(string symbol, JArray array)
        {

            long startTs = GetTxtEndTs(symbol);
            var s = "";
            var hasNew = false;
            for (int i = array.Count - 1; i >= 0; i--)
            {
                JArray item = (JArray)array[i];
                long itemTs = (long)(item[0]);
                if (itemTs > startTs)
                {
                    hasNew = true;
                    s = s + (s == "" ? "" : "\n") + item.ToString().Replace("\r\n", "").Replace(" ", "");
                }
            }
            using (StreamReader sr = new StreamReader(MyTools.dataDir + symbol + ".txt"))
            {
                s = s + "\n" + sr.ReadToEnd();
            }
            using (StreamWriter sw = new StreamWriter(MyTools.dataDir + symbol + ".txt"))
            {
                sw.Write(s);
            }
            return hasNew;
        }

        public static async Task UpdateMyOrders(List<string> symbols)
        {
            foreach (var sb in symbols)
            {
                var orderJson = await MyTools.QueryOrder(sb);
                var order = new SymbolOrder(orderJson);
                ActiveOrders.Instance.UpdateOrder(order);
                Thread.Sleep(100);
            }
            ActiveOrders.Instance.SaveFile();
        }
    }




    public class ActiveOrders
    {
        private static volatile ActiveOrders instance;
        public Dictionary<string, SymbolOrder> orders;

        public static ActiveOrders Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ActiveOrders();
                    instance.orders = new Dictionary<string, SymbolOrder>();
                }
                return instance;

            }
        }
        public void UpdateOrder(SymbolOrder order)
        {
            var key = order.symbol;
            if (!orders.ContainsKey(key))
            {
                orders.Add(key, order);
            }
            if (order.quantity > 0)
            {
                orders[key] = order;
            }
            else
            {
                orders.Remove(key);
            }
        }
        public void SaveFile()
        {

            JArray array = new JArray();
            foreach (var item in orders)
            {
                string orderJ = JsonConvert.SerializeObject(item.Value);
                array.Add(orderJ);
            }
            using (StreamWriter sw = new StreamWriter(MyTools.dataDir + "orders.txt"))
            {
                sw.Write(array.ToString());
            }
        }
    }

    public class SymbolOrder
    {
        public string symbol = "";
        public float quantity = 0f;
        public Side side = Side.BUY;
        public SymbolOrder(string json)
        {
            var array = JArray.Parse(json);
            var sumClose = 0f;
            for (int i = array.Count - 1; i >= 0; i--)
            {
                // 倒叙找到第一个open，对比是否全平
                var item = array[i];
                var q = (float)item["executedQty"];
                if (item["clientOrderId"].ToString().Contains("OPEN"))
                {
                    side = item["side"].ToString() == "SELL" ? Side.SELL : Side.BUY;
                    quantity = q - sumClose;
                    symbol = item["symbol"].ToString();
                    break;
                }
                else
                {
                    sumClose += q;
                }
            }
        }
    }


}