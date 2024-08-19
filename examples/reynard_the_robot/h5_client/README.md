# Reynard H5 C\# Robot Raconteur Client Example

This example demonstrates controlling Reynard using C\# compiled to JavaScript as a Robot Raconteur client.

The [h5 compiler](https://github.com/curiosity-ai/h5) is C\# to JavaScript compiler that allows running C\#
code in the browser. The [RobotRaconteurH5](https://www.nuget.org/packages/RobotRaconteurH5) library
is the RobotRaconteurWeb compiled for h5. Robot Raconteur uses [WebSockets](https://en.wikipedia.org/wiki/WebSocket)
to communicate between the client and server. WebSockets allow browsers to create a full-duplex connection
to servers using an HTTP upgrade mechanism, where an HTTP connections is upgrade to a two-way communication
channel. Robot Raconteur is designed to support both the Robot Raconteur protocol and WebSockets.

Server nodes may need configuration to allow WebSocket connections. WebSockets contain protection [for cross-site
scripting attacks (XSS)](https://en.wikipedia.org/wiki/Cross-site_scripting), and by default Robot Raconteur
will reject WebSocket connections from a different origin. See the
[Robot Raconteur Guide](https://robotraconteur.github.io/robotraconteur/doc/core/latest/getting_started/Web.html)
for more information on configuring the server nodes to accept incoming WebSocket connections.

## Setup

Compiling this project requires dotnet core 7.0 SDK. See https://dotnet.microsoft.com/en-us/download .

Install the required tools:

```
dotnet tool install -g RobotRaconteurWebGen
dotnet tool install -g h5-compiler
```

## Compile

The Robot Raconteur thunk code must be manually generated when using H5:

```bash
RobotRaconteurWebGen --thunksource --lang=csharp --outfile thunksource.cs ../robdef/experimental.reynard_the_robot.robdef
```

Compile the project using the following command:

```
dotnet build -p:Platform="AnyCPU"
```

## Run Example

Start Reynard the Robot server example in a separate terminal. See
[Reynard the Robot](https://github.com/robotraconteur/reynard-the-robot).

Open the `bin/Release/h5/index.html` file in a web browser to view the user interface. It will
connect to the Robot Raconteur url `rr+ws://localhost:29200?service=reynard`.

Note: When using the `file://` protocol to open the `index.html` file, Robot Raconteur will allow the incoming
connection. When service thi file over HTTP, the server node will need to be configured to accept incoming
connections by allowing the origin of the incoming connection. See the
[Robot Raconteur Guide](https://robotraconteur.github.io/robotraconteur/doc/core/latest/getting_started/Web.html)
for more information.
