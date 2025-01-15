using Coursework.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Coursework.Services
{
    public class DebtService : IDebtService
    {
        private readonly ITransactionService _transactionService;
        private readonly AuthenticationStateService _authStateService;

        public DebtService(
            ITransactionService transactionService,
            AuthenticationStateService authStateService)
        {
            _transactionService = transactionService;
            _authStateService = authStateService;
        }

        public async Task<IEnumerable<Debt>> GetAllDebtsAsync()
        {
            var transactions = await _transactionService.LoadUserTransactionsAsync();
            var debts = new Dictionary<string, Debt>();

            foreach (var transaction in transactions.Where(t => t.Type == "Debt"))
            {
                // For debt creation transactions (positive amount)
                if (transaction.Amount > 0)
                {
                    var debt = new Debt
                    {
                        Id = transaction.Id ?? Guid.NewGuid().ToString(),
                        Description = transaction.Description,
                        Amount = transaction.Amount,
                        Date = transaction.Date,
                        Source = transaction.Tags,
                        Labels = transaction.Labels,
                        UserName = transaction.UserName,
                        Status = "Pending"
                    };
                    debts[debt.Id] = debt;
                }
                // For debt payment transactions (negative amount)
                else if (debts.ContainsKey(transaction.Id))
                {
                    debts[transaction.Id].MakePayment(Math.Abs(transaction.Amount));
                }
            }

            return debts.Values;
        }

        public async Task<Debt> GetDebtByIdAsync(string id)
        {
            var debts = await GetAllDebtsAsync();
            return debts.FirstOrDefault(d => d.Id == id);
        }

        public async Task AddDebtAsync(Debt debt)
        {
            if (debt == null)
                throw new ArgumentNullException(nameof(debt));

            // Set the current user
            debt.UserName = _authStateService.GetAuthenticatedUser()?.UserName;

            // Create a transaction for the debt
            var transaction = new Transaction
            {
                Id = debt.Id,
                Description = debt.Description,
                Date = debt.Date,
                Amount = debt.Amount,
                Type = "Debt",
                Tags = debt.Source,
                Labels = debt.Labels,
                UserName = debt.UserName
            };

            await _transactionService.AddTransactionAsync(transaction);
        }

        public async Task MakePaymentAsync(string debtId, decimal paymentAmount)
        {
            var debt = await GetDebtByIdAsync(debtId);
            if (debt == null)
                throw new InvalidOperationException("Debt not found");

            if (paymentAmount <= 0)
                throw new ArgumentException("Payment amount must be greater than 0");

            // Create a payment transaction
            var paymentTransaction = new Transaction
            {
                Id = debt.Id,  // Link to original debt
                Description = $"Payment towards debt: {debt.Description}",
                Date = DateTime.Now,
                Amount = -paymentAmount,  // Negative amount to indicate payment
                Type = "Debt",
                Tags = "Debt Payment",
                Labels = "Auto Generated",
                UserName = debt.UserName
            };

            await _transactionService.AddTransactionAsync(paymentTransaction);
        }

        public async Task<decimal> GetTotalDebtAsync()
        {
            var debts = await GetAllDebtsAsync();
            return debts.Sum(d => d.Amount);
        }

        public async Task<decimal> GetTotalPaidDebtAsync()
        {
            var debts = await GetAllDebtsAsync();
            return debts.Sum(d => d.AmountPaid);
        }

        public async Task<decimal> GetRemainingDebtAsync()
        {
            var debts = await GetAllDebtsAsync();
            return debts.Sum(d => d.RemainingAmount);
        }
    }
}