using Coursework.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Coursework.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly string transactionsDirectory = Path.Combine(AppContext.BaseDirectory, "Transactions");
        private readonly AuthenticationStateService _authenticationStateService;

        public DashboardService(AuthenticationStateService authenticationStateService)
        {
            _authenticationStateService = authenticationStateService;
            // Ensure the directory exists
            Directory.CreateDirectory(transactionsDirectory);
        }

        private string GetUserTransactionsFilePath(string userName)
        {
            return Path.Combine(transactionsDirectory, $"{userName}_Transactions.json");
        }

        public async Task<IEnumerable<Transaction>> GetFilteredTransactions(DateTime? fromDate, DateTime? toDate, string tagFilter, string typeFilter)
        {
            var transactions = await LoadUserTransactionsAsync();
            var filtered = transactions.AsEnumerable();

            if (fromDate.HasValue)
                filtered = filtered.Where(t => t.Date >= fromDate.Value);

            if (toDate.HasValue)
                filtered = filtered.Where(t => t.Date <= toDate.Value);

            if (!string.IsNullOrEmpty(tagFilter))
                filtered = filtered.Where(t => t.Tags != null && t.Tags.Contains(tagFilter, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(typeFilter))
                filtered = filtered.Where(t => t.Type.Equals(typeFilter, StringComparison.OrdinalIgnoreCase));

            return filtered;
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            if (transaction == null || string.IsNullOrWhiteSpace(transaction.Description))
            {
                throw new ArgumentException("Invalid transaction data.");
            }

            var transactions = await LoadUserTransactionsAsync();
            transactions.Add(transaction);
            await SaveUserTransactionsAsync(transactions);
        }

        public async Task<decimal> CalculateTotal(string type)
        {
            var transactions = await LoadUserTransactionsAsync();

            if (transactions == null || string.IsNullOrEmpty(type))
            {
                return 0; // Return 0 if transactions list or type is invalid
            }

            if (type != "Debt")
            {
                // For Income and Expense, calculate normally
                return transactions.Where(t => t.Type != null && t.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                                   .Sum(t => t.Amount);
            }

            // For Debt, only include positive debt amounts (new debts)
            return transactions.Where(t => t.Type != null && t.Type.Equals(type, StringComparison.OrdinalIgnoreCase) && t.Amount > 0)
                               .Sum(t => t.Amount);
        }


        public async Task<decimal> CalculateAvailableBalance()
        {
            var transactions = await LoadUserTransactionsAsync();

            var totalIncome = transactions.Where(t => t.Type == "Income")
                                        .Sum(t => t.Amount);

            var totalExpense = transactions.Where(t => t.Type == "Expense")
                                         .Sum(t => t.Amount);

            var remainingDebt = await CalculateRemainingDebt();

            var availableBalance = totalIncome - totalExpense + remainingDebt;

            return Math.Max(0, availableBalance);
        }

        public async Task<decimal> CalculateRemainingDebt()
        {
            var transactions = await LoadUserTransactionsAsync();

            var originalDebt = transactions.Where(t => t.Type == "Debt" && t.Amount > 0)
                                         .Sum(t => t.Amount);

            var clearedDebt = transactions.Where(t => t.Type == "Debt" && t.Amount < 0)
                                        .Sum(t => Math.Abs(t.Amount));

            return Math.Max(0, originalDebt - clearedDebt);
        }

        public async Task<decimal> CalculateOriginalDebt()
        {
            var transactions = await LoadUserTransactionsAsync();
            return transactions.Where(t => t.Type == "Debt" && t.Amount > 0)
                             .Sum(t => t.Amount);
        }

        public async Task<decimal> CalculateClearedDebt()
        {
            var transactions = await LoadUserTransactionsAsync();
            return transactions.Where(t => t.Type == "Debt" && t.Amount < 0)
                             .Sum(t => Math.Abs(t.Amount));
        }

        private async Task<List<Transaction>> LoadUserTransactionsAsync()
        {
            try
            {
                var userName = GetAuthenticatedUserName();
                var filePath = GetUserTransactionsFilePath(userName);

                if (!File.Exists(filePath))
                {
                    return new List<Transaction>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading transactions: {ex.Message}");
                return new List<Transaction>();
            }
        }

        private async Task SaveUserTransactionsAsync(List<Transaction> transactions)
        {
            try
            {
                var userName = GetAuthenticatedUserName();
                var filePath = GetUserTransactionsFilePath(userName);

                var json = JsonSerializer.Serialize(transactions, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving transactions: {ex.Message}");
                throw;
            }
        }

        private string GetAuthenticatedUserName()
        {
            var authenticatedUser = _authenticationStateService.GetAuthenticatedUser();
            if (authenticatedUser != null)
            {
                return authenticatedUser.UserName;
            }
            throw new InvalidOperationException("No authenticated user found.");
        }

        public async Task<List<Transaction>> LoadAllTransactionsAsync()
        {
            try
            {
                var allTransactions = new List<Transaction>();
                var files = Directory.GetFiles(transactionsDirectory, "*_Transactions.json");

                foreach (var file in files)
                {
                    var json = await File.ReadAllTextAsync(file);
                    var transactions = JsonSerializer.Deserialize<List<Transaction>>(json);
                    if (transactions != null)
                    {
                        allTransactions.AddRange(transactions);
                    }
                }

                return allTransactions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading all transactions: {ex.Message}");
                return new List<Transaction>();
            }
        }

        public async Task SaveAllTransactionsAsync(List<Transaction> transactions)
        {
            try
            {
                foreach (var transaction in transactions)
                {
                    var filePath = GetUserTransactionsFilePath(transaction.UserName);
                    var existingTransactions = await LoadUserTransactionsAsync();
                    existingTransactions.Add(transaction);

                    var json = JsonSerializer.Serialize(existingTransactions, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(filePath, json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving all transactions: {ex.Message}");
                throw;
            }
        }
    }
}