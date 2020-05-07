/**
 * Simple CSV
 * Copyright 2019 William Rall
 */
namespace SimpleCsv
{
    using System.Collections.Generic;

    public class CsvWriteSchema : CsvSchema
    {
        public CsvWriteSchema(List<string> actualColumns)
        {
            this.ActualColumns = actualColumns;
        }
    }
}
