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
        static public ILogger logger;
        private static readonly object LOCK = new object();
        static private string dataDir = System.Environment.CurrentDirectory.Replace("\\", "/").Replace("Tests/Spot.Tests", "datas/");
        /// </summary>
        static private DateTime UTC_START = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        static readonly int HOOUR8_MS = 8 * 60 * 60 * 1000;
        static public long MsFromUTC0(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - UTC_START).TotalMilliseconds;
        }
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
                using (StreamWriter sw = new StreamWriter(dataDir + "log.txt", true))
                {
                    sw.Write("\n\n" + DateTime.Now + "\n" + output);
                }
            }

        }


        static public async Task KlineCandlestickData_Response(long startTime, string symbol)
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

        static public async Task FetchKlineData(string symbol)
        {
            // request klines
            File.Delete(dataDir + symbol + ".txt"); //删除指定文件;
            long startMS = MsFromUTC0(DateTime.UtcNow);
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

        static public void Data2Serializable(string symbol)
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
            sList.myKlines.Reverse();
            for (int i = 40; i < sList.myKlines.Count; i++)
            {
                var itemI = sList.myKlines[i];
                var sumClose = 0f;
                var sumPrice = 0f;
                var sumVolume = 0f;
                for (int j = 1; j <= 40; j++)
                {
                    var itemJ = sList.myKlines[i - j + 1];
                    sumClose += itemJ.closePrice;
                    sumPrice += itemJ.volumePrice;
                    sumVolume += itemJ.volume;
                    var idx = -1;
                    if (j == 5)
                    {
                        idx = 0;
                    }
                    else if (j == 10)
                    {
                        idx = 1;

                    }
                    else if (j == 20)
                    {
                        idx = 2;

                    }
                    else if (j == 40)
                    {
                        idx = 3;

                    }
                    if (idx > -1)
                    {
                        itemI.prevAveVolumeList[idx] = sumVolume / j;
                        itemI.prevAvePriceList[idx] = sumPrice / sumVolume;
                        itemI.prevMaxList[idx] = Math.Max(itemI.prevMaxList[idx], itemJ.maxPrice);
                        itemI.prevMinList[idx] = Math.Min(itemI.prevMinList[idx], itemJ.minPrice);
                        itemI.prevAveCloseList[idx] = sumClose / j;
                    }
                }
            }
            bf.Serialize(fileStream, sList);
            fileStream.Flush();
            fileStream.Close();
            sr.Close();
            // sw.Close();
        }

        static public KlineList Serializable2Data(string symbol)
        {
            FileStream fileStream = new FileStream(dataDir + symbol + ".data", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            BinaryFormatter bf = new BinaryFormatter();
            var kList = bf.Deserialize(fileStream) as KlineList;
            fileStream.Flush();
            fileStream.Close();
            return kList;
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
            var rate1 = DiffRate(a1, a2);
            var rate2 = DiffRate(b1, b2);
            return SimilarRate(rate1, rate2, percent, val, extraRange);
        }

        static public float SimilarRate(float f1, float f2, float percent = 0.1f, float val = 0.001f, float extraRange = 1f)
        {
            var maxDiff = Math.Abs(f1) * percent + val;
            var curDiff = Math.Abs(f1 - f2);
            var s = 1 - curDiff / maxDiff;
            // s = (float)((s > 0 ? 1 : -1) * s * s);
            if (s < -0f)
            {
                return -99999f;
            }
            return s;
        }

    }
}