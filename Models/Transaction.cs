using System;

namespace Coursework.Models
{
    public class Transaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Generates a unique ID
        public string UserName { get; set; } // Identifier for the user who owns this transaction

        public string Description { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } // Income, Expense, Debt
        public string Tags { get; set; } = string.Empty;

        public string Labels { get; set; } = string.Empty;




    }
}
