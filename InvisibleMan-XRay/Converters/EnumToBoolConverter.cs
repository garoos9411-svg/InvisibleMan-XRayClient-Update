using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace InvisibleManXRay.Converters
{
    /// <summary>
    /// Converts an enum value to a boolean by comparing it with the ConverterParameter.
    /// Used for segmented controls (e.g. theme selector radio buttons) bound to a single enum property.
    /// MarkupExtension so it can be used inline: {conv:EnumToBoolConverter}
    /// </summary>
    public class EnumToBoolConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string enumValue = value.ToString();
            string targetValue = parameter.ToString();

            return string.Equals(enumValue, targetValue, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
