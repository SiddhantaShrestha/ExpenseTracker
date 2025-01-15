using System;
using Coursework.Models;

namespace Coursework.Services
{
    public class AuthenticationStateService
    {
        private User authenticatedUser;

        public User GetAuthenticatedUser()
        {
            return authenticatedUser;
        }


        public void SetAuthenticatedUser(User user)
        {
            authenticatedUser = user;
            Console.WriteLine(authenticatedUser);
        }

        public string GetUserCurrency()
        {
            var user = GetAuthenticatedUser();
            return user?.Currency ?? "$"; // Default to $ if no currency is selected
        }

        public bool IsAuthenticated()
        {
            if (authenticatedUser != null)
            {
                return true;
            }
            return false;
        }

        public void Logout()
        {
            authenticatedUser = null;
        }
    }
}
