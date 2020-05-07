/**
 * Seattle Opera Seatcard Sorter
 * Copyright 2019 William Rall
 */
namespace SeatcardSorter
{
    using System;
    using System.Collections.Generic;

    using SimpleCsv;

    public class ResultRow : CsvRow
    {
        public static readonly List<string> BaseRows =
            new List<string>
            {
                "performance_dt",
                "customer_no",
                "location",
                "FullName",
                "lsal",
                "num_years_sub",
                "Version"
            };

        public string PerformanceDate
        {
            get { return this["performance_dt"]; }
            set { this["performance_dt"] = value; }
        }

        public uint CustomerNumber
        {
            get { return uint.Parse(this["customer_no"]); }
            set { this["customer_no"] = value.ToString(); }
        }

        public string Location
        {
            get { return this["location"]; }
            set { this["location"] = value; }
        }

        public string FullName
        {
            get { return this["FullName"]; }
            set { this["FullName"] = value; }
        }

        public string LetterSalutation
        {
            get { return this["lsal"]; }
            set { this["lsal"] = value; }
        }

        public int? YearsSubscribed
        {
            get
            {
                string strVal = this["num_years_sub"];
                int val;
                if (string.IsNullOrEmpty(strVal) ||
                    !int.TryParse(strVal, out val))
                {
                    return null;
                }

                return val;
            }

            set
            {
                this["num_years_sub"] = value?.ToString();
            }
        }

        public string Version
        {
            get { return this["Version"]; }
            set { this["Version"] = value; }
        }

        public string Level { get; set; }

        public string Section { get; set; }

        public string Row { get; set; }

        public uint SeatNumber { get; set; }

        public DateTime PerformanceFullDate { get; set; }
    }
}
