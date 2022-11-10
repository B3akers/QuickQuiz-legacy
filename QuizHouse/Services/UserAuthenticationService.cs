using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using QuizHouse.Dto;
using QuizHouse.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Services
{
    public class UserAuthenticationService : IUserAuthentication
    {
        private QuizService _quizService;
        public UserAuthenticationService(QuizService quizService)
        {
            _quizService = quizService;
        }

        public async Task<string> GetAuthenticatedUserId(HttpContext context)
        {
            var userId = context.Session.GetString("userId");
            if (!string.IsNullOrEmpty(userId))
                return userId;

            if (!context.Request.Cookies.TryGetValue("deviceKey", out string deviceKey))
                return null;

            var devices = _quizService.GetDevicesCollection();
            var accountDevice = await (await devices.FindAsync(x => x.Key == deviceKey)).FirstOrDefaultAsync();
            if (accountDevice == null)
                return null;

            await devices.UpdateOneAsync(x => x.Key == deviceKey, Builders<DeviceDTO>.Update.Set(x => x.LastUse, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            context.Session.SetString("userId", accountDevice.AccountId);

            return accountDevice.AccountId;
        }
    }
}
