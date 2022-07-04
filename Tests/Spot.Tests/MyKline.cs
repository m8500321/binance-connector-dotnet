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


    [Serializable]
    public class MyKline
    {
        public MyKline(JsonKline jk)
        {
            openTime = jk.openTime;
            openPrice = jk.openPrice;
            // maxPrice = jk.maxPrice;
            // minPrice = jk.minPrice;
            closePrice = jk.closePrice;
            volume = jk.volume;
            // closeTime = jk.closeTime;
            volumePrice = jk.volumePrice;
            // volumeCount = jk.volumeCount;
            // activeVolume = jk.activeVolume;
            // activeVolumePrice = jk.activeVolumePrice;

            perVolumePrice = volumePrice / volume;
            incPercent = (closePrice - openPrice) / openPrice;
            // maxIncPercent = (maxPrice - openPrice) / openPrice;
            // maxDescPercent = (minPrice - openPrice) / openPrice;
            // avePrice = volumePrice / volume;
            // volumePerHand = volumePrice / volumeCount;
            isUp = closePrice > openPrice ? true : false;

        }
        public long openTime; // 开盘时间
        public float openPrice; // 开盘价
        // public float maxPrice; // 最高价
        // public float minPrice; // 最低价
        public float closePrice; // 收盘价
        public float volume; // 成交量，手
        // public long closeTime; // 收盘时间
        public float volumePrice; // 成交额，价值
        public float perVolumePrice; // 单价
        // public int volumeCount; // 成交笔数
        // public float activeVolume; // 主动成交量
        // public float activeVolumePrice; // 主动成交额

        public float incPercent = 0f; // 涨幅
        // public float maxIncPercent = 0f; // 最大涨幅
        // public float maxDescPercent = 0f; // 最大跌幅
        // public float avePrice = 0f; // 平均价
        // public float volumePerHand = 0f; // 交易额/手
        public bool isUp = true; // 是否上涨

        // 1 2 4 8 16 32 64 128 256,9条
        // public float[] prevVolumePriceList = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // 前面平均单位量的价格
        // public float[] prev100VolumePriceList = new float[10]; // 往前100条的平均价格
        [NonSerialized]
        public float[] prevClosePriceList = new float[10]; // 平均收盘价，不包含自己
        // public float[] sumIncList = new float[10] {0f }; // 累积涨幅，包含自己
        public float[] equalVolumeList = new float[10]; // 等差数列，交易量均值，包含自己
        [NonSerialized]
        public float[] equalAveList = new float[10]; // 等差数列，开收盘均值，包含自己
        // 5,10,20,40；包含自己
        // public List<float> prevAveVolumeList = new List<float>() { 0, 0, 0, 0 }; // 前几次平均成交量
        // public float[] prevAvePriceList = new float[] { 0, 0, 0, 0 }; // 前几次平均时间单价
        // public float[] prevSingleVolumePriceList = new float[] { 0, 0, 0, 0 }; // 前面单条单位量的价格
        // public float[] prevVolumePriceList = new float[] { 0, 0, 0, 0 }; // 前面平均单位量的价格
        // public float[] prevVolumePriceRate = new float[] { 0, 0, 0, 0 }; // 当前对比均值的涨幅
        // public float[] prevClosePriceRate = new float[] { 0, 0, 0, 0 }; // 当前对比收盘价的涨幅
        // public float[] nextSumIncList = new float[] { 0, 0, 0, 0 }; // 后续上涨和，不包含自己


        // public float[] prevAveCloseList = new float[] { 0, 0, 0, 0 }; // 前几次平均收盘价
        // public List<float> prevMaxList = new List<float>() { 0, 0, 0, 0 }; // 前几次最高价
        // public List<float> prevMinList = new List<float>() { 999999, 999999, 999999, 999999 }; // 前几次最低价
        // public List<float> prevMaxList = new List<float>() { 0, 0, 0, 0 }; // 前几次最高价
        // public List<float> prevMinList = new List<float>() { 999999, 999999, 999999, 999999 }; // 前几次最低价
    }

    [Serializable]
    public class KlineList
    {
        public List<MyKline> myKlines = new List<MyKline>();
        public float FollowingInc(int idx, int len)
        {
            var sum = 0f;
            for (int i = idx; i < idx + len; i++)
            {
                sum += myKlines[idx].incPercent;
            }
            return sum;
        }

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