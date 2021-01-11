﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynChannel : IDynItems, IDynChannel
    {
        public string Name { get; set; }
        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();
    }
}
