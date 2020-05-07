/**
* Simple CSV
* Copyright 2019 William Rall
*/
namespace SimpleCsv
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Reads CSV data in RFC 4180 CSV format
    /// </summary>
    public class CsvReader : IDisposable
    {
        private readonly bool leaveOpen;
        private readonly CsvReadSchema schema;

        private TextReader reader;

        public CsvReader(TextReader reader, CsvReadSchema schema = null, bool leaveOpen = false)
        {
            this.reader = reader;
            this.leaveOpen = leaveOpen;
            this.schema = schema ?? new CsvReadSchema(allowExtraColumns: true);
        }

        public async Task<List<T>> ReadAllRows<T>()
            where T : CsvRow, new()
        {
            List<T> rows = new List<T>();
            char[] buffer = new char[1024];

            List<string> rowEntries = new List<string>();

            StringBuilder builder = new StringBuilder();
            int currentRow = 0;
            int currentColumn = 0;
            bool beginningOfColumn = true;
            bool isQuoted = false;
            bool isEscaped = false;
            bool readingHeader = true;
            bool readCR = false;
            while (true)
            {
                int length = await this.reader.ReadAsync(buffer, 0, buffer.Length);
                if (length == 0)
                {
                    break;
                }

                for (int i = 0; i < length; i++)
                {
                    char c = buffer[i];
                    if (beginningOfColumn)
                    {
                        beginningOfColumn = false;
                        if (c == '"')
                        {
                            isQuoted = true;
                            continue;
                        }
                        else
                        {
                            isQuoted = false;
                        }
                    }

                    if (isEscaped)
                    {
                        if (c == '"')
                        {
                            builder.Append(c);
                            continue;
                        }

                        if (c != ',')
                        {
                            throw new CsvParsingException($"cannot have an individual double-quote character in the middle of a quoted value. row {currentRow} column {currentColumn}");
                        }

                        isEscaped = false;
                        isQuoted = false;
                    }

                    if (c == '"')
                    {
                        if (readCR)
                        {
                            throw new CsvParsingException($"CR must be followed by an LF to indicate the end of a row, if unquoted. row {currentRow} column {currentColumn}");
                        }

                        if (!isQuoted)
                        {
                            throw new CsvParsingException($"Quotation mark in the middle of an unquoted row value? row {currentRow} column {currentColumn}");
                        }

                        // need to check the next character to see if it is a comma or another DQUOTE
                        isEscaped = true;
                        continue;
                    }

                    if (c == ',')
                    {
                        if (readCR)
                        {
                            throw new CsvParsingException($"CR must be followed by an LF to indicate the end of a row, if unquoted. row {currentRow} column {currentColumn}");
                        }

                        if (isQuoted)
                        {
                            builder.Append(c);
                        }
                        else
                        {
                            // end of column value:
                            rowEntries.Add(builder.ToString());
                            builder.Clear();

                            beginningOfColumn = true;
                            isQuoted = false;
                            isEscaped = false;
                            currentColumn++;
                        }

                        continue;
                    }

                    if (c == '\r')
                    {
                        if (readCR)
                        {
                            throw new CsvParsingException($"CR must be followed by an LF to indicate the end of a row, if unquoted. row {currentRow} column {currentColumn}");
                        }

                        if (isQuoted)
                        {
                            builder.Append(c);
                        }
                        else
                        {
                            readCR = true;
                        }

                        continue;
                    }

                    if (c == '\n')
                    {
                        if (isQuoted)
                        {
                            builder.Append(c);
                        }
                        else
                        {
                            if (!readCR)
                            {
                                throw new CsvParsingException($"LF must be preceded by a CR to indicate the end of a row, if unquoted. row {currentRow} column {currentColumn}");
                            }

                            // ignore empty rows (unless we are reading a single-column CSV)
                            // they are probably just extra newlines at the end of a file..
                            if (currentColumn != 0 || builder.Length != 0 || this.schema.ActualColumns.Count == 1)
                            {
                                rowEntries.Add(builder.ToString());
                                builder.Clear();

                                if (readingHeader)
                                {
                                    readingHeader = false;
                                    this.schema.ValidateHeaders(rowEntries);
                                }
                                else
                                {
                                    rows.Add(this.schema.CreateRow<T>(rowEntries));
                                }
                            }

                            currentRow++;
                            currentColumn = 0;
                            beginningOfColumn = true;
                            readCR = false;
                            rowEntries.Clear();
                        }

                        continue;
                    }

                    builder.Append(c);
                }
            }

            return rows;
        }

        public void Dispose()
        {
            if (this.reader != null)
            {
                if (!this.leaveOpen)
                {
                    this.reader.Dispose();
                }

                this.reader = null;
            }
        }
    }
}
