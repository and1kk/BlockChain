using System;
using System.Collections.Generic;
using System.Text;
using Models;

namespace Service
{
    public class TransactionService
    {
        public Transactions CreateTransaction(string from, string to, decimal amount)
        {
            return new Transactions(from, to, amount);
        }

        public (bool isValid, string message) ValidateTransaction(Transactions transaction)
        {
            if (string.IsNullOrWhiteSpace(transaction.From))
            {
                return (false, "Sender address is required.");
            }
            if (string.IsNullOrWhiteSpace(transaction.To))
            {
                return (false, "Recipient address is required.");
            }
            if (transaction.Amount <= 0)
            {
                return (false, "Amount must be greater than zero.");
            }
            return (true, "Transaction is valid.");
        }
    }
}
