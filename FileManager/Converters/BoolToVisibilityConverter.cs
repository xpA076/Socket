using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FileManager.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public BoolToVisibilityConverter()
           : this(true)
        {

        }

        public BoolToVisibilityConverter(bool collapsewhenInvisible)
            : base()
        {
            CollapseWhenInvisible = collapsewhenInvisible;
        }

        public bool CollapseWhenInvisible { get; set; } = true;

        public bool InverseVisible { get; set; } = false;

        public Visibility FalseVisible
        {
            get
            {
                return CollapseWhenInvisible ? Visibility.Collapsed : Visibility.Hidden;
            }
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Visible;
            return ((bool)value ^ InverseVisible) ? Visibility.Visible : FalseVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return true;
            return ((Visibility)value == Visibility.Visible) ^ InverseVisible;
        }
    }
}
