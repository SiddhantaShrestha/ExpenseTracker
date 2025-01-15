﻿using Coursework.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coursework.Services
{
    public interface IDebtService
    {
       
        Task<IEnumerable<Debt>> GetAllDebtsAsync();


        Task<Debt> GetDebtByIdAsync(string id);

   
        Task AddDebtAsync(Debt debt);

        Task MakePaymentAsync(string debtId, decimal paymentAmount);

        Task<decimal> GetTotalDebtAsync();

     
        Task<decimal> GetTotalPaidDebtAsync();


        Task<decimal> GetRemainingDebtAsync();
    }
}