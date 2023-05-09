namespace Cimon.Data.Users;

class TechnicalUsers : ITechnicalUsers
{
	public User MonitoringBot { get; } = new User("monitoring.bot", "Monitoring bot");
}