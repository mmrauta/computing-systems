using System;
using System.IO;
using System.Linq;

namespace VirtualMachine
{
    public class Parser : IDisposable
    {
        private readonly StreamWriter streamWriter;
        private readonly string fileName;
        private int LabelCounter = 0;

        public Parser(string path)
        {
            this.streamWriter = new StreamWriter(path);
            this.fileName = Path.GetFileNameWithoutExtension(path);
        }

        public void Parse(string commandLine)
        {
            var commandType = GetCommandType(commandLine);

            switch (commandType)
            {
                case CommandType.Arithmetic:
                    TranslateArithmeticInstruction(commandLine);
                    break;
                case CommandType.Push:
                    TranslatePushInstruction(commandLine);
                    break;
                case CommandType.Pop:
                    TranslatePopInstruction(commandLine);
                    break;
                case CommandType.Label:
                    break;
                case CommandType.Goto:
                    break;
                case CommandType.If:
                    break;
                case CommandType.Function:
                    break;
                case CommandType.Return:
                    break;
                case CommandType.Call:
                    break;
                case CommandType.Unrecognized:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(commandLine), "Unrecognized command type");
            }
        }

        private void TranslatePushInstruction(string command)
        {
            var offset = GetSecondParameter(command);
            var memorySegment = GetMemorySegment(command);

            if (memorySegment is MemorySegment.Constant)
            {
                // store value in D register (for constants offset is a value to store)
                Write($"@{offset}");    // A stores a value that should be pushed
                Write("D=A");           // D = value
            }
            else if (memorySegment is MemorySegment.Pointer or MemorySegment.Temp or MemorySegment.Static)
            {
                var address = GetAddress(memorySegment, offset, fileName);
                Write($"@{address}");   // A stores address of a value that should be pushed
                Write("D=M");           // D = value
            }
            else
            {
                // store offset in D register
                Write($"@{offset}");    // A stores an offset
                Write("D=A");           // D stores an offset

                // store value to push in D register
                var memorySegmentCode = GetMemorySegmentLabel(memorySegment);
                Write($"@{memorySegmentCode}");     // A stores an address of argument segment
                Write("A=M+D");                     // A stores an address of a destination (base of the segment + offset)
                Write("D=M");                       // D stores the value to push
            }

            // set value (stored in D) in the right place (on stack)
            Write("@SP");         // A stores StackPointerAddress (SPA)
            Write("A=M");         // A = RAM[SPA] -> A = TopOfTheStackAddress (TSA)
            Write("M=D");         // RAM[TSA] = D

            // increment SP
            Write("@SP");         // A stores SPA
            Write("M=M+1");       // RAM[SPA] = RAM[SPA] + 1
        }

        private void TranslatePopInstruction(string command)
        {
            var offset = GetSecondParameter(command);
            var memorySegment = GetMemorySegment(command);

            if (memorySegment is MemorySegment.Pointer or MemorySegment.Temp or MemorySegment.Static)
            {
                TranslatePopWithKnownAdresses(offset, memorySegment);
            }
            else
            {
                TranslatePopWithDynamicAdresses(offset, memorySegment);
            }
        }

        private void TranslatePopWithDynamicAdresses(int offset, MemorySegment memorySegment)
        {
            // get value from stack and store in R13
            Write("@SP");      // A stores StackPointerAddress (SPA)
            Write("A=M-1");    // A = RAM[SPA] -> A = TopOfTheStackAddress (TSA) - 1
            Write("D=M");      // D = RAM[TSA-1]
            Write("@R13");     // A stores address of R13
            Write("M=D");      // R13 stores the first argument

            // decrement SP
            Write($"@SP");         // A stores SPA
            Write($"M=M-1");       // RAM[SPA] = RAM[SPA] - 1

            // store offset in D register
            Write($"@{offset}");    // A stores an offset
            Write($"D=A");          // D stores an offset

            // set value in the right place (in memory)
            var memorySegmentCode = GetMemorySegmentLabel(memorySegment);

            // store the destination address in R14
            Write($"@{memorySegmentCode}"); // A stores an address of a segment
            Write("A=M+D");                 // A stores an address of a destination (base of the segment + offset)
            Write("D=A");                   // D stores a destination address
            Write("@R14");                  // A stores address of R14
            Write("M=D");                   // @R14 stores a destination address

            // move value to save from R13 to D
            Write("@R13");          // A stores address of R13
            Write("D=M");           // D stores value to save

            // save value of D to address stored in R14
            Write("@R14");          // A stores address of R14
            Write("A=M");           // A stores address of destination
            Write("M=D");           // D saved to destination 
        }

        private void TranslatePopWithKnownAdresses(int offset, MemorySegment memorySegment)
        {
            // get value from stack and store in D
            Write("@SP");      // A stores StackPointerAddress (SPA)
            Write("A=M-1");    // A = RAM[SPA] -> A = TopOfTheStackAddress (TSA) - 1
            Write("D=M");      // D = RAM[TSA-1]

            // decrement SP
            Write("@SP");       // A stores SPA
            Write("M=M-1");     // RAM[SPA] = RAM[SPA] - 1

            // store value of D in the destination
            var address = GetAddress(memorySegment, offset, fileName);
            Write($"@{address}");   // A stores the address of a destination
            Write("M=D");           // D saved to destination
        }

        private void TranslateArithmeticInstruction(string command)
        {
            var operation = GetFirstParameter(command);
            var isValidArithmeticCommand = Enum.TryParse(operation, ignoreCase: true, out ArithmeticCommandType commandType);

            if (!isValidArithmeticCommand)
            {
                throw new ArgumentException("Unrecognized arithmetic command.");
            }

            switch (commandType)
            {
                case ArithmeticCommandType.Add:
                case ArithmeticCommandType.Sub:
                case ArithmeticCommandType.And:
                case ArithmeticCommandType.Or:
                    TranslateBinaryCommand(commandType);
                    break;
                case ArithmeticCommandType.Eq:
                case ArithmeticCommandType.Lt:
                case ArithmeticCommandType.Gt:
                    TranslateComparisonCommand(commandType);
                    break;
                case ArithmeticCommandType.Neg:
                case ArithmeticCommandType.Not:
                    TranslateUnaryCommand(commandType);
                    break;
                default:
                    throw new ArgumentException("Unsupported arithmetic command.");
            }
        }

        private void TranslateBinaryCommand(ArithmeticCommandType commandType)
        {
            // store first argument in D register
            Write($"@SP");     // A stores StackPointerAddress (SPA)
            Write($"A=M-1");   // A=RAM[SPA]-1 -> A = TopOfTheStackAddress (TSA) - 1
            Write($"D=M");     // D=RAM[TSA-1] -> D = TopOfTheStackValue (TSV) (first argument)

            // decrement SP
            Write($"@SP");     // A stores StackPointerAddress
            Write($"M=M-1");   // RAM[SPA] = RAM[SPA]-1 -> RAM[SPA] = TSA-1

            // get TopOfTheStackAddress - 1
            Write($"A=M-1");   // A stores RAM[SPA] - 1  == TSA - 1

            var operationOperator = GetBinaryOperationOperator(commandType);
            Write($"M=M{operationOperator}D"); // RAM[TSA-1] = RAM[TSA-1] {*} D , where {*} could be: +,-,&,|
        }

        private void TranslateComparisonCommand(ArithmeticCommandType commandType)
        {
            // store first argument in R13 register
            Write($"@SP");      // A stores StackPointerAddress (SPA)
            Write($"A=M-1");    // A=RAM[SPA] - 1 -> A = TopOfTheStackAddress (TSA) - 1
            Write($"D=M");      // D=RAM[TSA-1] -> D = TopOfTheStackValue (TSV) (first argument)
            Write($"@R13");     // A stores address of R13
            Write($"M=D");      // R13 stores the first argument

            // decrement SP
            Write($"@SP");      // A stores SPA
            Write($"M=M-1");    // RAM[SPA] = RAM[SPA] - 1 -> RAM[SPA] = TSA - 1

            // store second argument in D
            Write("A=M-1");     // A stores RAM[SPA] - 1 -> A = TSA - 1
            Write("D=M");       // D stores RAM[TSA - 1] -> D = TSV (second argument)
            
            // subtract arguments
            Write("@R13");      // A stores address of R13
            Write("D=D-M");     // D = second argument - first argument

            var yesLabel = GetNextLabel();
            var jumpCondition = GetJumpCode(commandType);

            Write($"@{yesLabel}.start");    // A stores address of {yesLabel}.start
            Write($"D;{jumpCondition}");    // jump to label (address stored in A) if condition is TRUE

            // if not TRUE -> set 'false' on top of the stack
            Write("@SP");
            Write("A=M-1");                 // A = RAM[SPA] -> A = TopOfTheStackAddress (TSA)
            Write("M=0");                   // RAM[TSA] = 0 (false)
            Write($"@{yesLabel}.end");      // A stores address of {yesLabel}.end
            Write("0;JMP");                 // jump to label (to skip the YES block)

            // start of YES block
            Write($"({yesLabel}.start)");

            // if TRUE -> set 'true' on top of the stack
            Write("@SP");       // A stores SPA
            Write("A=M-1");     // A = RAM[SPA] -> A = TopOfTheStackAddress (TSA)
            Write("M=-1");      // RAM[TSA] = -1 (true)
            
            // end of YES block
            Write($"({yesLabel}.end)");
        }

        private void TranslateUnaryCommand(ArithmeticCommandType commandType)
        {
            // store the argument in D register
            Write($"@SP");     // A stores StackPointerAddress (SPA)
            Write($"A=M-1");   // A=RAM[SPA] - 1 -> A = TopOfTheStackAddress (TSA) - 1

            var operationOperator = GetUnaryOperationOperator(commandType);
            Write($"M={operationOperator}M");     // RAM[TSA-1] = {*}RAM[TSA-1], where {*} could be: -,!
        }

        private string GetNextLabel() => $"LABEL.{LabelCounter++}";

        private static CommandType GetCommandType(string commandLine)
        {
            var command = commandLine.Split().FirstOrDefault();

            if (command == "push")
            {
                return CommandType.Push;
            }

            if (command == "pop")
            {
                return CommandType.Pop;
            }

            var isArithmetic = Enum.TryParse(command, ignoreCase: true, out ArithmeticCommandType _);
            if (isArithmetic)
            {
                return CommandType.Arithmetic;
            }

            return CommandType.Unrecognized;
        }

        private static MemorySegment GetMemorySegment(string command)
        {
            var memorySegmentName = GetFirstParameter(command);

            var isValidMemorySegment = Enum.TryParse(memorySegmentName, ignoreCase: true, out MemorySegment memorySegment);
            if (!isValidMemorySegment)
            {
                throw new ArgumentException("Unrecognized memory segment.");
            }

            return memorySegment;
        }

        private static int GetSecondParameter(string command)
        {
            var commandType = GetCommandType(command);

            var isSupportedCommandType = commandType is CommandType.Push or CommandType.Pop or CommandType.Function or CommandType.Call;
            if (!isSupportedCommandType)
            {
                throw new ArgumentException("Wrong command provided.");
            }

            var words = command.Split();
            if (words.Length >= 2)
            {
                throw new ArgumentException($"Wrongly formatted command [{command}]. Two command params are expected.");
            }

            if (!int.TryParse(words[2], out int result))
            {
                throw new ArgumentException($"Wrongly formatted command [{command}]. Second parameter should be of type [int].");
            }

            return result;
        }

        private static string GetFirstParameter(string command)
        {
            var commandType = GetCommandType(command);
            if (commandType is CommandType.Return)
            {
                throw new ArgumentException("Wrong command provided.");
            }

            var words = command.Split();
            return words.Length >= 2 ? words[1] : words[0];
        }

        private static string GetJumpCode(ArithmeticCommandType commandType) => commandType switch
        {
            ArithmeticCommandType.Eq => "JEQ",
            ArithmeticCommandType.Lt => "JLT",
            ArithmeticCommandType.Gt => "JGT",
            _ => throw new ArgumentOutOfRangeException(nameof(commandType), "Invalid command type")
        };

        private static string GetBinaryOperationOperator(ArithmeticCommandType commandType) => commandType switch
        {
            ArithmeticCommandType.Add => "+",
            ArithmeticCommandType.Sub => "-",
            ArithmeticCommandType.And => "&",
            ArithmeticCommandType.Or => "|",
            _ => throw new ArgumentOutOfRangeException(nameof(commandType), "Invalid command type")
        };

        private static string GetUnaryOperationOperator(ArithmeticCommandType commandType) => commandType switch
        {
            ArithmeticCommandType.Neg => "-",
            ArithmeticCommandType.Not => "!",
            _ => throw new ArgumentOutOfRangeException(nameof(commandType), "Invalid command type")
        };

        private static string GetMemorySegmentLabel(MemorySegment segment) => segment switch
        {
            MemorySegment.Argument => "ARG",
            MemorySegment.Local=> "LCL",
            MemorySegment.This => "THIS",
            MemorySegment.That => "THAT",
            _ => throw new ArgumentOutOfRangeException(nameof(segment), "Invalid memory segment name")
        };

        private static string GetAddress(MemorySegment segment, int offset, string fileName) => segment switch
        {
            MemorySegment.Pointer => $"{3 + offset}",
            MemorySegment.Temp => $"{4 + offset}",
            MemorySegment.Static => $"{fileName}.{offset}",
            _ => throw new ArgumentOutOfRangeException(nameof(segment), "Invalid memory segment name")
        };

        private void Write(string line)
        {
            streamWriter.WriteLine(line);
        }

        public void Dispose()
        {
            streamWriter?.Dispose();
        }
    }
}