using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using QuizHouse.Dto;
using QuizHouse.Interfaces;
using QuizHouse.Utility;
using QuizHouse.WebSockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Services
{
    public class AccountRepositoryService : IAccountRepository
    {
        private WebSocketGameHandler _gameHandler;
        private DatabaseService _quizService;
        private IPasswordHasher _passwordHasher;
        private IEmailProvider _emailProvider;

        private Collation _ignoreCaseCollation;

        public AccountRepositoryService(DatabaseService quizService, IPasswordHasher passwordHasher, IEmailProvider emailProvider, WebSocketGameHandler gameHandler)
        {
            _quizService = quizService;
            _passwordHasher = passwordHasher;
            _emailProvider = emailProvider;
            _gameHandler = gameHandler;

            _ignoreCaseCollation = new Collation("en", strength: CollationStrength.Secondary);
        }

        public async Task<bool> AccountExists(string email, string username)
        {
            var accounts = _quizService.GetAccountsCollection();
            var builder = Builders<AccountDTO>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrEmpty(email))
            {
                filter = builder.Eq(x => x.Email, email);
                if (!string.IsNullOrEmpty(username))
                    filter |= builder.Eq(x => x.Username, username);
            }
            else if (!string.IsNullOrEmpty(username))
            {
                filter = builder.Eq(x => x.Username, username);
            }

            return await (await accounts.FindAsync(filter, new FindOptions<AccountDTO>() { Collation = _ignoreCaseCollation })).FirstOrDefaultAsync() != null;
        }

        public async Task<AccountDTO> CreateAccount(string email, string username, string password, bool confirmEmail = false)
        {
            var account = new AccountDTO() { Email = email, Username = username, Password = _passwordHasher.Hash(password), CreationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), EmailConfirmed = confirmEmail, IsAdmin = false, Connections = new List<AccountConnectionDTO>() };
            var accounts = _quizService.GetAccountsCollection();

            await accounts.InsertOneAsync(account);

            return account;
        }

        public Task SendTempPasswordEmail(AccountDTO account, string tempPassword)
        {
            _emailProvider.SendEmail(account.Email, "Twoje tymczasowe hasło", $"Twoje nowe tymczasowe hasło na QuizHouse.ovh, zaloguj się i zmień je jak najszybciej w panelu użytkownika!\r\nHasło: {tempPassword}");
            return Task.CompletedTask;
        }

        public async Task SendConfirmationEmail(AccountDTO account, IUrlHelper Url)
        {
            if (account.EmailConfirmed)
                return;

            var emails = _quizService.GetEmailConfirmationsCollection();
            var activeConfirmation = new EmailConfirmationDTO() { AccountId = account.Id, Email = account.Email, CreationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Used = false, Key = Randomizer.RandomString(125) };
            await emails.InsertOneAsync(activeConfirmation);

            _emailProvider.SendEmail(account.Email, "Potwierdź swój adres email", $"Aby potwierdzić swoje konto na QuizHouse.ovh kliknij tutaj: {Url.Action("ConfirmEmail", "Login", new { key = activeConfirmation.Key }, Url.ActionContext.HttpContext.Request.Scheme)}");
        }

        public async Task<(UserRequestConfrimStatus, string)> TryConfirmEmail(string key)
        {
            if (string.IsNullOrEmpty(key))
                return (UserRequestConfrimStatus.WrongKey, string.Empty);

            var emails = _quizService.GetEmailConfirmationsCollection();
            var activeConfirmation = await (await emails.FindAsync(x => x.Key == key)).FirstOrDefaultAsync();
            if (activeConfirmation == null)
                return (UserRequestConfrimStatus.WrongKey, string.Empty);

            if (activeConfirmation.Used)
                return (UserRequestConfrimStatus.AlreadyConfirmed, string.Empty);

            await emails.UpdateOneAsync(x => x.Id == activeConfirmation.Id, Builders<EmailConfirmationDTO>.Update.Set(x => x.Used, true));

            var accounts = _quizService.GetAccountsCollection();
            var account = await (await accounts.FindAsync(x => x.Id == activeConfirmation.AccountId)).FirstOrDefaultAsync();
            if (account == null)
                return (UserRequestConfrimStatus.WrongKey, string.Empty);

            if (account.EmailConfirmed)
                return (UserRequestConfrimStatus.AlreadyConfirmed, string.Empty);

            if (account.Email != activeConfirmation.Email)
                return (UserRequestConfrimStatus.WrongKey, string.Empty);

            await accounts.UpdateOneAsync(x => x.Id == activeConfirmation.AccountId && x.Email == activeConfirmation.Email, Builders<AccountDTO>.Update.Set(x => x.EmailConfirmed, true));

            return (UserRequestConfrimStatus.Confirmed, activeConfirmation.AccountId);
        }

        public async Task<AccountDTO> GetAccount(string accountId)
        {
            if (string.IsNullOrEmpty(accountId) || !ObjectId.TryParse(accountId, out _))
                return null;

            var accounts = _quizService.GetAccountsCollection();
            return await (await accounts.FindAsync(x => x.Id == accountId)).FirstOrDefaultAsync();
        }

        public async Task<AccountDTO> GetAccountByEmail(string email)
        {
            var accounts = _quizService.GetAccountsCollection();
            return await (await accounts.FindAsync(x => x.Email == email, new FindOptions<AccountDTO>() { Collation = _ignoreCaseCollation })).FirstOrDefaultAsync();
        }

        public async Task UpdateLastEmailConfirmSend(AccountDTO account, long time)
        {
            var accounts = _quizService.GetAccountsCollection();
            await accounts.UpdateOneAsync(x => x.Id == account.Id, Builders<AccountDTO>.Update.Set(x => x.LastEmailConfirmSend, time));
        }

        public async Task UpdateLastEmailPasswordSend(AccountDTO account, long time)
        {
            var accounts = _quizService.GetAccountsCollection();
            await accounts.UpdateOneAsync(x => x.Id == account.Id, Builders<AccountDTO>.Update.Set(x => x.LastEmailPasswordSend, time));
        }

        public async Task DeleteAccount(AccountDTO account)
        {
            if (account.EmailConfirmed)
                return;

            var accounts = _quizService.GetAccountsCollection();
            var confirmations = _quizService.GetEmailConfirmationsCollection();

            await accounts.DeleteOneAsync(x => x.Id == account.Id);
            await confirmations.DeleteManyAsync(x => x.AccountId == account.Id);
        }

        public async Task SendPasswordResetRequest(AccountDTO account, IUrlHelper Url)
        {
            var resets = _quizService.GetPasswordResetsCollection();
            var resetPassword = new PasswordResetDTO() { AccountId = account.Id, CreationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Used = false, Key = Randomizer.RandomString(125) };
            await resets.InsertOneAsync(resetPassword);

            _emailProvider.SendEmail(account.Email, "Zresetuj swoje hasło", $"Witaj {account.Username}, aby potwierdzić reset hasła na QuizHouse.ovh kliknij tutaj: {Url.Action("ResetPassword", "Login", new { key = resetPassword.Key }, Url.ActionContext.HttpContext.Request.Scheme)}");
        }

        public async Task<(UserRequestConfrimStatus, string)> TryConfirmPasswordReset(string key)
        {
            if (string.IsNullOrEmpty(key))
                return (UserRequestConfrimStatus.WrongKey, string.Empty);

            var resets = _quizService.GetPasswordResetsCollection();
            var activeRequest = await (await resets.FindAsync(x => x.Key == key)).FirstOrDefaultAsync();
            if (activeRequest == null)
                return (UserRequestConfrimStatus.WrongKey, string.Empty);

            if (activeRequest.Used)
                return (UserRequestConfrimStatus.AlreadyConfirmed, string.Empty);

            await resets.UpdateOneAsync(x => x.Id == activeRequest.Id, Builders<PasswordResetDTO>.Update.Set(x => x.Used, true));

            var account = await GetAccount(activeRequest.AccountId);
            if (account == null)
                return (UserRequestConfrimStatus.WrongKey, string.Empty);

            var tempPassword = Randomizer.RandomPassword(18);

            await ChangePassword(account, tempPassword, true);
            await SendTempPasswordEmail(account, tempPassword);

            return (UserRequestConfrimStatus.Confirmed, account.Id);
        }

        public async Task ChangePassword(AccountDTO account, string password, bool logout)
        {
            var devices = _quizService.GetDevicesCollection();
            var accounts = _quizService.GetAccountsCollection();

            if (logout)
            {
                await _gameHandler.DisconnectUser(account.Id);
                await devices.DeleteManyAsync(x => x.AccountId == account.Id);
                await accounts.UpdateOneAsync(x => x.Id == account.Id, Builders<AccountDTO>.Update.Set(x => x.Password, _passwordHasher.Hash(password)).Set(x => x.LastPasswordChange, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
                return;
            }

            await accounts.UpdateOneAsync(x => x.Id == account.Id, Builders<AccountDTO>.Update.Set(x => x.Password, _passwordHasher.Hash(password)));
        }

        public async Task ChangeUsername(AccountDTO account, string username)
        {
            var accounts = _quizService.GetAccountsCollection();
            await accounts.UpdateOneAsync(x => x.Id == account.Id, Builders<AccountDTO>.Update.Set(x => x.Username, username));
        }
    }
}
