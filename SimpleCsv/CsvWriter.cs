/**
* Simple CSV
* Copyright 2019 William Rall
*/
namespace SimpleCsv
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Writes CSV data in RFC 4180 CSV format
    /// </summary>
    public class CsvWriter : IDisposable
    {
        private readonly bool leaveOpen;
        private readonly CsvWriteSchema schema;

        private StreamWriter writer;

        public CsvWriter(StreamWriter writer, CsvWriteSchema schema, bool leaveOpen = false)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            this.writer = writer;
            this.leaveOpen = leaveOpen;

            this.schema = schema;
        }

        public async Task WriteRows<T>(List<T> rows)
            where T : CsvRow
        {
            // first, write headers
            bool first = true;
            foreach (string header in this.schema.ActualColumns)
            {
                if (!first)
                {
                    this.writer.Write(',');
                }

                await this.WriteString(header);
                first = false;
            }

            this.writer.WriteLine();

            foreach (CsvRow row in rows)
            {
                first = true;
                foreach (string header in this.schema.ActualColumns)
                {
                    if (!first)
                    {
                        await this.writer.WriteAsync(',');
                    }

                    if (row.TryGetValue(header, out string value))
                    {
                        await this.WriteString(value);
                    }

                    first = false;
                }

                await this.writer.WriteLineAsync();
            }
        }

        public void Dispose()
        {
            if (this.writer != null)
            {
                if (!this.leaveOpen)
                {
                    this.writer.Dispose();
                }

                this.writer = null;
            }
        }

        private async Task WriteString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            bool needsQuotation = false;
            bool replaceDoubleQuote = false;
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\"':
                        needsQuotation = true;
                        replaceDoubleQuote = true;
                        break;

                    case '\r':
                    case '\n':
                    case ',':
                        needsQuotation = true;
                        break;
                }

                if (replaceDoubleQuote)
                {
                    break;
                }
            }

            if (needsQuotation)
            {
                await this.writer.WriteAsync('"');
                await this.writer.WriteAsync(replaceDoubleQuote ? value.Replace("\"", "\"\"") : value);
                await this.writer.WriteAsync('"');
            }
            else
            {
                await this.writer.WriteAsync(value);
            }
        }
    }
}
