using System;
using System.Collections.Generic;
using System.Text;  

namespace Service
{
    public class BalanceService
    {
        public decimal GetBalance(string address, List<Models.Transactions> transactions, List<Models.Transactions>? pendingTransactions = null)
        {
            decimal balance = 0;

            foreach (var transaction in transactions)
            {
                if (transaction.To == address)
                {
                    balance += transaction.Amount;
                }
                if (transaction.From == address)
                {
                    balance -= transaction.Amount;
                }
            }

            // Віднімаємо вихідні транзакції в очікуванні (мемпул),
            // щоб не можна було витратити одні й ті самі кошти двічі до майнінгу
            if (pendingTransactions != null)
            {
                foreach (var pendingTx in pendingTransactions)
                {
                    if (pendingTx.From == address)
                    {
                        balance -= pendingTx.Amount;
                    }
                }
            }

            return balance;
        }

        public bool HasSufficientBalance(string address, decimal amount, List<Models.Transactions> transactions, List<Models.Transactions>? pendingTransactions = null)
        {
            if (amount <= 0) return false;
            decimal balance = GetBalance(address, transactions, pendingTransactions);
            return balance >= amount && (balance - amount) >= 0;
        }
        
    }
}