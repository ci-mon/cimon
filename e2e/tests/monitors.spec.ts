import {expect, Page, test} from "@playwright/test";
import {authAsUser} from "./auth";

authAsUser();

test.beforeEach(async ({page}) => {
    await page.goto('/');
});

async function allBuildInfosLoaded(page: Page){
    await Promise.all((await page.locator('.monitor .build-info-item.loading').all()).map(locator => expect(locator).toBeHidden({timeout: 15000})));
}
test('add monitor', async ({page}) => {
    const testRunId = Math.floor(Math.random()*1000);
    await page.getByLabel('monitor-list').click();
    await page.getByLabel('add-monitor').click();
    let cardCaption = page.getByTestId('monitor-item-title').getByText('Untitled').last();
    if (! await cardCaption.isVisible()) {
        await page.getByLabel('add-monitor').click();
    }
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
    const demoTab = page.getByRole('tablist').getByLabel('demo_main').getByRole('tab');
    await demoTab.click();
    await dialog.getByTestId('key').getByText('Cake_CakeMaster').click();
    await dialog.getByTestId('key').getByText('app.scope2.app3').click();
    await dialog.getByLabel('save-build-configs').click();
    await expect(dialog).not.toBeVisible();
    await page.getByTestId('close').click();
    cardCaption = page.getByTestId('monitor-item-title').getByText(monitorName);
    card = page.locator('.monitor-item').filter({has: cardCaption}).first();
    await expect(card).toBeVisible();
    await card.getByTestId('view').click();
    await page.waitForURL(/monitor/);
    await allBuildInfosLoaded(page);
    const greenBuildInfo = page.locator('.monitor .build-info-item').getByTestId('build-info-name')
        .getByText('Cake Develop');
    await expect(greenBuildInfo).toBeVisible({timeout: 15000});
    expect(await page.locator('.monitor .build-info-item').count()).toBe(2);
    await page.getByLabel('monitor-list').click();
    await card.getByTestId('setup').click();
    await page.waitForURL(/setupMonitor/);
    await page.getByTestId('add-build-config').click();
    await expect(dialog).toBeVisible();
    await demoTab.click();
    await dialog.getByLabel('select-all').click();
    await dialog.getByLabel('save-build-configs').click();
    await expect(dialog).not.toBeVisible();
    await page.getByTestId('close').click();
    await card.getByTestId('view').click();
    await page.waitForURL(/monitor/);
    await allBuildInfosLoaded(page);
    await expect(greenBuildInfo).toBeVisible({timeout: 15000});
    const redBuildInfo = page.locator('.monitor .build-info-item.failed.with-committers')
        .getByText('Integration (PostgreSQL)', {exact: true});
    await expect(redBuildInfo).toBeVisible({timeout: 15000});
    await expect(redBuildInfo).toBeVisible({timeout: 15000});
    expect(await page.locator('.monitor .build-info-item').count()).toBe(10);
    await page.getByLabel('monitor-list').click();
    await card.getByTestId('remove').click();
    await expect(card).not.toBeVisible();
});
