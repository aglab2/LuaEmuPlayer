﻿using LuaEmuPlayer.ProcessExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LuaEmuPlayer.Models
{
    internal class Emulator
    {
        private Process _process;
        private ulong _ramPtrBase = 0;
        private IntPtr _ptrRam;

        static readonly string[] s_ProcessNames = {
            "project64", "project64d",
            "mupen64-rerecording",
            "mupen64-pucrash",
            "mupen64_lua",
            "mupen64-wiivc",
            "mupen64-RTZ",
            "mupen64-rerecording-v2-reset",
            "mupen64-rrv8-avisplit",
            "mupen64-rerecording-v2-reset",
            "mupen64",
            "retroarch" };

        private Process FindEmulatorProcess()
        {
            foreach (string name in s_ProcessNames)
            {
                Process process = Process.GetProcessesByName(name).Where(p => !p.HasExited).FirstOrDefault();
                if (process != null)
                    return process;
            }
            return null;
        }

        public enum PrepareResult
        {
            NOT_FOUND,
            ONLY_EMULATOR,
            OK,
        }

        public PrepareResult Prepare()
        {
            PrepareResult result = PrepareResult.NOT_FOUND;
            try
            {
                if (!(_process is object) || _process.HasExited)
                {
                    _process = FindEmulatorProcess();
                }

                if (!(_process is object))
                {
                    return PrepareResult.NOT_FOUND;
                }

                result = PrepareResult.ONLY_EMULATOR;
                List<long> romPtrBaseSuggestions = new List<long>();
                List<long> ramPtrBaseSuggestions = new List<long>();

                var name = _process.ProcessName.ToLower();
                int offset = 0;

                if (name.Contains("project64") || name.Contains("wine-preloader"))
                {
                    DeepPointer[] ramPtrBaseSuggestionsDPtrs = { new DeepPointer("Project64.exe", 0xD6A1C),     //1.6
                        new DeepPointer("RSP 1.7.dll", 0x4C054), new DeepPointer("RSP 1.7.dll", 0x44B5C),        //2.3.2; 2.4 
                    };

                    DeepPointer[] romPtrBaseSuggestionsDPtrs = { new DeepPointer("Project64.exe", 0xD6A2C),     //1.6
                        new DeepPointer("RSP 1.7.dll", 0x4C050), new DeepPointer("RSP 1.7.dll", 0x44B58)        //2.3.2; 2.4
                    };

                    // Time to generate some addesses for magic check
                    foreach (DeepPointer romSuggestionPtr in romPtrBaseSuggestionsDPtrs)
                    {
                        int ptr = -1;
                        try
                        {
                            ptr = romSuggestionPtr.Deref<int>(_process);
                            romPtrBaseSuggestions.Add(ptr);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    foreach (DeepPointer ramSuggestionPtr in ramPtrBaseSuggestionsDPtrs)
                    {
                        int ptr = -1;
                        try
                        {
                            ptr = ramSuggestionPtr.Deref<int>(_process);
                            ramPtrBaseSuggestions.Add(ptr);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }

                if (name.Contains("mupen64"))
                {
                    if (name == "mupen64")
                    {
                        // Current mupen releases
                        {
                            ramPtrBaseSuggestions.Add(0x00505CB0); // 1.0.9
                            ramPtrBaseSuggestions.Add(0x00505D80); // 1.0.9.1
                            ramPtrBaseSuggestions.Add(0x0050B110); // 1.0.10
                        }
                    }
                    else
                    {
                        // Legacy mupen versions
                        Dictionary<string, int> mupenRAMSuggestions = new Dictionary<string, int>
                    {
                        { "mupen64-rerecording", 0x008EBA80 },
                        { "mupen64-pucrash", 0x00912300 },
                        { "mupen64_lua", 0x00888F60 },
                        { "mupen64-wiivc", 0x00901920 },
                        { "mupen64-RTZ", 0x00901920 },
                        { "mupen64-rrv8-avisplit", 0x008ECBB0 },
                        { "mupen64-rerecording-v2-reset", 0x008ECA90 },
                    };
                        ramPtrBaseSuggestions.Add(mupenRAMSuggestions[name]);
                    }

                    offset = 0x20;
                }

                if (name.Contains("retroarch"))
                {
                    ramPtrBaseSuggestions.Add(0x80000000);
                    romPtrBaseSuggestions.Add(0x90000000);
                    offset = 0x40;
                }

                MagicManager mm = new MagicManager(_process, romPtrBaseSuggestions.ToArray(), ramPtrBaseSuggestions.ToArray(), offset);
                _ramPtrBase = mm.ramPtrBase;
                _ptrRam = new IntPtr((long)_ramPtrBase);
                return PrepareResult.OK;
            }
            catch (Exception)
            { }

            return result;
        }

        private uint ToBigEndianEmuOffset1(uint ramOffset)
        {
            uint lo = ramOffset & 0x00000003;
            uint hi = ramOffset & 0xfffffffc;
            return hi | (3 - lo);
        }

        private uint ToBigEndianEmuOffset2(uint ramOffset)
        {
            uint lo = ramOffset & 0x00000003;
            uint hi = ramOffset & 0xfffffffc;
            return hi | (2 - lo);
        }

        // Writes carefully to emulator considering the endianness
        public void Write(uint ramAddr, byte[] data)
        {
            var offset = 0x7fffffff & ramAddr;
            if (data.Length == 1)
            {
                var emuPtr = new IntPtr((long)(_ramPtrBase + ToBigEndianEmuOffset1(offset)));
                _process.WriteBytes(emuPtr, data);
            }
            else if (data.Length == 2)
            {
                if (0 != ramAddr % 2)
                {
                    throw new ArgumentException($"Unsupported write to {ramAddr:X} for length {data.Length}");
                }

                byte[] dataClone = (byte[])data.Clone();
                Array.Reverse(dataClone);

                var emuPtr = new IntPtr((long)(_ramPtrBase + ToBigEndianEmuOffset2(offset)));
                _process.WriteBytes(emuPtr, dataClone);
            }
            else if (0 == (data.Length % 4))
            {
                if (0 != ramAddr % 4)
                {
                    throw new ArgumentException($"Unsupported write to {ramAddr:X} for length {data.Length}");
                }

                var emuPtr = new IntPtr((long)(_ramPtrBase + offset));
                byte[] dataClone = (byte[])data.Clone();
                for (int i = 0; i < dataClone.Length; i += 4)
                {
                    Array.Reverse(dataClone, i, 4);
                }

                _process.WriteBytes(emuPtr, dataClone);
            }
            else
            {
                throw new ArgumentException($"Unsupported write size to {ramAddr:X} for length {data.Length}");
            }
        }

        public byte ReadByte(uint ramAddr)
        {
            var offset = 0x7fffffff & ramAddr;
            var emuPtr = new IntPtr((long)(_ramPtrBase + ToBigEndianEmuOffset1(offset)));
            return _process.ReadValue<byte>(emuPtr);
        }

        public ushort ReadUShort(uint ramAddr)
        {
            var offset = 0x7fffffff & ramAddr;
            var emuPtr = new IntPtr((long)(_ramPtrBase + ToBigEndianEmuOffset2(offset)));
            return _process.ReadValue<ushort>(emuPtr);
        }

        public uint ReadUInt(uint ramAddr)
        {
            var offset = 0x7fffffff & ramAddr;
            var emuPtr = new IntPtr((long)(_ramPtrBase + offset));
            return _process.ReadValue<uint>(emuPtr);
        }

        public float ReadFloat(uint ramAddr)
        {
            var offset = 0x7fffffff & ramAddr;
            var emuPtr = new IntPtr((long)(_ramPtrBase + offset));
            return _process.ReadValue<float>(emuPtr);
        }

        public bool Ok()
        {
            return !_process.HasExited;
        }
    }
}
