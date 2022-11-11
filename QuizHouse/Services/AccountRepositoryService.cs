using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using QuizHouse.Dto;
using QuizHouse.Interfaces;
using QuizHouse.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Services
{
    public class AccountRepositoryService : IAccountRepository
    {
        private QuizService _quizService;
        private IPasswordHasher _passwordHasher;
        private IEmailProvider _emailProvider;

        private Collation _ignoreCaseCollation;

        public AccountRepositoryService(QuizService quizService, IPasswordHasher passwordHasher, IEmailProvider emailProvider)
        {
            _quizService = quizService;
            _passwordHasher = passwordHasher;
            _emailProvider = emailProvider;

            _ignoreCaseCollation = new Collation("en", strength: CollationStrength.Secondary);
        }

        public async Task<bool> AccountExists(string email, string username)
        {
            var accounts = _quizService.GetAccountsCollection();
            var builder = Builders<AccountDTO>.Filter;
            var filter = builder.Eq(x => x.Email, email);
            if (!string.IsNullOrEmpty(username))
                filter |= builder.Eq(x => x.Username, username);

            return await (await accounts.FindAsync(filter, new FindOptions<AccountDTO>() { Collation = _ignoreCaseCollation })).FirstOrDefaultAsync() != null;
        }

        public async Task<AccountDTO> CreateAccount(string email, string username, string password)
        {
            var account = new AccountDTO() { Email = email, Username = username, Password = _passwordHasher.Hash(password), EmailConfirmed = false, IsAdmin = false, Connections = new List<AccountConnection>() };
            var accounts = _quizService.GetAccountsCollection();

            await accounts.InsertOneAsync(account);

            return account;
        }

        public async Task SendConfirmationEmail(AccountDTO account, IUrlHelper Url)
        {
            if (account.EmailConfirmed)
                return;

            var emails = _quizService.EmailConfirmationsCollection();
            var activeConfirmation = await (await emails.FindAsync(x => x.AccountId == account.Id)).FirstOrDefaultAsync();
            if (activeConfirmation == null)
            {
                activeConfirmation = new EmailConfirmationDTO() { AccountId = account.Id, Email = account.Email, LastSend = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Used = false, Key = Randomizer.RandomString(80) };
                await emails.InsertOneAsync(activeConfirmation);
            }
            else
            {
                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (currentTime - activeConfirmation.LastSend < 900) //10min
                    return;

                await emails.UpdateOneAsync(x => x.Id == activeConfirmation.Id, Builders<EmailConfirmationDTO>.Update.Set(x => x.LastSend, currentTime));
            }
            _emailProvider.SendEmail(account.Email, "Potwierdź swój adres email", $"Aby potwierdzić swoje konto na QuizHouse.ovh kliknij tutaj: {Url.Action("ConfirmEmail", "Login", new { key = activeConfirmation.Key }, Url.ActionContext.HttpContext.Request.Scheme)}");
        }

        public async Task<(EmailConfirmationStatus, string)> TryConfirmEmail(string key)
        {
            if (string.IsNullOrEmpty(key))
                return (EmailConfirmationStatus.WrongKey, string.Empty);

            var emails = _quizService.EmailConfirmationsCollection();
            var activeConfirmation = await (await emails.FindAsync(x => x.Key == key)).FirstOrDefaultAsync();
            if (activeConfirmation == null)
                return (EmailConfirmationStatus.WrongKey, string.Empty);

            if (activeConfirmation.Used)
                return (EmailConfirmationStatus.AlreadyConfirmed, string.Empty);

            await emails.UpdateOneAsync(x => x.Id == activeConfirmation.Id, Builders<EmailConfirmationDTO>.Update.Set(x => x.Used, true));

            var accounts = _quizService.GetAccountsCollection();
            var account = await (await accounts.FindAsync(x => x.Id == activeConfirmation.AccountId)).FirstOrDefaultAsync();
            if (account == null)
                return (EmailConfirmationStatus.WrongKey, string.Empty);

            if (account.EmailConfirmed)
                return (EmailConfirmationStatus.AlreadyConfirmed, string.Empty);

            if (account.Email != activeConfirmation.Email)
                return (EmailConfirmationStatus.WrongKey, string.Empty);

            await accounts.UpdateOneAsync(x => x.Id == activeConfirmation.AccountId && x.Email == activeConfirmation.Email, Builders<AccountDTO>.Update.Set(x => x.EmailConfirmed, true));

            return (EmailConfirmationStatus.Confirmed, activeConfirmation.AccountId);
        }

        public async Task<AccountDTO> GetAccount(string accountId)
        {
            var accounts = _quizService.GetAccountsCollection();
            return await (await accounts.FindAsync(x => x.Id == accountId)).FirstOrDefaultAsync();
        }

        public async Task<AccountDTO> GetAccountByEmail(string email)
        {
            var accounts = _quizService.GetAccountsCollection();
            return await (await accounts.FindAsync(x => x.Email == email, new FindOptions<AccountDTO>() { Collation = _ignoreCaseCollation })).FirstOrDefaultAsync();
        }
    }
}
