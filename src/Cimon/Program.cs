using Cimon;
using Cimon.Auth;
using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Data;
using Cimon.Data.BuildInformation;
using Cimon.Data.Common;
using Cimon.Data.Jenkins;
using Cimon.Data.ML;
using Cimon.Data.Secrets;
using Cimon.Data.TeamCity;
using Cimon.Data.Users;
using Cimon.DB;
using Cimon.Internal;
using Cimon.Monitors;
using Cimon.NativeApp;
using Cimon.Shared;
using Cimon.Users;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Radzen;
using Serilog;
using NotificationService = Radzen.NotificationService;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
	loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration));
builder.Configuration.AddEnvironmentVariables("CIMON_");
builder.Services.AddAuth();
builder.Services.AddCors();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddTransient<UnprotectedLocalStorage>();

var isDevelopment = builder.Environment.IsDevelopment();
builder.Services.AddCimonData()
	.AddMediatR(configuration => {
		configuration
			.AddCimonData()
			.RegisterServicesFromAssemblyContaining<SavedMonitorInLocalStoragesBehaviour>()
			.AddBehavior<IPipelineBehavior<GetDefaultMonitorRequest, string>, SavedMonitorInLocalStoragesBehaviour>();
	});
builder.Services.AddCimonDb(builder.Configuration, isDevelopment);
builder.Services.AddCimonDataTeamCity();
builder.Services.AddCimonDataJenkins();
builder.Services.AddKeyedScoped<IBuildConfigProvider, DemoBuildConfigProvider>(CISystem.Demo);
builder.Services.AddCimonML();
builder.Services.AddKeyedScoped<IHubAccessor<IUserClientApi>, HubAccessor<UserHub, IUserClientApi>>(nameof(UserHub));

builder.Services.AddSingleton<NativeAppService>();
builder.Services.AddSingleton<INotificationService, SignalRNotificationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<GetCurrentPrincipal>(provider => {
	return async () => {
		var authState = provider.GetService<Task<AuthenticationState>>();
		if (authState == null) return provider.GetService<IHttpContextAccessor>()?.HttpContext?.User;
		var state = await authState;
		return state.User;
	};
});
builder.Services.AddScoped<AppInitialStateAccessor>();
	
builder.Services.AddOptions()
	.ConfigureSecrets<CimonSecrets>()
	.Configure<CimonDataSettings>(settings => {
		settings.IsDevelopment = isDevelopment;
	})
	.ConfigureSecretsFromConfig<VaultSecrets>()
	.ConfigureSecrets<TeamcitySecrets>()
	.ConfigureSecrets<JenkinsSecrets>()
	.ConfigureSecrets<LdapClientSecrets>()
	.ConfigureSecrets<NativeAppSecrets>()
	.AddTransient<BuildInfoMonitoringSettings>(provider => provider.GetRequiredService<IOptions<CimonSecrets>>().Value.BuildInfoMonitoring)
	.AddTransient<AuthOptions>(provider => provider.GetRequiredService<IOptions<CimonSecrets>>().Value.Auth)
	.AddTransient<JwtOptions>(provider => provider.GetRequiredService<IOptions<CimonSecrets>>().Value.Jwt);

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 20_000_000);

WebApplication app = builder.Build();

await DbInitializer.Init(app.Services);
if (!app.Environment.IsDevelopment()) {
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseCors(policyBuilder => policyBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapBlazorHub();
app.MapHub<UserHub>("/hubs/user");
app.MapGet("/", context => Task.Run(()=> context.Response.Redirect("/MonitorList")));
app.MapFallbackToPage("/_Host");
app.MapFallbackToPage("/buildDiscussion/{param?}", "/_Host");

AppActors.Init(app.Services);
app.Lifetime.ApplicationStopping.Register(() => AppActors.Instance.Stop().GetAwaiter().GetResult());
app.Run();
