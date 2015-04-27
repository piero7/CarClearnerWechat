using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WeChatPay.Models
{
    public class PayParms
    {
        public string appId = "wx551c10a5e99a84e5";

        public string timeStamp { get; set; }

        public string nonceStr = "e61463f8efa94090b1f366cccfbbb444";

        public string signType = "MD5";

        public string package { get; set; }

        public string paySign { get; set; }

        public void SetPackage(string prePayId)
        {
            this.package = "prepay_id=" + prePayId;
        }

        //appId" : "wx551c10a5e99a84e5",  //公众号名称，由商户传入 
        //     "timeStamp":"1395712654",         //时间戳，自1970 年以来的秒数 
        //     "nonceStr" : "e61463f8efa94090b1f366cccfbbb444", //随机串 
        //     "package" : "prepay_id=wx201504261343452e533e39770006103237", 
        //     "signType" : "MD5",        //微信签名方式: 
        //     "paySign"  : "895F0263835551777F3E5B1EE1C14C25"
    }
}