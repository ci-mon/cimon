using Cimon;
using Cimon.Auth;
using Cimon.Data;
using Cimon.Data.TeamCity;
using Cimon.Hubs;
using Microsoft.Extensions.Options;
using Radzen;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuth();
builder.Services.AddCors();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddSingleton<MonitorService>();
builder.Services.AddSingleton<BuildInfoService>();
builder.Services.AddSingleton<IBuildInfoProvider, TcBuildInfoProvider>();
builder.Services.AddSingleton<IList<IBuildInfoProvider>>(sp => sp.GetServices<IBuildInfoProvider>().ToList());
builder.Services.AddOptions()
	.Configure<CimonOptions>(builder.Configuration.GetSection("CimonOption"))
	.AddTransient<BuildInfoMonitoringSettings>(provider => provider.GetRequiredService<IOptions<CimonOptions>>().Value.BuildInfoMonitoring)
	.AddTransient<AuthOptions>(provider => provider.GetRequiredService<IOptions<CimonOptions>>().Value.Auth)
	.AddTransient<JwtOptions>(provider => provider.GetRequiredService<IOptions<CimonOptions>>().Value.Jwt);

// Radzen
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
	app.UseExceptionHandler("/Error");

	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseCors(policyBuilder => policyBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();
app.MapControllers();
app.MapHub<UserHub>("/hubs/user");
app.MapFallbackToPage("/_Host");
app.Run();
