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
        public byte[] PublicKey { get; set; } = Array.Empty<byte>();
        public byte[] Signature { get; set; } = Array.Empty<byte>();

        public Transactions(string from, string to, decimal amount, byte[]? publicKey = null)
        {
            Id = _nextId++;
            From = from;
            To = to;
            Amount = amount;
            TimeStamp = DateTime.Now;
            PublicKey = publicKey ?? Array.Empty<byte>();
        }

        public string ToRawString()
        {
            return $"{From}{To}{Amount}{TimeStamp}";
        }

        public override string ToString()
        {
            return $"From: {From}, To: {To}, Amount: {Amount}, TimeStamp: {TimeStamp}";
        }

        // Метод для отримання даних транзакції у вигляді масиву байтів, які будуть підписані відправником
        public byte[] GetDataToSign()
        {
            string dataToSign = $"{From}|{To}|{Amount}|{TimeStamp}";
            return Encoding.UTF8.GetBytes(dataToSign);
        }
    }
}
