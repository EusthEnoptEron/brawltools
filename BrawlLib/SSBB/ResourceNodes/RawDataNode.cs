﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BrawlLib.IO;
using System.ComponentModel;

namespace BrawlLib.SSBB.ResourceNodes
{
    unsafe class RawDataNode : ResourceNode
    {
        internal byte* Header { get { return (byte*)WorkingUncompressed.Address; } }

        public UnsafeBuffer _buffer;

        public int Size { get { return WorkingUncompressed.Length; } }

        public RawDataNode(string name) { _name = name; }

        [Browsable(true), TypeConverter(typeof(DropDownListCompression))]
        public override string Compression
        {
            get { return base.Compression; }
            set { base.Compression = value; }
        }

        public override bool OnInitialize()
        {
            _buffer = new UnsafeBuffer(WorkingUncompressed.Length);

            Memory.Move(_buffer.Address, (VoidPtr)Header, (uint)_buffer.Length);

            return false;
        }

        public override int OnCalculateSize(bool force)
        {
            return _buffer.Length;
        }

        public override unsafe void Export(string outPath)
        {
            using (FileStream stream = new FileStream(outPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 8, FileOptions.SequentialScan))
            {
                stream.SetLength(_buffer.Length);
                using (FileMap map = FileMap.FromStream(stream))
                    Memory.Move(map.Address, _buffer.Address, (uint)_buffer.Length);
            }
        }

        public override void OnRebuild(VoidPtr address, int length, bool force)
        {
            VoidPtr header = (VoidPtr)address;
            Memory.Move(header, _buffer.Address, (uint)length);
        }
    }
}
