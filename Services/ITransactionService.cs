using Coursework.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coursework.Services
{
    public interface ITransactionService
    {
        // Get filtered transactions based on the provided criteria
        Task<IEnumerable<Transaction>> GetFilteredTransactions(DateTime? fromDate, DateTime? toDate, string tagFilter, string typeFilter);

        // Add a new transaction to the user's list
        Task AddTransactionAsync(Transaction transaction);

        // Calculate the total amount for a given type (Income, Expense, Debt, etc.)
        Task<decimal> CalculateTotal(string type);

        // Calculate the available balance by considering income, expenses, and remaining debt
        Task<decimal> CalculateAvailableBalance();

        // Calculate the remaining debt (outstanding debt not yet paid)
        Task<decimal> CalculateRemainingDebt();

        // Calculate the original debt (the total debt owed initially)
        Task<decimal> CalculateOriginalDebt();

        // Calculate the cleared debt (debt amounts that have been paid off)
        Task<decimal> CalculateClearedDebt();

        // Load transactions for the currently authenticated user
        Task<List<Transaction>> LoadUserTransactionsAsync();

        // Load all transactions from all users' transaction files
        Task<List<Transaction>> LoadAllTransactionsAsync();

        // Save all transactions for the authenticated user
        Task SaveAllTransactionsAsync(List<Transaction> transactions);
    }
}
