using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;

namespace WeChatPay.Controllers
{
    public class PayHelperController : ApiController
    {
        [HttpGet]
        public Models.PayParms GetPayParm()
        {
            var prePayDic = new Dictionary<string, string>();
            string openid = System.Configuration.ConfigurationManager.AppSettings["openId"];
            prePayDic.Add("appid", "wx551c10a5e99a84e5");
            prePayDic.Add("body", "test");
            prePayDic.Add("mch_id", "10037082");
            prePayDic.Add("nonce_str", "1add1a30ac87aa2db72f57a2375d8fec");
            prePayDic.Add("notify_url", "http://www.baidu.com/");
            prePayDic.Add("out_trade_no", "15" + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0') + DateTime.Now.Hour.ToString().PadLeft(2, '0') + DateTime.Now.Minute.ToString().PadLeft(2, '0') + DateTime.Now.Second.ToString().PadLeft(2, '0'));
            prePayDic.Add("spbill_create_ip", "121.40.79.86");
            prePayDic.Add("total_fee", "1");
            prePayDic.Add("trade_type", "JSAPI");
            prePayDic.Add("openid", openid);

            //        <appid>wx551c10a5e99a84e5</appid>
            //<body>test</body>
            //<mch_id>10037082</mch_id>
            //<nonce_str>1add1a30ac87aa2db72f57a2375d8fec</nonce_str>
            //<notify_url>http://www.baidu.com/</notify_url>
            //<openid>oK7_ajtkA5qGiwrU2MNqa7vs3ttE</openid>
            //<out_trade_no>out_trade_no</out_trade_no>
            //<spbill_create_ip>121.40.79.86</spbill_create_ip>
            //<total_fee>1</total_fee>
            //<trade_type>JSAPI</trade_type>
            //<sign>00492A884A794C6B3892D4406BA6DD8A</sign>
            string sign = Helper.GetMD5(prePayDic);
            prePayDic.Add("sign", sign);
            string resStr = Helper.GerResponse(Helper.GetReqStr(prePayDic), "https://api.mch.weixin.qq.com/pay/unifiedorder");
            int startIndex = resStr.IndexOf("<prepay_id><![CDATA[") + "<prepay_id><![CDATA[".Length;
            int endIndex = resStr.IndexOf("]]></prepay_id>");

            string prepayId = resStr.Substring(startIndex, endIndex - startIndex);

            Models.PayParms pp = new Models.PayParms()
            {
                timeStamp = Helper.getTimestamp()
            };
            pp.SetPackage(prepayId);

            Helper.GetMD5(ref pp);

            var db = new Models.ModelContext();
            var user = db.UserSet.FirstOrDefault(u => u.OpenId == openid);
            if (user != null)
            {
                user.Balance += 0.01;
                db.SaveChanges();
            }
            return pp;
        }

    }

    public class Helper
    {
        public static string GetMD5(ref Models.PayParms pp)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();

            dic.Add("appId", pp.appId);
            dic.Add("timeStamp", pp.timeStamp);
            dic.Add("package", pp.package);
            dic.Add("signType", pp.signType);
            dic.Add("nonceStr", pp.nonceStr);
            pp.paySign = GetMD5(dic);

            return pp.paySign;
        }

        public static string GetMD5(Dictionary<string, string> dic)
        {
            var enStr = "";
            ArrayList al = new ArrayList(dic.Keys);
            al.Sort();
            foreach (string item in al)
            {
                enStr += item + "=" + dic[item] + "&";
            }
            enStr += "key=06ee7637f16b52183a55ad7083e24390";
            return GetMD5(enStr);

        }

        public static string GetMD5(string encypStr)
        {
            string retStr;
            MD5CryptoServiceProvider m5 = new MD5CryptoServiceProvider();

            //创建md5对象
            byte[] inputBye;
            byte[] outputBye;

            //使用GB2312编码方式把字符串转化为字节数组．
            inputBye = Encoding.GetEncoding("GB2312").GetBytes(encypStr);

            outputBye = m5.ComputeHash(inputBye);

            retStr = System.BitConverter.ToString(outputBye);
            retStr = retStr.Replace("-", "").ToUpper();
            return retStr;
        }

        public static string getTimestamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        public static string GetReqStr(Dictionary<string, string> dic)
        {
            var retStr = "<xml>";
            foreach (var key in dic.Keys)
            {
                retStr += string.Format("<{0}>{1}</{0}>", key, dic[key]);
            }

            retStr += "</xml>";
            return retStr;
        }

        public static string GerResponse(string data, string url)
        {
            HttpWebRequest myHttpWebRequest = null;
            string strReturnCode = string.Empty;
            //ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckValidationResult);
            myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            myHttpWebRequest.ProtocolVersion = HttpVersion.Version10;

            byte[] bs;

            myHttpWebRequest.Method = "POST";
            myHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
            bs = Encoding.UTF8.GetBytes(data);

            myHttpWebRequest.ContentLength = bs.Length;

            using (Stream reqStream = myHttpWebRequest.GetRequestStream())
            {
                reqStream.Write(bs, 0, bs.Length);
            }


            using (WebResponse myWebResponse = myHttpWebRequest.GetResponse())
            {
                StreamReader readStream = new StreamReader(myWebResponse.GetResponseStream(), Encoding.UTF8);
                strReturnCode = readStream.ReadToEnd();
            }

            return strReturnCode;

        }
    }
}
