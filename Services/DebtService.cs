using Coursework.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Coursework.Services
{
    public class DebtService : IDebtService
    {
        private readonly string _baseDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "Debts");
        private readonly AuthenticationStateService _authStateService;

        public DebtService(AuthenticationStateService authStateService)
        {
            _authStateService = authStateService;

            // Ensure the directory exists
            if (!Directory.Exists(_baseDirectory))
            {
                Directory.CreateDirectory(_baseDirectory);
            }
        }

        private string GetUserFilePath()
        {
            var currentUser = _authStateService.GetAuthenticatedUser()?.UserName;
            if (string.IsNullOrEmpty(currentUser))
            {
                throw new InvalidOperationException("No authenticated user.");
            }

            return Path.Combine(_baseDirectory, $"{currentUser}.json");
        }

        public async Task<List<Debt>> LoadDebtsAsync()
        {
            var filePath = GetUserFilePath();
            if (!File.Exists(filePath))
            {
                return new List<Debt>();
            }

            var jsonData = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<Debt>>(jsonData) ?? new List<Debt>();
        }

        public async Task SaveDebtsAsync(List<Debt> debts)
        {
            var filePath = GetUserFilePath();
            var jsonData = JsonSerializer.Serialize(debts, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, jsonData);
        }

        public async Task<IEnumerable<Debt>> GetAllDebtsAsync()
        {
            return await LoadDebtsAsync();
        }

        public async Task<Debt> GetDebtByIdAsync(string id)
        {
            var debts = await LoadDebtsAsync();
            return debts.FirstOrDefault(d => d.Id == id);
        }

        public async Task AddDebtAsync(Debt debt)
        {
            if (debt == null)
                throw new ArgumentNullException(nameof(debt));

            var currentUser = _authStateService.GetAuthenticatedUser()?.UserName;
            if (string.IsNullOrEmpty(currentUser))
                throw new InvalidOperationException("No authenticated user.");

            debt.UserName = currentUser;

            var debts = await LoadDebtsAsync();
            debts.Add(debt);
            await SaveDebtsAsync(debts);
        }

        public async Task MakePaymentAsync(string debtId, decimal paymentAmount)
        {
            var debts = await LoadDebtsAsync();
            var debt = debts.FirstOrDefault(d => d.Id == debtId);

            if (debt == null)
                throw new InvalidOperationException("Debt not found");

            debt.MakePayment(paymentAmount);
            await SaveDebtsAsync(debts);
        }

        public async Task<decimal> GetTotalDebtAsync()
        {
            var debts = await LoadDebtsAsync();
            return debts.Sum(d => d.Amount);
        }

        public async Task<decimal> GetTotalPaidDebtAsync()
        {
            var debts = await LoadDebtsAsync();
            return debts.Sum(d => d.AmountPaid);
        }

        public async Task<decimal> GetRemainingDebtAsync()
        {
            var debts = await LoadDebtsAsync();
            return debts.Sum(d => d.RemainingAmount);
        }

        // New method to get pending debts
        public async Task<IEnumerable<Debt>> GetPendingDebtsAsync()
        {
            var debts = await LoadDebtsAsync();

            // Return only debts that have remaining amounts (i.e., not paid off)
            return debts.Where(d => d.RemainingAmount > 0);
        }
    }
}
