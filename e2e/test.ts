import { test as base } from '@playwright/test';
import {BlazorPage} from "./blazor-page";

// Declare the types of your fixtures.
type MyFixtures = {
    blazorPage: BlazorPage;
};

export const test = base.extend<MyFixtures>({
    blazorPage: async ({ page }, use) => {
        await use(new BlazorPage(page));
    },
});
export { expect, Page } from '@playwright/test';
