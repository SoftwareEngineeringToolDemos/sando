﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sando.UI {
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:Sando.UI.View"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:Sando.UI.View;assembly=Sando.UI.View"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:CustomControl1/>
    ///
    /// </summary>
    public class TextBlockEx : TextBlock {
        static TextBlockEx() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextBlockEx), new FrameworkPropertyMetadata(typeof(TextBlockEx)));
        }

        public static Inline GetFormattedText(DependencyObject obj) {
            return (Inline)obj.GetValue(FormattedTextProperty);
        }

        public static void SetFormattedText(DependencyObject obj, Inline value) {
            obj.SetValue(FormattedTextProperty, value);
        }

        public static readonly DependencyProperty FormattedTextProperty =
            DependencyProperty.RegisterAttached(
                "FormattedText",
                typeof(Inline),
                typeof(TextBlockEx),
                new PropertyMetadata(null, OnFormattedTextChanged));

        private static void OnFormattedTextChanged(
            DependencyObject o,
            DependencyPropertyChangedEventArgs e) {
            var textBlock = o as TextBlock;
            if(textBlock == null) return;

            var inline = (Inline)e.NewValue;
            if(inline == null) {
                textBlock.Inlines.Clear();
            } else {
                textBlock.Inlines.Add(inline);
            }
        }
    }
}
