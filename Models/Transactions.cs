using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Transactions
    {
        private static int _nextId = 1;
        public int Id { get; set; }
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TimeStamp { get; set; }
        public Transactions(string from, string to, decimal amount)
        {
            Id = _nextId++;
            From = from;
            To = to;
            Amount = amount;
            TimeStamp = DateTime.Now;
        }
        public string ToRawString()
        {
            return $"{From}{To}{Amount}{TimeStamp}";
        }
        public override string ToString()
        {
            return $"From: {From}, To: {To}, Amount: {Amount}, TimeStamp: {TimeStamp}";
        }
    }
}