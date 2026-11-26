# RobotRaconteur.Sharp Blazor WebAssembly tests

Browser tests for RobotRaconteur.Sharp in Blazor WebAssembly. The app in this folder compiles the
standard client service tests from `RobotRaconteur.Sharp.Test` (`ServiceTestClient` and `ServiceTestClient2`)
unchanged, and runs them in the browser over `BrowserWebSocketTransport`. The client node is set up the way a
Blazor application would set it up: a bare `RobotRaconteurNode`, explicitly registered service factories, and
no other transports.

[Playwright](https://playwright.dev) (`e2e/`) does the following:

1. Starts the test server: `RobotRaconteur.Sharp.Test browsertestserver <tcp_port> <http_port> <wwwroot>`.
   It runs the test services on a `TcpTransport` port, allows WebSocket connections from the page origin,
   and serves the published app.
2. Opens the app in Chromium, Firefox and WebKit.
3. Waits for the app to set `#status` to `PASS` or `FAIL`, and prints the test log.

## Running with podman

```bash
test/RobotRaconteur.Sharp.BlazorTest/run-tests-podman.sh
```

Extra arguments are passed to `playwright test`, for example `--project=chromium`. The script publishes the app
and a self-contained test server in the .NET SDK container, then runs Playwright in the
`mcr.microsoft.com/playwright:v1.49.1-noble` container. The Playwright image version must match the
`@playwright/test` version in `e2e/package.json`. Neither container can reach the host or the local network.

## Running directly

You need the .NET 8 SDK and Node.js.

```bash
dotnet publish test/RobotRaconteur.Sharp.BlazorTest/RobotRaconteur.Sharp.BlazorTest.csproj -c Release -o test/RobotRaconteur.Sharp.BlazorTest/bin/publish-app
dotnet publish test/RobotRaconteur.Sharp.Test/RobotRaconteur.Sharp.Test.csproj -c Release -r linux-x64 --self-contained -o test/RobotRaconteur.Sharp.BlazorTest/bin/publish-server
cd test/RobotRaconteur.Sharp.BlazorTest/e2e
npm ci
npx playwright install --with-deps
npx playwright test
```

`RR_TCP_PORT` (default 22355) and `RR_HTTP_PORT` (default 22356) change the ports. `RR_BROWSER_TEST_SERVER`
changes the command that runs the test server.

To debug in a normal browser, start the server yourself, open
`http://localhost:22356/?server=localhost:22355` and watch the browser console:

```bash
test/RobotRaconteur.Sharp.BlazorTest/bin/publish-server/RobotRaconteur.Sharp.Test browsertestserver 22355 22356 test/RobotRaconteur.Sharp.BlazorTest/bin/publish-app/wwwroot
```
