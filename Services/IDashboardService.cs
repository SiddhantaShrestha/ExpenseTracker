using Coursework.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coursework.Services
{
    public interface IDashboardService
    {
        Task<IEnumerable<Transaction>> GetFilteredTransactions(DateTime? fromDate, DateTime? toDate, string tagFilter, string typeFilter);
        Task<decimal> CalculateTotal(string type);
        Task<decimal> CalculateAvailableBalance();
        Task<decimal> CalculateRemainingDebt();
        Task<decimal> CalculateOriginalDebt();
        Task<decimal> CalculateClearedDebt();

        Task<List<Transaction>> LoadAllTransactionsAsync();
        Task SaveAllTransactionsAsync(List<Transaction> transactions);
    }
}
