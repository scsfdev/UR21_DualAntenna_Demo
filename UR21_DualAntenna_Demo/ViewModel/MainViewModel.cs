using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Windows.Input;
using System.Windows.Threading;
using UR21_DualAntenna_Demo.Model;

namespace UR21_DualAntenna_Demo.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        //DispatcherTimer dComTimer;
        DispatcherTimer dMsgTimer;
        DispatcherTimer dComTimer;

        List<MyVoucher> lstVs = new List<MyVoucher>();
        List<MyProduct> lstPs = new List<MyProduct>();

        Ur21 ur1 = new Ur21();
        //Ur22 ur2 = new Ur22();


        #region ICOMMAND

        public ICommand CmdReset { get; private set; }
        public ICommand CmdConfigAction { get; private set; }
        public ICommand CmdPosAction { get; private set; }

        #endregion


        #region Getter / Setter

        private string _title;
        public string Title
        {
            get { return _title; }
            set { Set(ref _title, value); }
        }


        private string _defaultMsg;
        public string DefaultMsg
        {
            get { return _defaultMsg; }
            set { Set(ref _defaultMsg, value); }
        }


        private string _version;
        public string Version
        {
            get { return _version; }
            set { Set(ref _version, value); }
        }


        private string _statusMsg;
        public string StatusMsg
        {
            get { return _statusMsg; }
            set
            {
                Set(ref _statusMsg, value);
                if (value.Contains("Warning") || value.Contains("Error") || value.Contains("Info"))
                    dMsgTimer.Start();
            }
        }


        private int _comPort1;
        public int ComPort1
        {
            get { return _comPort1; }
            set { Set(ref _comPort1, value); }
        }


        private int _comPort2;
        public int ComPort2
        {
            get { return _comPort2; }
            set { Set(ref _comPort2, value); }
        }


        private bool _allowDuplicate;
        public bool AllowDuplicate
        {
            get { return _allowDuplicate; }
            set { Set(ref _allowDuplicate, value); }
        }


        private bool _allowSound;
        public bool AllowSound
        {
            get { return _allowSound; }
            set { Set(ref _allowSound, value); }
        }


        private string _selected_Antenna;
        public string Selected_Antenna
        {
            get { return _selected_Antenna; }
            set { Set(ref _selected_Antenna, value); }
        }


        private bool _gridConfig;
        public bool GridConfig
        {
            get { return _gridConfig; }
            set { Set(ref _gridConfig, value); }
        }


        private bool _gridPos;
        public bool GridPos
        {
            get { return _gridPos; }
            set { Set(ref _gridPos, value); }
        }


        private ObservableCollection<Tag> _tagList;
        public ObservableCollection<Tag> TagList
        {
            get { return _tagList; }
            set { Set(ref _tagList, value); }
        }


        private string _checkOutDate;
        public string CheckOutDate
        {
            get { return _checkOutDate; }
            set { Set(ref _checkOutDate, value); }
        }


        private decimal? _totalValue;
        public decimal? TotalValue
        {
            get { return _totalValue; }
            set
            {
                Set(ref _totalValue, value);

                FinalTotalValue = "-";
            }
        }


        private string _voucherNo;
        public string VoucherNo
        {
            get { return _voucherNo; }
            set
            {
                Set(ref _voucherNo, value);

                FinalTotalValue = "-";
            }
        }


        private string _finalTotalValue;
        public string FinalTotalValue
        {
            get { return _finalTotalValue; }
            set
            {
                string finalValue = "";

                // Calculate final total value.
                if (string.IsNullOrEmpty(_voucherNo))
                    finalValue = _totalValue.ToString();
                else
                {
                    decimal voucher = GetVoucherValue();

                    finalValue = (_totalValue - voucher).ToString();
                }

                Set(ref _finalTotalValue, finalValue);
            }
        }

        private decimal GetVoucherValue()
        {
            MyVoucher myV = (from v in lstVs
                             where v.No == _voucherNo.ToUpper().Trim()
                             select v).SingleOrDefault();
            if (myV != null)
                return MyConverter.ToDecimal(myV.Value);
            else
                return 0;
        }

        private bool inAction;
        public bool InAction
        {
            get { return inAction; }
            set { Set(ref inAction, value); }
        }


        private bool isReady;
        public bool IsReady
        {
            get { return isReady; }
            set { Set(ref isReady, value); }
        }



        private string _rfCmdText;
        public string RfCmdText
        {
            get { return _rfCmdText; }
            set { Set(ref _rfCmdText, value); }
        }


        private string _voucherValue;
        public string VoucherValue
        {
            get { return _voucherValue; }
            set { Set(ref _voucherValue, value); }
        }


        #endregion


        
        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _dataService.GetData(
                (item, error) =>
                {
                    if (error != null)
                    {
                        // Report error here
                        return;
                    }

                    
                });

            Title = "RFID Self-Checkout Demo";
            Version = General.gGetVersion();

            GridConfig = false;
            GridPos = false;

            Messenger.Default.Register<string>(this, MsgType.MAIN_VM, ShowMsg);


            DefaultMsg = _title + " By DIAS. Version: " + Version;

            CmdConfigAction = new RelayCommand<object>(ActionConfig);
            CmdPosAction = new RelayCommand<object>(ActionPos);

            Ur21.OnTagRead += Ur_OnTagRead;

            dComTimer = new DispatcherTimer();
            dComTimer.Interval = TimeSpan.FromSeconds(2);
            dComTimer.Tick += DTimer_Tick;

            dMsgTimer = new DispatcherTimer();
            dMsgTimer.Interval = TimeSpan.FromSeconds(5);
            dMsgTimer.Tick += DMsgTimer_Tick;

            StatusMsg = DefaultMsg;

            // ResetForm();
            //  dTimer.Start();
            RfCmdText = MyConst.SCAN;
            Selected_Antenna = Properties.Settings.Default.ANTENNA;
            AllowDuplicate = Properties.Settings.Default.DUPLICATE;
            AllowSound = Properties.Settings.Default.SOUND;

            // Load dummy data.
            Load_Csv_Dummy();

            // TODO :Debug usage only.
            // Test_debug();
            Close_Config();
        }

        private void ActionPos(object obj)
        {
            if (obj == null)
                return;

            string strTag = obj.ToString().Trim().ToUpper();

            switch (strTag)
            {
                case "CONFIG": Load_Config(); break;
                case "RESET": Reset_Pos(); break;
                case "SCAN": Scan_ProductTag(); break;
                case "MASTER": Payment_Master(); break;
                case "VISA": Payment_Visa(); break;
                case "AMERICAN": Payment_American(); break;
                case "CASH": Payment_Cash(); break;
            }
        }

        private void Payment_Cash()
        {
            StatusMsg = "Info: Cash payment finished";
            Messenger.Default.Send(MyConst.INFO + Environment.NewLine + "CASH payment finished.", MsgType.MAIN_V);
            Reset_Pos();
        }

        private void Payment_American()
        {
            StatusMsg = "Info: American Express payment finished";
            Messenger.Default.Send(MyConst.INFO + Environment.NewLine + "AMERICAN EXPRESS payment finished.", MsgType.MAIN_V);
            Reset_Pos();
        }

        private void Payment_Visa()
        {
            StatusMsg = "Info: Visa payment finished";
            Messenger.Default.Send(MyConst.INFO + Environment.NewLine + "VISA payment finished.", MsgType.MAIN_V);
            Reset_Pos();
        }

        private void Payment_Master()
        {
            StatusMsg = "Info: Master payment finished";
            Messenger.Default.Send(MyConst.INFO + Environment.NewLine + "MASTER payment finished.", MsgType.MAIN_V);
            Reset_Pos();
        }

        private void DMsgTimer_Tick(object sender, EventArgs e)
        {
            StatusMsg = DefaultMsg;
            dMsgTimer.Stop();
        }

        private void Test_debug()
        {
            GridPos = false;
            GridConfig = true;
            Load_Config();
        }

        private void Load_Csv_Dummy()
        {
            // Load Products.
            if (lstPs == null || lstPs.Count <= 0)
            {
                Result mR = new CsvHelper().Read_Csv_Product();

                if (mR.bOk == true)
                    lstPs = (List<MyProduct>)mR.sObj;
                else
                {
                    if (mR.bOk == false)
                    {
                        StatusMsg = "Warning: " + mR.sResult;
                        Messenger.Default.Send(MyConst.WARNING + Environment.NewLine + mR.sResult, MsgType.MAIN_V);
                    }
                    else
                    {
                        StatusMsg = mR.sResult.Replace(MyConst.ERROR + Environment.NewLine, "Error: ");
                        Messenger.Default.Send(mR.sResult + Environment.NewLine + Environment.NewLine + mR.sErrMsg, MsgType.MAIN_V);
                    }
                }
            }

            // Load Vouchers.
            if (lstVs == null || lstVs.Count <= 0)
            {
                Result mR = new XmlHelper().ReadXML_Voucher();

                if (mR.bOk == true)
                    lstVs = (List<MyVoucher>)mR.sObj;
                else
                {
                    if (mR.bOk == false)
                    {
                        StatusMsg = "Warning: " + mR.sResult;
                        Messenger.Default.Send(MyConst.WARNING + Environment.NewLine + mR.sResult, MsgType.MAIN_V);
                    }
                    else
                    {
                        StatusMsg = mR.sResult.Replace(MyConst.ERROR + Environment.NewLine, "Error: ");
                        Messenger.Default.Send(mR.sResult + Environment.NewLine + Environment.NewLine + mR.sErrMsg, MsgType.MAIN_V);
                    }
                }
            }
        }

        private void Load_XML_Dummy()
        {
            // Load Products.
            if (lstPs == null || lstPs.Count <= 0)
            {
                Result mR = new XmlHelper().ReadXML_Product();

                if (mR.bOk == true)
                    lstPs = (List<MyProduct>)mR.sObj;
                else
                {
                    if (mR.bOk == false)
                    {
                        StatusMsg = "Warning: " + mR.sResult;
                        Messenger.Default.Send(MyConst.WARNING + Environment.NewLine + mR.sResult, MsgType.MAIN_V);
                    }
                    else
                    {
                        StatusMsg = mR.sResult.Replace(MyConst.ERROR + Environment.NewLine, "Error: ");
                        Messenger.Default.Send(mR.sResult + Environment.NewLine + Environment.NewLine + mR.sErrMsg, MsgType.MAIN_V);
                    }
                }
            }

            // Load Vouchers.
            if (lstVs == null || lstVs.Count <= 0)
            {
                Result mR = new XmlHelper().ReadXML_Voucher();

                if (mR.bOk == true)
                    lstVs = (List<MyVoucher>)mR.sObj;
                else
                {
                    if (mR.bOk == false)
                    {
                        StatusMsg = "Warning: " + mR.sResult;
                        Messenger.Default.Send(MyConst.WARNING + Environment.NewLine + mR.sResult, MsgType.MAIN_V);
                    }
                    else
                    {
                        StatusMsg = mR.sResult.Replace(MyConst.ERROR + Environment.NewLine, "Error: ");
                        Messenger.Default.Send(mR.sResult + Environment.NewLine + Environment.NewLine + mR.sErrMsg, MsgType.MAIN_V);
                    }
                }
            }
        }

        private void Load_Pos()
        {
            RfCmdText = MyConst.SCAN;
            IsReady = false;
           // dComTimer.Start();
            Selected_Antenna = Properties.Settings.Default.ANTENNA;
           // ComPort1 = Properties.Settings.Default.COM1;
            ComPort1 = Convert.ToInt32(Properties.Settings.Default.ANTI_SCANNER_COM);
            Reset_Pos();
        }

        private void Reset_Pos()
        {
            Stop_RFID();

            //StatusMsg = _defaultMsg;

            RfCmdText = MyConst.SCAN;

            TagList = new ObservableCollection<Tag>();

            CheckOutDate = "";
            TotalValue = null;
            VoucherNo = "";
            FinalTotalValue = "";

            IsReady = false;
        }


        private void ActionConfig(object obj)
        {
            if (obj == null)
                return;

            string strTag = obj.ToString().Trim().ToUpper();

            switch (strTag)
            {
                case "UPDATE": Update_Config(); break;
                case "REFRESH": Auto_COM_Port(); break;
                case "TESTCOM1":
                    if (Check_COM_Port(_comPort1))
                    {
                        StatusMsg = "Info: COM Port is a valid port.";
                        Messenger.Default.Send(MyConst.INFO + Environment.NewLine + "COM Port is a valid port.", MsgType.MAIN_V);
                    }
                    break;
                case "CLOSE": Close_Config(); break;
            }
        }

        
        private void Close_Config()
        {
            GridConfig = false;
            GridPos = true;

            // Reload POS.
            Load_Pos();
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}



        private void DTimer_Tick(object sender, EventArgs e)
        {
            if (!Auto_COM_Port(true))
            {
                GridPos = false;
                GridConfig = true;
            }
            else
            {
                GridPos = true;
                GridConfig = false;
            }
        }

        private bool Auto_COM_Port(bool bSilent = false)
        {
            bool bOk = false;

            try
            {
                string strPORT = "";
                int iCount = 0;

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_SerialPort"))
                {
                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        if (queryObj["Caption"] != null && queryObj["Caption"].ToString().Trim().ToUpper().Contains("DENSO WAVE"))
                        {
                            if (queryObj["DeviceID"] != null && !queryObj["Caption"].ToString().Trim().ToUpper().Contains("DISCONNECTED") && queryObj["Caption"].ToString().Trim().ToUpper().Contains("CONNECTED"))
                            {
                                strPORT = queryObj["DeviceID"].ToString().ToUpper().Replace("COM", "");
                                iCount += 1;
                            }
                        }
                    }
                }

                if (iCount > 1)
                {
                    StatusMsg = "Warning: More than one DENSO WAVE USB-COM devices are detected. Disconnect the one you don't need.";

                    if (!bSilent)
                        Messenger.Default.Send(MyConst.WARNING + Environment.NewLine + "More than one DENSO WAVE USB-COM devices are detected. Disconnect the one you don't need",
                                            MsgType.MAIN_V);
                }
                else
                {
                    if (string.IsNullOrEmpty(strPORT))
                    {
                        StatusMsg = "Warning: No DENSO WAVE USB-COM device is connected to this PC!";

                        if (!bSilent)
                            Messenger.Default.Send(MyConst.WARNING + Environment.NewLine + "No DENSO WAVE USB-COM device is connected to this PC!", MsgType.MAIN_V);
                    }
                    else
                    {
                        ComPort1 = MyConverter.ToInt32(strPORT);      // Assign to variable.
                        //IsActionReady();
                        StatusMsg = DefaultMsg;
                        //dComTimer.Stop();
                        bOk = true;
                    }
                }
            }
            catch (ManagementException e)
            {
               // dComTimer.Stop();
                StatusMsg = "Error: Process failed while trying to retrieve COM port. Details: " + e.Message;
                Messenger.Default.Send(MyConst.ERROR + Environment.NewLine + "An error occurred while trying to retrieve COM port." + Environment.NewLine +
                                        Environment.NewLine + "Error detail: " + e.Message, MsgType.MAIN_V);
            }

            return bOk;
        }


        private bool Check_COM_Port(int iPort)
        {
            bool bOk = false;

            try
            {
                List<string> lstPort = new List<string>();

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_SerialPort"))
                {
                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        if (queryObj["Caption"] != null && queryObj["Caption"].ToString().Trim().ToUpper().Contains("DENSO WAVE"))
                        {
                            if (queryObj["DeviceID"] != null && !queryObj["Caption"].ToString().Trim().ToUpper().Contains("DISCONNECTED") && queryObj["Caption"].ToString().Trim().ToUpper().Contains("CONNECTED"))
                                lstPort.Add(queryObj["DeviceID"].ToString().ToUpper().Replace("COM", ""));
                        }
                    }
                }

                if (lstPort.Count <= 0)
                {
                    StatusMsg = "Warning: No DENSO WAVE USB-COM device is connected to this PC!";
                    Messenger.Default.Send(MyConst.WARNING + Environment.NewLine + "No DENSO WAVE USB-COM device is connected to this PC!", MsgType.MAIN_V);
                }
                else
                {
                    foreach (string strPort in lstPort)
                    {
                        if (strPort == iPort.ToString())
                        {
                            StatusMsg = DefaultMsg;
                            bOk = true;
                            break;
                        }
                    }

                    if (!bOk)
                    {
                        StatusMsg = "Warning: Given COM Port is not a valid / active one!";
                        Messenger.Default.Send(MyConst.WARNING + Environment.NewLine + "Given COM Port is not a valid / active one!", MsgType.MAIN_V);
                    }
                }               
            }
            catch (ManagementException e)
            {
                // dComTimer.Stop();
                StatusMsg = "Error: Failed to check COM Port. Details: " + e.Message;
                Messenger.Default.Send(MyConst.ERROR + Environment.NewLine + "An error occurred while trying to check COM port." + Environment.NewLine +
                                        Environment.NewLine + "Error detail: " + e.Message, MsgType.MAIN_V);
            }

            return bOk;
        }

        private void Load_Config()
        {
            // Stop RFID reading.
            Stop_RFID();
            GridPos = false;
            GridConfig = true;

            if (string.IsNullOrEmpty(Properties.Settings.Default.ANTENNA))
                Selected_Antenna = "DUAL";
            else
                Selected_Antenna = Properties.Settings.Default.ANTENNA;

            AllowDuplicate = Properties.Settings.Default.DUPLICATE;
            AllowSound = Properties.Settings.Default.SOUND;
            ComPort1 = Properties.Settings.Default.COM1;
            ComPort2 = Properties.Settings.Default.COM2;
        }

        

        private void Update_Config()
        {
            // Check Error.
            if (IsConfigErrorFree())
            {
                // Update it back to setting.
                Properties.Settings.Default.ANTENNA = _selected_Antenna;
                Properties.Settings.Default.DUPLICATE = _allowDuplicate;
                Properties.Settings.Default.SOUND = _allowSound;
                Properties.Settings.Default.COM1 = _comPort1;
                Properties.Settings.Default.COM2 = _comPort2;

                Properties.Settings.Default.Save();

                StatusMsg = "Info: Successfully updating Config info.";

                Close_Config();
            }

            // To clear error or success message.
           // dMsgTimer.Start();
        }

        private bool IsConfigErrorFree()
        {
            List<string> lstErr = new List<string>();

            // Check empty fields.
            if (string.IsNullOrEmpty(MyConverter.ToString(_comPort1)) || _comPort1 <= 0)
                lstErr.Add("COM Port 1");

            if (string.IsNullOrEmpty(MyConverter.ToString(_comPort2)) || _comPort2 < 0)
                lstErr.Add("COM Port 2");

            if (string.IsNullOrEmpty(_selected_Antenna))
                lstErr.Add("Antenna");

            if (lstErr.Count > 0)        // error.
            {
                string strErrMsg = MyConst.WARNING + Environment.NewLine +
                                    "Below values cannot leave empty | COM port cannot be <= 0." +
                                    Environment.NewLine + Environment.NewLine +
                                    string.Join(", ", lstErr.ToArray()) + ".";

                Messenger.Default.Send(strErrMsg, MsgType.MAIN_V);

                StatusMsg = "Warning: Fail to update config values!";
                return false;
            }
            else
                return true;
        }

        private void Scan_ProductTag()
        {
            // Start reading RFID tags and display related product info on screen.
          //  if (Auto_COM_Port(true))
                RfidAction(_rfCmdText);
        }

        private void Stop_RFID()
        {
            RfidAction(MyConst.STOP);
        }

        

        private void RfidAction(string actionMsg)
        {
            try
            {
                if (string.IsNullOrEmpty(MyConverter.ToString(_comPort1)) || _comPort1 <= 0)
                    return;

                // Get COM port number in byte.
                byte bPort1 = Convert.ToByte(_comPort1);

                //byte bPort2 = 0;
                //if (_comPort2 > 0)
                //    bPort2 = Convert.ToByte(_comPort2);

                if (actionMsg == MyConst.SCAN)
                {
                    // Check COM Port is valid or not.
                    if (!Check_COM_Port(_comPort1))
                        return;

                    if (dMsgTimer.IsEnabled)
                        dMsgTimer.Stop();

                    CheckOutDate = DateTime.Now.ToString("yyyy-MMM-dd  hh:mm:ss");

                    RfCmdText = MyConst.STOP;
                    // Start RFID reading.
                    if(_tagList == null)
                        TagList = new ObservableCollection<Tag>();

                    IsReady = false;
                    
                    ur1.StartRead(bPort1);

                    //if (!string.IsNullOrEmpty(MyConverter.ToString(_comPort2)) || _comPort2 > 0 && _comPort2 != _comPort1)
                    //    ur2.StartRead(bPort2);
                }
                else if (actionMsg == MyConst.STOP)
                {
                    // Stop RFID reading.
                    ur1.StopReading();
                    //ur2.StopReading();
                    
                    RfCmdText = MyConst.SCAN;

                    //CheckOutDate = "";

                    if (_tagList != null && _tagList.Count() > 0)
                        IsReady = true;
                    else
                        IsReady = false;
                }
            }
            catch (Exception e)
            {
                StatusMsg = "Error: Process failed while reading tag. Details: " + e.Message;
                Messenger.Default.Send(MyConst.ERROR + Environment.NewLine + e.Message, MsgType.MAIN_V);
            }
        }






        #region Custom Functions

        private void ShowMsg(string strMsg)
        {
            StatusMsg = strMsg.Replace(MyConst.ERROR, "Error:").Replace(Environment.NewLine, " ").Replace("Error Details", "Details");
            Messenger.Default.Send(strMsg, MsgType.MAIN_V);
            RfidAction(MyConst.STOP);
        }


        private void Ur_OnTagRead(object sender, TagArgs e)
        {
            if (e != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(delegate
                {
                    int iCount = 0;
                    int iQty = 1;

                    if (TagList == null)
                        TagList = new ObservableCollection<Tag>();
                    else
                        iCount = TagList.Count;

                    bool bExist = false;

                    if (TagList.Count > 0)
                    {
                        List<Tag> lst = TagList.ToList();

                        foreach (Tag ta in lst)
                        {
                            if (ta.Uii == e.Uii)
                            {
                                bExist = true;

                                if (_allowDuplicate)
                                {
                                    // Need to increase Qty.
                                    ta.Qty += 1;
                                    ta.ReadDate = DateTime.Now.ToString("yyyy-MM-dd");
                                    ta.ReadTime = DateTime.Now.ToString("hh:mm:ss tt");

                                    if (_allowSound)
                                        MyConst.PlaySound();
                                }
                            }
                        }

                        if (bExist)
                        {
                            TagList.Clear();
                            TagList = new ObservableCollection<Tag>(lst);
                        }
                    }

                    if (!bExist)
                    {
                        iCount++;

                        Tag t = new Tag();
                        t.Uii = e.Uii;
                        t.No = iCount;
                        t.Qty = iQty;
                        t.ReadDate = DateTime.Now.ToString("yyyy-MM-dd");
                        t.ReadTime = DateTime.Now.ToString("hh:mm:ss tt");


                        MyProduct myP = (from p in lstPs
                                         where p.Epc == t.Uii
                                         select p).SingleOrDefault();

                        if (myP != null)
                        {
                            t.Desc = myP.Desc;
                            t.Price = myP.Price;
                        }
                        else
                        {
                            // Do not add it.
                            return;
                            // If want to add unknown tag, uncomment below line.
                            // t.Desc = t.Uii;

                        }                           


                        decimal dTotal = t.Price * t.Qty;
                        TotalValue = _totalValue == null ? dTotal : _totalValue + dTotal;
                        TagList.Add(t);

                        if (_allowSound)
                            MyConst.PlaySound();
                    }



                });
            }
        }





        //private List<Tag> CloneTag(List<Tag> lstSource)
        //{
        //    List<Tag> lstReturn = new List<Tag>(lstSource.Count);

        //    lstSource.ForEach((item) =>
        //    {
        //        lstReturn.Add(new Tag(item));
        //    });

        //    return lstReturn;
        //}







        #endregion

    }
}