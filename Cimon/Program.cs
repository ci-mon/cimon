using Microsoft.AspNetCore.Authentication.Negotiate;
using Cimon.Data;
using Cimon.Data.TeamCity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
builder.Services.AddAuthorization(options => {
	// By default, all incoming requests will be authorized according to the default policy.
	options.FallbackPolicy = options.DefaultPolicy;
});
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<MonitorService>();
builder.Services.AddSingleton<BuildInfoService>();
builder.Services.AddSingleton<IBuildInfoProvider, TcBuildInfoProvider>();
builder.Services.AddSingleton<IList<IBuildInfoProvider>>(sp => sp.GetServices<IBuildInfoProvider>().ToList());
builder.Services.AddOptions()
	.Configure<BuildInfoMonitoringSettings>(builder.Configuration.GetSection("BuildInfoMonitoring"));
WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
	app.UseExceptionHandler("/Error");

	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
