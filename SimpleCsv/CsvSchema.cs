/**
 * Simple CSV
 * Copyright 2019 William Rall
 */
namespace SimpleCsv
{
    using System.Collections.Generic;

    public abstract class CsvSchema
    {
        protected static readonly List<string> EmptyList = new List<string>(0);

        public List<string> ActualColumns { get; protected set; }
    }
}
