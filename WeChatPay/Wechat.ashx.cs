using FirstPencil.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Xml;
using WeChatPay.Models;

namespace FirstPencil
{
    /// <summary>
    /// Wechat 的摘要说明
    /// </summary>
    public class Wechat : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            if (HttpContext.Current.Request.HttpMethod.ToLower() == "post")
            {
                var db = new WeChatPay.Models.ModelContext();
                string ret = "";
                Stream s = System.Web.HttpContext.Current.Request.InputStream;
                byte[] b = new byte[s.Length];
                s.Read(b, 0, (int)s.Length);
                var postStr = Encoding.UTF8.GetString(b);

                WriteTxt(postStr);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(postStr);
                XmlElement rootElement = doc.DocumentElement;

                XmlNode MsgType = rootElement.SelectSingleNode("MsgType");

                RequestXML requestXML = new RequestXML();
                requestXML.ToUserName = rootElement.SelectSingleNode("ToUserName").InnerText;
                requestXML.FromUserName = rootElement.SelectSingleNode("FromUserName").InnerText;
                requestXML.CreateTime = rootElement.SelectSingleNode("CreateTime").InnerText;
                requestXML.MsgType = MsgType.InnerText;


                if (requestXML.MsgType == "event")
                {
                    requestXML.Event = rootElement.SelectSingleNode("Event").InnerText;
                    var node = rootElement.SelectSingleNode("EventKey");
                    requestXML.EventKey = node == null ? "" : node.InnerText;
                    if (requestXML.Event.ToLower() == "subscribe" || requestXML.Event.ToLower() == "scan")
                    {
                        //查找user 没找到就插入
                        User user = db.UserSet.FirstOrDefault(u => u.OpenId == requestXML.FromUserName);
                        if (user == null)
                        {
                            user = new WeChatPay.Models.User
                            {
                                OpenId = requestXML.FromUserName,
                            };
                            db.UserSet.Add(user);
                        }
                        #region 博博会绑定标签
                        //根据事件号寻找标签
                        //var card = db.Cards.Include("Code").FirstOrDefault(c => "qrscene_" + c.Code.EventNumber ==
                        //                  (requestXML.Event == "subscribe" ? requestXML.EventKey : "qrscene_" + requestXML.EventKey));
                        //if (card != null)
                        //{
                        //    card.IsNeedSkip = false;
                        //    var userList = db.Users.Where(u => u.CardId == card.CardId);
                        //    foreach (var item in userList)
                        //    {
                        //        item.CardId = null;
                        //    }

                        //    //修改已经存在的user信息
                        //    user.CardId = card.CardId;
                        //}
                        //else
                        //{
                        //    if (string.IsNullOrEmpty(requestXML.EventKey))
                        //    {
                        //        user.Remarks = "Without any event key!";
                        //    }
                        //    else
                        //    {
                        //        user.Remarks = "With undefined event key " + requestXML.EventKey + "!";
                        //    }
                        //}

                        //if (user.WatchTime == null)
                        //{
                        //    user.WatchTime = new int?(0);
                        //}
                        //user.SubscribeTime = DateTime.Now;
                        //if (!string.IsNullOrEmpty(user.OpenId))
                        //{
                        //    Helper.UserHelper.GetUserInfo(user);
                        //}
                        #endregion

                        db.SaveChanges();

                        if (!string.IsNullOrEmpty(requestXML.EventKey))
                        {
                            requestXML.EventKey = requestXML.EventKey.Replace("qrscene_", "");
                            ret = Helper.WechatHelper.GetEventString(requestXML.FromUserName, requestXML.ToUserName, requestXML.EventKey);
                        }
                        else
                        {
                            ret = checkXML(requestXML, "欢迎使用爱点车联。");
                        }

                        //else
                        //{
                        //    ret = checkXML(requestXML, "事件编号：" + requestXML.EventKey);
                        //}
                    }
                 
                }
                HttpContext.Current.Response.Write(ret);
                HttpContext.Current.Response.End();

            }
            else
            {
                string echoString = HttpContext.Current.Request.QueryString["echostr"];
                string signature = HttpContext.Current.Request.QueryString["signature"];
                string timestamp = HttpContext.Current.Request.QueryString["timestamp"];
                string nonce = HttpContext.Current.Request.QueryString["nonce"];

                if (!string.IsNullOrEmpty(echoString))
                {

                    HttpContext.Current.Response.Write(echoString);
                    HttpContext.Current.Response.End();

                }
            }

        }
        /// <summary>
        /// 整理文字消息格式
        /// </summary>
        /// <param name="requestXML"></param>
        /// <param name="mesg"></param>
        /// <returns></returns>
        private string checkXML(RequestXML requestXML, string mesg)
        {
            string now = "";
            now = "<xml><ToUserName><![CDATA[" + requestXML.FromUserName + "]]></ToUserName><FromUserName><![CDATA[" + requestXML.ToUserName + "]]></FromUserName><CreateTime>" + ConvertDateTimeInt(DateTime.Now) +
                                "</CreateTime><MsgType><![CDATA[text]]></MsgType><Content><![CDATA[" +
                                mesg +
                                "]]></Content><FuncFlag>0</FuncFlag></xml>";
            return now;
        }

        /// <summary>  
        /// datetime转换为unixtime  
        /// </summary>  
        /// <param name="time"></param>  
        /// <returns></returns>  
        private int ConvertDateTimeInt(System.DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            return (int)(time - startTime).TotalSeconds;
        }



        /// <summary>
        /// 1. 将token、timestamp、nonce三个参数进行字典序排序
        /// 2. 将三个参数字符串拼接成一个字符串进行sha1加密
        /// 3. 开发者获得加密后的字符串可与signature对比，标识该请求来源于微信
        /// </summary>
        /// <param name="firmToken">厂家Token</param>
        /// <param name="signature">密文</param>
        /// <param name="timestamp">时间戳</param>
        /// <param name="nonce">随机数</param>
        /// <returns></returns>
        private bool Verity(string firmToken, string signature, string timestamp, string nonce)
        {

            string[] tArr = new string[] { firmToken, timestamp, nonce };
            Array.Sort(tArr);
            string tString = string.Join("", tArr);
            string res = FormsAuthentication.HashPasswordForStoringInConfigFile(tString, "SHA1").ToLower();
            return signature == res;
        }

        /// <summary>  
        /// 记录bug，以便调试  
        /// </summary>  
        /// <returns></returns>  
        private bool WriteTxt(string str)
        {
            try
            {
                FileStream fs = new FileStream(HttpContext.Current.Server.MapPath("/bugLog.txt"), FileMode.Append);
                StreamWriter sw = new StreamWriter(fs);
                //开始写入  
                sw.WriteLine(str + "\r\n" + DateTime.Now, ToString() + "\r\n" + "\r\n");
                //清空缓冲区  
                sw.Flush();
                //关闭流  
                sw.Close();
                fs.Close();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }


        public bool IsReusable
        {
            get { throw new NotImplementedException(); }
        }
    }


    public class RequestXML
    {

        public string ToUserName;
        public string FromUserName;
        public string CreateTime;
        public string MsgType;
        public string Location_X;
        public string Location_Y;
        public string Scale;
        public string Label;
        public string Content;
        public string PicUrl;
        public string EventKey;
        public string Event;
        public string Precision;
    }

}