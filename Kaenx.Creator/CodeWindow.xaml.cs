using Kaenx.Creator.Classes;
using Kaenx.Creator.Viewer;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Import;
using Kaenx.DataContext.Import.Dynamic;
using Kaenx.DataContext.Import.Values;
using Kaenx.DataContext.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Kaenx.Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CodeWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string CodeOld { get; set; }
        public string CodeNew { get; set; }

        public CodeWindow(string page, string code)
        {
			InitializeComponent();
            CodeOld = code;
            monaco.Source = new Uri(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Data", "Monaco", page)); //"index.html"));
            Load();
        }

        private async void ClickSave(object sender, RoutedEventArgs e)
        {
            object code = await monaco.ExecuteScriptAsync($"editor.getValue();");
            CodeNew = code.ToString();
            CodeNew = CodeNew.Substring(1, CodeNew.Length -2);
            CodeNew = CodeNew.Replace("\\\"", "\"").Replace("\\'", "'").Replace("\\r\\n", "\r\n");
            this.Close();
        }

        private void ClickClose(object sender, RoutedEventArgs e)
        {
            CodeNew = CodeOld;
            this.Close();
        }

        
        private async void Load()
        {
            System.Console.WriteLine("Load");
            await monaco.EnsureCoreWebView2Async();
            await System.Threading.Tasks.Task.Delay(1000);
            System.Console.WriteLine("Ausgeführt");
            string xml = CodeOld.Replace("'", "\\'").Replace("\r\n", "\\r\\n");
            await monaco.ExecuteScriptAsync($"editor.setValue('{xml}');");
            System.Console.WriteLine($"editor.setValue('{xml}');");
        }
    }
}