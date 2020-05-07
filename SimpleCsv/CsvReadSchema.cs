/**
 * Simple CSV
 * Copyright 2019 William Rall
 */
namespace SimpleCsv
{
    using System.Collections.Generic;

    public class CsvReadSchema : CsvSchema
    {
        private readonly List<string> requiredColumns;
        private readonly List<string> optionalColumns;
        private readonly bool allowExtraColumns;

        public CsvReadSchema(
            List<string> requiredColumns = null,
            List<string> optionalColumns = null,
            bool allowExtraColumns = true)
        {
            this.requiredColumns = requiredColumns ?? CsvSchema.EmptyList;
            this.optionalColumns = optionalColumns ?? CsvSchema.EmptyList;
            this.allowExtraColumns = allowExtraColumns;

            this.ActualColumns = new List<string>(this.requiredColumns.Count + this.optionalColumns.Count);
        }

        internal void ValidateHeaders(List<string> headerNames)
        {
            int requiredColumnsSeen = 0;
            foreach (string headerName in headerNames)
            {
                if (this.ActualColumns.Contains(headerName))
                {
                    throw new CsvParsingException($"Header {headerName} appears multiple times!");
                }

                if (this.requiredColumns.Contains(headerName))
                {
                    requiredColumnsSeen++;
                    this.ActualColumns.Add(headerName);
                }
                else if (this.requiredColumns.Contains(headerName))
                {
                    this.ActualColumns.Add(headerName);
                }
                else if (!this.allowExtraColumns)
                {
                    throw new CsvParsingException($"Unexpected header {headerName}!");
                }
                else
                {
                    this.ActualColumns.Add(headerName);
                }
            }

            if (requiredColumnsSeen != this.requiredColumns.Count)
            {
                throw new CsvParsingException($"One or more of the required columns ({string.Join(", ", this.requiredColumns)}) are missing ({string.Join(", ", this.ActualColumns)})!");
            }
        }

        internal T CreateRow<T>(List<string> rowValues)
            where T : CsvRow, new()
        {
            if (rowValues.Count != this.ActualColumns.Count)
            {
                throw new CsvParsingException($"Number of columns in the row ({rowValues.Count}) must match the number of columns in the header ({this.ActualColumns.Count})");
            }

            T row = new T();
            for (int i = 0; i < rowValues.Count; i++)
            {
                row[this.ActualColumns[i]] = rowValues[i];
            }

            return row;
        }
    }
}
