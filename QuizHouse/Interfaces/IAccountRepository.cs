using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Interfaces
{
    public interface IAccountRepository
    {
        public Task<bool> AccountExists(string email, string username = null);
    }
}
