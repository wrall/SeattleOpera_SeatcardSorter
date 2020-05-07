/**
 * Seattle Opera Seatcard Sorter
 * Copyright 2019 William Rall
 */

namespace SeatcardSorter
{
    using System;
    using System.Collections.Generic;

    using SimpleCsv;

    public class SourceRow : CsvRow
    {
        public static readonly List<string> RequiredRows =
            new List<string>
            {
                "performance_name",
                "performance_dt",
                "customer_no",
                "location",
                "section",
                "fname",
                "lname",
                "lsal",
                "num_years_sub",
                "aisle",
                "list1",
                "list2",
                "list3",
                "list4",
                "list5"
            };

        public string PerformanceName
        {
            get { return this["performance_name"]; }
            set { this["performance_name"] = value; }
        }

        public DateTime PerformanceDate
        {
            get { return DateTime.Parse(this["performance_dt"]); }
            set { this["performance_dt"] = value.ToString("d"); }
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

        public string Section
        {
            get { return this["section"]; }
            set { this["section"] = value; }
        }

        public string FirstName
        {
            get { return this["fname"]; }
            set { this["fname"] = value; }
        }

        public string LastName
        {
            get { return this["lname"]; }
            set { this["lname"] = value; }
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

        public string Aisle
        {
            get { return this["aisle"]; }
            set { this["aisle"] = value; }
        }

        public string List1
        {
            get { return this["list1"]; }
            set { this["list1"] = value; }
        }

        public string List2
        {
            get { return this["list2"]; }
            set { this["list2"] = value; }
        }

        public string List3
        {
            get { return this["list3"]; }
            set { this["list3"] = value; }
        }

        public string List4
        {
            get { return this["list4"]; }
            set { this["list4"] = value; }
        }

        public string List5
        {
            get { return this["list5"]; }
            set { this["list5"] = value; }
        }
    }
}
