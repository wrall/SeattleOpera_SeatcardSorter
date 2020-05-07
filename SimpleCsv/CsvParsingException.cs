/**
 * Simple CSV
 * Copyright 2019 William Rall
 */
namespace SimpleCsv
{
    using System;

    public class CsvParsingException : Exception
    {
        public CsvParsingException()
        {
        }

        public CsvParsingException(string message)
            : base(message)
        {
        }

        public CsvParsingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
