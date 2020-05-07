/**
 * Seattle Opera Seatcard Sorter
 * Copyright 2019 William Rall
 */
namespace SeatcardSorter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using SimpleCsv;

    public static class CsvParsingHelper
    {
        public static List<string> DefaultListVersions =
            new List<string>
            {
                "Bravo",
                "Subs",
                "STBsNonBravo",
                "AllOtherSTBs",
            };

        private static readonly string ErrorString = "ERROR ";

        private static readonly Regex sourceLocationParser = new Regex("((1ST BOX|1ST TIER|2ND BOX|2ND TIER|DRESS|GLRY|ORCH) (\\w):(\\w|\\w\\w)-(\\d+)(,\\d+)*( \\w+-\\d+)*)", RegexOptions.Compiled);
        private static readonly Regex sourceSectionParser = new Regex("(1st Tier|1st Tier Boxes|2nd Tier|2nd Tier Boxes|Dress|Gallery|Orchestra) - (Box|Section) (\\w+)", RegexOptions.Compiled);

        private static readonly Regex resultSectionParser = new Regex("^(1ST BOX|1ST TIER|2ND BOX|2ND TIER|DRESS|GLRY|ORCH) (\\w{1,2}):(\\w{1,2})-(\\d{1,2})$", RegexOptions.Compiled);

        public static async Task<List<string>> GetListVersions(string listVersionNameFile)
        {
            try
            {
                using (StreamReader reader = new StreamReader(File.OpenRead(listVersionNameFile)))
                {
                    List<string> listVersions = new List<string>(5);
                    while (!reader.EndOfStream)
                    {
                        listVersions.Add(await reader.ReadLineAsync());
                    }

                    return listVersions;
                }
            }
            catch (IOException ex)
            {
                throw new SeatcardSorterException("Unable to parse list-name file", ex);
            }
        }

        public static Tuple<List<ResultRow>, List<string>> MapSourceRowsToTargetRows(
            List<string> listVersions,
            Dictionary<string, Dictionary<DateTime, string>> versionMapping,
            List<string> versionColumnsPresent,
            List<SourceRow> sourceRows)
        {
            // Convert source rows to target rows
            Dictionary<uint, List<ResultRow>> resultRowsByCustomerId = new Dictionary<uint, List<ResultRow>>(sourceRows.Count);
            foreach (SourceRow sourceRow in sourceRows)
            {
                List<ResultRow> resultRowsPerCustomer;
                if (!resultRowsByCustomerId.TryGetValue(sourceRow.CustomerNumber, out resultRowsPerCustomer))
                {
                    resultRowsPerCustomer = new List<ResultRow>(1);
                    resultRowsByCustomerId.Add(sourceRow.CustomerNumber, resultRowsPerCustomer);
                }

                ResultRow resultRow = new ResultRow
                {
                    CustomerNumber = sourceRow.CustomerNumber,
                    PerformanceFullDate = sourceRow.PerformanceDate,
                    PerformanceDate = sourceRow.PerformanceDate.ToString("MM/dd")
                };

                string version = CsvParsingHelper.DetermineVersion(
                    sourceRow.CustomerNumber,
                    !string.IsNullOrEmpty(sourceRow.List1),
                    !string.IsNullOrEmpty(sourceRow.List2),
                    !string.IsNullOrEmpty(sourceRow.List3),
                    !string.IsNullOrEmpty(sourceRow.List4),
                    !string.IsNullOrEmpty(sourceRow.List5),
                    listVersions);

                resultRow.Version = version;

                if (versionMapping != null &&
                    versionMapping.TryGetValue(version, out Dictionary<DateTime, string> dateMapping) &&
                    dateMapping.TryGetValue(resultRow.PerformanceFullDate, out string mappedValue))
                {
                    resultRow[version] = mappedValue;
                }

                resultRow.YearsSubscribed = sourceRow.YearsSubscribed;
                resultRow.LetterSalutation = sourceRow.LetterSalutation;
                resultRow.FullName = CsvParsingHelper.ConstructFullName(sourceRow.FirstName, sourceRow.LastName);
                CsvParsingHelper.DetermineLocation(sourceRow, resultRow);

                resultRowsPerCustomer.Add(resultRow);
            }

            List<ResultRow> resultRows = new List<ResultRow>(resultRowsByCustomerId.Count);
            foreach (List<ResultRow> resultRowsPerCustomer in resultRowsByCustomerId.Values)
            {
                resultRowsPerCustomer.Sort(new ResultRowImportanceComparer());
                resultRows.Add(resultRowsPerCustomer[0]);
            }

            resultRows.Sort(new ResultRowLocationComparer());

            // Output target rows into a new CSV
            List<string> targetRows;
            if (versionColumnsPresent != null)
            {
                targetRows = new List<string>(ResultRow.BaseRows.Count + versionColumnsPresent.Count);
                targetRows.AddRange(ResultRow.BaseRows);
                targetRows.AddRange(versionColumnsPresent);
            }
            else
            {
                targetRows = ResultRow.BaseRows;
            }

            return new Tuple<List<ResultRow>, List<string>>(resultRows, targetRows);
        }

        public static async Task<List<SourceRow>> GetSourceRows(string sourceFile)
        {
            List<SourceRow> sourceRows;
            try
            {
                using (CsvReader reader = new CsvReader(new StreamReader(File.OpenRead(sourceFile)), schema: new CsvReadSchema(requiredColumns: SourceRow.RequiredRows)))
                {
                    sourceRows = await reader.ReadAllRows<SourceRow>();
                }
            }
            catch (Exception ex) when (ex is CsvParsingException || ex is IOException)
            {
                throw new SeatcardSorterException("Unable to parse source CSV", ex);
            }

            return sourceRows;
        }

        public static async Task WriteResultRows(string targetFile, List<ResultRow> rows, List<string> headers)
        {
            try
            {
                using (FileStream stream = File.Create(targetFile))
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    using (CsvWriter csvWriter = new CsvWriter(writer, new CsvWriteSchema(headers), leaveOpen: true))
                    {
                        await csvWriter.WriteRows(rows);
                    }

                    await writer.FlushAsync();
                    await stream.FlushAsync();
                }
            }
            catch (Exception ex) when (ex is CsvParsingException || ex is IOException)
            {
                throw new SeatcardSorterException("Unable to write target CSV", ex);
            }
        }

        public static async Task<Tuple<List<ResultRow>, List<string>>> GetResultRows(string sourceFile)
        {
            List<ResultRow> resultRows;
            List<string> headers;
            try
            {
                CsvReadSchema schema = new CsvReadSchema(requiredColumns: ResultRow.BaseRows);
                using (CsvReader reader = new CsvReader(new StreamReader(File.OpenRead(sourceFile)), schema))
                {
                    resultRows = await reader.ReadAllRows<ResultRow>();
                    headers = schema.ActualColumns;
                }
            }
            catch (Exception ex) when (ex is CsvParsingException || ex is IOException)
            {
                throw new SeatcardSorterException("Unable to parse source CSV", ex);
            }

            return new Tuple<List<ResultRow>, List<string>>(resultRows, headers);
        }

        public static async Task<Tuple<Dictionary<string, Dictionary<DateTime, string>>, List<string>>> GetVersionMappings(string versionMappingFile, List<string> listVersions)
        {
            List<VersionMappingRow> versionMappingRows;
            try
            {
                CsvReadSchema readSchema = new CsvReadSchema(requiredColumns: VersionMappingRow.RequiredRows, optionalColumns: listVersions);
                using (CsvReader reader = new CsvReader(new StreamReader(File.OpenRead(versionMappingFile)), schema: readSchema))
                {
                    versionMappingRows = await reader.ReadAllRows<VersionMappingRow>();
                }

                List<string> versionColumnsPresent = new List<string>(listVersions.Count);
                foreach (string version in listVersions)
                {
                    if (readSchema.ActualColumns.Contains(version))
                    {
                        versionColumnsPresent.Add(version);
                    }
                }

                Dictionary<string, Dictionary<DateTime, string>> versionMapping = new Dictionary<string, Dictionary<DateTime, string>>(versionColumnsPresent.Count);
                foreach (VersionMappingRow row in versionMappingRows)
                {
                    foreach (string version in versionColumnsPresent)
                    {
                        Dictionary<DateTime, string> dateMapping;
                        if (!versionMapping.TryGetValue(version, out dateMapping))
                        {
                            dateMapping = new Dictionary<DateTime, string>(versionMappingRows.Count);
                            versionMapping[version] = dateMapping;
                        }

                        string value;
                        if (!row.TryGetValue(version, out value))
                        {
                            value = null;
                        }

                        dateMapping[row.PerformanceDate] = value;
                    }
                }

                return new Tuple<Dictionary<string, Dictionary<DateTime, string>>, List<string>>(versionMapping, versionColumnsPresent);
            }
            catch (Exception ex) when (ex is CsvParsingException || ex is IOException)
            {
                throw new SeatcardSorterException("Unable to parse version mapping file", ex);
            }
        }

        public static void Resort(List<ResultRow> originalResults)
        {
            foreach (ResultRow result in originalResults)
            {
                Match match = CsvParsingHelper.resultSectionParser.Match(result.Location);
                if (match.Success)
                {
                    result.Level = match.Groups[1].Value;
                    result.Section = match.Groups[2].Value;
                    result.Row = match.Groups[3].Value;
                    result.SeatNumber = uint.Parse(match.Groups[4].Value);
                }
                else
                {
                    result.Level = CsvParsingHelper.ErrorString;
                }

                // pretend that all of the dates listed (which were originally in Month/Day format) are in a leap year
                // so that if we are parsing 2/29 we don't crash...
                result.PerformanceFullDate = DateTime.Parse(result.PerformanceDate + "/2016");
            }

            originalResults.Sort(new ResultRowLocationComparer());
        }

        public static List<ResultRow> FindDuplicates(List<ResultRow> originalResults)
        {
            if (originalResults == null || originalResults.Count < 2)
            {
                return originalResults;
            }

            List<ResultRow> duplicateRows = new List<ResultRow>();

            int lastRowAdded = -1;
            int i = 1;
            ResultRow previousRow = originalResults[0];
            while (i < originalResults.Count)
            {
                ResultRow currentRow = originalResults[i];
                if (previousRow.Location.Equals(currentRow.Location) &&
                    previousRow.PerformanceDate.Equals(currentRow.PerformanceDate))
                {
                    if (lastRowAdded != i - 1)
                    {
                        duplicateRows.Add(previousRow);
                    }

                    duplicateRows.Add(currentRow);
                    lastRowAdded = i;
                }

                previousRow = currentRow;
                i++;
            }

            return duplicateRows;
        }

        private static string ConstructFullName(string firstName, string lastName)
        {
            bool hasFirstName = !string.IsNullOrWhiteSpace(firstName);
            bool hasLastName = !string.IsNullOrWhiteSpace(lastName);

            if (!hasFirstName && !hasLastName)
            {
                return null;
            }

            string fullName;
            if (hasFirstName && !hasLastName)
            {
                fullName = firstName.Trim();
            }
            else if (!hasFirstName && hasLastName)
            {
                fullName = lastName.Trim();
            }
            else
            {
                fullName = $"{firstName.Trim()} {lastName.Trim()}";
            }

            if (fullName.EndsWith(" Household", StringComparison.OrdinalIgnoreCase))
            {
                fullName = fullName.Substring(0, fullName.Length - 10).TrimEnd();
            }

            return fullName;
        }

        private static void DetermineLocation(SourceRow sourceRow, ResultRow resultRow)
        {
            if (string.IsNullOrEmpty(sourceRow.Section) ||
                string.IsNullOrEmpty(sourceRow.Location))
            {
                resultRow.Level = CsvParsingHelper.ErrorString;
                resultRow.Location = CsvParsingHelper.ErrorString + sourceRow.Location;
                return;
            }

            Match sectionMatch = CsvParsingHelper.sourceSectionParser.Match(sourceRow.Section);
            if (!sectionMatch.Success)
            {
                throw new SeatcardSorterException($"Couldn't parse section for customer_no {sourceRow.CustomerNumber}");
            }

            MatchCollection locationMatches = CsvParsingHelper.sourceLocationParser.Matches(sourceRow.Location);
            if (locationMatches.Count < 1)
            {
                throw new SeatcardSorterException($"Couldn't parse location for customer_no {sourceRow.CustomerNumber}");
            }

            string sectionLevel = sectionMatch.Groups[1].Value;
            resultRow.Section = sectionMatch.Groups[3].Value;

            if (locationMatches.Count > 2)
            {
                resultRow.Level = CsvParsingHelper.ErrorString;
                resultRow.Location = CsvParsingHelper.ErrorString + sourceRow.Location;
                return;
            }

            if (locationMatches.Count == 1)
            {
                Match locationMatch = locationMatches[0];
                string locationLevel = locationMatch.Groups[2].Value;
                string row = locationMatch.Groups[4].Value;
                string seat = locationMatch.Groups[5].Value;
                if (!uint.TryParse(seat, out uint seatNumber))
                {
                    throw new SeatcardSorterException($"Couldn't parse location seat number for customer_no {sourceRow.CustomerNumber}");
                }

                if (!CsvParsingHelper.LevelsMatch(locationLevel, sectionLevel))
                {
                    resultRow.Level = CsvParsingHelper.ErrorString;
                    resultRow.Location = CsvParsingHelper.ErrorString + sourceRow.Location;
                }
                else
                {
                    resultRow.Level = locationLevel;
                    resultRow.Row = row;
                    resultRow.SeatNumber = seatNumber;

                    resultRow.Location = CsvParsingHelper.GenerateLocation(resultRow.Level, resultRow.Section, resultRow.Row, resultRow.SeatNumber);
                }
            }
            else // if (locationMatches.Count == 2)
            {
                Match firstLocationMatch = locationMatches[0];
                Match secondLocationMatch = locationMatches[1];

                string firstLocationLevel = firstLocationMatch.Groups[2].Value;
                string secondLocationLevel = secondLocationMatch.Groups[2].Value;

                char firstAisle = firstLocationMatch.Groups[3].Value[0];
                char secondAisle = secondLocationMatch.Groups[3].Value[0];
                int aisleDifference = firstAisle - secondAisle;
                if (!string.Equals(firstLocationLevel, secondLocationLevel) ||
                    !CsvParsingHelper.LevelsMatch(firstLocationLevel, sectionLevel) ||
                    aisleDifference < -1 ||
                    aisleDifference > 1)
                {
                    resultRow.Level = CsvParsingHelper.ErrorString;
                    resultRow.Location = CsvParsingHelper.ErrorString + sourceRow.Location;
                }
                else
                {
                    string row = firstLocationMatch.Groups[4].Value;
                    string seat = firstLocationMatch.Groups[5].Value;
                    uint seatNumber;
                    if (!uint.TryParse(seat, out seatNumber))
                    {
                        throw new SeatcardSorterException($"Couldn't parse location seat number for customer_no {sourceRow.CustomerNumber}");
                    }

                    resultRow.Level = firstLocationLevel;
                    resultRow.Row = row;
                    resultRow.SeatNumber = seatNumber;
                    resultRow.Location = CsvParsingHelper.GenerateLocation(resultRow.Level, resultRow.Section, resultRow.Row, resultRow.SeatNumber);
                }
            }
        }

        private static string GenerateLocation(string level, string section, string row, uint seatNumber)
        {
            return $"{level} {section}:{row}-{seatNumber}";
        }

        private static bool LevelsMatch(string locationLevel, string sectionLevel)
        {
            return (locationLevel.Equals("1ST BOX") && sectionLevel.Equals("1st Tier Boxes"))
                || (locationLevel.Equals("1ST TIER") && sectionLevel.Equals("1st Tier"))
                || (locationLevel.Equals("2ND BOX") && sectionLevel.Equals("2nd Tier Boxes"))
                || (locationLevel.Equals("2ND TIER") && sectionLevel.Equals("2nd Tier"))
                || (locationLevel.Equals("DRESS") && sectionLevel.Equals("Dress"))
                || (locationLevel.Equals("GLRY") && sectionLevel.Equals("Gallery"))
                || (locationLevel.Equals("ORCH") && sectionLevel.Equals("Orchestra"));
        }

        private static string DetermineVersion(uint customerId, bool list1, bool list2, bool list3, bool list4, bool list5, List<string> listVersions)
        {
            if (list1)
            {
                if (list2 || list3 || list4 || list5)
                {
                    throw new SeatcardSorterException($"multiple lists were present for a row (customer_no {customerId}!");
                }

                return listVersions.Count > 0 ? listVersions[0] : null;
            }

            if (list2)
            {
                if (list3 || list4 || list5)
                {
                    throw new SeatcardSorterException($"multiple lists were present for a row (customer_no {customerId}!");
                }

                return listVersions.Count > 1 ? listVersions[1] : null;
            }

            if (list3)
            {
                if (list4 || list5)
                {
                    throw new SeatcardSorterException($"multiple lists were present for a row (customer_no {customerId}!");
                }

                return listVersions.Count > 2 ? listVersions[2] : null;
            }

            if (list4)
            {
                if (list5)
                {
                    throw new SeatcardSorterException($"multiple lists were present for a row (customer_no {customerId}!");
                }

                return listVersions.Count > 3 ? listVersions[3] : null;
            }

            if (list5)
            {
                return listVersions.Count > 4 ? listVersions[4] : null;
            }

            return null;
        }

        private class ResultRowLocationComparer : IComparer<ResultRow>
        {
            private const int ErrorOrderingValue = 100;

            private static readonly Dictionary<string, int> LevelOrdering = new Dictionary<string, int>(7)
            {
                { "ORCH", 1 },
                { "GLRY", 2 },
                { "DRESS", 3 },
                { "1ST TIER", 4 },
                { "1ST BOX", 5 },
                { "2ND TIER", 6 },
                { "2ND BOX", 7 },
                { CsvParsingHelper.ErrorString, ResultRowLocationComparer.ErrorOrderingValue },
            };

            private static readonly Dictionary<string, int> SectionOrdering = new Dictionary<string, int>()
            {
                { "3", 1 },
                { "1", 2 },
                { "2", 3 },
                { "5", 4 },
                { "25", 5 },
                { "23", 6 },
                { "22", 7 },
                { "24", 8 },
                { "4", 9 },
                { "35", 10 },
                { "33", 11 },
                { "31", 12 },
                { "32", 13 },
                { "34", 14 },
                { "A", 15 },
                { "B", 16 },
                { "C", 17 },
                { "D", 18 },
                { "E", 19 },
                { "F", 20 },
                { "G", 21 },
                { "H", 22 },
                { "45", 23 },
                { "43", 24 },
                { "41", 25 },
                { "42", 26 },
                { "44", 27 },
                { "AA", 28 },
                { "BB", 29 },
                { "CC", 30 },
                { "DD", 31 },
                { "EE", 32 },
                { "FF", 33 },
                { "GG", 34 },
                { "HH", 35 },
            };

            private static readonly Dictionary<string, int> RowOrdering = new Dictionary<string, int>()
            {
                { "A", 1 },
                { "B", 2 },
                { "C", 3 },
                { "D", 4 },
                { "E", 5 },
                { "F", 6 },
                { "G", 7 },
                { "H", 8 },
                { "J", 9 },
                { "K", 10 },
                { "L", 11 },
                { "M", 12 },
                { "N", 13 },
                { "P", 14 },
                { "Q", 15 },
                { "R", 16 },
                { "S", 17 },
                { "T", 18 },
                { "U", 19 },
                { "V", 20 },
                { "W", 21 },
                { "X", 22 },
                { "Y", 23 },
                { "Z", 24 },
                { "AA", 25 },
                { "BB", 26 },
                { "CC", 27 },
            };

            public int Compare(ResultRow x, ResultRow y)
            {
                int xLevel;
                int yLevel;
                if (!ResultRowLocationComparer.LevelOrdering.TryGetValue(x.Level, out xLevel))
                {
                    throw new SeatcardSorterException($"unknown level {x.Level}");
                }
                else if (!ResultRowLocationComparer.LevelOrdering.TryGetValue(y.Level, out yLevel))
                {
                    throw new SeatcardSorterException($"unknown level {y.Level}");
                }

                // Handle Error scenario first
                if (xLevel == ResultRowLocationComparer.ErrorOrderingValue && yLevel == ResultRowLocationComparer.ErrorOrderingValue)
                {
                    return 0;
                }
                else if (xLevel == ResultRowLocationComparer.ErrorOrderingValue)
                {
                    return 1;
                }
                else if (yLevel == ResultRowLocationComparer.ErrorOrderingValue)
                {
                    return -1;
                }

                int dateDifference = x.PerformanceFullDate.CompareTo(y.PerformanceFullDate);
                if (dateDifference != 0)
                {
                    return dateDifference;
                }

                if (xLevel != yLevel)
                {
                    return xLevel - yLevel;
                }

                int xSection;
                int ySection;
                if (!ResultRowLocationComparer.SectionOrdering.TryGetValue(x.Section, out xSection))
                {
                    throw new SeatcardSorterException($"unknown section {x.Section}");
                }
                else if (!ResultRowLocationComparer.SectionOrdering.TryGetValue(y.Section, out ySection))
                {
                    throw new SeatcardSorterException($"unknown section {y.Section}");
                }
                else if (xSection != ySection)
                {
                    return xSection - ySection;
                }

                int xRow;
                int yRow;
                if (!ResultRowLocationComparer.RowOrdering.TryGetValue(x.Row, out xRow))
                {
                    throw new SeatcardSorterException($"unknown row {x.Row}");
                }
                else if (!ResultRowLocationComparer.RowOrdering.TryGetValue(y.Row, out yRow))
                {
                    throw new SeatcardSorterException($"unknown row {y.Row}");
                }
                else if (xRow != yRow)
                {
                    return xRow - yRow;
                }

                return (int)x.SeatNumber - (int)y.SeatNumber;
            }
        }

        /// <summary>
        /// Sorts according to lower is higher importance
        /// First consider error-age, then consider lower dates better
        /// </summary>
        private class ResultRowImportanceComparer : IComparer<ResultRow>
        {
            public int Compare(ResultRow x, ResultRow y)
            {
                if (x.Level.Equals(CsvParsingHelper.ErrorString))
                {
                    if (y.Level.Equals(CsvParsingHelper.ErrorString))
                    {
                        return 0;
                    }

                    return 1;
                }
                else if (y.Level.Equals(CsvParsingHelper.ErrorString))
                {
                    return -1;
                }

                return x.PerformanceFullDate.CompareTo(y.PerformanceFullDate);
            }
        }

        private class ResultRowCustomerNumberEqualityComparer : IEqualityComparer<ResultRow>
        {
            public bool Equals(ResultRow x, ResultRow y)
            {
                return x.CustomerNumber == y.CustomerNumber;
            }

            public int GetHashCode(ResultRow obj)
            {
                return obj.CustomerNumber.GetHashCode();
            }
        }
    }
}
