using Assembler;
using Xunit;

namespace AssemblerTest
{
    public class ParserTest
    {
        [Theory]
        [InlineData("D=1", CommandType.C)]
        [InlineData("M=D|M", CommandType.C)]
        [InlineData("0;JMP", CommandType.C)]
        [InlineData("D;JGT", CommandType.C)]
        [InlineData("M=D-A", CommandType.C)]
        [InlineData("M=M+1", CommandType.C)]
        [InlineData("(abc)", CommandType.L)]
        [InlineData("(TEST)", CommandType.L)]
        [InlineData("@TEST", CommandType.A)]
        [InlineData("@some.var", CommandType.A)]
        [InlineData("@2", CommandType.A)]
        public void Recognize_Command_Type(string command, CommandType expectedType)
        {
            var type = Parser.GetCommandType(command);
            Assert.Equal(expectedType, type);
        }

        [Theory]
        [InlineData("","000")]
        [InlineData("AMD", "111")]
        [InlineData("M", "001")]
        [InlineData("AD", "110")]
        [InlineData("DA", "110")]
        public void Translate_Destination_Part(string destination, string expectedValue)
        {
            var value = Parser.GetDestinationCode(destination);
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData("0;JMP", "1110101010000111")]
        [InlineData("M=D+M", "1111000010001000")]
        [InlineData("D=M", "1111110000010000")]
        [InlineData("A=M", "1111110000100000")]
        [InlineData("M=-1", "1110111010001000")]
        [InlineData("MD=M-1", "1111110010011000")]
        [InlineData("M=!M", "1111110001001000")]
        //[InlineData("M=M+D", "1111000010001000")] //TODO swapping arguments currently not supported
        public void Translate_C_Command(string command, string expectedValue)
        {
            var value = Parser.TranslateCInstruction(command);
            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [InlineData("@1", "0000000000000001")]
        [InlineData("@12", "0000000000001100")]
        public void Translate_A_Command(string command, string expectedValue)
        {
            var value = Parser.TranslateAInstruction(command);
            Assert.Equal(expectedValue, value);
        }
    }
}
