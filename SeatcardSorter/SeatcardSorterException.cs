/**
 * Seattle Opera Seatcard Sorter
 * Copyright 2019 William Rall
 */
namespace SeatcardSorter
{
    using System;

    public class SeatcardSorterException : Exception
    {
        public SeatcardSorterException() : base()
        {
        }

        public SeatcardSorterException(string message) : base(message)
        {
        }

        public SeatcardSorterException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
