// Playwright configuration for the RobotRaconteur.Sharp Blazor WebAssembly tests. See ../README.md.
//
// Expects the Blazor app published to ../bin/publish-app and the test server (RobotRaconteur.Sharp.Test)
// published to ../bin/publish-server. Environment variables:
//   RR_BROWSER_TEST_SERVER  command that runs RobotRaconteur.Sharp.Test
//                           (default: the self-contained ../bin/publish-server/RobotRaconteur.Sharp.Test)
//   RR_TCP_PORT             TcpTransport port for the test services (default 22355)
//   RR_HTTP_PORT            HTTP port serving the Blazor app (default 22356)
import { defineConfig, devices } from '@playwright/test';

const tcpPort = process.env.RR_TCP_PORT || '22355';
const httpPort = process.env.RR_HTTP_PORT || '22356';
const server = process.env.RR_BROWSER_TEST_SERVER || '../bin/publish-server/RobotRaconteur.Sharp.Test';

export default defineConfig({
  testDir: '.',
  timeout: 300_000,
  retries: 0,
  // The tests share one server, so run the browsers one at a time
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL: `http://localhost:${httpPort}`,
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'webkit', use: { ...devices['Desktop Safari'] } },
  ],
  webServer: {
    command: `${server} browsertestserver ${tcpPort} ${httpPort} ../bin/publish-app/wwwroot`,
    url: `http://localhost:${httpPort}/index.html`,
    timeout: 120_000,
    reuseExistingServer: false,
    stdout: 'pipe',
    stderr: 'pipe',
  },
});
