﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace UR21_DualAntenna_Demo.Model
{
    public class Result
    {
        public bool? bOk { get; set; }

        public string sResult { get; set; }

        public string sErrMsg { get; set; }

        public object sObj { get; set; }

        public Result()
        {
            bOk = false;
            sResult = "";
            sErrMsg = "";
        }

        public Result(bool? b, string s)
        {
            bOk = b;
            sResult = s;
            sErrMsg = "";
        }
    }


    class MyVoucher
    {
        public string No { get; set; }
        public string Value{ get; set; }
    }

    class MyProduct
    {
        public string PTag { get; set; }
        public string Epc { get; set; }
        public string Desc{ get; set; }
        public decimal Price { get; set; }
    }

    class CsvHelper
    {
        public Result Read_Csv_Product()
        {
            Result fr = new Result();

            string dummyFile = Properties.Settings.Default.PRODUCT;

            try
            {
                if (!File.Exists(dummyFile))
                {
                    // Create default xml file.
                    fr.sResult = "Dummy Product file is missing!";
                    fr.bOk = null;
                }
                else
                {
                    string[] rawData = File.ReadAllLines(dummyFile);
                    List<MyProduct> myProducts = new List<MyProduct>();

                    foreach (var raw in rawData)
                    {
                        if (raw.ToUpper().StartsWith("BARCODE,EPC"))
                            continue;

                        string[] splitData = raw.Split(new string[] { "," }, StringSplitOptions.None);
                        MyProduct p = new MyProduct() {
                            PTag = splitData[0].Trim(),
                            Epc = splitData[1].Trim(),
                            Desc = splitData[3].Trim() + " " + splitData[5].Trim() + " " + splitData[8].Trim() + " " + splitData[7].Trim(),
                            Price = Convert.ToDecimal(splitData[9].Trim())
                        };

                        myProducts.Add(p);
                    }

                    fr.sObj = myProducts;
                    fr.bOk = true;
                }
            }
            catch (Exception e)
            {
                fr.sResult = MyConst.ERROR + Environment.NewLine + "Fail to read " + dummyFile + " file.";
                fr.sErrMsg = ErrCode.Err_ReadXML_Product + ": " + e.Message;
            }

            return fr;
        }
    }

    class XmlHelper
    {

        public Result ReadXML_Voucher()
        {
            Result fr = new Result();

            string strXML = Properties.Settings.Default.VOUCHER;

            try
            {
                if (!File.Exists(strXML))
                {
                    // Create default xml file.
                    fr.sResult = "Voucher XML file is missing!";
                    fr.bOk = null;
                }
                else
                {
                    XDocument xD = XDocument.Load(strXML);

                    List<MyVoucher> myVouchers = (from s in xD.Descendants(MyConst.xVoucher)
                                            select new MyVoucher
                                            {
                                                No = s.Element(MyConst.xNo).Value,
                                                Value = s.Element(MyConst.xValue).Value
                                            }).ToList();

                    fr.sObj = myVouchers;
                    fr.bOk = true;
                }                
            }
            catch (Exception e)
            {
                fr.sResult = MyConst.ERROR + Environment.NewLine + "Fail to read " + strXML + " file.";
                fr.sErrMsg = ErrCode.Err_ReadXML_Voucher + ": " + e.Message;
            }

            return fr;
        }


        public Result ReadXML_Product()
        {
            Result fr = new Result();

            string strXML = Properties.Settings.Default.PRODUCT;

            try
            {
                if (!File.Exists(strXML))
                {
                    // Create default xml file.
                    fr.sResult = "Product XML file is missing!";
                    fr.bOk = null;
                }
                else
                {
                    XDocument xD = XDocument.Load(strXML);

                    List<MyProduct> myProducts = (from s in xD.Descendants(MyConst.xTag)
                                                  select new MyProduct
                                                  {
                                                      PTag = s.Element(MyConst.xPTag).Value,
                                                      Desc = s.Element(MyConst.xDesc).Value,
                                                      Price = MyConverter.ToDecimal(s.Element(MyConst.xPrice).Value)
                                                  }).ToList();

                    fr.sObj = myProducts;
                    fr.bOk = true;
                }
            }
            catch (Exception e)
            {
                fr.sResult = MyConst.ERROR + Environment.NewLine + "Fail to read " + strXML + " file.";
                fr.sErrMsg = ErrCode.Err_ReadXML_Product + ": " + e.Message;
            }

            return fr;
        }
    }
}
