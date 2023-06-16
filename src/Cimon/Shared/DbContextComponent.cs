using Cimon.DB;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Radzen.Blazor;

namespace Cimon.Shared;

public class DbContextComponent<TItem> : ComponentBase where TItem : class, IEntityCreator<TItem>
{
	[Inject] protected CimonDbContext DbContext { get; set; } = null!;

	protected RadzenDataGrid<TItem> Grid = null!;
	protected IQueryable<TItem?> Items = null!;

	protected TItem? ItemToInsert;
	protected TItem? ItemToUpdate;

	protected void Reset() {
		ItemToInsert = default;
		ItemToUpdate = default;
	}

	protected override async Task OnInitializedAsync() {
		await base.OnInitializedAsync();
		Items = InitItems();
	}

	protected virtual IQueryable<TItem> InitItems() {
		return DbContext.Set<TItem>();
	}

	protected async Task EditRow(TItem team) {
		ItemToUpdate = team;
		await Grid.EditRow(team);
	}

	protected void OnUpdateRow(TItem team) {
		if (team == ItemToInsert) {
			ItemToInsert = null;
		}
		ItemToUpdate = null;
		DbContext.Update(team);
		DbContext.SaveChanges();
	}

	protected async Task SaveRow(TItem item) {
		await Grid.UpdateRow(item);
	}

	protected void CancelEdit(TItem? item) {
		if (item == null) {
			return;
		}
		if (item == ItemToInsert) {
			ItemToInsert = null;
		}
		ItemToUpdate = null;
		Grid.CancelEditRow(item);
		var entry = DbContext.Entry(item);
		if (entry.State == EntityState.Modified) {
			entry.CurrentValues.SetValues(entry.OriginalValues);
			entry.State = EntityState.Unchanged;
		}
	}

	protected async Task DeleteRow(TItem? item) {
		if (item == null) {
			return;
		}
		if (item == ItemToInsert) {
			ItemToInsert = null;
		}
		if (item == ItemToUpdate) {
			ItemToUpdate = null;
		}
		if (Items.Contains(item)) {
			DbContext.Remove(item);
			await DbContext.SaveChangesAsync();
			await Grid.Reload();
		} else {
			Grid.CancelEditRow(item);
			await Grid.Reload();
		}
	}

	protected async Task InsertRow() {
		ItemToInsert = TItem.Create();
		await Grid.InsertRow(ItemToInsert);
	}

	protected void OnCreateRow(TItem team) {
		DbContext.Add(team);
		DbContext.SaveChanges();
		ItemToInsert = null;
	}
}
