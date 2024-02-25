import {test, expect, type Page,} from '@playwright/test';
import {admin_storage_state, authAsAdmin, authAsUser, doLogin, simple_user_storage_state} from "./auth";

test.describe.configure({ mode: 'serial' });

async function expectCommonItemsVisible(page: Page, isAdmin: boolean){
    await page.goto('/');
    await expect(page.getByLabel('monitor-list')).toBeVisible();
    await expect(page.getByLabel('last-monitor')).toBeVisible();
    await expect(page.getByLabel('native-app')).toBeVisible();
    await expect(page.getByLabel('teams-list')).toBeVisible({visible: isAdmin});
    await expect(page.getByLabel('users-list')).toBeVisible({visible: isAdmin});
    await expect(page.getByLabel('connectors-setup')).toBeVisible({visible: isAdmin});
}

test("login as simple user", async ({browser}) => {
    const context = await browser.newContext({ storageState: simple_user_storage_state });
    const page = await context.newPage();
    await expectCommonItemsVisible(page, false);
    await context.close();
});
test("login as admin", async ({browser}) => {
    const context = await browser.newContext({ storageState: admin_storage_state });
    const page = await context.newPage();
    await expectCommonItemsVisible(page, true);
    await context.close();
});

