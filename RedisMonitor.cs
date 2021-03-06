﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiShi_WinForm
{
    public partial class RedisMonitor : Form
    {

        bool working = false;
        string UserName = ConfigurationManager.AppSettings["FTPUserName"];
        string Password = ConfigurationManager.AppSettings["FTPPassword"];
        System.Uri Uri = new Uri("ftp://" + ConfigurationManager.AppSettings["FTPServer"] + ":" + ConfigurationManager.AppSettings["FTPPortNO"]);
        IDatabase db = SeRedis.redis.GetDatabase();
        public RedisMonitor()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            this.timer1.Enabled = true;
            this.button1.Text = "运行中";
            this.button1.Enabled = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!working)
            {
                working = true;
                InsertDecl();
                InsertDeclList();
                working = false;
            }
        }
        private void InsertDeclList()
        {
            string json_decllist = db.ListLeftPop("redis_declarelist");
            if (!string.IsNullOrEmpty(json_decllist))
            {
                JObject jo_decllist = (JObject)JsonConvert.DeserializeObject(json_decllist);
                //添加报关单明细
                try
                {
                    string sql = @"insert into list_decllist(id
                        , PREDECLID, ORDERNO, ITEMNO, COMMODITYNO, ADDITIONALNO, COMMODITYNAME
                        , SPECIFICATIONSMODEL, LEGALQUANTITY, LEGALUNIT, SQUANTITY, SUNIT
                        , CADQUANTITY, CADUNIT, UNITPRICE, TOTALPRICE, CURRENCY
                        , CURRENCYCODE, TAXPAID, VERSIONNO, ARTNO, PROCESSINGFEES
                        , GOODSNW, LICENSENO, ISOUEQUIPMENT, ISWASTEMATERIALS, ISFORCELAWCONDITION

                        , PURPOSE, CUSTOMREGULATORY, INSPECTIONREGULATORY, CIQCODE, CREATEBY
                        , CREATEDATE, ISINVALID, PREDECLCODE, COUNTRYORIGIN, COUNTRYORIGINCODE
                        , UNITYCODE, NEWNO, ISSPECIAL, DESTCOUNTRYCODE, DESTCOUNTRYNAME
                        , INOUTDATE
                        ) values(list_decllist_id.nextval
                        ,'{0}','{1}','{2}','{3}','{4}','{5}'
                        ,'{6}','{7}','{8}','{9}','{10}'
                        ,'{11}','{12}','{13}','{14}','{15}'
                        ,'{16}','{17}','{18}','{19}','{20}'
                        ,'{21}','{22}','{23}','{24}','{25}'

                        ,'{26}','{27}','{28}','{29}','{30}'
                        ,{31},'{32}','{33}','{34}','{35}'
                        ,'{36}','{37}','{38}','{39}','{40}' 
                        ,{41}  
                        )";
                    sql = string.Format(sql
                        , jo_decllist.Value<string>("PREDECLID"), jo_decllist.Value<string>("ORDERNO"), jo_decllist.Value<string>("ITEMNO"), jo_decllist.Value<string>("COMMODITYNO"), jo_decllist.Value<string>("ADDITIONALNO"), jo_decllist.Value<string>("COMMODITYNAME")
                                , jo_decllist.Value<string>("SPECIFICATIONSMODEL"), jo_decllist.Value<string>("LEGALQUANTITY"), jo_decllist.Value<string>("LEGALUNIT"), jo_decllist.Value<string>("SQUANTITY"), jo_decllist.Value<string>("SUNIT")
                                , jo_decllist.Value<string>("CADQUANTITY"), jo_decllist.Value<string>("CADUNIT"), jo_decllist.Value<string>("UNITPRICE"), jo_decllist.Value<string>("TOTALPRICE"), jo_decllist.Value<string>("CURRENCY")
                                , jo_decllist.Value<string>("CURRENCYCODE"), jo_decllist.Value<string>("TAXPAID"), jo_decllist.Value<string>("VERSIONNO"), jo_decllist.Value<string>("ARTNO"), jo_decllist.Value<string>("PROCESSINGFEES")
                                , jo_decllist.Value<string>("GOODSNW"), jo_decllist.Value<string>("LICENSENO"), jo_decllist.Value<string>("ISOUEQUIPMENT"), jo_decllist.Value<string>("ISWASTEMATERIALS"), jo_decllist.Value<string>("ISFORCELAWCONDITION")

                                , jo_decllist.Value<string>("PURPOSE"), jo_decllist.Value<string>("CUSTOMREGULATORY"), jo_decllist.Value<string>("INSPECTIONREGULATORY"), jo_decllist.Value<string>("CIQCODE"), jo_decllist.Value<string>("CREATEBY")
                                , "to_date('" + jo_decllist.Value<string>("CREATEDATE") + "','yyyy-MM-dd HH24:mi:ss')", jo_decllist.Value<string>("ISINVALID"), jo_decllist.Value<string>("PREDECLCODE"), jo_decllist.Value<string>("COUNTRYORIGIN"), jo_decllist.Value<string>("COUNTRYORIGINCODE")
                                , jo_decllist.Value<string>("UNITYCODE"), jo_decllist.Value<string>("NEWNO"), jo_decllist.Value<string>("ISSPECIAL"), jo_decllist.Value<string>("DESTCOUNTRYCODE"), jo_decllist.Value<string>("DESTCOUNTRYNAME")
                                , "to_date('" + jo_decllist.Value<string>("INOUTDATE") + "','yyyy-MM-dd HH24:mi:ss')"
                        );
                    DBMgr.ExecuteNonQuery(sql);
                }
                catch (Exception ex)
                {
                    db.ListRightPush("redis_declarelist", json_decllist);
                    this.button1.Text = ex.Message;
                }
            }
        }
        private void InsertDecl()
        {
            string json_decl = db.ListLeftPop("Redis_Declare");
            if (!string.IsNullOrEmpty(json_decl))
            {
                JObject jo_decl = (JObject)JsonConvert.DeserializeObject(json_decl);
                try
                {
                    if (JudgeIsJiShiOrder(jo_decl))
                    {
                        string sql = "delete from list_declaration where code='" + jo_decl.Value<string>("CODE") + "'"; //先删除已经存在的预制报关单号
                        DBMgr.ExecuteNonQuery(sql);
                        sql = "delete from list_decllist where PREDECLCODE='" + jo_decl.Value<string>("CODE") + "'";//删除报关单明细
                        DBMgr.ExecuteNonQuery(sql);
                        //添加到报关单表
                        sql = @"insert into list_declaration(id
                                , code, codetype, ordercode, declarationcode, unitycode, currentcode, customareacode, declway, decltype, portcode, contractno                                
                                , recordcode, channel, conshippercode , conshippername, busiunitcode, busiunitname, repunitcode, repunitname, transmodel, transname                                
                                , voyageno, blno, exemptioncode, tradecode, tradecountrycode, secountrycode, seportcode , seplacecode, goodsnum, packagecode                                
                                , licenseno, goodsgw, goodsnw, tradetermscode, fgcode, freight, fgunitcode, ipcode, insurancepremium, ipunitcode
                                , aecode, additionalexpenses, aeunitcode, specialrelation, priceimpact, paypoyalties, taxrate, taxunitcode, taxunitname, listinfo

                                , remark, isinvalid, ispause, moedit, coedit, mostarttime, moendtime, mostartid, mostartname, moendid
                                , moendname, costarttime, coendtime, costartid, costartname, coendid, coendname, prestarttime, preendtime, prestartid                                
                                , prestartname, preendid, preendname, ckfinishtime, ckid , ckname, repstarttime, repid, repname, relatedtime
                                , relateduserid, relatedusername, rependtime, rependid, rependname, repovertime, repoverid, repovername, conshippernum, busiunitnum                                
                                , repunitnum, isneedclearance, ishaveclearance, isforcelaw, issplit, warehouseno, yardcode, status, sheetnum, presheetnum      
                          
                                , commoditynum, isaccept, modifyflag, checkflag, preedit, wpid, wpname, wptime, cusno, dataconfirm                                
                                , relatedflag, repoverflag, preacctime, preaccuserid, preaccusername, repwayid, customsstatus, prependtime, prependuserid, prependusername                                
                                , totalnw, totalmoney, totalnum, isprint, printtime, printnum, preedituserid, preeditusername, preedittime, listtype                                
                                , formatauto, busitype, associatepedeclno, associatedeclno, declcodesource, declremark, pauseuserid, pauseusername, specialdecl, dataconfirmuserid                                
                                , dataconfirmusername, dataconfirmusertime, mocurrentid, cocurrentid
                                )
                        values (list_declaration_id.nextval
                                ,'{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}'
                                ,'{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}'
                                ,'{21}','{22}','{23}','{24}','{25}','{26}','{27}','{28}','{29}','{30}'
                                ,'{31}','{32}','{33}','{34}','{35}','{36}','{37}','{38}','{39}','{40}'
                                ,'{41}','{42}','{43}','{44}','{45}','{46}','{47}','{48}','{49}','{50}'

                                ,'{51}','{52}','{53}','{54}','{55}',{56},{57},'{58}','{59}','{60}'
                                ,'{61}',{62},{63},'{64}','{65}','{66}','{67}',{68},{69},'{70}'
                                ,'{71}','{72}','{73}',{74},'{75}','{76}',{77},'{78}','{79}',{80}
                                ,'{81}','{82}',{83},'{84}','{85}',{86},'{87}','{88}','{89}','{90}'
                                ,'{91}','{92}','{93}','{94}','{95}','{96}','{97}','{98}','{99}','{100}'

                                ,'{101}','{102}','{103}','{104}','{105}','{106}','{107}',{108},'{109}','{110}'
                                ,'{111}','{112}',{113},'{114}','{115}','{116}','{117}',{118},'{119}','{120}'
                                ,'{121}','{122}','{123}','{124}',{125},'{126}','{127}','{128}',{129},'{130}'
                                ,'{131}','{132}','{133}','{134}','{135}','{136}','{137}','{138}','{139}','{140}'
                                ,'{141}',{142},'{143}','{144}'
                                )";
                        sql = string.Format(sql
                         , jo_decl.Value<string>("CODE"), jo_decl.Value<string>("CODETYPE"), jo_decl.Value<string>("ORDERCODE"), jo_decl.Value<string>("DECLARATIONCODE"), jo_decl.Value<string>("UNITYCODE"), jo_decl.Value<string>("CURRENTCODE"), jo_decl.Value<string>("CUSTOMAREACODE"), jo_decl.Value<string>("DECLWAY"), jo_decl.Value<string>("DECLTYPE"), jo_decl.Value<string>("PORTCODE"), jo_decl.Value<string>("CONTRACTNO")
                                     , jo_decl.Value<string>("RECORDCODE"), jo_decl.Value<string>("CHANNEL"), jo_decl.Value<string>("CONSHIPPERCODE"), jo_decl.Value<string>("CONSHIPPERNAME"), jo_decl.Value<string>("BUSIUNITCODE"), jo_decl.Value<string>("BUSIUNITNAME"), jo_decl.Value<string>("REPUNITCODE"), jo_decl.Value<string>("REPUNITNAME"), jo_decl.Value<string>("TRANSMODEL"), jo_decl.Value<string>("TRANSNAME ")
                                     , jo_decl.Value<string>("VOYAGENO"), jo_decl.Value<string>("BLNO"), jo_decl.Value<string>("EXEMPTIONCODE"), jo_decl.Value<string>("TRADECODE"), jo_decl.Value<string>("TRADECOUNTRYCODE"), jo_decl.Value<string>("SECOUNTRYCODE"), jo_decl.Value<string>("SEPORTCODE"), jo_decl.Value<string>("SEPLACECODE"), jo_decl.Value<string>("GOODSNUM"), jo_decl.Value<string>("PACKAGECODE")
                                     , jo_decl.Value<string>("LICENSENO"), jo_decl.Value<string>("GOODSGW"), jo_decl.Value<string>("GOODSNW"), jo_decl.Value<string>("TRADETERMSCODE"), jo_decl.Value<string>("FGCODE"), jo_decl.Value<string>("FREIGHT"), jo_decl.Value<string>("FGUNITCODE"), jo_decl.Value<string>("IPCODE"), jo_decl.Value<string>("INSURANCEPREMIUM"), jo_decl.Value<string>("IPUNITCODE")
                                     , jo_decl.Value<string>("AECODE"), jo_decl.Value<string>("ADDITIONALEXPENSES"), jo_decl.Value<string>("AEUNITCODE"), jo_decl.Value<string>("SPECIALRELATION"), jo_decl.Value<string>("PRICEIMPACT"), jo_decl.Value<string>("PAYPOYALTIES"), jo_decl.Value<string>("TAXRATE"), jo_decl.Value<string>("TAXUNITCODE"), jo_decl.Value<string>("TAXUNITNAME"), jo_decl.Value<string>("LISTINFO")

                                     , jo_decl.Value<string>("REMARK"), jo_decl.Value<string>("ISINVALID"), jo_decl.Value<string>("ISPAUSE"), jo_decl.Value<string>("MOEDIT"), jo_decl.Value<string>("COEDIT"), "TO_DATE('" + jo_decl.Value<string>("MOSTARTTIME") + "','yyyy-MM-dd HH24:mi:ss')", "TO_DATE('" + jo_decl.Value<string>("MOENDTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("MOSTARTID"), jo_decl.Value<string>("MOSTARTNAME"), jo_decl.Value<string>("MOENDID")
                                     , jo_decl.Value<string>("MOENDNAME"), "TO_DATE('" + jo_decl.Value<string>("COSTARTTIME") + "','yyyy-MM-dd HH24:mi:ss')", "TO_DATE('" + jo_decl.Value<string>("COENDTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("COSTARTID"), jo_decl.Value<string>("COSTARTNAME"), jo_decl.Value<string>("COENDID"), jo_decl.Value<string>("COENDNAME"), "TO_DATE('" + jo_decl.Value<string>("PRESTARTTIME") + "','yyyy-MM-dd HH24:mi:ss')", "TO_DATE('" + jo_decl.Value<string>("PREENDTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("PRESTARTID")
                                     , jo_decl.Value<string>("PRESTARTNAME"), jo_decl.Value<string>("PREENDID"), jo_decl.Value<string>("PREENDNAME"), "TO_DATE('" + jo_decl.Value<string>("CKFINISHTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("CKID"), jo_decl.Value<string>("CKNAME"), "TO_DATE('" + jo_decl.Value<string>("REPSTARTTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("REPID"), jo_decl.Value<string>("REPNAME"), "TO_DATE('" + jo_decl.Value<string>("RELATEDTIME") + "','yyyy-MM-dd HH24:mi:ss')"
                                     , jo_decl.Value<string>("RELATEDUSERID"), jo_decl.Value<string>("RELATEDUSERNAME"), "TO_DATE('" + jo_decl.Value<string>("REPENDTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("REPENDID"), jo_decl.Value<string>("REPENDNAME"), "TO_DATE('" + jo_decl.Value<string>("REPOVERTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("REPOVERID"), jo_decl.Value<string>("REPOVERNAME"), jo_decl.Value<string>("CONSHIPPERNUM"), jo_decl.Value<string>("BUSIUNITNUM")
                                     , jo_decl.Value<string>("REPUNITNUM"), jo_decl.Value<string>("ISNEEDCLEARANCE"), jo_decl.Value<string>("ISHAVECLEARANCE"), jo_decl.Value<string>("ISFORCELAW"), jo_decl.Value<string>("ISSPLIT"), jo_decl.Value<string>("WAREHOUSENO"), jo_decl.Value<string>("YARDCODE"), jo_decl.Value<string>("STATUS"), jo_decl.Value<string>("SHEETNUM"), jo_decl.Value<string>("PRESHEETNUM")

                                     , jo_decl.Value<string>("COMMODITYNUM"), jo_decl.Value<string>("ISACCEPT"), jo_decl.Value<string>("MODIFYFLAG"), jo_decl.Value<string>("CHECKFLAG"), jo_decl.Value<string>("PREEDIT"), jo_decl.Value<string>("WPID"), jo_decl.Value<string>("WPNAME"), "TO_DATE('" + jo_decl.Value<string>("WPTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("CUSNO"), jo_decl.Value<string>("DATACONFIRM")
                                     , jo_decl.Value<string>("RELATEDFLAG"), jo_decl.Value<string>("REPOVERFLAG"), "TO_DATE('" + jo_decl.Value<string>("PREACCTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("PREACCUSERID"), jo_decl.Value<string>("PREACCUSERNAME"), jo_decl.Value<string>("REPWAYID"), jo_decl.Value<string>("CUSTOMSSTATUS"), "TO_DATE('" + jo_decl.Value<string>("PREPENDTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("PREPENDUSERID"), jo_decl.Value<string>("PREPENDUSERNAME")
                                     , jo_decl.Value<string>("TOTALNW"), jo_decl.Value<string>("TOTALMONEY"), jo_decl.Value<string>("TOTALNUM"), jo_decl.Value<string>("ISPRINT"), "TO_DATE('" + jo_decl.Value<string>("PRINTTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("PRINTNUM"), jo_decl.Value<string>("PREEDITUSERID"), jo_decl.Value<string>("PREEDITUSERNAME"), "TO_DATE('" + jo_decl.Value<string>("PREEDITTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("LISTTYPE")
                                     , jo_decl.Value<string>("FORMATAUTO"), jo_decl.Value<string>("BUSITYPE"), jo_decl.Value<string>("ASSOCIATEPEDECLNO"), jo_decl.Value<string>("ASSOCIATEDECLNO"), jo_decl.Value<string>("DECLCODESOURCE"), jo_decl.Value<string>("DECLREMARK"), jo_decl.Value<string>("PAUSEUSERID"), jo_decl.Value<string>("PAUSEUSERNAME"), jo_decl.Value<string>("SPECIALDECL"), jo_decl.Value<string>("DATACONFIRMUSERID")
                                     , jo_decl.Value<string>("DATACONFIRMUSERNAME"), "TO_DATE('" + jo_decl.Value<string>("DATACONFIRMUSERTIME") + "','yyyy-MM-dd HH24:mi:ss')", jo_decl.Value<string>("MOCURRENTID"), jo_decl.Value<string>("COCURRENTID")
                                     );
                        DBMgr.ExecuteNonQuery(sql);
                        db.ListRightPush("jishi_synced_decl", json_decl);
                    }
                    else//如果不是吉时的单子
                    {
                        db.ListRightPush("unsync_decl", json_decl);
                    }
                }
                catch (Exception ex)
                {
                    db.ListRightPush("redis_declare", json_decl);
                    this.button1.Text = ex.Message;
                }
            }
        }
        //获取订单状态变更日志
        private void GetStatusLog()
        {
            string json_status = db.ListLeftPop("statuslog");
            if (!string.IsNullOrEmpty(json_status))
            {
                try
                {
                    JObject jo_status = (JObject)JsonConvert.DeserializeObject(json_status);
                    if (JudgeIsJiShiOrder(jo_status))
                    {
                        string ORDERCODE = jo_status.Value<string>("ORDERCODE");
                        string sql = "delete from list_statuslog where ordercode='" + jo_status.Value<string>("ORDERCODE") + "' AND type='" + jo_status.Value<string>("TYPE") + "' and statuscode='" + jo_status.Value<string>("STATUSCODE") + "'";
                        DBMgr.ExecuteNonQuery(sql);
                        string syncresult = "failure"; //同步SAP结果
                        switch (jo_status.Value<string>("STATUSCODE"))
                        {
                            case "15"://关务接单
                                //吉时在此处填充SAP接口代码
                                syncresult = "success";
                                ZSGWJD(ORDERCODE);
                                break;
                            case "20"://单证制单
                                //吉时在此处填充SAP接口代码
                                syncresult = "success";
                                ZSDZZD(ORDERCODE);
                                break;
                            case "40"://单证审单
                                //吉时在此处填充SAP接口代码
                                syncresult = "success";
                                ZSDZSD(ORDERCODE);
                                break;
                            case "80"://单证输机
                                //吉时在此处填充SAP接口代码
                                syncresult = "success";
                                ZSDZSJ(ORDERCODE);
                                break;
                            case "100"://报关单发送
                                //吉时在此处填充SAP接口代码
                                syncresult = "success";
                                ZSDZFS(ORDERCODE);
                                break;
                            case "110"://提前报关单发送
                                //吉时在此处填充SAP接口代码
                                syncresult = "success";
                                ZSTQDZFS(ORDERCODE);
                                break;
                        }
                        //添加状态变更到list_statuslog表
                        sql = @"insert into list_statuslog(id,ordercode,statuscode,statusname,type,statuctime,syncstatus) values(list_statuslog_id.nextval,
                        '{0}','{1}','{2}','{3}','{4}','{5}')";
                        sql = string.Format(sql, jo_status.Value<string>("ORDERCODE"), jo_status.Value<string>("STATUSCODE"), jo_status.Value<string>("STATUSNAME"),
                        jo_status.Value<string>("TYPE"), "TO_DATE('" + jo_status.Value<string>("STATUSTIME") + "','yyyy-MM-dd HH24:mi:ss')", syncresult);
                        DBMgr.ExecuteNonQuery(sql);
                        db.ListRightPush("jishi_synced_status", json_status);
                    }
                    else
                    {
                        db.ListRightPush("unsync_status", json_status);
                    }
                }
                catch (Exception ex)
                {
                    db.ListRightPush("statuslog", json_status);
                    this.button1.Text = ex.Message;
                }
            }
        }
        //判定是不是吉时的单子
        private bool JudgeIsJiShiOrder(JObject jo)
        {
            bool rtn = false;
            string sql = "select * from list_order where code='" + jo.Value<string>("ORDERCODE") + "'";
            DataTable dt = DBMgr.GetDataTable(sql);
            if (dt.Rows.Count > 0)   //如果是吉时的单子
            {
                rtn = true;
            }
            return rtn;
        }


        //关务接单
        public static void ZSGWJD(string CODE)
        {
            sap.SI_CUS_CUS1002Service api = new sap.SI_CUS_CUS1002Service();
            api.Timeout = 6000000;
            api.Credentials = new NetworkCredential("soapcall", "soapcall");
            sap.DT_CUS_CUS1002_REQITEM m = new sap.DT_CUS_CUS1002_REQITEM();//模型
            string sql = "select *　from list_order where code ='" + CODE + "'";
            DataTable dt = DBMgr.GetDataTable(sql);
            string FWONO = "";
            string FOONO = "";
            string EVENT_DAT = "";
            if (dt.Rows.Count > 0)
            {
                FWONO = dt.Rows[0]["FWONO"] + "";
                FOONO = dt.Rows[0]["FOONO"] + "";
                if (!string.IsNullOrEmpty(FOONO))
                {
                    FOONO = FOONO.Remove(0, 4);
                    string datetime = DateTime.Now.ToLocalTime().ToString("yyyyMMddHHmmss");
                    m.EVENT_CODE = "ZSGWJD";
                    m.FWO_ID = FWONO;
                    m.FOO_ID = FOONO;
                    m.EVENT_DAT = EVENT_DAT;
                    sap.DT_CUS_CUS1002_REQITEM[] mlist = new sap.DT_CUS_CUS1002_REQITEM[1];
                    mlist[0] = m;
                    sap.DT_CUS_CUS1002_RES res;
                    try
                    {
                        res = api.SI_CUS_CUS1002(mlist);
                        save_log(res.EV_ERROR + "", "ZSGWJD(" + res.EV_MSG + ")", CODE, "4");
                    }
                    catch (Exception e)
                    {
                        save_log("E", "ZSGWJD(接口回调报错)", CODE, "4");
                    }
                }
            }
        }
        //单证制单
        public static void ZSDZZD(string CODE)
        {
            sap.SI_CUS_CUS1002Service api = new sap.SI_CUS_CUS1002Service();
            api.Timeout = 6000000;
            api.Credentials = new NetworkCredential("soapcall", "soapcall");
            sap.DT_CUS_CUS1002_REQITEM m = new sap.DT_CUS_CUS1002_REQITEM();//模型
            string sql = "select *　from list_order where code ='" + CODE + "'";
            DataTable dt = DBMgr.GetDataTable(sql);
            string FWONO = "";
            string FOONO = "";
            string EVENT_DAT = "";
            string datetime = DateTime.Now.ToLocalTime().ToString("yyyyMMddHHmmss");
            if (dt.Rows.Count > 0)
            {
                FWONO = dt.Rows[0]["FWONO"] + "";
                //FOONO 报关Foo
                if (!string.IsNullOrEmpty(dt.Rows[0]["FOONO"] + ""))
                {
                    FOONO = dt.Rows[0]["FOONO"] + "";
                    FOONO = FOONO.Remove(0, 4);
                    m.EVENT_CODE = "ZSDZZD";
                    m.FWO_ID = FWONO;
                    m.FOO_ID = FOONO;
                    m.EVENT_DAT = EVENT_DAT;
                    sap.DT_CUS_CUS1002_REQITEM[] mlist = new sap.DT_CUS_CUS1002_REQITEM[1];
                    mlist[0] = m;
                    sap.DT_CUS_CUS1002_RES res;
                    try
                    {
                        res = api.SI_CUS_CUS1002(mlist);
                        save_log(res.EV_ERROR + "", "ZSDZZD报关(" + res.EV_MSG + ")", CODE, "4");
                    }
                    catch (Exception e)
                    {
                        save_log("E", "ZSDZZD报关(接口回调报错)", CODE, "4");
                    }
                }
                //FOONO 报检Foo
                if (!string.IsNullOrEmpty(dt.Rows[0]["FOONOBJ"] + ""))
                {
                    FOONO = dt.Rows[0]["FOONOBJ"] + "";
                    FOONO = FOONO.Remove(0, 4);
                    m.EVENT_CODE = "ZSDZZD";
                    m.FWO_ID = FWONO;
                    m.FOO_ID = FOONO;
                    m.EVENT_DAT = EVENT_DAT;
                    sap.DT_CUS_CUS1002_REQITEM[] mlist = new sap.DT_CUS_CUS1002_REQITEM[1];
                    mlist[0] = m;
                    sap.DT_CUS_CUS1002_RES res;
                    try
                    {
                        res = api.SI_CUS_CUS1002(mlist);
                        save_log(res.EV_ERROR + "", "ZSDZZD报检(" + res.EV_MSG + ")", CODE, "4");
                    }
                    catch (Exception e)
                    {
                        save_log("E", "ZSDZZD报检(接口回调报错)", CODE, "4");
                    }
                }
            }
        }
        //单证审单
        public static void ZSDZSD(string CODE)
        {
            sap.SI_CUS_CUS1002Service api = new sap.SI_CUS_CUS1002Service();
            api.Timeout = 6000000;
            api.Credentials = new NetworkCredential("soapcall", "soapcall");
            sap.DT_CUS_CUS1002_REQITEM m = new sap.DT_CUS_CUS1002_REQITEM();//模型
            string sql = "select *　from list_order where code ='" + CODE + "'";
            DataTable dt = DBMgr.GetDataTable(sql);
            string FWONO = "";
            string FOONO = "";
            string EVENT_DAT = "";
            if (dt.Rows.Count > 0)
            {
                FWONO = dt.Rows[0]["FWONO"] + "";
                FOONO = dt.Rows[0]["FOONO"] + "";
                if (!string.IsNullOrEmpty(FOONO))
                {
                    FOONO = FOONO.Remove(0, 4);
                    string datetime = DateTime.Now.ToLocalTime().ToString("yyyyMMddHHmmss");
                    m.EVENT_CODE = "ZSDZSD";
                    m.FWO_ID = FWONO;
                    m.FOO_ID = FOONO;
                    m.EVENT_DAT = EVENT_DAT;
                    sap.DT_CUS_CUS1002_REQITEM[] mlist = new sap.DT_CUS_CUS1002_REQITEM[1];
                    mlist[0] = m;
                    sap.DT_CUS_CUS1002_RES res;
                    try
                    {
                        res = api.SI_CUS_CUS1002(mlist);
                        save_log(res.EV_ERROR + "", "ZSDZSD(" + res.EV_MSG + ")", CODE, "4");
                    }
                    catch (Exception e)
                    {
                        save_log("E", "ZSDZSD(接口回调报错)", CODE, "4");
                    }
                }
            }
        }
        //单证输机
        public static void ZSDZSJ(string CODE)
        {
            sap.SI_CUS_CUS1002Service api = new sap.SI_CUS_CUS1002Service();
            api.Timeout = 6000000;
            api.Credentials = new NetworkCredential("soapcall", "soapcall");
            sap.DT_CUS_CUS1002_REQITEM m = new sap.DT_CUS_CUS1002_REQITEM();//模型
            string sql = "select *　from list_order where code ='" + CODE + "'";
            DataTable dt = DBMgr.GetDataTable(sql);
            string FWONO = "";
            string FOONO = "";
            string EVENT_DAT = "";
            string datetime = DateTime.Now.ToLocalTime().ToString("yyyyMMddHHmmss");
            if (dt.Rows.Count > 0)
            {
                FWONO = dt.Rows[0]["FWONO"] + "";
                //FOONO 报关Foo
                if (!string.IsNullOrEmpty(dt.Rows[0]["FOONO"] + ""))
                {
                    FOONO = dt.Rows[0]["FOONO"] + "";
                    FOONO = FOONO.Remove(0, 4);
                    m.EVENT_CODE = "ZSDZSJ";
                    m.FWO_ID = FWONO;
                    m.FOO_ID = FOONO;
                    m.EVENT_DAT = EVENT_DAT;
                    sap.DT_CUS_CUS1002_REQITEM[] mlist = new sap.DT_CUS_CUS1002_REQITEM[1];
                    mlist[0] = m;
                    sap.DT_CUS_CUS1002_RES res;
                    try
                    {
                        res = api.SI_CUS_CUS1002(mlist);
                        save_log(res.EV_ERROR + "", "ZSDZSJ报关(" + res.EV_MSG + ")", CODE, "4");
                    }
                    catch (Exception e)
                    {
                        save_log("E", "ZSDZSJ报关(接口回调报错)", CODE, "4");
                    }
                }
                //FOONO 报检Foo
                if (!string.IsNullOrEmpty(dt.Rows[0]["FOONOBJ"] + ""))
                {
                    FOONO = dt.Rows[0]["FOONOBJ"] + "";
                    FOONO = FOONO.Remove(0, 4);
                    m.EVENT_CODE = "ZSDZSJ";
                    m.FWO_ID = FWONO;
                    m.FOO_ID = FOONO;
                    m.EVENT_DAT = EVENT_DAT;
                    sap.DT_CUS_CUS1002_REQITEM[] mlist = new sap.DT_CUS_CUS1002_REQITEM[1];
                    mlist[0] = m;
                    sap.DT_CUS_CUS1002_RES res;
                    try
                    {
                        res = api.SI_CUS_CUS1002(mlist);
                        save_log(res.EV_ERROR + "", "ZSDZSJ报检(" + res.EV_MSG + ")", CODE, "4");
                    }
                    catch (Exception e)
                    {
                        save_log("E", "ZSDZSJ报检(接口回调报错)", CODE, "4");
                    }
                }
            }
        }
        //报关单发送
        public static void ZSDZFS(string CODE)
        {
            sap.SI_CUS_CUS1002Service api = new sap.SI_CUS_CUS1002Service();
            api.Timeout = 6000000;
            api.Credentials = new NetworkCredential("soapcall", "soapcall");
            sap.DT_CUS_CUS1002_REQITEM m = new sap.DT_CUS_CUS1002_REQITEM();//模型
            string sql = "select *　from list_order where code ='" + CODE + "'";
            DataTable dt = DBMgr.GetDataTable(sql);
            string FWONO = "";
            string FOONO = "";
            string EVENT_DAT = "";
            if (dt.Rows.Count > 0)
            {
                FWONO = dt.Rows[0]["FWONO"] + "";
                FOONO = dt.Rows[0]["FOONO"] + "";
                if (!string.IsNullOrEmpty(FOONO))
                {
                    FOONO = FOONO.Remove(0, 4);
                    string datetime = DateTime.Now.ToLocalTime().ToString("yyyyMMddHHmmss");
                    m.EVENT_CODE = "ZSDZFS";
                    m.FWO_ID = FWONO;
                    m.FOO_ID = FOONO;
                    m.EVENT_DAT = EVENT_DAT;
                    sap.DT_CUS_CUS1002_REQITEM[] mlist = new sap.DT_CUS_CUS1002_REQITEM[1];
                    mlist[0] = m;
                    sap.DT_CUS_CUS1002_RES res;
                    try
                    {
                        res = api.SI_CUS_CUS1002(mlist);
                        save_log(res.EV_ERROR + "", "ZSDZFS(" + res.EV_MSG + ")", CODE, "4");
                    }
                    catch (Exception e)
                    {
                        save_log("E", "ZSDZFS(接口回调报错)", CODE, "4");
                    }
                }
            }
        }
        //提前报关单发送
        public static void ZSTQDZFS(string CODE)
        {
            sap.SI_CUS_CUS1002Service api = new sap.SI_CUS_CUS1002Service();
            api.Timeout = 6000000;
            api.Credentials = new NetworkCredential("soapcall", "soapcall");
            sap.DT_CUS_CUS1002_REQITEM m = new sap.DT_CUS_CUS1002_REQITEM();//模型
            string sql = "select *　from list_order where code ='" + CODE + "'";
            DataTable dt = DBMgr.GetDataTable(sql);
            string FWONO = "";
            string FOONO = "";
            string EVENT_DAT = "";
            if (dt.Rows.Count > 0)
            {
                FWONO = dt.Rows[0]["FWONO"] + "";
                FOONO = dt.Rows[0]["FOONO"] + "";
                if (!string.IsNullOrEmpty(FOONO))
                {
                    FOONO = FOONO.Remove(0, 4);
                    string datetime = DateTime.Now.ToLocalTime().ToString("yyyyMMddHHmmss");
                    m.EVENT_CODE = "ZSTQDZFS";
                    m.FWO_ID = FWONO;
                    m.FOO_ID = FOONO;
                    m.EVENT_DAT = EVENT_DAT;
                    sap.DT_CUS_CUS1002_REQITEM[] mlist = new sap.DT_CUS_CUS1002_REQITEM[1];
                    mlist[0] = m;
                    sap.DT_CUS_CUS1002_RES res;
                    try
                    {
                        res = api.SI_CUS_CUS1002(mlist);
                        save_log(res.EV_ERROR + "", "ZSTQDZFS(" + res.EV_MSG + ")", CODE, "4");
                    }
                    catch (Exception e)
                    {
                        save_log("E", "ZSTQDZFS(接口回调报错)", CODE, "4");
                    }
                }
            }
        }
        //存日志  1 sap->现场  2 现场->单证云
        public static void save_log(string MSG_TYPE, string MSG_TXT, string CODE, string source)
        {
            if (source == "3")
            {
                source = "新关务->SAP";
            }
            string STATUS;
            if (MSG_TYPE == "E")
            {
                STATUS = "失败";
            }
            else
            {
                STATUS = "成功";
            }
            string TEXT = "";

            if (!string.IsNullOrEmpty(MSG_TXT))
            {
                TEXT += "[" + MSG_TXT + "]";
            }
            string sql = @"INSERT INTO MSG (ID,FWONO,SOURCE,TEXT,STATUS,CREATETIME) VALUES (MSG_ID.Nextval,'" + CODE + "','" + source + "','" + TEXT + "','" + STATUS + "',sysdate)";
            DBMgr.ExecuteNonQuery(sql);
        }
    }
}
