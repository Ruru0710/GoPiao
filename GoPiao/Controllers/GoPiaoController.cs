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
	}
}