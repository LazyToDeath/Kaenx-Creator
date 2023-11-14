﻿using Kaenx.Creator.Classes;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using System.Xml.Linq;
using Uno.UI.Xaml;

namespace Kaenx.Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public static MainWindow Instance { get; set; }

        private Models.MainModel _general;

        public Models.MainModel General
        {
            get { return _general; }
            set { _general = value; Changed("General"); }
        }

        private ObservableCollection<Models.MaskVersion> bcus;
        public ObservableCollection<Models.MaskVersion> BCUs
        {
            get { return bcus; }
            set { bcus = value; Changed("BCUs"); }
        }

        private static ObservableCollection<Models.DataPointType> dpts;
        public static ObservableCollection<Models.DataPointType> DPTs
        {
            get { return dpts; }
            set { dpts = value; }
        }

        public ObservableCollection<Models.ExportItem> Exports { get; set; } = new ObservableCollection<Models.ExportItem>();
        public ObservableCollection<Models.PublishAction> PublishActions { get; set; } = new ObservableCollection<Models.PublishAction>();

        public event PropertyChangedEventHandler PropertyChanged;

        private List<Models.EtsVersion> EtsVersions = new List<Models.EtsVersion>() {
            new Models.EtsVersion(11, "ETS 4.0 (11)", "4.0"),
            new Models.EtsVersion(12, "ETS 5.0 (12)", "5.0"),
            new Models.EtsVersion(13, "ETS 5.1 (13)", "5.1"),
            new Models.EtsVersion(14, "ETS 5.6 (14)", "5.6"),
            new Models.EtsVersion(20, "ETS 5.7 (20)", "5.7"),
            new Models.EtsVersion(21, "ETS 6.0 (21)", "6.0"),
            new Models.EtsVersion(22, "ETS 6.1 (22)", "6.1")
        };
        
        private int VersionCurrent = 8;


        public MainWindow()
        {
            Instance = this;
            string lang = Properties.Settings.Default.language;
            if(lang != "def")
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(lang);
            InitializeComponent();
            this.DataContext = this;
            LoadBcus();
            LoadDpts();
            CheckLangs();
            CheckOutput();
            CheckEtsVersions();
            LoadTemplates();

            
            MenuDebug.IsChecked = Properties.Settings.Default.isDebug;
            MenuUpdate.IsChecked = Properties.Settings.Default.autoUpdate;
            if(Properties.Settings.Default.autoUpdate) AutoCheckUpdate();

            if(!string.IsNullOrEmpty(App.FilePath))
            {
                DoOpen(App.FilePath);
                MenuSaveBtn.IsEnabled = true;
            }
        }

        private async void AutoCheckUpdate()
        {
            System.Diagnostics.Debug.WriteLine("Checking Auto Update");
            (bool update, string vers) response = await CheckUpdate();
            if(response.update)
            {
                if(MessageBoxResult.Yes == MessageBox.Show(string.Format(Properties.Messages.update_new, response.vers), Properties.Messages.update_title, MessageBoxButton.YesNo, MessageBoxImage.Question))
                {
                    Process.Start(new ProcessStartInfo("https://github.com/OpenKNX/Kaenx-Creator/releases/latest") { UseShellExecute = true });
                }
            } 
        }

        public void GoToItem(object item, object module)
        {
            if(module != null)
            {
                VersionTabs.SelectedIndex = 7;
                int index2 = item switch {
                    Models.Union => 4,
                    Models.Parameter => 7,
                    Models.ParameterRef => 8,
                    Models.ComObject => 9,
                    Models.ComObjectRef => 10,
                    Models.Dynamic.IDynItems => 14,
                    _ => -1
                };

                (VersionTabs.SelectedContent as ISelectable).ShowItem(module);
                (VersionTabs.SelectedContent as ISelectable).ShowItem(item);
                return;
            }


            int index = item switch{
                Models.ParameterType => 4,
                Models.Union => 5,
                Models.Module => 7,
                Models.Parameter => 8,
                Models.ParameterRef => 9,
                Models.ComObject => 10,
                Models.ComObjectRef => 11,
                Models.Dynamic.IDynItems => 15,
                _ => -1
            };

            if(index == -1) return;
            VersionTabs.SelectedIndex = index;
            ((VersionTabs.Items[index] as TabItem).Content as ISelectable).ShowItem(item);
        }

        private void CheckEtsVersions() {
            foreach(Models.EtsVersion v in EtsVersions)
                v.IsEnabled = !string.IsNullOrEmpty(GetAssemblyPath(v.Number));
            NamespaceSelection.ItemsSource = EtsVersions;
        }

        private void LoadTemplates() {
            foreach(string path in Directory.GetFiles(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates")))
            {
                string name = path.Substring(path.LastIndexOf('\\')+1);
                name = name.Substring(0, name.IndexOf('.'));
                MenuItem item = new MenuItem() { Header = name};
                item.Tag = path;
                item.Click += ClickOpenTemplate;
                MenuLoad.Items.Add(item);
            }
        }

        private void CheckLangs()
        {
            string lang = Properties.Settings.Default.language;
            bool wasset = false;
            foreach(UIElement ele in MenuLang.Items)
            {
                if(ele is MenuItem item)
                {
                    item.IsChecked = item.Tag?.ToString() == lang;
                    if(item.IsChecked) wasset = true;
                }
            }

            if(!wasset)
                (MenuLang.Items[2] as MenuItem).IsChecked = true;
        }

        private void CheckOutput()
        {
            string outp = Properties.Settings.Default.Output;

            bool valid = false;
            foreach(UIElement ele in MenuOutput.Items)
            {
                if(ele is MenuItem item)
                {
                    if(item.Tag?.ToString() == outp)
                    {
                        valid = true;
                        break;
                    }
                }
            }
            if(!valid)
            {
                outp = "exe";
                Properties.Settings.Default.Output = outp;
                Properties.Settings.Default.Save();
            }


            bool wasset = false;
            foreach(UIElement ele in MenuOutput.Items)
            {
                if(ele is MenuItem item)
                {
                    item.IsChecked = item.Tag?.ToString() == outp;
                    if(item.IsChecked) wasset = true;
                }
            }
            
            if(!wasset)
                (MenuOutput.Items[0] as MenuItem).IsChecked = true;
        }

        private void ClickNew(object sender, RoutedEventArgs e)
        {
            General = new Models.MainModel() { ImportVersion = VersionCurrent, Guid = Guid.NewGuid().ToString() };
            var currentLang = System.Threading.Thread.CurrentThread.CurrentUICulture.IetfLanguageTag;
            if(!ImportHelper._langTexts.ContainsKey(currentLang))
                if(currentLang.Contains("-"))
                    currentLang = currentLang.Split("-")[0];

            if(!currentLang.Contains("-"))
            {
                currentLang = ImportHelper._langTexts.Keys.FirstOrDefault(l => l.Split("-")[0] == currentLang);
                if(string.IsNullOrEmpty(currentLang)) currentLang = "en-US";
            }
            General.Languages.Add(new Models.Language(System.Threading.Thread.CurrentThread.CurrentUICulture.DisplayName, currentLang));
            General.Catalog.Add(new Models.CatalogItem() { Name = Properties.Messages.main_def_cat });

            foreach(Models.Language lang in General.Languages)
            {
                if(!General.Info.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    General.Info.Text.Add(new Models.Translation(lang, ""));
                if(!General.Info.Description.Any(t => t.Language.CultureCode == lang.CultureCode))
                    General.Info.Description.Add(new Models.Translation(lang, ""));
            }

            
            General.Application.Languages.Add(new Models.Language(System.Threading.Thread.CurrentThread.CurrentUICulture.DisplayName, currentLang));
            foreach(Models.Language lang in General.Languages)
            {
                if(!General.Application.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    General.Application.Text.Add(new Models.Translation(lang, ""));
            }

            General.Application.Dynamics.Add(new Models.Dynamic.DynamicMain());

            SetButtons(true);
            MenuSaveBtn.IsEnabled = false;
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        private void LoadBcus()
        {
            string jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "maskversion.json");
            string xmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "maskversion.xml");
            if (System.IO.File.Exists(jsonPath))
            {
                BCUs = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<Models.MaskVersion>>(System.IO.File.ReadAllText(jsonPath));
            } else
            {
                BCUs = new ObservableCollection<Models.MaskVersion>();
                XDocument xdoc = XDocument.Load(xmlPath);
                foreach(XElement xmask in xdoc.Root.Elements())
                {
                    Models.MaskVersion mask = new Models.MaskVersion();
                    mask.Id = xmask.Attribute("Id").Value;
                    mask.MediumTypes = xmask.Attribute("MediumTypeRefId").Value;
                    if(xmask.Attribute("OtherMediumTypeRefId") != null) mask.MediumTypes += " " + xmask.Attribute("OtherMediumTypeRefId").Value;

                    string eleStr = xmask.ToString();
                    if (eleStr.Contains("<Procedure ProcedureType=\"Load\""))
                    {
                        XElement prodLoad = xmask.Descendants(XName.Get("Procedure")).First(p => p.Attribute("ProcedureType")?.Value == "Load");
                        if (prodLoad.ToString().Contains("<LdCtrlMerge"))
                            mask.Procedure = Models.ProcedureTypes.Merged;
                        else
                            mask.Procedure = Models.ProcedureTypes.Default;
                    } else
                    {
                        mask.Procedure = Models.ProcedureTypes.Product;
                    }


                    if(mask.Procedure != Models.ProcedureTypes.Product)
                    {
                        if (eleStr.Contains("<LdCtrlAbsSegment"))
                        {
                            mask.Memory = Models.MemoryTypes.Absolute;
                        }
                        else if (eleStr.Contains("<LdCtrlWriteRelMem"))
                        {
                            mask.Memory = Models.MemoryTypes.Relative;
                        }
                        else if (eleStr.Contains("<LdCtrlWriteMem"))
                        {
                            mask.Memory = Models.MemoryTypes.Absolute;
                        }
                        else
                        {
                            continue;
                        }
                    }


                    if(xmask.Descendants(XName.Get("Procedures")).Count() > 0) {
                        foreach(XElement xproc in xmask.Element(XName.Get("HawkConfigurationData")).Element(XName.Get("Procedures")).Elements()) {
                            Models.Procedure proc = new Models.Procedure();
                            proc.Type = xproc.Attribute("ProcedureType").Value;
                            proc.SubType = xproc.Attribute("ProcedureSubType").Value;

                            StringBuilder sb = new StringBuilder();

                            foreach (XNode node in xproc.Nodes())
                                sb.Append(node.ToString() + "\r\n");

                            proc.Controls = sb.ToString();
                            mask.Procedures.Add(proc);
                        }
                    }

                    BCUs.Add(mask);
                }

                System.IO.File.WriteAllText(jsonPath, Newtonsoft.Json.JsonConvert.SerializeObject(BCUs));
            }
        }

        private void LoadDpts()
        {
            string jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "datapoints.json");
            string xmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "datapoints.xml");
            if (System.IO.File.Exists(jsonPath))
            {
                DPTs = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<Models.DataPointType>>(System.IO.File.ReadAllText(jsonPath));
            } else
            {
                DPTs = new ObservableCollection<Models.DataPointType>();
                XDocument xdoc = XDocument.Load(xmlPath);
                IEnumerable<XElement> xdpts = xdoc.Descendants(XName.Get("DatapointType"));
                
                DPTs.Add(new Models.DataPointType() {
                    Name = Properties.Messages.main_empty_dpt,
                    Number = "0",
                    Size = 0
                });

                foreach(XElement xdpt in xdpts)
                {
                    Models.DataPointType dpt = new Models.DataPointType();
                    dpt.Name = xdpt.Attribute("Name").Value + " " + xdpt.Attribute("Text").Value;
                    dpt.Number = xdpt.Attribute("Number").Value;
                    dpt.Size = int.Parse(xdpt.Attribute("SizeInBit").Value);

                    IEnumerable<XElement> xsubs = xdpt.Descendants(XName.Get("DatapointSubtype"));

                    foreach(XElement xsub in xsubs)
                    {
                        Models.DataPointSubType dpst = new Models.DataPointSubType();
                        dpst.Name = dpt.Number + "." + Fill(xsub.Attribute("Number").Value, 3, "0") + " " + xsub.Attribute("Text").Value;
                        dpst.Number = xsub.Attribute("Number").Value;
                        dpst.ParentNumber = dpt.Number;
                        dpt.SubTypes.Add(dpst);
                    }

                    DPTs.Add(dpt);
                }


                System.IO.File.WriteAllText(jsonPath, Newtonsoft.Json.JsonConvert.SerializeObject(DPTs));
            }
        }

        private string Fill(string input, int length, string fill)
        {
            for(int i = input.Length; i < length; i++)
            {
                input = fill + input;
            }
            return input;
        }

        #region Clicks

        #region Clicks Add/Remove

        private void ClickAddHardDevice(object sender, RoutedEventArgs e)
        {
            Models.Hardware hard = (sender as Button).DataContext as Models.Hardware;
            hard.Devices.Add(new Models.Device());
        }
/*
        private void ClickAddHardApp(object sender, RoutedEventArgs e)
        {
            if(InHardApp.SelectedItem == null)
            {
                MessageBox.Show(Properties.Messages.main_add_hard_error, Properties.Messages.main_add_hard_error_title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Models.Hardware hard = (sender as Button).DataContext as Models.Hardware;
            if(!hard.Apps.Contains(InHardApp.SelectedItem as Models.Application)) {
                hard.Apps.Add(InHardApp.SelectedItem as Models.Application);
            }
            InHardApp.SelectedItem = null;
        }

        private void ClickAddVersion(object sender, RoutedEventArgs e)
        {
            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersion newVer = new Models.AppVersion() { Name = app.Name };
            var currentLang = System.Threading.Thread.CurrentThread.CurrentUICulture.IetfLanguageTag;
            if(!ImportHelper._langTexts.ContainsKey(currentLang))
                if(currentLang.Contains("-"))
                    currentLang = currentLang.Split("-")[0];

            if(!currentLang.Contains("-"))
            {
                currentLang = ImportHelper._langTexts.Keys.FirstOrDefault(l => l.Split("-")[0] == currentLang);
                if(string.IsNullOrEmpty(currentLang)) currentLang = "en-US";
            }
            Models.Language lang = new Models.Language(System.Threading.Thread.CurrentThread.CurrentUICulture.DisplayName, currentLang);
            newVer.Languages.Add(lang);
            newVer.DefaultLanguage = lang.CultureCode;
            newVer.Text.Add(new Models.Translation(lang, "Dummy"));
            newVer.Dynamics.Add(new Models.Dynamic.DynamicMain());

            if(app.Mask.Procedure == Models.ProcedureTypes.Product)
            {
                newVer.Procedure = "<LoadProcedures>\r\n<LoadProcedure>\r\n<LdCtrlConnect />\r\n<LdCtrlCompareProp ObjIdx=\"0\" PropId=\"78\" InlineData=\"00000000012700000000\" />\r\n<LdCtrlUnload LsmIdx=\"1\" />\r\n<LdCtrlUnload LsmIdx=\"2\" />\r\n<LdCtrlUnload LsmIdx=\"3\" />\r\n<LdCtrlLoad LsmIdx=\"1\" />\r\n<LdCtrlAbsSegment LsmIdx=\"1\" SegType=\"0\" Address=\"16384\" Size=\"513\" Access=\"255\" MemType=\"3\" SegFlags=\"128\" />\r\n<LdCtrlTaskSegment LsmIdx=\"1\" Address=\"16384\" />\r\n<LdCtrlLoadCompleted LsmIdx=\"1\" />\r\n<LdCtrlLoad LsmIdx=\"2\" />\r\n<LdCtrlAbsSegment LsmIdx=\"2\" SegType=\"0\" Address=\"16897\" Size=\"511\" Access=\"255\" MemType=\"3\" SegFlags=\"128\" />\r\n<LdCtrlTaskSegment LsmIdx=\"2\" Address=\"16897\" />\r\n<LdCtrlLoadCompleted LsmIdx=\"2\" />\r\n<LdCtrlLoad LsmIdx=\"3\" />\r\n<LdCtrlAbsSegment LsmIdx=\"3\" SegType=\"0\" Address=\"1792\" Size=\"152\" Access=\"0\" MemType=\"2\" SegFlags=\"0\" />\r\n<LdCtrlAbsSegment LsmIdx=\"3\" SegType=\"1\" Address=\"1944\" Size=\"1\" Access=\"0\" MemType=\"2\" SegFlags=\"0\" />\r\n<LdCtrlAbsSegment LsmIdx=\"3\" SegType=\"0\" Address=\"17408\" Size=\"394\" Access=\"255\" MemType=\"3\" SegFlags=\"128\" />\r\n<LdCtrlTaskSegment LsmIdx=\"3\" Address=\"17408\" />\r\n<LdCtrlLoadCompleted LsmIdx=\"3\" />\r\n<LdCtrlRestart />\r\n<LdCtrlDisconnect />\r\n</LoadProcedure>\r\n</LoadProcedures>";
            } else if(app.Mask.Procedure == Models.ProcedureTypes.Merged)
            {
                newVer.Procedure = "<LoadProcedures>\r\n<LoadProcedure MergeId=\"2\">\r\n<LdCtrlRelSegment  AppliesTo=\"full\" LsmIdx=\"4\" Size=\"1\" Mode=\"0\" Fill=\"0\" />\r\n</LoadProcedure>\r\n<LoadProcedure MergeId=\"4\">\r\n<LdCtrlWriteRelMem ObjIdx=\"4\" Offset=\"0\" Size=\"1\" Verify=\"true\" />\r\n</LoadProcedure>\r\n</LoadProcedures>";
            }

            if(app.Versions.Count > 0) {
                Models.AppVersionModel ver = app.Versions.OrderByDescending(v => v.Number).ElementAt(0);
                newVer.Number = ver.Number + 1;
            }

            Models.AppVersionModel model = new Models.AppVersionModel() {
                Name = newVer.Name,
                Number = newVer.Number,
                Namespace = newVer.NamespaceVersion,
                Version = Newtonsoft.Json.JsonConvert.SerializeObject(newVer, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects })
            };
            
            app.Versions.Add(model);
        }
*/
        private void ClickAddMemory(object sender, RoutedEventArgs e)
        {
            General.Application.Memories.Add(new Models.Memory() { Type = General.Info.Mask.Memory, UId = AutoHelper.GetNextFreeUId(General.Application.Memories) });
        }

        private void ClickRemoveMemory(object sender, RoutedEventArgs e)
        {
            Models.Memory mem = ListMemories.SelectedItem as Models.Memory;
            RecursiveRemoveMemory(General.Application, mem);
            General.Application.Memories.Remove(mem);
        }

        private void RecursiveRemoveMemory(Models.IVersionBase vbase, Models.Memory mem)
        {
            foreach(Models.Parameter para in vbase.Parameters.Where(p => p.SavePath == Models.SavePaths.Memory && p.SaveObject == mem))
                para.SaveObject = null;

            foreach(Models.Module mod in vbase.Modules)
                RecursiveRemoveMemory(mod, mem);
        }

        private void ClickOpenViewer(object sender, RoutedEventArgs e)
        {
            if(MessageBoxResult.Cancel == MessageBox.Show(Properties.Messages.main_open_viewer, Properties.Messages.main_open_viewer_title, MessageBoxButton.OKCancel, MessageBoxImage.Question)) return;
            
            AutoHelper.CheckIds(General.Application);

            ObservableCollection<Models.PublishAction> actions = new ObservableCollection<Models.PublishAction>();
            CheckHelper.CheckVersion(General, actions);
            if(actions.Any(a => a.State == Models.PublishState.Fail))
            {
                MessageBox.Show(Properties.Messages.main_open_viewer_error, Properties.Messages.main_open_viewer_title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ViewerWindow viewer = new ViewerWindow(new Viewer.ImporterCreator(General));
            viewer.Show();
        }

        private void ClickAddLanguageVers(object sender, RoutedEventArgs e)
        {
            if(LanguagesListVers.SelectedItem == null){
                MessageBox.Show(Properties.Messages.main_lang_select);
                return;
            }
            Models.Language lang = LanguagesListVers.SelectedItem as Models.Language;
            LanguagesListVers.SelectedItem = null;
            
            if(General.Application.Languages.Any(l => l.CultureCode == lang.CultureCode))
                MessageBox.Show(Properties.Messages.main_lang_add_error);
            else {
                General.Application.Languages.Add(lang);
                if(!General.Application.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    General.Application.Text.Add(new Models.Translation(lang, ""));
                
                foreach(Models.ParameterType type in General.Application.ParameterTypes) {
                    if(type.Type != Models.ParameterTypes.Enum) continue;

                    foreach(Models.ParameterTypeEnum enu in type.Enums)
                        if(!enu.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                            enu.Text.Add(new Models.Translation(lang, ""));
                }
                foreach(Models.Message msg in General.Application.Messages) {
                    if(!msg.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        msg.Text.Add(new Models.Translation(lang, ""));
                }
                foreach(Models.Helptext msg in General.Application.Helptexts){
                    if(!msg.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        msg.Text.Add(new Models.Translation(lang, ""));
                }

                addLangToVersion(General.Application, lang);
                addLangToVersion(General.Application.Dynamics[0], lang);
                foreach(Models.Module mod in General.Application.Modules)
                {
                    addLangToVersion(mod, lang);
                    addLangToVersion(mod.Dynamics[0], lang);
                }
            }
        }

        private void ClickRemoveLanguageVers(object sender, RoutedEventArgs e) {
            if(SupportedLanguagesVers.SelectedItem == null){
                MessageBox.Show(Properties.Messages.main_lang_select);
                return;
            }
            Models.Language lang = SupportedLanguagesVers.SelectedItem as Models.Language;

            if(General.Application.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                General.Application.Text.Remove(General.Application.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            General.Application.Languages.Remove(General.Application.Languages.Single(l => l.CultureCode == lang.CultureCode));
            

            foreach(Models.ParameterType type in General.Application.ParameterTypes) {
                if(type.Type != Models.ParameterTypes.Enum) continue;

                foreach(Models.ParameterTypeEnum enu in type.Enums) {
                    if(enu.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        enu.Text.Remove(enu.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                }
            }
            foreach(Models.Message msg in General.Application.Messages) {
                if(msg.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    msg.Text.Remove(msg.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            }
            foreach(Models.Helptext msg in General.Application.Helptexts){
                if(msg.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    msg.Text.Remove(msg.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            }

            removeLangFromVersion(General.Application, lang);
            foreach(Models.Module mod in General.Application.Modules)
                removeLangFromVersion(mod, lang);
        }

        private void addLangToVersion(Models.IVersionBase vbase, Models.Language lang)
        {
            foreach(Models.Parameter para in vbase.Parameters)
            {
                if(!para.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Text.Add(new Models.Translation(lang, ""));
                if(!para.Suffix.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Suffix.Add(new Models.Translation(lang, ""));
            }
            foreach(Models.ParameterRef para in vbase.ParameterRefs)
            {
                if(!para.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Text.Add(new Models.Translation(lang, ""));
                if(!para.Suffix.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Suffix.Add(new Models.Translation(lang, ""));
            }
            foreach(Models.ComObject com in vbase.ComObjects) {
                if(!com.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.Text.Add(new Models.Translation(lang, ""));
                if(!com.FunctionText.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.FunctionText.Add(new Models.Translation(lang, ""));
            }
            foreach(Models.ComObjectRef com in vbase.ComObjectRefs) {
                if(!com.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.Text.Add(new Models.Translation(lang, ""));
                if(!com.FunctionText.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.FunctionText.Add(new Models.Translation(lang, ""));
            }
        }

        private void addLangToVersion(Models.Dynamic.IDynItems parent, Models.Language lang)
        {
            switch(parent)
            {
                case Models.Dynamic.DynChannel dch:
                    if(!dch.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        dch.Text.Add(new Models.Translation(lang, ""));
                    break;
                    
                case Models.Dynamic.DynParaBlock dpb:
                    if(!dpb.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        dpb.Text.Add(new Models.Translation(lang, ""));
                    break;

                case Models.Dynamic.DynSeparator ds:
                    if(!ds.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        ds.Text.Add(new Models.Translation(lang, ""));
                    break;
                    
                case Models.Dynamic.DynButton db:
                    if(!db.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        db.Text.Add(new Models.Translation(lang, ""));
                    break;
            }

            if(parent.Items?.Count > 0)
                foreach(Models.Dynamic.IDynItems item in parent.Items)
                    addLangToVersion(item, lang);
        }

        private void removeLangFromVersion(Models.IVersionBase vbase, Models.Language lang)
        {
            foreach(Models.Parameter para in vbase.Parameters) {
                if(para.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Text.Remove(para.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                if(para.Suffix.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Suffix.Remove(para.Suffix.Single(l => l.Language.CultureCode == lang.CultureCode));
            } 
            foreach(Models.ParameterRef para in vbase.ParameterRefs) {
                if(para.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Text.Remove(para.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                if(para.Suffix.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Suffix.Remove(para.Suffix.Single(l => l.Language.CultureCode == lang.CultureCode));
            } 
            foreach(Models.ComObject com in vbase.ComObjects) {
                if(com.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.Text.Remove(com.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                if(com.FunctionText.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.FunctionText.Remove(com.FunctionText.Single(l => l.Language.CultureCode == lang.CultureCode));
            }
            foreach(Models.ComObjectRef com in vbase.ComObjectRefs) {
                if(com.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.Text.Remove(com.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                if(com.FunctionText.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.FunctionText.Remove(com.FunctionText.Single(l => l.Language.CultureCode == lang.CultureCode));
            }
        }

        private void removeLangToVersion(Models.Dynamic.IDynItems parent, Models.Language lang)
        {
            switch(parent)
            {
                case Models.Dynamic.DynChannel dch:
                    if(dch.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        dch.Text.Remove(dch.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                    break;
                    
                case Models.Dynamic.DynParaBlock dpb:
                    if(dpb.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        dpb.Text.Remove(dpb.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                    break;

                case Models.Dynamic.DynSeparator ds:
                    if(ds.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        ds.Text.Remove(ds.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                    break;

                case Models.Dynamic.DynButton db:
                    if(db.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        db.Text.Remove(db.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                    break;
            }

            if(parent.Items?.Count > 0)
                foreach(Models.Dynamic.IDynItems item in parent.Items)
                    addLangToVersion(item, lang);
        }


        private void ClickAddLanguageGen(object sender, RoutedEventArgs e)
        {
            if(LanguagesListGen.SelectedItem == null){
                MessageBox.Show(Properties.Messages.main_lang_select);
                return;
            }
            Models.Language lang = LanguagesListGen.SelectedItem as Models.Language;
            LanguagesListGen.SelectedItem = null;
            
            if(_general.Languages.Any(l => l.CultureCode == lang.CultureCode))
                MessageBox.Show(Properties.Messages.main_lang_add_error);
            else {
                _general.Languages.Add(lang);
                LanguageCatalogItemAdd(_general.Catalog[0], lang);
                if(!General.Info.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    General.Info.Text.Add(new Models.Translation(lang, ""));
                if(!General.Info.Description.Any(t => t.Language.CultureCode == lang.CultureCode))
                    General.Info.Description.Add(new Models.Translation(lang, ""));
            }
        }

        private void LanguageCatalogItemAdd(Models.CatalogItem parent, Models.Language lang)
        {
            foreach(Models.CatalogItem item in parent.Items) {
                if(!item.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    item.Text.Add(new Models.Translation(lang, ""));

                LanguageCatalogItemAdd(item, lang);
            }
        }

        private void LanguageCatalogItemRemove(Models.CatalogItem parent, Models.Language lang)
        {
            foreach(Models.CatalogItem item in parent.Items) {
                if(item.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    item.Text.Remove(item.Text.Single(l => l.Language.CultureCode == lang.CultureCode));

                LanguageCatalogItemRemove(item, lang);
            }
        }

        private void ClickRemoveLanguageGen(object sender, RoutedEventArgs e) {
            if(SupportedLanguagesGen.SelectedItem == null){
                MessageBox.Show(Properties.Messages.main_lang_select);
                return;
            }
            Models.Language lang = SupportedLanguagesGen.SelectedItem as Models.Language;


            _general.Languages.Remove(_general.Languages.Single(l => l.CultureCode == lang.CultureCode));
            LanguageCatalogItemRemove(_general.Catalog[0], lang);
            if(General.Info.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                General.Info.Text.Remove(General.Info.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            if(General.Info.Description.Any(t => t.Language.CultureCode == lang.CultureCode))
                General.Info.Description.Remove(General.Info.Description.Single(l => l.Language.CultureCode == lang.CultureCode));
        }
        #endregion

        private void ClickSave(object sender, RoutedEventArgs e)
        {
            General.ImportVersion = VersionCurrent;
            DoSave();
        }

        private void ClickClose(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show("Projekt wirklich schließen?\r\nNicht gespeicherte Änderungen gehen verloren", "Projekt schließen", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
                
            General = null;
            SetButtons(false);
            MenuSaveBtn.IsEnabled = false;
            System.GC.Collect();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // for .NET Core you need to add UseShellExecute = true
            // see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void ClickSaveAs(object sender, RoutedEventArgs e)
        {
            General.ImportVersion = VersionCurrent;
            SaveFileDialog diag = new SaveFileDialog();
            diag.FileName = General.ProjectName;
            diag.Title = Properties.Messages.main_project_save_title;
            diag.Filter = Properties.Messages.main_project_filter + " (*.ae-manu)|*.ae-manu";
            
            if(diag.ShowDialog() == true)
            {
                App.FilePath = diag.FileName;
                DoSave();
                MenuSaveBtn.IsEnabled = true;
            }
        }

        private void DoSave()
        {
            // using (MemoryStream ms = new MemoryStream())
            // using (Newtonsoft.Json.Bson.BsonDataWriter datawriter = new Newtonsoft.Json.Bson.BsonDataWriter(ms))
            // {
            //     JsonSerializer serializer = JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            //     serializer.Serialize(datawriter, General);
            //     File.WriteAllBytes(App.FilePath, ms.ToArray());
            // }
            // <PackageReference Include="Newtonsoft.Json.Bson" Version="1.0.2" />
            string general = Newtonsoft.Json.JsonConvert.SerializeObject(General, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            System.IO.File.WriteAllText(App.FilePath, general);
        }

        private void ClickSaveTemplate(object sender, RoutedEventArgs e)
        {
            General.ImportVersion = VersionCurrent;
            while(true) {
                Controls.PromptDialog diag = new Controls.PromptDialog(Properties.Messages.main_save_template, Properties.Messages.main_save_template_title);
                if(diag.ShowDialog() == false) {
                    return;
                }

                if(string.IsNullOrEmpty(diag.Answer))
                {
                    System.Windows.MessageBox.Show(Properties.Messages.main_save_template_empty, Properties.Messages.main_save_template_title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Error);
                    continue;
                }

                if(System.IO.File.Exists("Templates\\" + diag.Answer + ".temp"))
                {
                    var res = System.Windows.MessageBox.Show(string.Format(Properties.Messages.main_save_template_duplicate, diag.Answer), Properties.Messages.main_save_template_title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                    if(res == System.Windows.MessageBoxResult.No)
                        continue;
                }

                string general = Newtonsoft.Json.JsonConvert.SerializeObject(General, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
                System.IO.File.WriteAllText("Templates\\" + diag.Answer + ".temp", general);
                return;
            }
        }


        private void ClickOpenTemplate(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            DoOpen(item.Tag.ToString());
            MenuSaveBtn.IsEnabled = false;
            General.Guid = Guid.NewGuid().ToString();
        }

        private void ClickOpen(object sender, RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = Properties.Messages.main_project_open_title;
            diag.Filter = Properties.Messages.main_project_filter + " (*.ae-manu)|*.ae-manu";
            if(diag.ShowDialog() == true)
            {
                DoOpen(diag.FileName);
                MenuSaveBtn.IsEnabled = true;
            }
        }

        private void ClickOpenTranslator(object sender, RoutedEventArgs e)
        {
            TranslatorWindow window = new TranslatorWindow(General.Application);
            window.ShowDialog();
        }


        private void DoOpen(string path)
        {
            if(!File.Exists(path)) return;
            
            App.FilePath = path;
            string general = System.IO.File.ReadAllText(path);

            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("\"ImportVersion\":[ ]?([0-9]+)");
            System.Text.RegularExpressions.Match match = reg.Match(general);

            int VersionToOpen = 0;
            if(match.Success)
            {
                VersionToOpen = int.Parse(match.Groups[1].Value);
            }

            if(VersionToOpen < VersionCurrent && MessageBox.Show(Properties.Messages.main_project_open_old, Properties.Messages.main_project_open_format, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                general = CheckHelper.CheckImportVersion(general, VersionToOpen);
            }
            if(VersionToOpen > VersionCurrent)
            {
                MessageBox.Show(Properties.Messages.main_project_open_new, Properties.Messages.main_project_open_format, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
                
            try{
                General = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.MainModel>(general, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
                
                // using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(path)))
                // using (Newtonsoft.Json.Bson.BsonDataReader reader = new Newtonsoft.Json.Bson.BsonDataReader(ms))
                // {
                //     JsonSerializer serializer = JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
                //     General = serializer.Deserialize<Models.MainModel>(reader);
                // }
                
                AutoHelper.LoadVersion(General, General.Application);
            } catch {
                MessageBox.Show(Properties.Messages.main_project_open_error, Properties.Messages.main_project_open_format, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            General.ImportVersion = VersionCurrent;

            if (!string.IsNullOrEmpty(General.Info._maskId))
            {
                General.Info.Mask = BCUs.Single(bcu => bcu.Id == General.Info._maskId);
            }

            SetSubCatalogItems(General.Catalog[0]);

            SetButtons(true);
            MenuSave.IsEnabled = true;
        }

        private void SetSubCatalogItems(Models.CatalogItem parent)
        {
            foreach(Models.CatalogItem item in parent.Items)
            {
                item.Parent = parent;
                SetSubCatalogItems(item);
            }
        }

        private void SetButtons(bool enable)
        {
            MenuSave.IsEnabled = enable;
            MenuClose.IsEnabled = enable;
            TabsEdit.IsEnabled = enable;
            
            if(General != null)
            {
                TabsEdit.Visibility = Visibility.Visible;
                LogoGrid.Visibility = Visibility.Collapsed;
                if(TabsEdit.SelectedIndex == 6)
                    TabsEdit.SelectedIndex = 5;
            } else {
                TabsEdit.SelectedIndex = 0;
                TabsEdit.Visibility = Visibility.Collapsed;
                LogoGrid.Visibility = Visibility.Visible;
            }
        }

        private void ClickShowVersion(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(string.Format(Properties.Messages.update_uptodate, string.Join('.', System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString().Split('.').Take(3))), Properties.Messages.update_title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ClickDoResetParaIds(object sender, RoutedEventArgs e)
        {
            ClearHelper.ResetParameterIds(General.Application);
        }


        private void ClickSignFolder(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string sourcePath = dialog.SelectedPath;
                string targetPath = Path.Combine(Path.GetTempPath(), "sign");
                if(Directory.Exists(targetPath))
                    Directory.Delete(targetPath, true);

                foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    string x = dirPath.Substring(sourcePath.Length+1);
                    if(!x.StartsWith("M-")) continue;
                    if(x.Split('\\').Length > 2)
                    {
                        x = x.Substring(x.IndexOf('\\')+1);
                        if(!x.StartsWith("Baggages"))
                            continue;
                    }
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
                }
                
                
                int ns = 0;
                foreach(string filePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
                {
                    string path = Path.GetDirectoryName(filePath);
                    if(!Directory.Exists(path.Replace(sourcePath, targetPath))) continue;
                    string relativePath = path.Replace(sourcePath, "");
                    if(relativePath == "") continue;
                    relativePath = relativePath.Substring(7);

                    if(!relativePath.StartsWith("\\Baggages"))
                    {
                        if(!filePath.EndsWith(".xml")
                            && !filePath.EndsWith(".mtxml"))
                            continue;
                    }
                    if(filePath.EndsWith(".xsd") || filePath.EndsWith(".mtproj") || filePath.Contains("knx_master")) continue;
                    if(ns == 0 && filePath.Contains("_A-"))
                    {
                        string content = File.ReadAllText(filePath);
                        System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("xmlns=\"http://knx\\.org/xml/project/([0-9]{2})");
                        System.Text.RegularExpressions.Match m = reg.Match(content);
                        if(!m.Success)
                        {
                            MessageBox.Show("NamespaceVersion konnte nicht ermittelt werden");
                            return;
                        }
                        ns = int.Parse(m.Groups[1].Value);
                    }
                    File.Copy(filePath, filePath.Replace(sourcePath, targetPath).Replace(".mtxml", ".xml"));
                }

                string assPath = GetAssemblyPath(ns);
                ExportHelper helper = null;//todo = new ExportHelper(General, assPath, Path.Combine(sourcePath, "sign.knxprod"));
                helper.SetNamespace(ns);
                helper.SignOutput(targetPath);

                System.Windows.MessageBox.Show(Properties.Messages.main_export_success, Properties.Messages.main_export_title);
            }
        }

        private void ClickImport(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> filters = new Dictionary<string, string>() {
                {"knxprod", "KNX Produktdatenbank (*.knxprod)|*.knxprod"},
                {"xml", "XML Produktatenbank (*.xml)|*.xml"}
            };

            string prod = (sender as MenuItem).Tag.ToString();
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = filters[prod];
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                _general = new Models.MainModel();
                _general.Catalog.Add(new Models.CatalogItem() { Name = Properties.Messages.main_def_cat });
                ImportHelper helper = new ImportHelper(dialog.FileName, bcus);
                switch(prod)
                {
                    case "knxprod":
                        helper.StartZip(General, DPTs);
                        SetButtons(true);
                        Changed("General");
                        break;

                    case "xml":
                        helper.StartXml(_general, DPTs);
                        SetButtons(true);
                        Changed("General");
                        break;

                    default:
                        throw new Exception("Unbekannter Dateityp: " + prod);
                }
            }
            System.GC.Collect();
        }

        private void ClickCatalogContext(object sender, RoutedEventArgs e)
        {
            Models.CatalogItem parent = (sender as MenuItem).DataContext as Models.CatalogItem;
            Models.CatalogItem item = new Models.CatalogItem() { Name = Properties.Messages.main_def_category, Parent = parent };
            foreach(Models.Language lang in _general.Languages) {
                item.Text.Add(new Models.Translation(lang, ""));
            }
            parent.Items.Add(item);
        }

        private void ClickCatalogContextRemove(object sender, RoutedEventArgs e)
        {
            Models.CatalogItem item = (sender as MenuItem).DataContext as Models.CatalogItem;
            item.Parent.Items.Remove(item);
        }




        #endregion

        private void ClickCalcHeatmap(object sender, RoutedEventArgs e)
        {
            Models.Memory mem = (sender as Button).DataContext as Models.Memory;
            AutoHelper.MemoryCalculation(General.Application, mem);
            
        }

        private string GetAssemblyPath(int ns)
        {
            List<string> dirs = new List<string>()
            {
                @"C:\Program Files (x86)\ETS6",
                @"C:\Program Files (x86)\ETS5",
                @"C:\Program Files (x86)\ETS4",
                @"C:\Program Files\ETS6",
                @"C:\Program Files\ETS5",
                @"C:\Program Files\ETS4"
            };

            
            if(Directory.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CV"))) {
                foreach(string path in Directory.GetDirectories(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CV")))
                    dirs.Insert(0, path);
            }

            foreach(string path in dirs)
            {
                if(!File.Exists(System.IO.Path.Combine(path, "Knx.Ets.XmlSigning.dll"))) continue;
                string versionInfo = FileVersionInfo.GetVersionInfo(System.IO.Path.Combine(path, "Knx.Ets.XmlSigning.dll")).FileVersion.Substring(0,3);
                
                if(versionInfo == "6.1" && ns < 23)
                    return path;

                if(versionInfo == "6.0" && ns < 22)
                    return path;

                if(versionInfo == "5.7" && ns == 20)
                    return path;

                if(versionInfo == "5.6" && ns == 14)
                    return path;

                if(versionInfo == "5.1" && ns == 13)
                    return path;

                if(versionInfo == "4.0" && ns == 11)
                    return path;
            }

            return "";
        }

        private void TabChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.RemovedItems.Count > 0 && (e.RemovedItems[0] as TabItem) != null && (e.RemovedItems[0] as TabItem).Content is IFilterable mx1)
                mx1.FilterHide();

            if(e.AddedItems.Count > 0 && (e.AddedItems[0] as TabItem) != null &&(e.AddedItems[0] as TabItem).Content is IFilterable mx2)
                mx2.FilterShow();
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            if((sender as Button).DataContext is Models.Module) {
                ((sender as Button).DataContext as Models.Module).Id = -1;
            } else {
                throw new Exception("Unbekannter Typ zum ID löschen: " + (sender as Button).DataContext.GetType().ToString());
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if(General.FileName.EndsWith(".knxprod"))
                General.FileName = General.FileName.Substring(0, General.FileName.LastIndexOf('.'));

            string fileFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");

            switch(Properties.Settings.Default.Output)
            {
                case "exe":
                #if DEBUG
                    fileFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
                #else
                    fileFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                #endif
                    break;

                case "ae":
                    fileFolder = Path.GetDirectoryName(App.FilePath);
                    break;

                default:
                    MessageBox.Show("Einstellungen für Output nicht gültig");
                    return;
            }

            string filePath = System.IO.Path.Combine(fileFolder, General.FileName + ".knxprod");
            if(File.Exists(filePath))
            {
                if(MessageBoxResult.No == MessageBox.Show(string.Format(Properties.Messages.main_export_duplicate, General.FileName), Properties.Messages.main_export_title, MessageBoxButton.YesNo, MessageBoxImage.Question))
                    return;
            }


            PublishActions.Clear();
            await Task.Delay(1000);

            string assPath = GetAssemblyPath(General.Application.NamespaceVersion);
            if(string.IsNullOrEmpty(assPath))
            {
                MessageBox.Show($"Für den Namespace {General.Application.NamespaceVersion} wurde keine passende ETS installation gefunden");
                return;
            }

            CheckHelper.CheckThis(General, PublishActions);


            if(PublishActions.Count(pa => pa.State == Models.PublishState.Fail) > 0)
            {
                PublishActions.Add(new Models.PublishAction() { Text = Properties.Messages.main_export_failed, State = Models.PublishState.Fail });
                return;
            }
            else
                PublishActions.Add(new Models.PublishAction() { Text = Properties.Messages.main_export_checked, State = Models.PublishState.Success });

            await Task.Delay(1000);

            PublishActions.Add(new Models.PublishAction() { Text = Properties.Messages.main_export_create, State = Models.PublishState.Info });

            await Task.Delay(1000);
            
            string headerPath = Path.Combine(Path.GetDirectoryName(filePath), "knxprod.h");
            ExportHelper helper = new ExportHelper(General, assPath, filePath, headerPath);
            bool success = helper.ExportEts(PublishActions);
            if(!success)
            {
                MessageBox.Show(Properties.Messages.main_export_error, Properties.Messages.main_export_title);
                return;
            }
            helper.SignOutput(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Temp"));
            PublishActions.Add(new Models.PublishAction() { Text = Properties.Messages.main_export_success, State = Models.PublishState.Success } );
        }

        private void ChangeLang(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem)
            {
                string tag = (sender as MenuItem).Tag.ToString();
                Properties.Settings.Default.language = tag;
                Properties.Settings.Default.Save();
                CheckLangs();
            }
        }

        private void ChangeOutput(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem)
            {
                string tag = (sender as MenuItem).Tag.ToString();
                Properties.Settings.Default.Output = tag;
                Properties.Settings.Default.Save();
                CheckOutput();
            }
        }

        private void ChangeAutoUpdate(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem)
            {
                Properties.Settings.Default.autoUpdate = (sender as MenuItem).IsChecked;
                Properties.Settings.Default.Save();
                CheckLangs();
            }
        }

        private void ClickToggleDebug(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem)
            {
                bool tag = (sender as MenuItem).IsChecked;
                Properties.Settings.Default.isDebug = tag;
                Properties.Settings.Default.Save();
            }
        }

        private void ClickHelp(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/OpenKNX/Kaenx-Creator/wiki") { UseShellExecute = true });
        }

        private async void ClickCheckVersion(object sender, RoutedEventArgs e)
        {
            (bool update, string vers) response = await CheckUpdate();
            if(response.update)
            {
                if(MessageBoxResult.Yes == MessageBox.Show(string.Format(Properties.Messages.update_new, response.vers), Properties.Messages.update_title, MessageBoxButton.YesNo, MessageBoxImage.Question))
                {
                    Process.Start(new ProcessStartInfo("https://github.com/OpenKNX/Kaenx-Creator/releases/latest") { UseShellExecute = true });
                }
            } else 
                MessageBox.Show(string.Format(Properties.Messages.update_uptodate, string.Join('.', System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString().Split('.').Take(3)), Properties.Messages.update_title, MessageBoxButton.OK, MessageBoxImage.Information));
                    

        }

        private async Task<(bool, string)> CheckUpdate()
        {
            try{
                HttpClient client = new HttpClient();
                    
                HttpResponseMessage resp = await client.GetAsync("https://github.com/OpenKNX/Kaenx-Creator/releases/latest", HttpCompletionOption.ResponseHeadersRead);
                string version = resp.RequestMessage.RequestUri.ToString();
                version = version.Substring(version.LastIndexOf('/') + 2);
                string[] newVers = version.Split('.');
                string[] oldVers = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString().Split('.');
                bool flag = false;

                for(int i = 0; i < 3; i++)
                {
                    int comp = newVers[i].CompareTo(oldVers[i]);
                    if(comp == 1)
                    {
                        flag = true;
                        break;
                    }
                    if(comp == -1)
                    {
                        break;
                    }
                }
                return (flag, version);
            } catch {
                MessageBox.Show(Properties.Messages.update_new, Properties.Messages.update_title, MessageBoxButton.OK, MessageBoxImage.Error);
                return (false, "");
            }
        }
    }
}
