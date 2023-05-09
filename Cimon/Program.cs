using Cimon;
using Cimon.Auth;
using Cimon.Data;
using Cimon.Data.TeamCity;
using Cimon.Data.Users;
using Cimon.Hubs;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Radzen;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuth();
builder.Services.AddCors();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddCimonData();

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
	.Configure<CimonOptions>(builder.Configuration.GetSection("CimonOption"))
	.AddTransient<BuildInfoMonitoringSettings>(provider => provider.GetRequiredService<IOptions<CimonOptions>>().Value.BuildInfoMonitoring)
	.AddTransient<AuthOptions>(provider => provider.GetRequiredService<IOptions<CimonOptions>>().Value.Auth)
	.AddTransient<JwtOptions>(provider => provider.GetRequiredService<IOptions<CimonOptions>>().Value.Jwt);

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 20_000_000);

WebApplication app = builder.Build();

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
app.MapFallbackToPage("/_Host");
app.MapFallbackToPage("/buildDiscussion/{param?}", "/_Host");
app.Run();
