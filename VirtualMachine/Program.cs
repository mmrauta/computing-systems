using System;
using System.IO;
using System.Linq;

namespace VirtualMachine
{
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
            using var parser = new Parser(destinationPath);
            string line;
            while ((line = sr.ReadLine()) is not null)
            {
                line = line.TrimStart();

                if (!IsCommand(line))
                    continue;

                parser.Parse(line);
            }
        }

        private static bool IsCommand(string line) =>
            !string.IsNullOrWhiteSpace(line) && !line.StartsWith("//");

        private static (string source, string destination) GetPaths(string fileName)
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string source = "SimpleAdd.vm", destination = "SimpleAdd.asm";

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var nameNoExtension = fileName.Split('.').FirstOrDefault() ?? "destination";
                source = Path.Combine(folder, fileName);
                destination = Path.Combine(folder, $"{nameNoExtension}.asm");
            }

            return (source, destination);
        }
    }
}
