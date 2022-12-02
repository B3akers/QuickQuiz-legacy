using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuickQuiz.Interfaces;
using QuickQuiz.Services;
using QuickQuiz.WebSockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickQuiz
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddSession(options =>
			{
				options.Cookie.Name = "session";
				options.IdleTimeout = TimeSpan.FromMinutes(15);
			});

			services.AddAntiforgery(options =>
			{
				options.FormFieldName = "csrfToken";
				options.HeaderName = "X-Csrf-Token-Value";
			});

			services.AddControllersWithViews();
			services.AddResponseCaching();
			services.AddHttpClient();

			services.AddSingleton<DatabaseService>();
			services.AddSingleton<ConnectionManagerOld>();
			services.AddSingleton<WebSocketHandlerOld>();
			services.AddSingleton<GamesServiceOld>();
			services.AddSingleton<GameManagerService>();
			services.AddSingleton<WebSocketGameHandler>();
			services.AddSingleton<LobbyManagerService>();
			services.AddSingleton<IJwtTokenHandler, JwtTokenHandlerService>();
			services.AddSingleton<IAccountConnector, AccountConnectorService>();
			services.AddSingleton<IEmailProvider, EmailProviderService>();
			services.AddSingleton<IPasswordHasher, PasswordHasher>();
			services.AddSingleton<IAccountRepository, AccountRepositoryService>();
			services.AddSingleton<IUserAuthentication, UserAuthenticationService>();
			services.AddSingleton<ICdnUploader, FileCdnUploaderService>();
			services.AddHostedService<GamesTickServiceOld>();
			services.AddHostedService<GameTickService>();
			services.AddHostedService<DatabaseBackgroundService>();
			services.AddHostedService<ConfigureMongoDbService>();

			JsonConvert.DefaultSettings = () => new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			};
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}
			app.UseResponseCaching();

			app.UseSession();

			app.UseStaticFiles();

			app.UseRouting();

			var wsOptions = new WebSocketOptions()
			{
				KeepAliveInterval = TimeSpan.FromSeconds(120)
			};
			app.UseWebSockets(wsOptions);

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=GameOld}/{action=Index}");
			});
		}
	}
}
