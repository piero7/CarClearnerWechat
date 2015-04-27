using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstPencil.Helper
{
    class WechatHelper
    {
        /// <summary>  
        /// datetime转换为unixtime  
        /// </summary>  
        /// <param name="time"></param>  
        /// <returns></returns>  
        public static int ConvertDateTimeInt(System.DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            return (int)(time - startTime).TotalSeconds;
        }

        public static string GetEventString(string toUser, string fromUser, string key)
        {
            var db = new Models.ModelContext();
            var code = db.TwoDCodeSet.Include("Event").FirstOrDefault(c => c.EventNumber == key );
            if (code != null)
            {
                //关注事件
                if (code.Event.EeventType == Models.WechatEventType.Subscribe)
                {
                    var msgList = db.ImgMsgListSet.FirstOrDefault(ml => ml.ImgMsgListId == code.Event.ImgMsgListId);
                    if (msgList != null)
                    {
                        return msgList.GetWechatString(toUser, fromUser);
                    }
                    else return "";
                }
                else if (code.Event.EeventType == Models.WechatEventType.BeSalesman)
                {
                    var salesman = db.SalesmanSet.FirstOrDefault(s => s.EventId == code.EventId);
                    if (salesman != null)
                    {
                        var user = db.UserSet.FirstOrDefault(u => u.OpenId == toUser);
                        if (user != null)
                        {
                            salesman.UserId = user.UserId;
                            db.SaveChanges();
                            return GetTextMsg("注册经销商成功！", toUser, fromUser);
                        }
                        else return "";
                    }
                    else return "";
                }
                else return "";
            }
            else return "";
        }

        public static string GetTextMsg(string content, string toUser, string fromUser)
        {

            string now = "";
            now = "<xml><ToUserName><![CDATA[" + toUser + "]]></ToUserName><FromUserName><![CDATA[" + fromUser + "]]></FromUserName><CreateTime>" + ConvertDateTimeInt(DateTime.Now) +
                                "</CreateTime><MsgType><![CDATA[text]]></MsgType><Content><![CDATA[" +
                                content +
                                "]]></Content><FuncFlag>0</FuncFlag></xml>";
            return now;

        }
    }

    public class Location
    {
        /// <summary>
        /// 纬度
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// 经度
        /// </summary>
        public double Y { get; set; }

        //计算距离
        private const double EARTH_RADIUS = 6378.137;//地球半径
        //private static double rad(double d)
        //{
        //    return d * Math.PI / 180.0;
        //}

        public static double GetDistance(Location a, Location b)
        {
            double radLat1 = (a.X * Math.PI / 180.0);
            double radLat2 = (b.X * Math.PI / 180.0);
            double x = radLat1 - radLat2;
            double y = (a.Y - b.Y) * Math.PI / 180.0;

            double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(x / 2), 2) +
                       Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(y / 2), 2)));
            s = s * EARTH_RADIUS;
            s = Math.Round(s * 10000) / 10000;
            return s;

        }
    }
}


