<p align="center"><img src="images/logo-header.svg"></p>

# RobotRaconteur.Sharp Documentation

Welcome to the RobotRaconteur.Sharp documentation!

Robot Raconteur is a communication framework for robotics, automation, and the Internet of Things. RobotRaconteur.Sharp
is an implementation of Robot Raconteur developed in pure C#. This is in contrast to RobotRaconteurNET, which
is a wrapper for the native RobotRaconteurCore implementation that is developed in C++. RobotRaconteur.Sharp
is more portable since it is pure C#. For web browsers, use the Robot Raconteur TypeScript implementation, or
RobotRaconteur.Sharp with Blazor WebAssembly. RobotRaconteur.Sharp can also be used with ASP.NET web servers using
WebSockets. See the examples for a demonstration of how this works. This capability allows for Robot Raconteur to
be easily integrated into cloud and information systems without requiring any modifications to the infrastructure!

RobotRaconteur.Sharp was formerly named RobotRaconteurWeb.

**See the API Documentation here!!** [api/index.html](api/index.html)

RobotRaconteur.Sharp is very similar to RobotRaconteurNET. The main difference is that RobotRaconteur.Sharp
uses the asynchronous Task framework instead of blocking. All function that could potentially block
are instead asynchronous, and should be used with async/await.

See the RobotRaconteurCore documentation for more information on Robot Raconteur and how to use the library:
https://github.com/robotraconteur/robotraconteur/wiki/Documentation

## Robot Raconteur

Robot Raconteur as framework and ecosystem is documented on the Robot Raconteur product page and GitHub homepage.
The tutorials are targeted at Python and C++, however the concepts are identical regardless of the programming
language interface used. It is recommended the user familiarizes themselves with the framework using Python
before using C\#.

* Homepage: http://robotraconteur.com
* GitHub Page: https://github.com/robotraconteur/robotraconteur
* Documentation: https://github.com/robotraconteur/robotraconteur/wiki/Documentation
* RobotRaconteurCore: https://github.com/robotraconteur/robotraconteur

## Package Readme

See the individual package Readme files for more information:

* [RobotRaconteur.Sharp](articles/robotraconteursharp_readme.md)
* [RobotRaconteur.Sharp.Gen](articles/robotraconteursharpgen_readme.md)

Copyright (C) 2024 Wason Technology, LLC
