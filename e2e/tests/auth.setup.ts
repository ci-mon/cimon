import {test as setup} from '@playwright/test';
import {admin_storage_state, doLogin, simple_user_storage_state} from "./auth";
import * as fs from "fs";

setup('authenticate as admin', async ({ page }) => {
    if (fs.existsSync(admin_storage_state)) return;
    await doLogin(page, 'admin', 'admin');
    await page.context().storageState({ path: admin_storage_state });
});

setup('authenticate as user', async ({ page }) => {
    if (fs.existsSync(admin_storage_state)) return;
    await doLogin(page, 'test', 'test');
    await page.context().storageState({ path: simple_user_storage_state });
});
