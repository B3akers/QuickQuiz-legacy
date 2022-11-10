using MongoDB.Driver;
using QuizHouse.Dto;
using QuizHouse.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Services
{
    public class AccountRepositoryService : IAccountRepository
    {
        private QuizService _quizService;
        public AccountRepositoryService(QuizService quizService)
        {
            _quizService = quizService;
        }

        public async Task<bool> AccountExists(string email, string username)
        {
            var accounts = _quizService.GetAccountsCollection();
            var builder = Builders<AccountDTO>.Filter;
            var filter = builder.Eq(x => x.Email, email);
            if (!string.IsNullOrEmpty(username))
                filter |= builder.Eq(x => x.Username, username);

            return await (await accounts.FindAsync(filter, new FindOptions<AccountDTO>() { Collation = new Collation("en", strength: CollationStrength.Secondary) })).FirstOrDefaultAsync() != null;
        }
    }
}
