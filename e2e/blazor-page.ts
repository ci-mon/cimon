import type { Page } from '@playwright/test';

export class BlazorPage {
    constructor(public readonly page: Page) {
    }

    async waitForBlazor() {
        await this.page.waitForFunction(arg => {
            return Boolean(window['DotNet'] && window['Blazor']?.['runtime']);
        })
    }
}
