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

    public class MyOrderMgr
    {

        public static async Task StartRobot()
        {

            // 补齐到最新
            foreach (var symbol in MyTest.symbolAll)
            {
                await FetchLatestData(symbol);
            }
            foreach (var symbol in MyTest.symbolAll)
            {
                MyTools.LoadFileData(symbol);
            }
            while (true)
            {

                // MyTools.LogMsg(DateTime.Now.ToString());
                // await MyTools.KlineCandlestickData_Response(0, "BTCUSDT_F", "kline");

                // await MyTools.RequestData("BTCUSDT_F", "kline");
                Thread.Sleep(1000);
            }
            // Console.Read();
        }
        public static async Task FetchLatestData(string symbol)
        {
            StreamReader sr = new StreamReader(MyTools.dataDir + symbol + ".txt");
            // var sList = new KlineList();
            //判断文件中是否有字符
            var firstLine = sr.ReadLine();
            if (firstLine != null)
            {
                // [1655775600000,
                var startTs = Int64.Parse(firstLine.Substring(1, 13));
                var current = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var perRequestTime = 500 * 5 * 60 * 1000;
                var endTs = Math.Min(startTs + perRequestTime, current);
                while (startTs < endTs)
                {

                    var str = await MyTools.FetchMarketData(startTs, symbol, "kline", 500);
                    startTs += perRequestTime;
                }

                // var obj = new JsonKline(sr.ReadLine());
                // var myobj = new MyKline(obj);
                // sList.myKlines.Add(myobj);
            }
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