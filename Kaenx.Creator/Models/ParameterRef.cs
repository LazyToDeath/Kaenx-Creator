﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ParameterRef : INotifyPropertyChanged
    {

        public ParameterRef() {}
        public ParameterRef(Parameter para) {
            IsAutoGenerated = true;
            ParameterObject = para;
            Name = para.Name;
        }

        private void Para_PropertyChanged(object sender, PropertyChangedEventArgs e = null) {
            if (!IsAutoGenerated || e == null || e.PropertyName != "Name") return;
            Name = (sender as Parameter).Name;
        }

        private int _uid = -1;
        public int UId
        {
            get { return _uid; }
            set { _uid = value; Changed("UId"); }
        }

        private long _id = -1;
        public long Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); }
        }

        private int _displayOrder = -1;
        public int DisplayOrder
        {
            get { return _displayOrder; }
            set { _displayOrder = value; Changed("DisplayOrder"); }
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private bool _isNotUsed = false;
        [JsonIgnore]
        public bool IsNotUsed
        {
            get { return _isNotUsed; }
            set { _isNotUsed = value; Changed("IsNotUsed"); }
        }

        //TODO set false if checkbox is unchecked
        public bool IsAutoGenerated {get;set;} = false;

        private Parameter _parameterObject;
        [JsonIgnore]
        public Parameter ParameterObject
        {
            get { return _parameterObject; }
            set { 
                if(_parameterObject != null)
                    if(value == null || _parameterObject != value)
                        _parameterObject.PropertyChanged -= ParameterChanged;
                        
                _parameterObject = value;
                if(_parameterObject != null)
                    _parameterObject.PropertyChanged += ParameterChanged;
                Changed("ParameterObject");
            }
        }

        [JsonIgnore]
        public int _parameter;
        public int Parameter
        {
            get { return ParameterObject?.UId ?? -1; }
            set { _parameter = value; }
        }


        private bool _overValue = false;
        public bool OverwriteValue
        {
            get { return _overValue; }
            set { _overValue = value; Changed("OverwriteValue"); }
        }

        private bool _overAccess = false;
        public bool OverwriteAccess
        {
            get { return _overAccess; }
            set { _overAccess = value; Changed("OverwriteAccess"); }
        }


        public ParamAccess Access { get; set; } = ParamAccess.ReadWrite;
        public string Value { get; set; } = "";


        private bool _transText = false;
        public bool TranslationText
        {
            get { return _transText; }
            set { _transText = value; Changed("TranslationText"); }
        }

        private bool _transSuffix = false;
        public bool TranslationSuffix
        {
            get { return _transSuffix; }
            set { _transSuffix = value; Changed("TranslationSuffix"); }
        }


        public ObservableCollection<Translation> Text {get;set;} = new ObservableCollection<Translation>();
        public ObservableCollection<Translation> Suffix {get;set;} = new ObservableCollection<Translation>();
        private bool _overText = false;
        public bool OverwriteText
        {
            get { return _overText; }
            set { _overText = value; Changed("OverwriteText"); }
        }
        private bool _overSuffix = false;
        public bool OverwriteSuffix
        {
            get { return _overSuffix; }
            set { _overSuffix = value; Changed("OverwriteSuffix"); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ParameterChanged(object sender, PropertyChangedEventArgs e)
        {
            if(!IsAutoGenerated || e.PropertyName != "Name") return;
            Name = ParameterObject.Name;
        }
        
        public ParameterRef Copy()
        {
            ParameterRef para = (ParameterRef)this.MemberwiseClone();
            
            /* overwrite old reference with deep copy of the Translation Objects*/
            para.Text = new ObservableCollection<Translation>();
            foreach (Translation translation in this.Text)
                para.Text.Add(new Translation(translation.Language, translation.Text));  
            
            return para;
        }
    }
}
