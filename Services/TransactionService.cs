using Coursework.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Coursework.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly string transactionsDirectory = Path.Combine(AppContext.BaseDirectory, "Transactions");
        private readonly AuthenticationStateService _authenticationStateService;

        public TransactionService(AuthenticationStateService authenticationStateService)
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

            // Only proceed if the transaction type is "Income"
            if (transaction.Type == "Income")
            {
                // Calculate the remaining debt before adding the income transaction
                var remainingDebt = await CalculateRemainingDebt();
                if (remainingDebt > 0)
                {
                    // Calculate the amount to be used to clear the debt
                    var debtToClear = Math.Min(remainingDebt, transaction.Amount);

                    // Add a new "Debt Cleared" transaction
                    transactions.Add(new Transaction
                    {
                        Description = "Debt Cleared",
                        Date = DateTime.Now,
                        Amount = -debtToClear,  // Negative amount to indicate debt clearing
                        Type = "Debt",
                        Tags = "Debt Cleared",
                        Labels = "Auto Generated"
                    });

                    // Subtract the cleared debt from the income amount
                    transaction.Amount -= debtToClear;

                    // If there's any amount left after clearing the debt, we can add it to the balance
                    if (transaction.Amount > 0)
                    {
                        transactions.Add(transaction);
                    }
                }
                else
                {
                    // No debt left, add income normally
                    transactions.Add(transaction);
                }
            }
            else
            {
                // If the transaction is not "Income", just add it normally
                transactions.Add(transaction);
            }

            // Save all transactions after adding the new transaction
            await SaveUserTransactionsAsync(transactions);
        }


        public async Task<decimal> CalculateTotal(string type)
        {
            var transactions = await LoadUserTransactionsAsync();

            if (transactions == null || string.IsNullOrEmpty(type))
            {
                return 0; // Return 0 if transactions list or type is invalid
            }

            return transactions
                .Where(t => !string.IsNullOrEmpty(t.Type)
                            && t.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount); // Directly sum the Amount
        }



        public async Task<decimal> CalculateAvailableBalance()
        {
            var transactions = await LoadUserTransactionsAsync();

            // Calculate total income (excluding debt-clearing transactions)
            var totalIncome = transactions.Where(t => t.Type == "Income")
                                          .Sum(t => t.Amount);

            // Calculate total expenses
            var totalExpense = transactions.Where(t => t.Type == "Expense")
                                           .Sum(t => t.Amount);

            // Calculate remaining debt (updated to match old logic)
            var remainingDebt = await CalculateRemainingDebt();

            // Available balance is income minus expenses, reduced by any remaining debt
            var availableBalance = totalIncome + remainingDebt - totalExpense;

            // Ensure balance is never negative
            return Math.Max(0, availableBalance);
        }

        public async Task<decimal> CalculateRemainingDebt()
        {
            var transactions = await LoadUserTransactionsAsync();

            // Calculate total original debt (positive debt transactions)
            var originalDebt = transactions.Where(t => t.Type == "Debt" && t.Amount > 0)
                                           .Sum(t => t.Amount);

            // Calculate total cleared debt (negative debt transactions)
            var clearedDebt = transactions.Where(t => t.Type == "Debt" && t.Amount < 0)
                                          .Sum(t => Math.Abs(t.Amount));

            // Remaining debt is original minus cleared, minimum 0
            return Math.Max(0, originalDebt - clearedDebt);
        }

        public async Task<decimal> CalculateOriginalDebt()
        {
            var transactions = await LoadUserTransactionsAsync();

            // Original debt is the sum of positive "Debt" transactions
            return transactions.Where(t => t.Type == "Debt" && t.Amount > 0)
                               .Sum(t => t.Amount);
        }

        public async Task<decimal> CalculateClearedDebt()
        {
            var transactions = await LoadUserTransactionsAsync();

            // Cleared debt is the sum of negative "Debt" transactions
            return transactions.Where(t => t.Type == "Debt" && t.Amount < 0)
                               .Sum(t => Math.Abs(t.Amount));
        }


        public async Task<List<Transaction>> LoadUserTransactionsAsync()
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
                var transactions = JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();

                // Ensure all transactions have valid Type
                foreach (var transaction in transactions)
                {
                    if (string.IsNullOrEmpty(transaction.Type))
                    {
                        transaction.Type = "Unknown"; // Default value
                    }
                }

                return transactions;
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
