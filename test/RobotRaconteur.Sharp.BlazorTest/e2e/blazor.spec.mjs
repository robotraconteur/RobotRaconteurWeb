import { test, expect } from '@playwright/test';

const tcpPort = process.env.RR_TCP_PORT || '22355';

test('client service tests pass in Blazor WebAssembly', async ({ page }) => {
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));

  await page.goto(`/?server=localhost:${tcpPort}`);

  // The app sets #status to PASS or FAIL when the tests finish
  const status = page.locator('#status');
  await expect(status).toHaveText(/^(PASS|FAIL)$/, { timeout: 280_000 });

  const log = await page.locator('#log').innerText();
  console.log(log);
  expect(errors, 'uncaught page errors').toEqual([]);
  expect(await status.innerText(), log).toBe('PASS');
});
