using Cimon.DB;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Radzen.Blazor;

namespace Cimon.Shared;

public class DbContextComponent<TItem> : ComponentBase where TItem : class, new()
{
	[Inject] protected CimonDbContext DbContext { get; set; } = null!;

	protected RadzenDataGrid<TItem?> Grid;
	protected IEnumerable<TItem?> Items = null!;

	protected TItem? itemToInsert;
	protected TItem? itemToUpdate;

	protected void Reset() {
		itemToInsert = default;
		itemToUpdate = default;
	}

	protected override async Task OnInitializedAsync() {
		await base.OnInitializedAsync();
		Items = DbContext.Set<TItem>();
	}

	protected async Task EditRow(TItem? team) {
		itemToUpdate = team;
		await Grid.EditRow(team);
	}

	protected void OnUpdateRow(TItem team) {
		if (team == itemToInsert) {
			itemToInsert = null;
		}
		itemToUpdate = null;
		DbContext.Update(team);
		DbContext.SaveChanges();
	}

	protected async Task SaveRow(TItem? team) {
		await Grid.UpdateRow(team);
	}

	protected void CancelEdit(TItem? team) {
		if (team == itemToInsert) {
			itemToInsert = null;
		}
		itemToUpdate = null;
		Grid.CancelEditRow(team);
		var entry = DbContext.Entry(team);
		if (entry.State == EntityState.Modified) {
			entry.CurrentValues.SetValues(entry.OriginalValues);
			entry.State = EntityState.Unchanged;
		}
	}

	protected async Task DeleteRow(TItem? team) {
		if (team == itemToInsert) {
			itemToInsert = null;
		}
		if (team == itemToUpdate) {
			itemToUpdate = null;
		}
		if (Items.Contains(team)) {
			DbContext.Remove(team);
			await DbContext.SaveChangesAsync();
			await Grid.Reload();
		} else {
			Grid.CancelEditRow(team);
			await Grid.Reload();
		}
	}

	protected async Task InsertRow() {
		itemToInsert = new TItem();
		await Grid.InsertRow(itemToInsert);
	}

	protected void OnCreateRow(TItem team) {
		DbContext.Add(team);
		DbContext.SaveChanges();
		itemToInsert = null;
	}
}
