using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace GoPiao.Controllers
{
    public class GoPiaoController : Controller
    {
        // GET: GoPiao
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GoPiaoSearch()
        {
            return View();
        }

        public ActionResult GetRealtimePrice(string symbols1)
        {
            //GetRealtimePriceOut outModel = new GetRealtimePriceOut();
            //outModel.ErrorMsg = "";

            StringBuilder ExCode = new StringBuilder();
            string[] symbols = symbols1.Split(',');
            foreach (string symbol in symbols)
            {
                ExCode.Append("tse_" + symbol + ".tw|");
            }

            // 呼叫網址
            string url = "https://mis.twse.com.tw/stock/api/getStockInfo.jsp";
            url += "?json=1&delay=0&ex_ch=" + ExCode;

            string downloadedData = "";
            using (WebClient wClient = new WebClient())
            {
                // 取得網頁資料
                wClient.Encoding = Encoding.UTF8;
                downloadedData = wClient.DownloadString(url);
                downloadedData = downloadedData.Trim();
            }

            // 解析 JSON 数据
            JObject jsonObject = JObject.Parse(downloadedData);

            // 獲取 "msgArray" 数组
            JArray msgArray = (JArray)jsonObject["msgArray"];

            // 遍历数组并获取每个对象的 "c" 值
            string[,] dataArray = new string[msgArray.Count, 8];
            for (int i = 0; i < msgArray.Count; i++)
            {
                JObject item = (JObject)msgArray[i];

                // 获取 "c" 值(代碼)
                string cValue = (string)item["c"];
                dataArray[i, 0] = cValue;

                // 获取 "n" 值(公司名稱)
                string nValue = (string)item["n"];
                dataArray[i, 1] = nValue;

                // 获取 "o" 值(開盤)
                string oValue = (string)item["o"];
                dataArray[i, 2] = oValue;

                // 获取 "h" 值(最高價)
                string hValue = (string)item["h"];
                dataArray[i, 3] = hValue;

                // 获取 "l" 值(最低價)
                string lValue = (string)item["l"];
                dataArray[i, 4] = lValue;

                // 获取 "z" 值(收盤價)
                string zValue = (string)item["z"];
                dataArray[i, 5] = zValue;

                // 获取 "a" 值(最低委賣價)
                string aValue = (string)item["a"];
                //dataArray[i, 6] = aValue;
                if (aValue.IndexOf("_") > -1)
                {
                    dataArray[i, 6] = aValue.Split('_')[0];
                }

                // 获取 "b" 值(最高委買價)
                string bValue = (string)item["b"];
                //dataArray[i, 7] = bValue;
                if (bValue.IndexOf("_") > -1)
                {
                    dataArray[i, 7] = bValue.Split('_')[0];
                }
            }

            //// 輸出json
            ContentResult result = new ContentResult();
            result.ContentType = "application/json";
            result.Content = JsonConvert.SerializeObject(dataArray);
            return result;
        }

        public ActionResult GetMonthPrice(string symbols1, string startdate, string enddate)
        {
            //symbols1 股票代碼
            //startdate 搜尋年月(起日)
            //enddate 搜尋年月(迄日)


            // 解析起始和结束日期
            DateTime startDate = DateTime.ParseExact(startdate, "yyyy-MM-dd", null);
            DateTime endDate = DateTime.ParseExact(enddate, "yyyy-MM-dd", null);

            List<object> results = new List<object>();

            // 获取起始和结束日期的月份
            DateTime currentMonth = new DateTime(startDate.Year, startDate.Month, 1);
            DateTime endMonth = new DateTime(endDate.Year, endDate.Month, 1);

            // 循环处理每个月的数据，直到覆盖到结束月份
            while (currentMonth <= endMonth)
            {
                // 确保我们总是查询每个月的第一天
                string dateString = currentMonth.ToString("yyyyMMdd");

                // 构建请求URL
                // 格式https://www.twse.com.tw/exchangeReport/STOCK_DAY?date=20240501&stockNo=0050
                string url = $"http://www.twse.com.tw/exchangeReport/STOCK_DAY?date={dateString}&stockNo={symbols1}";

                // 发起HTTP请求并处理响应
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResult = response.Content.ReadAsStringAsync().Result;
                        var apiResult = JsonConvert.DeserializeObject<object>(jsonResult);
                        results.Add(apiResult);
                    }
                }

                // 将日期增加一个月
                currentMonth = currentMonth.AddMonths(1);
            }

            // 将每个 object 反序列化为 GetMonthPriceModel，并合并 data 部分
            var jsonDataList = results
                .Select(item => JsonConvert.DeserializeObject<GetMonthPriceModel>(JsonConvert.SerializeObject(item)))
                .ToList();

            // 合併當月所有 data
            List<List<string>> combinedData = new List<List<string>>();
            foreach (var jsonData in jsonDataList)
            {
                combinedData.AddRange(jsonData.data);
            }

            // 想要提取的日期(傳入2個西元日期，格式2024-05-01)
            string[] targetDates = GetDateRange(startDate, endDate);


            // 使用 LINQ 筛选数据
            var filteredData = combinedData.Where(row => targetDates.Contains(row[0])).ToList();

            // 返回合併後的 data 结果
            ContentResult result = new ContentResult
            {
                ContentType = "application/json",
                Content = JsonConvert.SerializeObject(filteredData)
            };

            return result;

        }

        // 想要提取的日期(將傳進來的2個日期的區間顯示出來)
        public static string[] GetDateRange(DateTime startDate, DateTime endDate)
        {
            List<string> dateList = new List<string>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                string minguoDate = (date.Year - 1911).ToString("000") + date.ToString("/MM/dd");
                dateList.Add(minguoDate);
            }

            return dateList.ToArray();
        }

        public class GetMonthPriceModel
        {
            public List<List<string>> data { get; set; }
        }
    }
}