using Cimon.DB;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Radzen.Blazor;

namespace Cimon.Shared;

public class DbContextComponent<TItem> : ReactiveComponent where TItem : class, IEntityCreator<TItem>
{
	[Inject] protected CimonDbContext DbContext { get; set; } = null!;

	protected RadzenDataGrid<TItem> Grid = null!;
	protected IQueryable<TItem> Items = null!;

	protected TItem? ItemToInsert;
	protected TItem? ItemToUpdate;

	protected void Reset() {
		ItemToInsert = default;
		ItemToUpdate = default;
	}

	protected override async Task OnInitializedAsync() {
		await base.OnInitializedAsync();
		RefreshItems();
	}

	protected virtual void RefreshItems() {
		Items = GetItems();
	}

	public void Refresh() => RefreshItems();

	protected virtual IQueryable<TItem> GetItems() {
		return DbContext.Set<TItem>();
	}

	protected virtual async Task EditRow(TItem item) {
		ItemToUpdate = item;
		await Grid.EditRow(item);
	}

	protected void OnUpdateRow(TItem team) {
		if (team == ItemToInsert) {
			ItemToInsert = null;
		}
		ItemToUpdate = null;
		DbContext.Update(team);
		DbContext.SaveChanges();
	}

	protected virtual async Task SaveRow(TItem item) {
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

	protected virtual async Task DeleteRow(TItem? item) {
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

	protected virtual async Task InsertRow() {
		ItemToInsert = TItem.Create();
		await Grid.InsertRow(ItemToInsert);
	}

	protected virtual async Task OnCreateRow(TItem team) {
		await DbContext.AddAsync(team);
		await DbContext.SaveChangesAsync();
		ItemToInsert = null;
	}
}
