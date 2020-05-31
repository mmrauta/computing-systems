using System;
using System.Collections.Generic;

namespace Assembler
{
    public static class SymbolTable
    {
        private static readonly Dictionary<string, int> Entries;
        public static int Index = 0;
        public static int RamIndex = 16;

        static SymbolTable()
        {
            Entries = new Dictionary<string, int>();
            InitializeDefaultSymbols();
        }

        public static void Add(string symbol, int address)
        {
            Entries.Add(symbol, address);
        }

        public static void Add(string symbol)
        {
            if (Entries.ContainsKey(symbol))
            {
                return;
            }

            Entries.Add(symbol, Index);
            if (Index == 16384)
            {
                throw new ArgumentOutOfRangeException(nameof(Index), "Too many symbols loaded!");
            }
        }

        public static int Get(string symbol)
        {
            if (!Entries.ContainsKey(symbol))
            {
                Entries.Add(symbol, RamIndex++);
            }

            return Entries[symbol];
        }

        private static void InitializeDefaultSymbols()
        {
            Add("SP", 0);
            Add("LCL", 1);
            Add("ARG", 2);
            Add("THIS", 3);
            Add("THAT", 4);
            Add("SCREEN", 16384);
            Add("KBD", 24576);

            for (var i = 0; i < 16; i++)
            {
                Add($"R{i}", i);
            }
        }
    }
}
