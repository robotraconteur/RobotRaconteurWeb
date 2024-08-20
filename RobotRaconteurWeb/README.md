![](https://raw.githubusercontent.com/robotraconteur/RobotRaconteurWeb/master/docs/figures/logo-header.svg)

# RobotRaconteurWeb

**RobotRaconteurWeb is a pure C\# implementation of the Robot Raconteur framework for use with .NET Core**

[http://robotraconteur.com](http://robotraconteur.com)

[J. Wason and J. T. Wen, "Robot Raconteur Updates on an Open Source Interoperable Middleware for Robotics", in Proc. IEEE Conference on Automation Science and Engineering, 2023, pp. 1-8.](https://files2.wasontech.com/RobotRaconteur_CASE2023.pdf)

Github Repository: https://github.com/robotraconteur/RobotRaconteurWeb

## Introduction

Robot Raconteur is a communication framework for robotics, automation, and the Internet of Things. RobotRaconteurWeb
is an implementation of Robot Raconteur developed in pure C#. This is in contrast to
[RobotRaconteurNET](https://www.nuget.org/packages/RobotRaconteurNET),
which is a wrapper for the native RobotRaconteurCore implementation that is developed in C++.
RobotRaconteurWeb can be used with ASP.NET web servers using WebSockets, allowing for
Robot Raconteur to be easily integrated into cloud and information systems without requiring any modifications
to clients.

RobotRaconteurWeb uses async/await for all blocking operations, making it easy to use with C\# projects.
Unlike RobotRaconteurNET, there are no blocking functions available.

## Installation

For most platforms, the easiest way to install Robot Raconteur is using `dotnet`:

```bash
dotnet add package RobotRaconteurWeb
```

RobotRaconteurWeb is a pure C\# `netstandard2.1` library, making it very portable and easy to use with .NET Core.

## Code Generation

RobotRaconteurWeb uses service definition files (*.robdef) to define the available services and data types. These files
are used to generate C\# code for RobotRaconteurWeb using the
[RobotRaconteurWebGen](https://www.nuget.org/packages/RobotRaconteurWebGen) tool. It can be installed using the
following command:

```bash
dotnet tool install --global RobotRaconteurWebGen
```

The RobotRaconteurWeb nuget package provides build integration to automatically generate the code when the project is built.
The following 'ItemGroup' can be added to the project file to specify the service definition files:

```xml
<ItemGroup>
    <RobotRaconteurGenCSharp Include="path/to/robdef/experimental.my_service.robdef" />
</ItemGroup>
```

The item types `<RobotRaconteurIncludeCSharp>` can be used to "include" a robdef without generating the code. This is useful
when a service definition file is included in another file. The `<RobotRaconteurGenIncludePath>` can be
used to specify a directory containing robdef files to include. Files in this path will be "auto-imported" if they
are included in another robdef file.

## Documentation

See the [Robot Raconteur Web Documentation](https://github.com/robotraconteur/RobotRaconteurWeb/wiki/Documentation)

See the [Robot Raconteur Web Examples](https://github.com/robotraconteur/RobotRaconteurWeb/tree/master/examples)

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

The following is an example RobotRaconteurWeb C\# client for Reynard the Robot:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb;

// Initialize the client node
using (var node_setup = new ClientNodeSetup(args: args))
{
    // Connect to the Reynard service using a URL
    var c = (experimental.reynard_the_robot.Reynard)await RobotRaconteurNode.s.ConnectService(
        "rr+tcp://localhost:29200?service=reynard");

    // Teleport the robot
    await c.teleport(0.1, -0.2);

    // Drive the robot with no timeout
    await c.drive_robot(0.5, -0.2, -1, false);

    // Wait for one second
    await Task.Delay(1000);

    // Stop the robot
    await c.drive_robot(0, 0, -1, false);

    // Set the arm position
    await c.setf_arm_position(100.0 * (Math.PI / 180), -30 * (Math.PI / 180), -70 * (Math.PI / 180));

    //  Set the color to red
    await c.set_color(new double[] { 1.0, 0.0, 0.0 });

    // Say hello
    await c.say("Hello, World From C#!");
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
