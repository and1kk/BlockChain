using System;
using Models;
using WalletApp;

namespace Service
{
    public class TransactionService
    {
        private readonly WalletService _walletService = new WalletService();

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

        public (bool isValid, string errorMessage) ValidateTransaction(Transactions transaction)
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
