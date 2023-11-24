﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ComObjectRef : INotifyPropertyChanged
    {

        public ComObjectRef() { }
        public ComObjectRef(ComObject com)
        {
            IsAutoGenerated = true;
            ComObjectObject = com;
            Name = com.Name;
        }

        private void Com_PropertyChanged(object sender, PropertyChangedEventArgs e = null)
        {
            if (!IsAutoGenerated || e == null || e.PropertyName != "Name" || (sender as ComObject).UId != ComObjectObject.UId) return;
            Name = (sender as ComObject).Name;
        }

        public bool IsAutoGenerated { get; set; } = false;

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

        private string _name = "Kurze Beschreibung";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        public ObservableCollection<Translation> Text {get;set;} = new ObservableCollection<Translation>();
        private bool _transText = false;
        public bool TranslationText
        {
            get { return _transText; }
            set { _transText = value; Changed("TranslationText"); }
        }
        private bool _overText = false;
        public bool OverwriteText
        {
            get { return _overText; }
            set { _overText = value; Changed("OverwriteText"); }
        }

        public ObservableCollection<Translation> FunctionText {get;set;} = new ObservableCollection<Translation>();
        private bool _transFuncText = false;
        public bool TranslationFunctionText
        {
            get { return _transFuncText; }
            set { _transFuncText = value; Changed("TranslationFunctionText"); }
        }
        private bool _overFunc = false;
        public bool OverwriteFunctionText
        {
            get { return _overFunc; }
            set { _overFunc = value; Changed("OverwriteFunctionText"); }
        }

        private bool _overDpt = false;
        public bool OverwriteDpt
        {
            get { return _overDpt; }
            set { _overDpt = value; Changed("OverwriteDpt"); }
        }

        private bool _overDpst = false;
        public bool OverwriteDpst
        {
            get { return _overDpst; }
            set { _overDpst = value; Changed("OverwriteDpst"); }
        }

        private ComObject _comObjectObject;
        [JsonIgnore]
        public ComObject ComObjectObject
        {
            get { return _comObjectObject; }
            set { 
                if(value == null) _comObject = -1;
                if(_comObjectObject != null)
                    if(value == null || _comObjectObject != value)
                        _comObjectObject.PropertyChanged -= Com_PropertyChanged;
                        
                _comObjectObject = value;
                if(_comObjectObject != null)
                    _comObjectObject.PropertyChanged += Com_PropertyChanged;
                Changed("ComObjectObject");
            }
        }


        [JsonIgnore]
        public string _subTypeNumber;
        public string SubTypeNumber
        {
            get { return SubType?.Number; }
            set { _subTypeNumber = value; Changed("SubTypeNumber"); }
        }

        private DataPointSubType _subType;
        public DataPointSubType SubType
        {
            get { return _subType; }
            set { if (value == null) return; _subType = value; Changed("SubType"); }
        }

        [JsonIgnore]
        public string _typeNumber;
        public string TypeNumber
        {
            get { return Type?.Number; }
            set { if (value == null) return; _typeNumber = value; Changed("TypeNumber"); }
        }

        private DataPointType _type;
        public DataPointType Type
        {
            get { return _type; }
            set { 
                if (value == null) return; 
                ObjectSize = value.Size;
                _type = value; Changed("Type"); 
            }
        }

        private bool _overOS = false;
        public bool OverwriteOS
        {
            get { return _overOS; }
            set { _overOS = value; Changed("OverwriteOS"); }
        }

        private int _objSize = 1;
        public int ObjectSize
        {
            get { return _objSize; }
            set { _objSize = value; Changed("ObjectSize"); }
        }


        [JsonIgnore]
        public int _comObject;
        public int ComObject
        {
            get { return ComObjectObject?.UId ?? _comObject; }
            set { _comObject = value; }
        }

        private bool _flagRead = false;
        public bool FlagRead
        {
            get { return _flagRead; }
            set { _flagRead = value; Changed("FlagRead"); }
        }

        private bool _overFR = false;
        public bool OverwriteFR
        {
            get { return _overFR; }
            set { _overFR = value; Changed("OverwriteFR"); if(!value) FlagRead = false; }
        }

        private bool _flagWrite = false;
        public bool FlagWrite
        {
            get { return _flagWrite; }
            set { _flagWrite = value; Changed("FlagWrite"); }
        }

        private bool _overFW = false;
        public bool OverwriteFW
        {
            get { return _overFW; }
            set { _overFW = value; Changed("OverwriteFW"); if(!value) FlagWrite = false; }
        }

        private bool _flagTrans = false;
        public bool FlagTrans
        {
            get { return _flagTrans; }
            set { _flagTrans = value; Changed("FlagTrans"); }
        }

        private bool _overFT = false;
        public bool OverwriteFT
        {
            get { return _overFT; }
            set { _overFT = value; Changed("OverwriteFT"); if(!value) FlagTrans = false; }
        }

        private bool _flagComm = true;
        public bool FlagComm
        {
            get { return _flagComm; }
            set { _flagComm = value; Changed("FlagComm"); }
        }

        private bool _overFC = false;
        public bool OverwriteFC
        {
            get { return _overFC; }
            set { _overFC = value; Changed("OverwriteFC"); if(!value) FlagComm = false; }
        }

        private bool _flagUpdate = false;
        public bool FlagUpdate
        {
            get { return _flagUpdate; }
            set { _flagUpdate = value; Changed("FlagUpdate"); }
        }

        private bool _overFU = false;
        public bool OverwriteFU
        {
            get { return _overFU; }
            set { _overFU = value; Changed("OverwriteFU"); if(!value) FlagUpdate = false; }
        }

        private bool _flagOnInit = false;
        public bool FlagOnInit
        {
            get { return _flagOnInit; }
            set { _flagOnInit = value; Changed("FlagOnInit"); }
        }

        private bool _overFOI = false;
        public bool OverwriteFOI
        {
            get { return _overFOI; }
            set { _overFOI = value; Changed("OverwriteFOI"); if(!value) FlagOnInit = false; }
        }

        private bool _isNotUsed = false;
        [JsonIgnore]
        public bool IsNotUsed
        {
            get { return _isNotUsed; }
            set { _isNotUsed = value; Changed("IsNotUsed"); }
        }


        
        private bool _useTextParam = false;
        public bool UseTextParameter
        {
            get { return _useTextParam; }
            set { 
                _useTextParam = value; 
                Changed("UseTextParameter"); 
                if(!_useTextParam)
                    ParameterRefObject = null;
            }
        }

        private ParameterRef _parameterRefObject;
        [JsonIgnore]
        public ParameterRef ParameterRefObject
        {
            get { return _parameterRefObject; }
            set { _parameterRefObject = value; Changed("ParameterRefObject"); if(value == null) _parameterRef = -1; }
        }

        [JsonIgnore]
        public int _parameterRef;
        public int ParameterRef
        {
            get { return ParameterRefObject?.UId ?? _parameterRef; }
            set { _parameterRef = value; }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        
        public ComObjectRef Copy()
        {
            ComObjectRef com = (ComObjectRef)this.MemberwiseClone();
            
            /* overwrite old reference with deep copy of the Translation Objects*/
            com.Text = new ObservableCollection<Translation>();
            foreach (Translation translation in this.Text)
                com.Text.Add(new Translation(translation.Language, translation.Text));  
            
            com.FunctionText = new ObservableCollection<Translation>();
            foreach (Translation translation in this.FunctionText)
                com.FunctionText.Add(new Translation(translation.Language, translation.Text));  
            
            return com;
        }
    }
}
