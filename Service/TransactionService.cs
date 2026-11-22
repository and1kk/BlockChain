using System;
using Models;
using WalletApp;

namespace Service
{
    public class TransactionService
    {
        private readonly WalletService _walletService = new WalletService();
        private readonly BalanceService _balanceService = new BalanceService();
        public Transactions CreateTransaction(string from, string to, decimal amount, Wallet wallet)
        {
            var tx = new Transactions(from, to, amount, wallet.PublicKey);
            tx.Signature = wallet.Sign(tx.GetDataToSign());

            var validationResult = ValidateTransaction(tx);
            if (!validationResult.isValid)
            {
                throw new ArgumentException(validationResult.errorMessage);
            }

            return tx;
        }
        public (bool isValid, string errorMessage) ValidateBalance(Transactions transaction, List<Transactions> allTransactions, List<Transactions>? pendingTransactions = null)
        {
            if (transaction.isCoinbase)
            {
                return (true, string.Empty);
            }

            if (!_balanceService.HasSufficientBalance(transaction.From, transaction.Amount, allTransactions, pendingTransactions))
            {
                decimal currentBalance = _balanceService.GetBalance(transaction.From, allTransactions, pendingTransactions);
                return (false, $"Insufficient balance for {transaction.From} (available balance: {currentBalance}, required: {transaction.Amount}).");
            }

            return ValidateTransaction(transaction);
        }

        public (bool isValid, string errorMessage) ValidateTransaction(Transactions transaction)
        {
            if (transaction.isCoinbase)
            {
                if (string.IsNullOrWhiteSpace(transaction.To))
                {
                    return (false, "Miner recipient address is required for coinbase transaction.");
                }
                if (transaction.Amount <= 0)
                {
                    return (false, "Coinbase reward amount must be greater than zero.");
                }
                return (true, string.Empty);
            }

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
                return (false, "Transaction amount must be greater than zero.");
            }
            if (transaction.PublicKey == null || transaction.PublicKey.Length == 0)
            {
                return (false, "Public key is required.");
            }
            string expectedAddress = _walletService.GetAddress(transaction.PublicKey);
            if (!string.Equals(transaction.From, expectedAddress, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Sender address '{transaction.From}' does not match public key address '{expectedAddress}'.");
            }
            if (transaction.Signature == null || transaction.Signature.Length == 0)
            {
                return (false, "Signature is required.");
            }
            if (!_walletService.VerifySignature(transaction.GetDataToSign(), transaction.Signature, transaction.PublicKey))
            {
                return (false, "Invalid signature.");
            }
            return (true, string.Empty);
        }
    }
}
