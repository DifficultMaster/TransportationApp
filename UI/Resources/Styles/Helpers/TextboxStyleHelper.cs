using System.Windows;
using System.Windows.Controls;

namespace AppClient.UI
{
    public class TextboxStyleHelper : DependencyObject
    {
        public static readonly DependencyProperty IsTextValidProperty =
            DependencyProperty.RegisterAttached(
                "IsTextValid",
                typeof(bool?),
                typeof(TextboxStyleHelper),
                new PropertyMetadata(null));

        public static bool? GetIsTextValid(DependencyObject obj)
        {
            return (bool?)obj.GetValue(IsTextValidProperty);
        }

        public static void SetIsTextValid(DependencyObject obj, bool? value)
        {
            obj.SetValue(IsTextValidProperty, value);
        }

        public TextboxStyleHelper() { }
    }
}

