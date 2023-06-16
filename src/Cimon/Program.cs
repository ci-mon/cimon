using Cimon;
using Cimon.Auth;
using Cimon.Contracts.Services;
using Cimon.Data;
using Cimon.Data.BuildInformation;
using Cimon.Data.Secrets;
using Cimon.Data.TeamCity;
using Cimon.Data.Users;
using Cimon.DB;
using Cimon.Hubs;
using Cimon.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Radzen;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuth();
builder.Services.AddCors();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddTransient<UnprotectedLocalStorage>();

var isDevelopment = builder.Environment.IsDevelopment();
builder.Services.AddCimonData();
builder.Services.AddCimonDb(builder.Configuration, isDevelopment);
builder.Services.AddCimonDataTeamCity();

builder.Services.AddSingleton<IBuildLocatorProvider, TcBuildLocatorProvider>();
builder.Services.AddSingleton<IBuildInfoProvider, TcBuildInfoProvider>();
builder.Services.AddSingleton<INotificationService, Cimon.Users.NotificationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<GetCurrentPrincipal>(provider => {
	return async () => {
		var authState = provider.GetService<Task<AuthenticationState>>();
		if (authState == null) return provider.GetService<IHttpContextAccessor>()?.HttpContext?.User;
		var state = await authState;
		return state.User;
	};
});

builder.Services.AddOptions()
	.Configure<CimonOptions>(builder.Configuration.GetSection("CimonOptions"))
	.Configure<CimonDataSettings>(settings => {
		settings.IsDevelopment = isDevelopment;
	})
	.ConfigureVaultSecrets<TeamCitySecrets>()
	.Configure<VaultSettings>(builder.Configuration.GetSection("Vault"))
	.AddTransient<BuildInfoMonitoringSettings>(provider => provider.GetRequiredService<IOptions<CimonOptions>>().Value.BuildInfoMonitoring)
	.AddTransient<AuthOptions>(provider => provider.GetRequiredService<IOptions<CimonOptions>>().Value.Auth)
	.AddTransient<JwtOptions>(provider => provider.GetRequiredService<IOptions<CimonOptions>>().Value.Jwt);

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

app.Run();
