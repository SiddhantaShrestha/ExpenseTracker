using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Coursework.Models
{
    public class Debt
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public DateTime? DueDate { get; set; }

        public decimal AmountPaid { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, InProgress, Cleared

        public string Source { get; set; }

        public string Labels { get; set; }

        public string UserName { get; set; }

        // Calculated properties
        public decimal RemainingAmount => Amount - AmountPaid;

        public bool IsCleared => RemainingAmount <= 0;

        public decimal PercentagePaid => Amount > 0 ? (AmountPaid / Amount) * 100 : 0;

        public void MakePayment(decimal paymentAmount)
        {
            if (paymentAmount <= 0)
                throw new ArgumentException("Payment amount must be greater than 0", nameof(paymentAmount));

            AmountPaid += paymentAmount;

            // Update status based on payment
            if (IsCleared)
            {
                Status = "Cleared";
            }
            else if (AmountPaid > 0)
            {
                Status = "InProgress";
            }
        }
    }
}