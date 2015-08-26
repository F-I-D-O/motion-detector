using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ZVI
{
    public enum Output
    {
        Source,
        Differenced,
        Greyscaled,
        Thresholded
    }
    public enum Method
    {
        TwoFrame,
        ThreeFrame,
        ChangingBackground
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        

    }
}
