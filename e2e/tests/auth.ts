import {expect, Locator, type Page, test} from '@playwright/test';

export const admin_storage_state = 'playwright/.auth/admin.json';
export const simple_user_storage_state = 'playwright/.auth/test.json';

export function authAsAdmin() {
    test.use({ storageState:  admin_storage_state});
}
export function authAsUser() {
    test.use({ storageState:  simple_user_storage_state});
}

export async function doLogin(page: Page, name: string, pass: string) {
    await page.goto('/');
    const loginMenu = page.getByLabel('show-login-options');
    const loginByPassMenuItem = page.getByLabel('login-via-password');
    await loginMenu.click();
    while (!(await loginByPassMenuItem.isVisible())) {
        await loginMenu.click();
    }
    await loginByPassMenuItem.click({force: true});
    await page.waitForURL(/\/Login/);
    await setInputValue(page.locator('input[name="Username"]'), name);
    await setInputValue(page.locator('input[name="Password"]'), pass);
    await page.locator('.rz-login-buttons button[type="submit"]').click();
    await expect(page.getByLabel('profile-user-name')).toBeVisible();
}

async function setInputValue(locator: Locator, value: string) {
    await locator.fill(value);
    if (await locator.textContent() !== value) {
        await locator.fill(value);
    }
}
