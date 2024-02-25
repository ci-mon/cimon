import {expect, type Page, test} from '@playwright/test';

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
    while (!(await loginByPassMenuItem.isVisible())){
        await loginMenu.click();
    }
    await loginByPassMenuItem.click();
    await page.waitForURL(/\/Login/);
    await page.locator('input[name="Username"]').fill(name);
    await page.locator('input[name="Password"]').fill(pass);
    await page.locator('.rz-login-buttons button[type="submit"]').click();
    await expect(page.getByLabel('profile-user-name')).toBeVisible();
}
