using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static UR21_DualAntenna_Demo.Model.NativeMethods;

namespace UR21_DualAntenna_Demo.Model
{
    internal static class MyConst
    {
        internal const string ERROR = "<< ERROR >>";
        internal const string WARNING = "<< WARNING >>";
        internal const string INFO = "<< INFO >>";

        internal const string OK = "OK";
        internal const string CANCEL = "CANCEL";

        internal const string EXIT = "EXIT";

        internal const string SCAN = "SCAN";
        internal const string STOP = "STOP";

        internal const string TITLE = "UR21 READ DEMO APP";

        internal const string xVoucher = "voucher";
        internal const string xNo = "no";
        internal const string xValue = "value";

        internal const string xTag = "tag";
        internal const string xEpc = "epc";
        internal const string xPTag = "ptag";
        internal const string xDesc = "desc";
        internal const string xPrice = "price";

        internal static void PlaySound()
        {
            using (var soundPlayer = new SoundPlayer(Properties.Resources.soundOK))
            {
                soundPlayer.Play(); // can also use soundPlayer.PlaySync()
            }
        }
    }

    public enum MsgType
    {
        MAIN_V,
        MAIN_V_CONFIRM,
        MAIN_VM,
        TERMINATOR
    }

    public enum ErrCode
    {
        Err_Null = 0,
        Err_ReadXML_Voucher = 1,
        Err_ReadXML_Product = 2
    }

    internal static class General
    {
        internal static string gReplyMsg(ErrCode errCode, string strMainmsg, bool? bErr = null)
        {
            string strTitle = "";
            string strErrCode = "";

            if (bErr == null)
                strTitle = MyConst.INFO;
            else if (bErr == true)
                strTitle = MyConst.ERROR;
            else
                strTitle = MyConst.WARNING;

            strErrCode = ((int)errCode).ToString();

            if (strErrCode == "0")
                return strTitle + Environment.NewLine + Environment.NewLine + strMainmsg;
            else if (bErr == true)
                return strTitle + Environment.NewLine + Environment.NewLine +
                       "Error Code: " + strErrCode + Environment.NewLine +
                       "Message: " + strMainmsg;
            else
                return strTitle + Environment.NewLine + Environment.NewLine +
                       "Message: " + strMainmsg;
        }

        internal static string gGetVersion()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetEntryAssembly();
            return asm.GetName().Version.Major.ToString() + "." + asm.GetName().Version.Minor.ToString() + "." + asm.GetName().Version.Revision.ToString();
        }

        internal static string gGetExeLocation()
        {
            string strCompany = ((System.Reflection.AssemblyCompanyAttribute)Attribute.GetCustomAttribute(
                                System.Reflection.Assembly.GetExecutingAssembly(), typeof(System.Reflection.AssemblyCompanyAttribute), false)).Company;
            string strPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), strCompany);
            strPath = Path.Combine(strPath, System.Reflection.Assembly.GetEntryAssembly().GetName().Name);

            return strPath;
        }


        internal static string gBytesToHex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        internal static string gHexToString(string strHex)
        {
            string strData = "";
            while (strHex.Length > 0)
            {
                strData += Convert.ToChar(Convert.ToInt32(strHex.Substring(0, 2), 16)).ToString();
                strHex = strHex.Substring(2, strHex.Length - 2);
            }
            return strData;

        }

        internal static bool gSaveData(string strFileName, List<Tag> lst)
        {
            try
            {
                using (StreamWriter objWriter = new StreamWriter(strFileName, false))
                {
                    objWriter.AutoFlush = true;

                    objWriter.WriteLine("No,UII,Qty,Date,Time");

                    foreach (Tag t in lst)
                        objWriter.WriteLine(t.No + "," + t.Uii + "," + t.Qty + "," + t.ReadDate + "," + t.ReadTime);
                }
                return true;
            }
            catch (Exception e)
            {
                Messenger.Default.Send(MyConst.ERROR + Environment.NewLine + "An error occured while saving data to file!" +
                                       Environment.NewLine + Environment.NewLine + "Error Details: " + e.Message, MsgType.MAIN_VM);
                return false;
            }
            finally
            {
                GC.Collect();
            }
        }
    }


    

    public class Tag
    {
        public string Uii { get; set; }
        public int No { get; set; }
        public int Qty { get; set; }
        public string Desc { get; set; }
        public decimal Price { get; set; }

        public string ReadDate { get; set; }
        public string ReadTime { get; set; }

        public Tag() { }

        public Tag(Tag t)
        {
            Uii = t.Uii;
            No = t.No;
            Qty = t.Qty;
            Desc = t.Desc;
            Price = t.Price;
            ReadDate = t.ReadDate;
            ReadTime = t.ReadTime;
        }
    }


    // Create custom C# event >> https://www.codeproject.com/Articles/9355/Creating-advanced-C-custom-events
    public class TagArgs : EventArgs
    {
        public TagArgs()
        {
            Uii = null;
            Qty = 0;
        }

        //public string uii { get; set; }
        //public int qty { get; set; }
        public int qty;
        public int Qty
        {
            get { return qty; }
            set { qty = value; }
        }


        private string uii;
        public string Uii
        {
            get { return uii; }
            set { uii = value; }
        }

    }

    unsafe public class Ur21
    {
        public static UiiData ud = new UiiData();

        // Create custom C# event >> https://www.codeproject.com/Articles/9355/Creating-advanced-C-custom-events
        public delegate void TagHandler(object sender, TagArgs e);
        public static event TagHandler OnTagRead;

        Thread t1;

        byte bPort = 0;

        bool bTrue = false;

        public bool StartRead(byte byPort)
        {
            bPort = byPort;
            t1 = new Thread(ReadTag);
            t1.Start();

            return true;
        }

        internal void StopReading()
        {
            bTrue = false;
            if (t1 != null && t1.IsAlive)
                t1.Abort();
        }


        public void ReadTag()
        {
            try
            {
                // int iCounter = 0;
                bTrue = true;
                uint iReturn;
                uint iReadCount;
                uint iRemainCount;
                uint iBufCount = 500;
                int i = 0;

                // More info on Marshal class >> https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal?view=netframework-4.5.1
                // More info on IntPtr >> https://docs.microsoft.com/en-us/dotnet/api/system.intptr?view=netframework-4.5.1

                // More info on what is void* in C# >> https://stackoverflow.com/questions/15527985/what-is-void-in-c
                IntPtr uiiBuf = Marshal.AllocHGlobal(sizeof(UiiData) * (int)iBufCount);
                //IntPtr uiiBuf = Marshal.AllocCoTaskMem(sizeof(UiiData) * (int)iBufCount);

                iReturn = UtsOpen(bPort);
                if (iReturn != 0)
                    throw new Exception("Open port error-" + iReturn.ToString("X2"));

                iReturn = UtsAbort(bPort);
                if (iReturn != 0)
                    throw new Exception("Abort port error-" + iReturn.ToString("X2"));

                while (bTrue)
                {
                    iReturn = UtsReadUii(bPort);
                    if (iReturn != 0)        // There is an error, but keep going.
                        continue;

                    do
                    {
                        iReturn = UtsGetUii(bPort, (void*)uiiBuf, iBufCount, out iReadCount, out iRemainCount);

                        if (iReturn == 1)
                        {
                            iRemainCount = 1;
                            continue;
                        }

                        //Console.WriteLine("Read: " + iReadCount);

                        for (i = 0; i < iReadCount; i++)
                        {
                            TagArgs e = new TagArgs();

                            // IntPtr to Structure >> https://stackoverflow.com/a/27680642/770989
                            ud = (UiiData)Marshal.PtrToStructure(uiiBuf + (sizeof(UiiData) * i), typeof(UiiData));
                            //                            ud = (UiiData)Marshal.PtrToStructure((IntPtr)((uint)uiiBuf + (sizeof(UiiData) * i)), typeof(UiiData));

                            // More info on fixed >> https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/fixed-statement
                            // Reference from here >> https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs1666
                            // If we do not use fixed, it will throw CS1666 error.
                            fixed (UiiData* uf = &ud)
                            {
                                byte[] bUii = new byte[uf->length];


                                // How to get IntPtr from byte[] >> https://stackoverflow.com/questions/537573/how-to-get-intptr-from-byte-in-c-sharp
                                // Another example >> https://stackoverflow.com/a/27680642/770989
                                Marshal.Copy((IntPtr)uf->uii, bUii, 0, (int)uf->length);

                                // Console.WriteLine(BitConverter.ToString(br));
                                e.Uii = BitConverter.ToString(bUii).Replace("-", "");
                            }

                            //byte[] bUii = new byte[ud.length];
                            //Marshal.Copy((IntPtr)ud.uii, bUii, 0, ud.length);

                            //Console.WriteLine("  Read: " + i);

                            if (!OnTagRead.Equals(null))
                                OnTagRead(this, e);
                            // Console.WriteLine(General.HexToString(BitConverter.ToString(br).Replace("-", "")));
                        }
                    }
                    while ((iRemainCount > 0) && bTrue);
                }

                iReturn = UtsClose(bPort);
                if (iReturn > 0)
                    throw new Exception("Close port error-" + iReturn.ToString("X2"));
            }
            catch (ThreadAbortException)
            {
                UtsClose(bPort);
            }
            catch (Exception e)
            {
                Messenger.Default.Send(MyConst.ERROR + Environment.NewLine + "An error occured while running UR2x Api!" +
                                        Environment.NewLine + Environment.NewLine + "Error Details: " + e.Message, MsgType.MAIN_VM);
            }
        }

    }


    //unsafe public class Ur22
    //{
    //    public static UiiData ud = new UiiData();

    //    // Create custom C# event >> https://www.codeproject.com/Articles/9355/Creating-advanced-C-custom-events
    //    public delegate void TagHandler(object sender, TagArgs e);
    //    public static event TagHandler OnTagRead;

    //    Thread t2;

    //    byte bPort = 0;

    //    bool bTrue = false;

    //    public bool StartRead(byte byPort)
    //    {
    //        bPort = byPort;
    //        t2 = new Thread(ReadTag2);
    //        t2.Start();

    //        return true;
    //    }

    //    internal void StopReading()
    //    {
    //        bTrue = false;
    //        if (t2 != null && t2.IsAlive)
    //            t2.Abort();
    //    }


    //    public void ReadTag2()
    //    {
    //        try
    //        {
    //            // int iCounter = 0;
    //            bTrue = true;
    //            uint iReturn;
    //            uint iReadCount;
    //            uint iRemainCount;
    //            uint iBufCount = 500;
    //            int i = 0;

    //            // More info on Marshal class >> https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal?view=netframework-4.5.1
    //            // More info on IntPtr >> https://docs.microsoft.com/en-us/dotnet/api/system.intptr?view=netframework-4.5.1

    //            // More info on what is void* in C# >> https://stackoverflow.com/questions/15527985/what-is-void-in-c
    //            IntPtr uiiBuf = Marshal.AllocHGlobal(sizeof(UiiData) * (int)iBufCount);
    //            //IntPtr uiiBuf = Marshal.AllocCoTaskMem(sizeof(UiiData) * (int)iBufCount);

    //            iReturn = UtsOpen(bPort);
    //            if (iReturn != 0)
    //                throw new Exception("Open port error-" + iReturn.ToString("X2"));

    //            iReturn = UtsAbort(bPort);
    //            if (iReturn != 0)
    //                throw new Exception("Abort port error-" + iReturn.ToString("X2"));

    //            while (bTrue)
    //            {
    //                iReturn = UtsReadUii(bPort);
    //                if (iReturn != 0)        // There is an error, but keep going.
    //                    continue;

    //                do
    //                {
    //                    iReturn = UtsGetUii(bPort, (void*)uiiBuf, iBufCount, out iReadCount, out iRemainCount);

    //                    if (iReturn == 1)
    //                    {
    //                        iRemainCount = 1;
    //                        continue;
    //                    }

    //                    //Console.WriteLine("Read: " + iReadCount);

    //                    for (i = 0; i < iReadCount; i++)
    //                    {
    //                        TagArgs e = new TagArgs();

    //                        // IntPtr to Structure >> https://stackoverflow.com/a/27680642/770989
    //                        ud = (UiiData)Marshal.PtrToStructure(uiiBuf + (sizeof(UiiData) * i), typeof(UiiData));
    //                        //                            ud = (UiiData)Marshal.PtrToStructure((IntPtr)((uint)uiiBuf + (sizeof(UiiData) * i)), typeof(UiiData));

    //                        // More info on fixed >> https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/fixed-statement
    //                        // Reference from here >> https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs1666
    //                        // If we do not use fixed, it will throw CS1666 error.
    //                        fixed (UiiData* uf = &ud)
    //                        {
    //                            byte[] bUii = new byte[uf->length];


    //                            // How to get IntPtr from byte[] >> https://stackoverflow.com/questions/537573/how-to-get-intptr-from-byte-in-c-sharp
    //                            // Another example >> https://stackoverflow.com/a/27680642/770989
    //                            Marshal.Copy((IntPtr)uf->uii, bUii, 0, (int)uf->length);

    //                            // Console.WriteLine(BitConverter.ToString(br));
    //                            e.Uii = BitConverter.ToString(bUii).Replace("-", "");
    //                        }

    //                        //byte[] bUii = new byte[ud.length];
    //                        //Marshal.Copy((IntPtr)ud.uii, bUii, 0, ud.length);

    //                        //Console.WriteLine("  Read: " + i);

    //                        if (!OnTagRead.Equals(null))
    //                            OnTagRead(this, e);
    //                        // Console.WriteLine(General.HexToString(BitConverter.ToString(br).Replace("-", "")));
    //                    }
    //                }
    //                while ((iRemainCount > 0) && bTrue);
    //            }

    //            iReturn = UtsClose(bPort);
    //            if (iReturn > 0)
    //                throw new Exception("Close port error-" + iReturn.ToString("X2"));
    //        }
    //        catch (ThreadAbortException)
    //        {
    //            UtsClose(bPort);
    //        }
    //        catch (Exception e)
    //        {
    //            Messenger.Default.Send(MyConst.ERROR + Environment.NewLine + "An error occured while running UR2x Api!" +
    //                                    Environment.NewLine + Environment.NewLine + "Error Details: " + e.Message, MsgType.MAIN_VM);
    //        }
    //    }

    //}


    unsafe public class NativeMethods
    {
        // Data size definition
        public const int PC_SIZE = 2;
        public const int UII_SIZE = 62;
        public const int PWD_SIZE = 4;
        public const int TAGDATA_SIZE = 256;

        // Structure definition
        // UII data structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe public struct UiiData
        {
            public uint length;                 // Effective data length of UII information stored in uii
            public fixed byte pc[PC_SIZE];      // PC information on RF tag obtained
            public fixed byte uii[UII_SIZE];    // UII information on RF tag obtained. Stored from the head, 0x00 stored beyond the length specified by length
        };


        // UII data structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe public struct UiiDataInfo
        {
            public uint length;                 // Effective data length of UII information stored in uii
            public fixed byte pc[PC_SIZE];      // PC information on RF tag obtained
            public fixed byte uii[UII_SIZE];    // UII information on RF tag obtained. Stored from the head, 0x00 stored beyond the length specified by length
            public short rssi;                  // Stores the electric field strength value that received the read tag
            public ushort antenna;              // Stores number of antenna that received the read tag
        };


        // Tag data structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe public struct RESULTDATA
        {
            public ushort result;                   // Result of communication with RF tag
            public fixed byte reserved[2];          // Reserved (0x00,0x00 fixed)
            public uint uiilength;                  // Effective data length of UII information stored in uii
            public uint datalength;                 // Effective data length of data from memory stored in data
            public fixed byte pc[PC_SIZE];          // PC information on RF tag obtained
            public fixed byte uii[UII_SIZE];        // UII information on RF tag obtained. Stored from the head, 0x00 stored beyond the length specified by uiilength
            public fixed byte data[TAGDATA_SIZE];   // Data from memory on read RF tag. Stored from the head, 0x00 stored beyond the length specified by datalength
        };


        // Tag data structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe public struct RESULTDATA2
        {
            public ushort result;                   // Result of communication with RF tag
            public fixed byte reserved[2];          // Reserved (0x00,0x00 fixed)
            public uint uiilength;                  // Effective data length of UII information stored in uii
            public uint datalength;                 // Effective data length of data from memory stored in data
            public fixed byte pc[PC_SIZE];          // PC information on RF tag obtained
            public fixed byte uii[UII_SIZE];        // UII information on RF tag obtained. Stored from the head, 0x00 stored beyond the length specified by uiilength
            public fixed byte data[TAGDATA_SIZE];   // Data from memory on read RF tag. Stored from the head, 0x00 stored beyond the length specified by datalength
            public short rssi;                      // Stores the electric field strength value that received the read tag
            public ushort antenna;                  // Stores number of antenna that received the read tag
        };


        //Tag communication Read from Memory structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe public struct READPARAM
        {
            public byte bank;                           // Bank area to be read
            public byte reserved;                       // Reserved (0x00 fixed)
            public ushort size;                         // Reading size(2-256, only even number acceptable)
            public ushort ptr;                          // Pointer to the beginning of reading (only even number acceptable)
            public fixed byte accesspwd[PWD_SIZE];      // Password for authentication of RF tag (ALL 0x00: RF tag not authenticated)
        };


        //Tag communication Write to Memory structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe public struct WRITEPARAM
        {
            public byte bank;                           // Bank area to be read
            public byte reserved;                       // Reserved (0x00 fixed)
            public ushort size;                         // Writing size(2-64, even number only)
            public ushort ptr;                          // Pointer to the beginning of reading (only even number acceptable)
            public fixed byte accesspwd[PWD_SIZE];      // Password for authentication of RF tag (ALL 0x00: RF tag not authenticated)
            public fixed byte data[TAGDATA_SIZE];       // Data written to RF tag. Stored from the head, set 0x00 stored beyond the length specified by size
        };


        // Tag communication Lock structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe public struct LOCKPARAM
        {
            public byte target;                         // To be locked
            public byte locktype;                       // Locked state after change
            public fixed byte accesspwd[PWD_SIZE];      // Password for authentication of RF tag (ALL 0x00: RF tag not authenticated)
        };


        // Tag communication Kill structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe public struct KILLPARAM
        {
            public fixed byte killpwd[PWD_SIZE];        // Password for killing RF tag (ALL 0x00: RF tag cannot be killed)
        };


        // TLV parameter structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe public struct TLVPARAM
        {
            public ushort tag;              // Parameter tag number
            public ushort length;           // Value length (4 bytes)
            public uint value;              // Parameter setting
        };


        // Device list structure
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe public struct DEVLIST
        {
            public uint IPaddress;          // IP address
            public ushort TcpPort;          // Connection destination Tcp port number
            public ushort DevNo;            // Terminal control number
            public uint Status;             // Status of terminal 0x00000000: not used (before opening) 0x00000001:in use (while open)
        };


        // Interface to UtsOpen
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsOpen(byte Port);


        // Interface to UtsClose
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsClose(byte Port);


        // Interface to UtsAbort
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsAbort(byte Port);


        // Interface to UtsReadUii
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsReadUii(byte Port);


        // Interface to UtsGetUii
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        unsafe internal static extern uint UtsGetUii(byte Port, void* UIIBUF, uint BufCount, out uint GetCount, out uint RestCount);


        // Interface to UtsGetUiiInfo
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        unsafe internal static extern uint UtsGetUiiInfo(byte Port, void* UIIBUFINFO, uint BufCount, out uint GetCount, out uint RestCount);


        // Interface to UtsStartContinuousRead
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsStartContinuousRead(byte Port);


        // Interface to UtsStartContinuousReadEx
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsStartContinuousReadEx(byte Port);


        // Interface to UtsStopContinuousRead
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsStopContinuousRead(byte Port);


        // Interface to UtsGetContinuousReadResult
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        unsafe internal static extern uint UtsGetContinuousReadResult(byte Port, void* UIIBUF, uint BufCount, out uint GetCount);


        // Interface to UtsGetContinuousReadResultInfo
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        unsafe internal static extern uint UtsGetContinuousReadResultInfo(byte Port, void* UIIBUFINFO, uint BufCount, out uint GetCount);


        // Interface to UtsStartTagComm
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        unsafe internal static extern uint UtsStartTagComm(byte Port, byte TagCmd, ushort Antenna, void* Param, byte ListEnable, ushort ListNum, void* UIIBUF);


        // Interface to UtsGetTagCommResult
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        unsafe internal static extern uint UtsGetTagCommResult(byte Port, void* RESULTBUF, uint BufCount, out uint GetCount, out uint RestCount);

        // Interface to UtsGetTagCommResultInfo
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        unsafe internal static extern uint UtsGetTagCommResultInfo(byte Port, void* RESULTBUFINFO, uint BufCount, out uint GetCount, out uint RestCount);


        // Interface to UtsSetTagList
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern uint UtsSetTagList(byte Port, byte Type, ushort ListNum, void* UIIBUF);


        // Interface to UtsGetVersions
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsGetVersions(byte Port, out byte OSVer, out byte MainVer, out byte RFVer, out byte ChipVer, out byte OEMver);


        // Interface to UtsGetProductNo
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsGetProductNo(byte Port, out byte MainNo, out byte RFNo);


        // Interface to UtsGetParameter
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsGetParameter(byte Port, ushort Tag, out TLVPARAM TLVDATA);


        // Interface to UtsSetParameter
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsSetParameter(byte Port, TLVPARAM TLVDATA);


        // Interface to UtsLoadParameter
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsLoadParameter(byte Port, ref byte FilePath);


        // Interface to UtsUpdateDevice
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern uint UtsUpdateDevice(byte Port, ref byte FilePath);


        // Interface to UtsInitialReset
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsInitialReset(byte Port);


        // Interface to UtsCheckAlive
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsCheckAlive(byte Port);


        // Interface to UtsGetNetworkConfig
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsGetNetworkConfig(byte Port, out uint IPaddress, out uint SubnetMask, out uint Gateway);


        // Interface to UtsSetNetworkConfig
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsSetNetworkConfig(byte Port, uint IPaddress, uint SubnetMask, uint Gateway);


        // Interface to UtsCreateLanDevice
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsCreateLanDevice(uint IPaddress, ushort TcpPort, out ushort DevNo);

        // Interface to UtsDeleteLanDevice
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsDeleteLanDevice(ushort DevNo);


        // Interface to UtsSetCurrentLanDevice
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsSetCurrentLanDevice(ushort DevNo);


        // Interface to UtsGetCurrentLanDevice
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsGetCurrentLanDevice(out ushort DevNo);


        // Interface to UtsGetLanDeviceInfo
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint UtsGetLanDeviceInfo(ushort DevNo, out uint IPaddress, out ushort TcpPort, out uint Status);


        // Interface to UtsListLanDevice
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        unsafe internal static extern uint UtsListLanDevice(out ushort DevCount, void* DEVICELIST);


        // Interface to UtsSetLanDevice
        [DllImport("RfidTs.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        unsafe internal static extern uint UtsSetLanDevice(ushort DevCount, void* DEVICELIST);
    }

}
