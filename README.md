<p align="center"><img src="docs/figures/logo-header.svg"></p>

# RobotRaconteurWeb

**RobotRaconteurWeb is a pure C\# implementation of the Robot Raconteur framework for use with .NET Core and
JavaScript!**

Robot Raconteur is a communication framework for robotics, automation, and the Internet of Things. RobotRaconteurWeb
is an implementation of Robot Raconteur developed in pure C#. This is in contrast to RobotRaconteurNET, which
is a wrapper for the native RobotRaconteurCore implementation that is developed in C++. RobotRaconteurWeb
is more portable since it is pure C#. RobotRaconteurWeb is also available for use with the H5 C#-to-JavaScript
compiler. RobotRaconteurH5 is RobotRaconteurWeb compiled to JavaScript for use with any modern web browser.
It uses WebSockets to communicate with Robot Raconteur services. RobotRaconteurWeb can also be used with ASP.NET
web servers using WebSockets. See the examples for a demonstration of how this work. This capability allows
for Robot Raconteur to be easily integrated into cloud and information systems without requiring any modifications
to the infrastructure!

**See the Documentation here!!** [RobotRaconteurWeb Documentation](https://github.com/robotraconteur/RobotRaconteurWeb/wiki/Documentation)

RobotRaconteurWeb is very similar to RobotRaconteurNET. The main difference is that RobotRaconteurWeb
uses the asynchronous Task framework instead of blocking. All function that could potentially block
are instead asynchronous, and should be used with async/await.

See the `examples/` directory for examples of how to use RobotRaconteurWeb.

See the RobotRaconteurCore documentation for more information on Robot Raconteur and how to use the library:
https://github.com/robotraconteur/robotraconteur/wiki/Documentation

Also see the H5 compiler: https://github.com/theolivenbaum/h5

## Robot Raconteur

Robot Raconteur as framework and ecosystem is documented on the Robot Raconteur product page and GitHub homepage.
The tutorials are targeted at Python and C++, however the concepts are identical regardless of the programming
language interface used. It is recommended the user familiarizes themselves with the framework using Python
before using C\#.

* Homepage: http://robotraconteur.com
* GitHub Page: https://github.com/robotraconteur/robotraconteur
* Documentation Listing: https://github.com/robotraconteur/robotraconteur/wiki/Documentation
* RobotRaconteurCore: https://github.com/robotraconteur/robotraconteur

## Installation
### .NET Installation

`RobotRaconteurWeb` and `RobotRaconteurH5` can be easily installed using the NuGet package:

* NuGet RobotRaconteurWeb homepage: https://www.nuget.org/packages/RobotRaconteurWeb/
* NuGet RobotRaconteurH5 homepage: https://www.nuget.org/packages/RobotRaconteurH5/

Copyright (C) 2024 Wason Technology, LLC

## Acknowledgment

This work was supported in part by the Advanced Robotics for Manufacturing ("ARM") Institute under Agreement Number W911NF-17-3-0004 sponsored by the Office of the Secretary of Defense. The views and conclusions contained in this document are those of the authors and should not be interpreted as representing the official policies, either expressed or implied, of either ARM or the Office of the Secretary of Defense of the U.S. Government. The U.S. Government is authorized to reproduce and distribute reprints for Government purposes, notwithstanding any copyright notation herein.

This work was supported in part by the New York State Empire State Development Division of Science, Technology and Innovation (NYSTAR) under contract C160142.

![](docs/figures/arm_logo.jpg) ![](docs/figures/nys_logo.jpg)
