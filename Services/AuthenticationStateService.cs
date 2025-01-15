using System;
using Coursework.Models;

namespace Coursework.Services
{
    public class AuthenticationStateService
    {
        private User authenticatedUser;

        public event Action OnAuthenticationStateChanged;

        public User GetAuthenticatedUser()
        {
            return authenticatedUser;
        }

        public void SetAuthenticatedUser(User user)
        {
            authenticatedUser = user;
            Console.WriteLine(authenticatedUser);
            OnAuthenticationStateChanged?.Invoke(); // Notify subscribers about the state change
        }

        public string GetUserCurrency()
        {
            var user = GetAuthenticatedUser();
            return user?.Currency ?? "$"; // Default to $ if no currency is selected
        }

        public bool IsAuthenticated()
        {
            return authenticatedUser != null;
        }

        public void Logout()
        {
            authenticatedUser = null;
            OnAuthenticationStateChanged?.Invoke(); // Notify subscribers about the state change
        }
    }
}
