import {test, expect, type Page,} from '@playwright/test';

test.describe.configure({ mode: 'serial' });

test.beforeEach(async ({page}) => {
    await page.goto('/');
})

async function login(page: Page, name: string, pass: string) {
    await page.getByLabel('login-menu').click();
    await page.getByLabel('login-via-password').click();
    await page.waitForURL(/\/Login/);
    await page.locator('input[name="Username"]').fill(name);
    await page.locator('input[name="Password"]').fill(pass);
    await page.locator('.rz-login-buttons button[type="submit"]').click();
    await expect(page.getByLabel('profile-user-name')).toBeVisible();
}

test("login as simple user", async ({page}) => {
    await login(page, 'test', 'test');
    await expect(page.getByLabel('monitor-list')).toBeVisible();
    await expect(page.getByLabel('last-monitor')).toBeVisible();
    await expect(page.getByLabel('native-app')).toBeVisible();
});

test('add monitor', async ({page}) => {
    const testRunId = Math.floor(Math.random()*1000);
    await login(page, 'test', 'test');
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
