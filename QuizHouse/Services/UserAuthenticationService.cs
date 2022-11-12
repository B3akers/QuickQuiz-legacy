using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
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
	public class UserAuthenticationService : IUserAuthentication
	{
		private DatabaseService _quizService;
		private IPasswordHasher _passwordHasher;
		private IAccountRepository _accountRepository;

		public UserAuthenticationService(DatabaseService quizService, IPasswordHasher passwordHasher, IAccountRepository accountRepository)
		{
			_quizService = quizService;
			_passwordHasher = passwordHasher;
			_accountRepository = accountRepository;
		}

		public async Task AuthorizeForUser(HttpContext context, string accountId, bool permanent)
		{
			var accounts = _quizService.GetAccountsCollection();
			var account = await (await accounts.FindAsync(x => x.Id == accountId)).FirstOrDefaultAsync();
			if (account == null)
				return;

			if (permanent)
			{
				var devices = _quizService.GetDevicesCollection();
				var device = new DeviceDTO() { AccountId = account.Id, Key = Randomizer.RandomString(64), LastUse = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };

				await devices.InsertOneAsync(device);

				context.Response.Cookies.Append("deviceKey", device.Key);
			}

			context.Session.SetString("passTimestamp", account.LastPasswordChange.ToString());
			context.Session.SetString("userId", accountId);
		}

		public bool CheckCredentials(AccountDTO account, string password)
		{
			return _passwordHasher.Check(account.Password, password);
		}

		public async Task LogoutUser(HttpContext context)
		{
			if (context.Request.Cookies.TryGetValue("deviceKey", out var deviceKey))
			{
				var devices = _quizService.GetDevicesCollection();
				await devices.DeleteOneAsync(x => x.Key == deviceKey);
			}

			context.Session.Remove("passTimestamp");
			context.Session.Remove("userId");
			context.Response.Cookies.Delete("deviceKey");
		}

		public async Task<AccountDTO> GetAuthenticatedUser(HttpContext context)
		{
			DeviceDTO accountDevice = null;
			var devices = _quizService.GetDevicesCollection();
			var userId = context.Session.GetString("userId");
			if (string.IsNullOrEmpty(userId))
			{
				if (!context.Request.Cookies.TryGetValue("deviceKey", out string deviceKey))
					return null;

				accountDevice = await (await devices.FindAsync(x => x.Key == deviceKey)).FirstOrDefaultAsync();
				if (accountDevice == null)
					return null;

				context.Session.SetString("userId", accountDevice.AccountId);

				userId = accountDevice.AccountId;
			}

			var account = await _accountRepository.GetAccount(userId);

			if (account == null)
			{
				context.Session.Remove("userId");
				if (accountDevice != null)
				{
					context.Response.Cookies.Delete("deviceKey");
					await devices.DeleteOneAsync(x => x.Id == accountDevice.Id);
				}
			}
			else if (accountDevice != null)
			{
				await devices.UpdateOneAsync(x => x.Id == accountDevice.Id, Builders<DeviceDTO>.Update.Set(x => x.LastUse, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
				context.Session.SetString("passTimestamp", account.LastPasswordChange.ToString());
			}
			else
			{
				if (context.Session.GetString("passTimestamp") != account.LastPasswordChange.ToString())
				{
					context.Session.Remove("passTimestamp");
					context.Session.Remove("userId");
					context.Response.Cookies.Delete("deviceKey");
					return null;
				}
			}

			return account;
		}
	}
}
