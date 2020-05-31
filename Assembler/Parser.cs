using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Assembler
{
    public static class Parser
    {
        private static readonly Regex LCommandRegex = new Regex(@"^\((?<label>[\w.$]*)\)");
        private static readonly Regex CCommandRegex = new Regex(@"^((?<dest>[DMA]*)\=)?(?<comp>([DMA]|\d*)?([-+&|!]?[DMA\d*])+)?;?(?<jmp>\w*)?");

        public static string Parse(string command)
        {
            var type = Parser.GetCommandType(command);
            return type switch
            {
                CommandType.A => TranslateAInstruction(command),
                CommandType.C => TranslateCInstruction(command),
                _ => throw new ArgumentOutOfRangeException(nameof(command), "Only A & C instructions should be parsed")
            };
        }

        public static CommandType GetCommandType(string command)
        {
            if (command.StartsWith("@"))
            {
                return CommandType.A;
            }

            if (LCommandRegex.IsMatch(command))
            {
                return CommandType.L;
            }

            if (CCommandRegex.IsMatch(command))
            {
                return CommandType.C;
            }

            return CommandType.Unrecognized;
        }

        public static string GetSymbol(string command)
        {
            if (GetCommandType(command) != CommandType.A)
            {
                throw new ArgumentException("Expected A command");
            }

            var symbol= command.Split('@')[1].TrimEnd();
            return symbol;
        }

        public static string GetLabel(string command)
        {
            if (GetCommandType(command) != CommandType.L)
            {
                throw new ArgumentException("Expected A command");
            }

            var groups = LCommandRegex.Match(command).Groups;

            var label = groups["label"].Value;
            return label;
        }

        public static bool IsLabel(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
            {
                return false;
            }

            var type = GetCommandType(line);
            return type is CommandType.L;
        }

        public static string TranslateAInstruction(string command)
        {
            var symbol = GetSymbol(command);

            var isNumber = int.TryParse(symbol, out var number);

            if (!isNumber)
            {
                number = SymbolTable.Get(symbol);
            }

            var address = Convert.ToString(number, 2);
            var instruction = address.PrependZeros(16);
            return instruction;
        }

        public static string TranslateCInstruction(string command)
        {
            if (GetCommandType(command) != CommandType.C)
            {
                throw new ArgumentException("Expected C command");
            }

            var groups = CCommandRegex.Match(command).Groups;

            var destination = GetDestinationCode(groups["dest"].Value);
            var computation = GetComputationCode(groups["comp"].Value);
            var jump = GetJumpCode(groups["jmp"].Value);

            return $"111{computation}{destination}{jump}";
        }

        // returns 7 bits
        public static string GetComputationCode(string comp)
        {
            return comp switch
            {
                null => throw new ArgumentException("Computation code was not properly translated"),
                "" => "",
                "0" => "0101010",
                "1" => "0111111",
                "-1" => "0111010",
                "D" => "0001100",
                "A" => "0110000",
                "M" => "1110000",
                "!D" => "0001101",
                "!A" => "0110001",
                "!M" => "1110001",
                "-D" => "0001111",
                "-A" => "0011111",
                "-M" => "1011111",
                "D+1" => "0011111",
                "A+1" => "0110111",
                "M+1" => "1110111",
                "D-1" => "0001110",
                "A-1" => "0110010",
                "M-1" => "1110010",
                "D+A" => "0000010",
                "D+M" => "1000010",
                "D-A" => "0010011",
                "D-M" => "1010011",
                "A-D" => "0000111",
                "M-D" => "1000111",
                "D&A" => "0000000",
                "D&M" => "1000000",
                "D|A" => "0010101",
                "D|M" => "1010101",

                _ => throw new ArgumentException("Computation code was not properly translated"),
            };
        }

        // returns 3 bits
        public static string GetJumpCode(string jmp)
        {
            return jmp switch
            {
                null => "000",
                "" => "000",
                "JGT" => "001",
                "JEQ" => "010",
                "JGE" => "011",
                "JLT" => "100",
                "JNE" => "101",
                "JLE" => "110",
                "JMP" => "111",
                _ => throw new ArgumentException("Jump code was not properly translated"),
            };
        }

        // returns 3 bits
        public static string GetDestinationCode(string dest)
        {
            var a = dest.GetBit('A');
            var m = dest.GetBit('M');
            var d = dest.GetBit('D');

            return $"{a}{d}{m}";
        }

        private static int GetBit(this string dest, char code)
        {
            return dest.Contains(code) ? 1 : 0;
        }

        private static string PrependZeros(this string text, int desiredTotalLength)
        {
            var currentLength = text.Length;
            var zerosToInsert = desiredTotalLength - currentLength;
            var zeros = new string(Enumerable.Range(0, zerosToInsert).Select(x => '0').ToArray());
            return text.Insert(0, zeros);
        }
    }
}
