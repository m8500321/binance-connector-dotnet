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
using Newtonsoft.Json.Schema;
using Castle.Core.Internal;

namespace Binance.Common.Tests
{

    public class MyTradeMgr
    {

        public static async Task StartRobot()
        {

            // 检查数据时间戳

            // 补齐到最新
            MyTools.LogMsg("StartRobot");
            // var sb = new List<string> { "SOLUSDT" };
            // foreach (var symbol in sb)
            foreach (var symbol in MyTest.symbolAll)
            {
                await MergeLatestData(symbol, true);
                MyTools.Text2Serializable(symbol);
                MyTools.LoadFileData(symbol);
            }
            // foreach (var symbol in MyTest.symbolAll)
            // {
            // }
            MyTools.LogMsg("init done");
            // UpdateMyOrders();

            while (true)
            {
                // var nowDt = DateTime.UtcNow;
                // if (nowDt.Second > 50)
                // {
                //     MyTools.LogMsg(nowDt.Minute, nowDt.Second);
                // }
                // if (nowDt.Second > 50 && nowDt.Second < 55 && nowDt.Minute % 5 == 4)
                // {
                //     UpdateMyOrders(MyTest.symbolAll);
                // }
                // if (nowDt.Second > 0 && nowDt.Second < 10 && nowDt.Minute % 5 == 0)
                // {

                //     foreach (var symbol in MyTest.symbolAll)
                //     {
                //         var hasNew = await MergeLatestData(symbol);
                //         if (hasNew)
                //         {
                //             if (ActiveOrders.Instance.orders[symbol] != null)
                //             {
                //                 // todo 已有订单

                //             }
                //             else
                //             {
                //                 MyTools.Text2Serializable(symbol);
                //                 MyTools.LoadFileData(symbol, true);
                //                 var tend = CheckTradeNum(symbol);
                //                 MyTools.LogMsg(symbol, tend.ToString());
                //                 if (Math.Abs(tend) > 0.0001) ;
                //                 {
                //                     await MyTools.MarketTrade(symbol.Replace("_F", ""), false, (tend > 0 ? Side.BUY : Side.SELL), 0.001m);
                //                     UpdateMyOrders(new List<string> { symbol });
                //                 }
                //             }
                //         }
                //     }
                // }


                // await MyTools.KlineCandlestickData_Response(0, "BTCUSDT_F", "kline");

                // await MyTools.RequestData("BTCUSDT_F", "kline");
                Thread.Sleep(1000);
            }
            // Console.Read();
        }

        // 用最新的K线计算交易值
        public static float CheckTradeNum(string symbol)
        {

            return 0f;
        }
        // 获取到最新
        public static async Task<bool> MergeLatestData(string symbol, bool isFuture)
        {
            long startTs = GetTxtEndTs(symbol);

            var current = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var perRequestTime = 500 * 5 * 60 * 1000;
            var endTs = Math.Min(startTs + perRequestTime, current);
            var jarray = new JArray();
            // JArray jarray = new JArray();
            while (startTs < endTs)
            {

                var data = await MyTools.FetchMarketData(startTs, symbol, "kline", 500, isFuture);
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
                var lastLine = "";
                while (sr.Peek() > -1)
                {
                    var current = sr.ReadLine();
                    if (!current.IsNullOrEmpty())
                    {
                        lastLine = current;
                    }
                }
                return long.Parse(lastLine.Substring(1, 13));
            }
            return 0;
        }
        static bool MergeIntoText(string symbol, JArray array)
        {

            long startTs = GetTxtEndTs(symbol);
            var s = "";
            var hasNew = false;
            using (StreamWriter sw = new StreamWriter(MyTools.dataDir + symbol + ".txt", true))
            {
                for (int i = 0; i < array.Count; i++)
                {
                    JArray item = (JArray)array[i];
                    long itemTs = (long)(item[0]);
                    if (itemTs > startTs)
                    {
                        hasNew = true;
                        sw.WriteLine(item.ToString().Replace("\r\n", "").Replace(" ", ""));
                    }
                }
            }
            return hasNew;
        }
        public static async Task UpdateMyOrders()
        {
            var json = await MyTools.CheckAccountInfo();
            AccountInfo.Instance.Init(json);
        }
        public static async Task GetExchangeInfo()
        {
            var json = await MyTools.GetExchangeInfo();
            ExchangeInfo.Instance.Init(json);
        }
    }



    // "symbol": "BLZUSDT",  // 交易对
    // "pair": "BLZUSDT",  // 标的交易对
    // "contractType": "PERPETUAL",    // 合约类型
    // "deliveryDate": 4133404800000,  // 交割日期
    // "onboardDate": 1598252400000,     // 上线日期
    // "status": "TRADING",  // 交易对状态
    // "maintMarginPercent": "2.5000",  // 请忽略
    // "requiredMarginPercent": "5.0000", // 请忽略
    // "baseAsset": "BLZ",  // 标的资产
    // "quoteAsset": "USDT", // 报价资产
    // "marginAsset": "USDT", // 保证金资产
    // "pricePrecision": 5,  // 价格小数点位数(仅作为系统精度使用，注意同tickSize 区分）
    // "quantityPrecision": 0,  // 数量小数点位数(仅作为系统精度使用，注意同stepSize 区分）
    // "baseAssetPrecision": 8,  // 标的资产精度
    // "quotePrecision": 8,  // 报价资产精度
    // "underlyingType": "COIN",
    // "underlyingSubType": ["STORAGE"],
    // "settlePlan": 0,
    // "triggerProtect": "0.15", // 开启"priceProtect"的条件订单的触发阈值
    public class ExchangeInfo
    {
        // string rawStr = "";
        JObject rawJson;
        public Dictionary<string, JObject> symbols;
        private static volatile ExchangeInfo instance;
        public static ExchangeInfo Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ExchangeInfo();
                }
                return instance;

            }
        }
        public void Init(string json)
        {
            // rawStr = json;
            rawJson = JObject.Parse(json);
            symbols = new Dictionary<string, JObject>();
            foreach (var item in rawJson["symbols"])
            {
                symbols.TryAdd((string)item["symbol"], (JObject)item);
            }
        }
    }


    // {
    //     "asset": "USDT",        //资产
    //     "walletBalance": "23.72469206",  //余额
    //     "unrealizedProfit": "0.00000000",  // 未实现盈亏
    //     "marginBalance": "23.72469206",  // 保证金余额
    //     "maintMargin": "0.00000000",    // 维持保证金
    //     "initialMargin": "0.00000000",  // 当前所需起始保证金
    //     "positionInitialMargin": "0.00000000",  // 持仓所需起始保证金(基于最新标记价格)
    //     "openOrderInitialMargin": "0.00000000", // 当前挂单所需起始保证金(基于最新标记价格)
    //     "crossWalletBalance": "23.72469206",  //全仓账户余额
    //     "crossUnPnl": "0.00000000" // 全仓持仓未实现盈亏
    //     "availableBalance": "23.72469206",       // 可用余额
    //     "maxWithdrawAmount": "23.72469206",     // 最大可转出余额
    //     "marginAvailable": true,   // 是否可用作联合保证金
    //     "updateTime": 1625474304765  //更新时间
    // },
    //         {
    //   "asset": "USDT",
    //   "walletBalance": "50.25799394", aa
    //   "unrealizedProfit": "-0.01331405",
    //   "marginBalance": "50.24467989",
    //   "maintMargin": "0.11487572",
    //   "initialMargin": "13.60758202",
    //   "positionInitialMargin": "13.60758202",
    //   "openOrderInitialMargin": "0.00000000",
    //   "maxWithdrawAmount": "36.63621669",
    //   "crossWalletBalance": "36.63621669",
    //   "crossUnPnl": "0.00000000",
    //   "availableBalance": "36.63621669", aa
    //   "marginAvailable": true,
    //   "updateTime": 1656081270383
    // },


    // "positions": [  // 头寸，将返回所有市场symbol。
    //     //根据用户持仓模式展示持仓方向，即单向模式下只返回BOTH持仓情况，双向模式下只返回 LONG 和 SHORT 持仓情况
    //     {
    //         "symbol": "BTCUSDT",  // 交易对
    //         "initialMargin": "0",   // 当前所需起始保证金(基于最新标记价格)
    //         "maintMargin": "0", //维持保证金
    //         "unrealizedProfit": "0.00000000",  // 持仓未实现盈亏
    //         "positionInitialMargin": "0",  // 持仓所需起始保证金(基于最新标记价格)
    //         "openOrderInitialMargin": "0",  // 当前挂单所需起始保证金(基于最新标记价格)
    //         "leverage": "100",  // 杠杆倍率
    //         "isolated": true,  // 是否是逐仓模式
    //         "entryPrice": "0.00000",  // 持仓成本价
    //         "maxNotional": "250000",  // 当前杠杆下用户可用的最大名义价值
    //         "bidNotional": "0",  // 买单净值，忽略
    //         "askNotional": "0",  // 卖单净值，忽略
    //         "positionSide": "BOTH",  // 持仓方向
    //         "positionAmt": "0",      // 持仓数量
    //         "updateTime": 0         // 更新时间 
    //     }
    // ]
    // {
    //   "symbol": "ETHUSDT", aa
    //   "initialMargin": "3.00753202",
    //   "maintMargin": "0.03007532",
    //   "unrealizedProfit": "0.01248595", aa
    //   "positionInitialMargin": "3.00753202",
    //   "openOrderInitialMargin": "0",
    //   "leverage": "2", aa
    //   "isolated": true,
    //   "entryPrice": "1205.51", aa
    //   "maxNotional": "150000000",
    //   "positionSide": "BOTH",
    //   "positionAmt": "-0.005",  aa
    //   "notional": "-6.01506404",
    //   "isolatedWallet": "3.01391398",
    //   "updateTime": 1656081270383,
    //   "bidNotional": "0",
    //   "askNotional": "0"
    // },    
    // {
    //   "symbol": "BTCUSDT",
    //   "initialMargin": "10.60005000",
    //   "maintMargin": "0.08480040",
    //   "unrealizedProfit": "-0.02580000",
    //   "positionInitialMargin": "10.60005000",
    //   "openOrderInitialMargin": "0",
    //   "leverage": "2",
    //   "isolated": true,
    //   "entryPrice": "21225.9",
    //   "maxNotional": "300000000",
    //   "positionSide": "BOTH",
    //   "positionAmt": "0.001",  aa
    //   "notional": "21.20010000",
    //   "isolatedWallet": "10.60786327",
    //   "updateTime": 1656081254656,
    //   "bidNotional": "0",
    //   "askNotional": "0"
    // },
    public class AccountInfo
    {
        // string rawStr = "";
        JObject rawJson;
        public Dictionary<string, JObject> positions;
        public Dictionary<string, float> positionsAmt;
        public JObject usdt;
        public float availableBalance;
        public float walletBalance;
        private static volatile AccountInfo instance;
        public static AccountInfo Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AccountInfo();
                }
                return instance;

            }
        }
        public void Init(string json)
        {
            // rawStr = json;
            rawJson = JObject.Parse(json);
            foreach (var item in rawJson["assets"])
            {
                if (item["asset"].ToString() == "USDT")
                {
                    usdt = (JObject)item;
                    availableBalance = (float)item["availableBalance"];
                    walletBalance = (float)item["walletBalance"];
                }
            }
            positions = new Dictionary<string, JObject>();
            positionsAmt = new Dictionary<string, float>();
            foreach (var item in rawJson["positions"])
            {
                if (Math.Abs((float)item["positionAmt"]) > 0.0001f)
                {
                    positions.TryAdd((string)item["symbol"], (JObject)item);
                    positionsAmt.TryAdd((string)item["symbol"], (float)item["positionAmt"]);
                }
            }
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