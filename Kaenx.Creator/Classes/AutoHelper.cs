﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kaenx.Creator.Classes
{
    public static class AutoHelper
    {

        public static void MemoryCalculation(AppVersion ver, Memory mem)
        {
            mem.Sections.Clear();

            if(mem.Type == MemoryTypes.Absolute)
                mem.StartAddress = mem.Address - (mem.Address % 16);
            else
            {
                mem.StartAddress = 0;
                mem.Address = 0;
            }

            foreach(Module mod in ver.Modules)
                mod.Memory.Sections.Clear();

            if(!mem.IsAutoSize)
                mem.AddBytes(mem.Size);

            if(mem.Type == MemoryTypes.Absolute)
            {
                if(ver.AddressMemoryObject == mem)
                    MemoryCalculationGroups(ver, mem);
                if(ver.AssociationMemoryObject == mem)
                    MemoryCalculationAssocs(ver, mem);
                if(ver.ComObjectMemoryObject == mem)
                    MemoryCalculationComs(ver, mem);
            }
            MemoryCalculationRegular(ver, mem);
        }

        private static void MemoryCalculationGroups(AppVersion ver, Memory mem)
        {
            int maxSize = (ver.AddressTableMaxCount+2) * 2;
            maxSize--; //TODO check why the heck it is smaller
            if(mem.IsAutoSize && (maxSize + ver.AddressTableOffset) > mem.GetCount())
                mem.AddBytes((maxSize + ver.AddressTableOffset) - mem.GetCount());
            //if(mem.Size < maxSize) maxSize = mem.Size;
            mem.SetBytesUsed(MemoryByteUsage.GroupAddress, maxSize, ver.AddressTableOffset);
        }

        private static void MemoryCalculationAssocs(AppVersion ver, Memory mem)
        {
            int maxSize = (ver.AssociationTableMaxCount+1) * 2;
            maxSize--;
            if(mem.IsAutoSize && (maxSize + ver.AssociationTableOffset) > mem.GetCount())
                mem.AddBytes((maxSize + ver.AssociationTableOffset) - mem.GetCount());
            //if(mem.Size < maxSize) maxSize = mem.Size;
            mem.SetBytesUsed(MemoryByteUsage.Association, maxSize, ver.AssociationTableOffset);
        }

        private static void MemoryCalculationComs(AppVersion ver, Memory mem)
        {

            int maxSize = (ver.ComObjects.Count * 3) + 2;
            if(mem.IsAutoSize && (maxSize + ver.ComObjectTableOffset) > mem.GetCount())
                mem.AddBytes((maxSize + ver.ComObjectTableOffset) - mem.GetCount());
            //if(mem.Size < maxSize) maxSize = mem.Size;
            mem.SetBytesUsed(MemoryByteUsage.Coms, maxSize, ver.ComObjectTableOffset);
        }

        private static void MemCalcStatics(IVersionBase vbase, Memory mem, int memId)
        {
            List<Parameter> paras = vbase.Parameters.Where(p => p.MemoryId == memId && p.IsInUnion == false).ToList();

            foreach(Parameter para in paras.Where(p => p.Offset != -1))
            {
                if(para.Offset >= mem.GetCount())
                {
                    if(!mem.IsAutoSize) throw new Exception("Parameter liegt außerhalb des Speichers");
                    
                    int toadd = (para.Offset - mem.GetCount()) + 1;
                    if(para.ParameterTypeObject.SizeInBit > 8) toadd += (para.ParameterTypeObject.SizeInBit / 8) - 1;
                    mem.AddBytes(toadd);
                }

                mem.SetBytesUsed(para);
            }

            foreach (Union union in vbase.Unions.Where(u => u.MemoryId == mem.UId && u.Offset != -1))
            {
                if(union.Offset >= mem.GetCount())
                {
                    if(!mem.IsAutoSize) throw new Exception("Parameter liegt außerhalb des Speichers");

                    int toadd = 1;
                    if(union.SizeInBit > 8) toadd = (union.Offset - mem.GetCount()) + (union.SizeInBit / 8);
                    mem.AddBytes(toadd);
                }

                mem.SetBytesUsed(union, vbase.Parameters.Where(p => p.UnionId == union.UId).ToList());
            }
        }

        private static void MemCalcAuto(IVersionBase vbase, Memory mem, int memId)
        {
            List<Parameter> paras = vbase.Parameters.Where(p => p.MemoryId == memId && p.IsInUnion == false).ToList();
            IEnumerable<Parameter> list1;
            if(mem.IsAutoOrder) list1 = paras.ToList();
            else list1 = paras.Where(p => p.Offset == -1);
            foreach(Parameter para in list1)
            {
                (int offset, int offsetbit) result = mem.GetFreeOffset(para.ParameterTypeObject.SizeInBit);
                para.Offset = result.offset;
                para.OffsetBit = result.offsetbit;
                mem.SetBytesUsed(para);
            }

            IEnumerable<Union> list2;
            if(mem.IsAutoOrder) list2 = vbase.Unions.Where(u => u.MemoryId == memId);
            else list2 = vbase.Unions.Where(u => u.MemoryId == memId && u.Offset == -1);
            foreach (Union union in list2)
            {
                (int offset, int offsetbit) result = mem.GetFreeOffset(union.SizeInBit);
                union.Offset = result.offset;
                union.OffsetBit = result.offsetbit;
                mem.SetBytesUsed(union, vbase.Parameters.Where(p => p.UnionId == union.UId).ToList());
            }
        }

        private static void MemoryCalculationRegular(AppVersion ver, Memory mem)
        {
            if(!mem.IsAutoPara || (mem.IsAutoPara && !mem.IsAutoOrder))
            {
                foreach(Module mod in ver.Modules)
                    MemCalcStatics(mod, mod.Memory, mem.UId);
                    
                MemCalcStatics(ver, mem, mem.UId);
            }

            if(mem.IsAutoPara)
            {
                foreach(Module mod in ver.Modules)
                    MemCalcAuto(mod, mod.Memory, mem.UId);

                MemCalcAuto(ver, mem, mem.UId);
            }

            List<Models.Dynamic.DynModule> mods = new List<Models.Dynamic.DynModule>();
            GetModules(ver.Dynamics[0], mods);
            int highestComNumber = ver.ComObjects.OrderByDescending(c => c.Number).FirstOrDefault()?.Number ?? -1;
            foreach(Models.Dynamic.DynModule dmod in mods)
            {
                Models.Dynamic.DynModuleArg argParas = dmod.Arguments.SingleOrDefault(a => a.ArgumentId == dmod.ModuleObject.ParameterBaseOffsetUId);
                if(argParas == null) continue;

                if(!mem.IsAutoPara || (mem.IsAutoPara && !mem.IsAutoOrder && !string.IsNullOrEmpty(argParas.Value)))
                {
                    int modSize = dmod.ModuleObject.Memory.GetCount();
                    int start = int.Parse(argParas.Value);
                    mem.SetBytesUsed(MemoryByteUsage.Module, modSize, start);
                }

                if(mem.IsAutoPara && (string.IsNullOrEmpty(argParas.Value) || mem.IsAutoOrder))
                {
                    int modSize = dmod.ModuleObject.Memory.GetCount();
                    (int offset, int offsetbit) result = mem.GetFreeOffset(modSize * 8);
                    argParas.Value = result.offset.ToString();
                    mem.SetBytesUsed(MemoryByteUsage.Module, modSize, result.offset);
                }

                if(dmod.ModuleObject.IsComObjectBaseNumberAuto)
                {
                    int highestComNumber2 = dmod.ModuleObject.ComObjects.OrderByDescending(c => c.Number).FirstOrDefault()?.Number ?? 0;
                    Models.Dynamic.DynModuleArg argComs = dmod.Arguments.SingleOrDefault(a => a.ArgumentId == dmod.ModuleObject.ComObjectBaseNumberUId);
                    if(argComs != null)
                    {
                        argComs.Value = (++highestComNumber).ToString();
                        highestComNumber += highestComNumber2;
                    }
                }
            }

            if (mem.IsAutoSize)
                mem.Size = mem.GetCount();
        }

        public static void GetModules(Models.Dynamic.IDynItems item, List<Models.Dynamic.DynModule> mods)
        {
            if(item is Models.Dynamic.DynModule dm)
                mods.Add(dm);

            if(item.Items == null) return;

            foreach(Models.Dynamic.IDynItems i in item.Items)
                GetModules(i, mods);
        }

        public static int GetNextFreeUId(object list, int start = 1) {
            int id = start;

            if(list is System.Collections.ObjectModel.ObservableCollection<Parameter>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Parameter>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<ParameterRef>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<ParameterRef>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<ComObject>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<ComObject>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<ComObjectRef>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<ComObjectRef>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<Memory>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Memory>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<ParameterType>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<ParameterType>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<Union>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Union>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<Module>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Module>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<Argument>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Argument>).Any(i => i.UId == id))
                    id++;
            } else if(list is System.Collections.ObjectModel.ObservableCollection<Baggage>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Baggage>).Any(i => i.UId == id))
                    id++;
            } else if(list is System.Collections.ObjectModel.ObservableCollection<Message>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Message>).Any(i => i.UId == id))
                    id++;
            } else if(list is System.Collections.ObjectModel.ObservableCollection<Helptext>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Helptext>).Any(i => i.UId == id))
                    id++;
            } else {
                throw new Exception("Can't get NextFreeUId. Type not implemented.");
            }
            return id;
        }

        public static int GetNextFreeId(IVersionBase vbase, string list, int start = 1) {
            int id = start;

            if(list == "Parameters") {
                return ++vbase.LastParameterId;
            } else if(list == "ParameterRefs") {
                return ++vbase.LastParameterRefId;
            } else {
                var x = vbase.GetType().GetProperty(list).GetValue(vbase);
                if(x is System.Collections.ObjectModel.ObservableCollection<ComObject> lc) {
                    while(lc.Any(i => i.Id == id))
                        id++;
                }else if(x is System.Collections.ObjectModel.ObservableCollection<ComObjectRef> lcr) {
                    while(lcr.Any(i => i.Id == id))
                        id++;
                }else if(x is System.Collections.ObjectModel.ObservableCollection<Argument> la) {
                    while(la.Any(i => i.Id == id))
                        id++;
                }else if(x is System.Collections.ObjectModel.ObservableCollection<Module> lm) {
                    while(lm.Any(i => i.Id == id))
                        id++;
                }else if(x is System.Collections.ObjectModel.ObservableCollection<Message> ls) {
                    while(ls.Any(i => i.Id == id))
                        id++;
                }
                return id;
            }
        }
    
        public static void CheckIds(AppVersion version)
        {
            counterBlock = 1;
            counterSeparator = 1;

            CheckIdsModule(version, version);
            CheckDynamicIds(version.Dynamics[0]);

            foreach(Module mod in version.Modules)
            {
                if(mod.Id == -1) mod.Id = GetNextFreeId(version, "Modules");
                counterBlock = 1;
                counterSeparator = 1;
                CheckIdsModule(version, mod);
                CheckDynamicIds(mod.Dynamics[0]);
            }
        }

        private static void CheckIdsModule(AppVersion version, IVersionBase vbase)
        {
            foreach(Parameter para in vbase.Parameters)
                if(para.Id == -1) para.Id = GetNextFreeId(vbase, "Parameters");

            foreach(ParameterRef pref in vbase.ParameterRefs)
                if(pref.Id == -1) pref.Id = GetNextFreeId(vbase, "ParameterRefs");

            foreach(ComObject com in vbase.ComObjects)
                if(com.Id == -1) com.Id = GetNextFreeId(vbase, "ComObjects");

            foreach(ComObjectRef cref in vbase.ComObjectRefs)
                if(cref.Id == -1) cref.Id = GetNextFreeId(vbase, "ComObjectRefs");

            if(vbase is Module mod)
            {
                foreach(Argument arg in mod.Arguments)
                    if(arg.Id == -1) arg.Id = GetNextFreeId(vbase, "Arguments");
            }
        }


        private static int counterBlock = 1;
        private static int counterSeparator = 1;
        public static void CheckDynamicIds(IDynItems parent)
        {
            foreach(IDynItems item in parent.Items)
            {
                switch(item)
                {
                    case DynParaBlock dpb:
                        dpb.Id = counterBlock++;
                        break;

                    case DynSeparator ds:
                        ds.Id = counterSeparator++;
                        break;
                }

                if(item.Items != null)
                    CheckDynamicIds(item);
            }
        }

        public static byte[] GetFileBytes(string file)
        {
            byte[] data;
            BitmapImage image = new BitmapImage(new Uri(file));
            BitmapEncoder encoder;

            switch(Path.GetExtension(file).ToLower())
            {
                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;

                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;

                default:
                    throw new Exception("Dataityp " + Path.GetExtension(file).ToLower() + " wird nicht unterstützt");
            }
            
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                data = new byte[ms.Length];
                ms.ToArray().CopyTo(data, 0);
            }
            image = null;
            encoder.Frames.RemoveAt(0);
            encoder = null;
            
            return data;
        }
    
    }
}
