using Cimon.Contracts.Services;
using Cimon.DB;
using Cimon.DB.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.CIConnectors;

public record RefreshCIConnectorSettings(EntityEntry<CIConnector> Connector): INotification;

public class CIConnectorCreatedHandler(IServiceProvider serviceProvider)
	: INotificationHandler<EntityCreatedNotification<CIConnector>>, INotificationHandler<RefreshCIConnectorSettings>
{
	public async Task Handle(EntityCreatedNotification<CIConnector> notification, CancellationToken cancellationToken) {
		var entry = notification.Entry;
		await AddSettings(cancellationToken, entry, false);
	}

	private async Task AddSettings(CancellationToken cancellationToken, EntityEntry<CIConnector> entry, bool sync) {
		var connector = entry.Entity;
		var ciSystem = connector.CISystem;
		var provider = serviceProvider.GetKeyedService<IBuildConfigProvider>(ciSystem);
		var settings = provider.GetSettings();
		var existing = new HashSet<string>();
		if (sync) {
			var keys = await entry.Context.Set<CIConnectorSetting>().Where(x => x.CIConnector.Id == connector.Id)
				.Select(x => x.Key).ToListAsync(cancellationToken: cancellationToken);
			existing = keys.ToHashSet();
		}
		foreach (var item in settings) {
			if (existing.Contains(item.Key)) continue;
			var settingItem = CIConnectorSetting.Create();
			settingItem.Key = item.Key;
			settingItem.Value = item.Value;
			settingItem.CIConnector = connector;
			entry.Context.Add(settingItem);
		}
	}

	public async Task Handle(RefreshCIConnectorSettings notification, CancellationToken cancellationToken) {
		await AddSettings(cancellationToken, notification.Connector, true);
		await notification.Connector.Context.SaveChangesAsync(cancellationToken);
	}
}
