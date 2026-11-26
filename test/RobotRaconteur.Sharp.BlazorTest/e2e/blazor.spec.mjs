import { test, expect } from '@playwright/test';

const tcpPort = process.env.RR_TCP_PORT || '22355';

test('client service tests pass in Blazor WebAssembly', async ({ page }) => {
  // Collect browser problems, so a failure to start the app explains itself in the test output
  const errors = [];
  const console_messages = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('crash', () => errors.push('page crashed'));
  page.on('console', (m) => {
    if (m.type() === 'error' || m.type() === 'warning') {
      console_messages.push(`[${m.type()}] ${m.text()}`);
    }
  });

  await page.goto(`/?server=localhost:${tcpPort}`);

  // The app sets #status to PASS or FAIL when the tests finish
  const status = page.locator('#status');
  try {
    await expect(status).toHaveText(/^(PASS|FAIL)$/, { timeout: 280_000 });
  } catch (e) {
    console.log('Browser console errors and warnings:\n' + (console_messages.join('\n') || '(none)'));
    console.log('Page errors:\n' + (errors.join('\n') || '(none)'));
    throw e;
  }

  const log = await page.locator('#log').innerText();
  console.log(log);
  expect(errors, 'uncaught page errors').toEqual([]);
  expect(await status.innerText(), log).toBe('PASS');
});
