<p align="center"><img src="images/logo-header.svg"></p>

# RobotRaconteurWeb Documentation

Welcome to the RobotRaconteurWeb documentation!

Robot Raconteur is a communication framework for robotics, automation, and the Internet of Things. RobotRaconteurWeb
is an implementation of Robot Raconteur developed in pure C#. This is in contrast to RobotRaconteurNET, which
is a wrapper for the native RobotRaconteurCore implementation that is developed in C++. RobotRaconteurWeb
is more portable since it is pure C#. RobotRaconteurWeb is also available for use with the H5 C#-to-JavaScript
compiler. RobotRaconteurH5 is RobotRaconteurWeb compiled to JavaScript for use with any modern web browser.
It uses WebSockets to communicate with Robot Raconteur services. RobotRaconteurWeb can also be used with ASP.NET
web servers using WebSockets. See the examples for a demonstration of how this work. This capability allows
for Robot Raconteur to be easily integrated into cloud and information systems without requiring any modifications
to the infrastructure!

**See the API Documentation here!!** [api/index.html](api/index.html)

RobotRaconteurWeb is very similar to RobotRaconteurNET. The main difference is that RobotRaconteurWeb
uses the asynchronous Task framework instead of blocking. All function that could potentially block
are instead asynchronous, and should be used with async/await.

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
* Documentation: https://github.com/robotraconteur/robotraconteur/wiki/Documentation
* RobotRaconteurCore: https://github.com/robotraconteur/robotraconteur

## Package Readme

See the individual package Readme files for more information:

* [RobotRaconteurWeb](articles/robotraconteurweb_readme.md)
* [RobotRaconteurWebGen](articles/robotraconteurwebgen_readme.md)
* [RobotRaconteurH5](articles/robotraconteurh5_readme.md)

Copyright (C) 2024 Wason Technology, LLC
