using System;
using System.Windows;
using System.Windows.Data;

namespace UR21_DualAntenna_Demo.Model
{
    public class Bol2VisColl : IValueConverter
    {
        public Bol2VisColl() { }

        public bool Collapse { get; set; }
        public bool Reverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bValue = (bool)value;

            if (bValue != Reverse)
            {
                return Visibility.Visible;
            }
            else
            {
                if (Collapse)
                    return Visibility.Collapsed;
                else
                    return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;

            if (visibility == Visibility.Visible)
                return !Reverse;
            else
                return Reverse;
        }
    }

    public class Value2Visible : IValueConverter
    {
        public bool Collapse { get; set; }
        public bool Reverse { get; set; }

        public bool bVisible { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bValue = false;

            if (value != null && value.ToString().ToUpper() != "FALSE" && value.ToString().ToUpper() != "F" && value.ToString().ToUpper() != "0")
                bValue = true;

            if (bVisible)
            {
                if (bValue != Reverse)
                {
                    return Visibility.Visible;
                }
                else
                {
                    if (Collapse)
                        return Visibility.Collapsed;
                    else
                        return Visibility.Hidden;
                }
            }
            else
            {
                if (bValue != Reverse)
                    return true;
                else
                    return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (bVisible)
            {
                Visibility visibility = (Visibility)value;

                if (visibility == Visibility.Visible)
                    return !Reverse;
                else
                    return Reverse;
            }
            else
            {
                bool bValue = (bool)value;
                return Reverse & bValue;
            }

        }
    }

    public class Obj2Enable : IValueConverter
    {
        public bool Reverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string strValue = value == null ? "0" : value.ToString().Trim().ToUpper();

            bool bReturn = false;

            switch (strValue)
            {
                case "1":
                case "TRUE":
                case "T":
                    bReturn = true;
                    break;
                case "FALSE":
                case "F":
                case "":
                case "0":
                    bReturn = false;
                    break;
                default:
                    bReturn = false;
                    break;
            }

            if (Reverse)
                return !bReturn;
            else
                return bReturn;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool)value == true)
                    return "1";
                else
                    return "0";
            }
            return "0";
        }
    }

    internal static class MyConverter
    {
        internal static string ToString(object obj)
        {
            if (obj is DBNull || obj == null)
                return "";
            else
                return obj.ToString().Trim();
        }

        internal static decimal ToDecimal(object obj)
        {
            decimal dOut = 0;
            if (obj is DBNull || obj == null)
                return 0.0M;

            if (decimal.TryParse(obj.ToString().Trim(), out dOut))
                return dOut;
            else
                return 0.0M;
        }

        internal static bool ToBool(object obj)
        {
            if (obj is DBNull || obj == null)
                return false;
            else if (obj.ToString().Trim() == "1")
                return true;
            else if (obj.ToString().Trim() == "0")
                return false;
            else
            {
                bool bReturn = false;

                if (bool.TryParse(obj.ToString().Trim(), out bReturn))
                    return bReturn;
                else
                    return false;
            }
        }

        internal static int ToInt32(object obj)
        {
            int iOut = 0;
            if (obj is DBNull || obj == null)
                return 0;

            if (int.TryParse(obj.ToString().Trim(), out iOut))
                return iOut;
            else
                return 0;
        }
    }
}
