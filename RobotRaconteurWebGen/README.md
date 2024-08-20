![](https://raw.githubusercontent.com/robotraconteur/RobotRaconteurWeb/master/docs/figures/logo-header.svg)

# RobotRaconteurWebGen

**RobotRaconteurWeb is a pure C\# implementation of the Robot Raconteur framework for use with .NET Core**

[http://robotraconteur.com](http://robotraconteur.com)

[J. Wason and J. T. Wen, "Robot Raconteur Updates on an Open Source Interoperable Middleware for Robotics", in Proc. IEEE Conference on Automation Science and Engineering, 2023, pp. 1-8.](https://files2.wasontech.com/RobotRaconteur_CASE2023.pdf)

Github Repository: https://github.com/robotraconteur/RobotRaconteurWeb

## Introduction

RobotRaconteurWebGen is a tool used with [RobotRaconteurWeb](https://www.nuget.org/packages/RobotRaconteurWeb)
to generate C# code from service definition files (*.robdef).
The generated code can be used to interact with RobotRaconteur services in C#. This tool can be called
from the command line or integrated into a build system. See the
[RobotRaconteurWeb](https://www.nuget.org/packages/RobotRaconteurWeb) documentation for more information.

## Installation

For most platforms, the easiest way to install Robot Raconteur is using `dotnet`:

```bash
dotnet tool install -g RobotRaconteurWebGen
```

## Usage

```bash
RobotRaconteurWebGen [options] [robdef-files]
```

Options:

* `--version` - Display the version of RobotRaconteurWebGen and exit
* `--thunksource` - Generate the thunk source code
* `--lang=` - Specify the output computer language. Only `csharp` is supported for RobotRaconteurWebGen.
* `--outfile=` - Specify the output file for the generated code
* `--include-path=` or `-I` - Specify the include path for the service definition files
* `--auto-import` - Automatically import robdef files in the include path
* `--import=` - Import a specific robdef file
* `--help` or `-h` - Display the help message and exit

Example usage:

```bash
RobotRaconteurWebGen --thunksource --lang=csharp --outfile thunksource.cs experimental.reynard_the_robot.robdef
```

## Documentation

See the [Robot Raconteur Web Documentation](https://github.com/robotraconteur/RobotRaconteurWeb/wiki/Documentation)

See the [Robot Raconteur Web Examples](https://github.com/robotraconteur/RobotRaconteurWeb/tree/master/examples)

## License

Apache 2.0

## Support

Please report bugs and issues on the [GitHub issue tracker](https://github.com/robotraconteur/RobotRaconteurWeb/issues).

Ask questions on the [Github discussions](https://github.com/robotraconteur/RobotRaconteurWeb/discussions).

## Acknowledgment

This work was supported in part by Subaward No. ARM-TEC-18-01-F-19 and ARM-TEC-19-01-F-24 from the Advanced Robotics for Manufacturing ("ARM") Institute under Agreement Number W911NF-17-3-0004 sponsored by the Office of the Secretary of Defense. ARM Project Management was provided by Christopher Adams. The views and conclusions contained in this document are those of the authors and should not be interpreted as representing the official policies, either expressed or implied, of either ARM or the Office of the Secretary of Defense of the U.S. Government. The U.S. Government is authorized to reproduce and distribute reprints for Government purposes, notwithstanding any copyright notation herein.

This work was supported in part by the New York State Empire State Development Division of Science, Technology and Innovation (NYSTAR) under contract C160142.
