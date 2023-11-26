using MediatR;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Cimon.DB;

public record EntityCreatedNotification<TEntity>(EntityEntry<TEntity> Entry): INotification
	where TEntity : class;
