﻿using Sando.DependencyInjection;
using Sando.UI.View;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
namespace Sando.UI.View.Search.Converters
{
    public class BackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {       
            //TODO:Check this later
            //if (value!=null && SearchViewControl.PleaseAddDirectoriesMessage.Equals(value.ToString()))
            //    return Brushes.MistyRose;
            //else
            //{
            //    return ColorGenerator.GetToolBackgroundColor();         
            //}

            return ColorGenerator.GetToolBackgroundColor(); 
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
