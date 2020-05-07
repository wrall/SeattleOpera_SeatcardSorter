/**
 * Simple CSV
 * Copyright 2019 William Rall
 */
namespace SimpleCsv
{
    using System.Collections.Generic;

    public class CsvRow : Dictionary<string, string>
    {
        public CsvRow()
        {
        }

        public CsvRow(int capacity)
            : base(capacity)
        {
        }
    }
}
