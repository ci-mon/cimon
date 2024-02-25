import {expect, test} from "@playwright/test";
import {authAsUser} from "./auth";

authAsUser(null);
test.beforeEach(async ({page}) => {
    await page.goto('/');
});

test('add monitor', async ({page}) => {
    const testRunId = Math.floor(Math.random()*1000);
    await page.getByLabel('monitor-list').click();
    await page.getByLabel('add-monitor').click();
    let cardCaption = page.getByTestId('monitor-item-title').getByText('Untitled').last();
    let card = page.locator('.monitor-item').filter({has: cardCaption}).first();
    await card.getByTestId('setup').click();
    await page.waitForURL(/setupMonitor/);
    const titleEdit = page.getByLabel('edit-title');
    await expect(titleEdit).toHaveValue('Untitled');
    const monitorName = `My test monitor ${testRunId}`;
    await titleEdit.fill(monitorName);
    await page.getByLabel('edit-share').click();
    await page.getByTestId('save').click();
    await page.getByTestId('add-build-config').click();
    const dialog = page.getByTestId('build-config-dialog');
    await expect(dialog).toBeVisible();
    //await page.getByRole('tablist').getByText('Demo:demo_main').click();
    await dialog.getByTestId('key').getByText('Unit', {exact: true}).click();
    await dialog.getByTestId('key').getByText('Integration (MSSQL)').click();
    await dialog.getByLabel('save-build-configs').click();
    await expect(dialog).not.toBeVisible();
    await page.getByTestId('add-build-config').click();
    await expect(dialog).toBeVisible();
    await dialog.getByLabel('select-all').click();
    await dialog.getByLabel('save-build-configs').click();
    await expect(dialog).not.toBeVisible();
    await page.getByTestId('close').click();
    cardCaption = page.getByTestId('monitor-item-title').getByText(monitorName);
    card = page.locator('.monitor-item').filter({has: cardCaption}).first();
    await expect(card).toBeVisible();
    await card.getByTestId('view').click();
    await page.waitForURL(/monitor/);
    await expect(page.locator('.monitor .build-info-item').getByTestId('build-info-name')
        .getByText('Unit', {exact: true})).toBeVisible({timeout: 15000});
    await expect(page.locator('.monitor .build-info-item.failed.with-committers')
        .getByText('Integration (PostgreSQL)', {exact: true})).toBeVisible({timeout: 15000});
    await page.getByLabel('monitor-list').click();
    await card.getByTestId('remove').click();
    await expect(card).not.toBeVisible();
});
