﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using BrawlLib.SSBB.ResourceNodes;

namespace BrawlLib.SSBBTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SakuraiArchiveHeader
    {
        public const int Size = 0x20;

        public bint _fileSize;
        public bint _lookupOffset;
        public bint _lookupEntryCount;
        public bint _sectionCount; //Has string entry
        public bint _externalSubRoutineCount; //Has string entry
        public int _pad1, _pad2, _pad3;

        //From here begins file data. All offsets are relative to this location (0x20).
        public VoidPtr BaseAddress { get { return Address + 0x20; } }

        public bint* LookupEntries { get { return (bint*)(BaseAddress + _lookupOffset); } }

        public sStringEntry* Sections { get { return (sStringEntry*)(BaseAddress + _lookupOffset + _lookupEntryCount * 4); } }
        public sStringEntry* ExternalSubRoutines { get { return (sStringEntry*)(BaseAddress + _lookupOffset + _lookupEntryCount * 4 + _sectionCount * 8); } }

        //For Sections and References
        public sStringTable* StringTable { get { return (sStringTable*)(BaseAddress + _lookupOffset + _lookupEntryCount * 4 + _sectionCount * 8 + _externalSubRoutineCount * 8); } }

        private VoidPtr Address { get { fixed (void* ptr = &this)return ptr; } }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct sStringTable
    {
        public VoidPtr Address { get { fixed (void* ptr = &this)return ptr; } }
        public string GetString(int offset)
        {
            return new String((sbyte*)Address + offset);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct sStringEntry
    {
        public bint _dataOffset;
        public bint _stringOffset; //Base is string table
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct sListOffset
    {
        public const int Size = 8;

        public bint _startOffset;
        public bint _listCount;

        public VoidPtr Address { get { fixed (void* ptr = &this)return ptr; } }
    }
}