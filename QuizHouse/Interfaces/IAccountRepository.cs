using Microsoft.AspNetCore.Mvc;
using QuizHouse.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Interfaces
{
    public enum EmailConfirmationStatus
    {
        WrongKey,
        AlreadyConfirmed,
        Confirmed
    };

    public interface IAccountRepository
    {
        public Task<bool> AccountExists(string email, string username);
        public Task<AccountDTO> CreateAccount(string email, string username, string password);
        public Task SendConfirmationEmail(AccountDTO account, IUrlHelper Url);
        public Task<(EmailConfirmationStatus, string)> TryConfirmEmail(string key);
        public Task<AccountDTO> GetAccount(string accountId);
        public Task<AccountDTO> GetAccountByEmail(string email);
		public Task UpdateLastEmailConfirmSend(AccountDTO account, long time);

	}
}
