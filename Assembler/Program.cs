using System;
using System.IO;
using System.Linq;

namespace Assembler
{
    /// <summary>
    /// Assembler translating .asm into .hack files.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please provide asm source file name.");
            Console.WriteLine("Source file should reside in Documents folder.");

            var fileName = Console.ReadLine();

            try
            {
                var (sourcePath, destinationPath) = GetPaths(fileName);
                InitializeSymbolTable(sourcePath);
                TranslateCode(sourcePath, destinationPath);

                Console.WriteLine($"File successfully parsed to {destinationPath} file.");
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }

        private static void TranslateCode(string sourcePath, string destinationPath)
        {
            using var sr = new StreamReader(sourcePath);
            using var sw = new StreamWriter(destinationPath);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = line.TrimStart();

                if (!IsInstruction(line))
                    continue;

                var parsedLine = Parser.Parse(line);

                sw.WriteLine(parsedLine);
            }
        }

        private static void InitializeSymbolTable(string sourcePath)
        {
            using var sr = new StreamReader(sourcePath);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = line.TrimStart();
                if (IsInstruction(line))
                {
                    SymbolTable.Index++;
                }

                if (!Parser.IsLabel(line))
                    continue;

                var symbol = Parser.GetLabel(line);
                SymbolTable.Add(symbol);
            }
        }

        private static bool IsInstruction(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
            {
                return false;
            }

            var type = Parser.GetCommandType(line);
            return !(type is CommandType.L || type is CommandType.Unrecognized);
        }


        private static (string source, string destination) GetPaths(string fileName)
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string source = "Add.asm", destination = "Add.hack";

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var nameNoExtension = fileName.Split('.').FirstOrDefault() ?? "destination";
                source = Path.Combine(folder, fileName);
                destination = Path.Combine(folder, $"{nameNoExtension}.hack");
            }

            return (source, destination);
        }
    }
}
