/**
 * Seattle Opera Seatcard Sorter
 * Copyright 2019 William Rall
 */

namespace SeatcardSorter
{
    using System;
    using System.Collections.Generic;

    using SimpleCsv;

    internal class VersionMappingRow : CsvRow
    {
        public static readonly List<string> RequiredRows =
            new List<string>
            {
                "PerfDate",
            };

        public DateTime PerformanceDate
        {
            get { return DateTime.Parse(this["PerfDate"]); }
            set { this["PerfDate"] = value.ToString("d"); }
        }
    }
}
