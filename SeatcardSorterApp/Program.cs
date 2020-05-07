/**
 * Seattle Opera Seatcard Sorter
 * Copyright 2019 William Rall
 */
namespace SeatcardSorter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Program.PrintHelp(error: "Missing required arguments");
                return;
            }

            if (args.Length > 4)
            {
                Program.PrintHelp(error: "Too many arguments");
                return;
            }

            string sourceFile = null;
            string targetFile = null;
            string listVersionNameFile = null;
            string versionMappingFile = null;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("/?"))
                {
                    Program.PrintHelp();
                    return;
                }

                if (arg.StartsWith("/s:"))
                {
                    sourceFile = arg.Substring(3);
                }
                else if (arg.StartsWith("/t:"))
                {
                    targetFile = arg.Substring(3);
                }
                else if (arg.StartsWith("/v:"))
                {
                    listVersionNameFile = arg.Substring(3);
                }
                else if (arg.StartsWith("/m:"))
                {
                    versionMappingFile = arg.Substring(3);
                }
                else
                {
                    Program.PrintHelp($"Unknown argument {arg}");
                    return;
                }
            }

            if (string.IsNullOrEmpty(sourceFile) ||
                !File.Exists(sourceFile))
            {
                Program.PrintHelp($"Source file {sourceFile} doesn't exist");
                return;
            }

            if (targetFile == null)
            {
                targetFile = Path.Combine(Path.GetDirectoryName(sourceFile), Path.GetFileNameWithoutExtension(sourceFile) + ".sorted.csv");
            }

            if (File.Exists(targetFile))
            {
                Program.PrintHelp($"Target file {targetFile} already exists");
                return;
            }

            if (!string.IsNullOrEmpty(listVersionNameFile) && !File.Exists(listVersionNameFile))
            {
                Program.PrintHelp($"List version name file {listVersionNameFile} doesn't exist");
                return;
            }

            if (string.IsNullOrEmpty(versionMappingFile) || !File.Exists(versionMappingFile))
            {
                Program.PrintHelp($"Version-mapping file {versionMappingFile} doesn't exist");
                return;
            }

            try
            {
                // Parse list version-name file:
                List<string> listVersions = CsvParsingHelper.DefaultListVersions;
                if (!string.IsNullOrEmpty(listVersionNameFile))
                {
                    listVersions = await CsvParsingHelper.GetListVersions(listVersionNameFile);
                }

                // Parse version mapping file:
                Dictionary<string, Dictionary<DateTime, string>> versionMapping = null;
                List<string> versionColumnsPresent = null;
                if (!string.IsNullOrEmpty(versionMappingFile))
                {
                    (versionMapping, versionColumnsPresent) = await CsvParsingHelper.GetVersionMappings(versionMappingFile, listVersions);
                }

                // Parse source rows from the CSV
                List<SourceRow> sourceRows = await CsvParsingHelper.GetSourceRows(sourceFile);

                (List<ResultRow> rows, List<string> headers) = CsvParsingHelper.MapSourceRowsToTargetRows(listVersions, versionMapping, versionColumnsPresent, sourceRows);

                await CsvParsingHelper.WriteResultRows(targetFile, rows, headers);
            }
            catch (SeatcardSorterException ex)
            {
                Program.PrintHelp(ex.ToString());
            }
        }

        private static void PrintHelp(string error = null)
        {
            if (error != null)
            {
                Console.Error.WriteLine(error);
                Console.Error.WriteLine();
            }

            Console.Out.WriteLine("Usage:");
            Console.Out.WriteLine("  Convert:" + Environment.NewLine + "    SeatcardSorter.exe /s:sourceFile /m:versionMappingFile [/t:targetFile] [/v:listVersionNameFile]");
        }
    }
}
