![](https://raw.githubusercontent.com/robotraconteur/RobotRaconteurWeb/master/docs/figures/logo-header.svg)

# RobotRaconteurH5

**RobotRaconteurH5 is a C\# implementation of the Robot Raconteur framework designed to work with
the [H5 C\#-to-JavaScript compiler](https://github.com/curiosity-ai/h5)!**

[http://robotraconteur.com](http://robotraconteur.com)

[J. Wason and J. T. Wen, "Robot Raconteur Updates on an Open Source Interoperable Middleware for Robotics", in Proc. IEEE Conference on Automation Science and Engineering, 2023, pp. 1-8.](https://files2.wasontech.com/RobotRaconteur_CASE2023.pdf)

Github Repository: https://github.com/robotraconteur/RobotRaconteurWeb

## Introduction

Robot Raconteur is a communication framework for robotics, automation, and the Internet of Things. RobotRaconteurH5
is an implementation of Robot Raconteur developed in pure C# intended to be used with the
[H5 C\#-to-JavaScript compiler](https://github.com/curiosity-ai/h5).
It is a variant of the [RobotRaconteurWeb](https://www.nuget.org/packages/RobotRaconteurWeb) library.
H5 provides a way to compile C# code to JavaScript, allowing for the use of C# libraries in web browsers. It
also provides access to the DOM and many other browser APIs. RobotRaconteurH5 uses WebSockets to communicate with
Robot Raconteur services.

RobotRaconteurH5 uses async/await for all blocking operations, making it compatible with the single
threaded JavaScript environment. This allows for Robot Raconteur to be easily used in web browsers.

Note that WebSockets may require security configuration to work with devices.
See the
[Robot Raconteur Guide](https://robotraconteur.github.io/robotraconteur/doc/core/latest/getting_started/Web.html)
for more information on configuring services to accept incoming WebSocket connections.

## Installation

For most platforms, the easiest way to install Robot Raconteur is using `dotnet`:

```bash
dotnet add package h5
dotnet add package h5.Core
dotnet add package RobotRaconteurH5
```

Note that the first XML element in the `csproj` file must be
`<Project Sdk="h5.Target/23.11.43676">` to use the H5 compiler. See the
[H5 documentation](https://github.com/curiosity-ai/h5) for more information.

## Code Generation

RobotRaconteurH5 uses service definition files (*.robdef) to define the available services and data types. These files
are used to generate C\# code for RobotRaconteurH5 using the
[RobotRaconteurWebGen](https://www.nuget.org/packages/RobotRaconteurWebGen) tool. It can be installed using the
following command:

```bash
dotnet tool install --global RobotRaconteurWebGen
```

The RobotRaconteurH5 package does not include automatic code generation. The user must manually generate the code
using the RobotRaconteurWebGen tool. The generated code should be included in the project to be compiled
with the other source files.

## Documentation

See the [Robot Raconteur H5/Web Documentation](https://github.com/robotraconteur/RobotRaconteurWeb/wiki/Documentation)

See the [Robot Raconteur H5/Web Examples](https://github.com/robotraconteur/RobotRaconteurWeb/tree/master/examples)

## Example

This example demonstrates a simple client for the Reynard the Robot cartoon robot. See
[Reynard the Robot](https://github.com/robotraconteur/reynard-the-robot) for more information
and setup instructions.

In a terminal,run the following command to start the Reynard the Robot server:

```bash
python -m reynard_the_robot
```

On Linux, you may need to use `python3` instead of `python`.

Open a browser to [http://localhost:29201](http://localhost:29201) to view the Reynard user interface.

The following is an example RobotRaconteurH5 C\# client for Reynard the Robot that can be compiled to JavaScript
using the H5 compiler to run in a web browser:

```csharp
using System;
using System.Threading.Tasks;
using H5;
using H5.Core;
using static H5.Core.es5;
using static H5.Core.dom;
using RobotRaconteurWeb;
using experimental.reynard_the_robot;

namespace h5_client
{
    class Program
    {
        static async void Main(string[] args)
        {
            var status_div = document.getElementById("status");
            status_div.innerHTML = "Connecting...";

            // Thunk source must me manually generated and registered when using H5
            RobotRaconteurNode.s.RegisterServiceType(new experimental__reynard_the_robotFactory());

            // Connect to Reynard the Robot service using websocket
            string url = "rr+tcp://localhost:29200?service=reynard";
            var c = (Reynard)await RobotRaconteurNode.s.ConnectService(url);

            var received_messages_div = document.getElementById("received_messages");

            // Connect a callback function to listen for new messages
            c.new_message += msg => received_messages_div.innerHTML += $"{msg}<br>";

            // Handle when the send_message button is clicked
            var send_button = document.getElementById("send_message");
            send_button.onclick = async delegate(MouseEvent ev)
            {
                var message = document.getElementById("message").As<HTMLInputElement>().value;
                await c.say(message);
            };

            var teleport_button = document.getElementById("teleport");
            teleport_button.onclick = async delegate(MouseEvent ev)
            {
                var x = double.Parse(document.getElementById("teleport_x").As<HTMLInputElement>().value) * 1e-3;
                var y = double.Parse(document.getElementById("teleport_y").As<HTMLInputElement>().value) * 1e-3;
                await c.teleport(x, y);
            };

            status_div.innerHTML = "Connected";
        }
    }
}
```

## License

Apache 2.0

## Support

Please report bugs and issues on the [GitHub issue tracker](https://github.com/robotraconteur/RobotRaconteurWeb/issues).

Ask questions on the [Github discussions](https://github.com/robotraconteur/RobotRaconteurWeb/discussions).

## Acknowledgment

This work was supported in part by Subaward No. ARM-TEC-18-01-F-19 and ARM-TEC-19-01-F-24 from the Advanced Robotics for Manufacturing ("ARM") Institute under Agreement Number W911NF-17-3-0004 sponsored by the Office of the Secretary of Defense. ARM Project Management was provided by Christopher Adams. The views and conclusions contained in this document are those of the authors and should not be interpreted as representing the official policies, either expressed or implied, of either ARM or the Office of the Secretary of Defense of the U.S. Government. The U.S. Government is authorized to reproduce and distribute reprints for Government purposes, notwithstanding any copyright notation herein.

This work was supported in part by the New York State Empire State Development Division of Science, Technology and Innovation (NYSTAR) under contract C160142.
