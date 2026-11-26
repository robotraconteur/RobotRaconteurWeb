<p align="center"><img src="docs/figures/logo-header.svg"></p>

# RobotRaconteur.Sharp

**RobotRaconteur.Sharp is a pure C\# implementation of the Robot Raconteur framework for use with .NET!**

Robot Raconteur is a communication framework for robotics, automation, and the Internet of Things. RobotRaconteur.Sharp
is an implementation of Robot Raconteur developed in pure C#. This is in contrast to RobotRaconteurNET, which
is a wrapper for the native RobotRaconteurCore implementation that is developed in C++. RobotRaconteur.Sharp
is more portable since it is pure C#. For web browsers, use the Robot Raconteur TypeScript implementation, or
RobotRaconteur.Sharp with Blazor WebAssembly. RobotRaconteur.Sharp can also be used with ASP.NET web servers using
WebSockets. See the [ASP.NET Core server example](https://github.com/robotraconteur/RobotRaconteur.Sharp/tree/master/examples/aspnet_server) for a demonstration. This capability allows for Robot Raconteur to
be easily integrated into cloud and information systems without requiring any modifications to the infrastructure!

RobotRaconteur.Sharp was formerly named RobotRaconteurWeb.

**See the Documentation here!!** [RobotRaconteur.Sharp Documentation](https://github.com/robotraconteur/RobotRaconteur.Sharp/wiki/Documentation)

RobotRaconteur.Sharp is very similar to RobotRaconteurNET. The main difference is that RobotRaconteur.Sharp
uses the asynchronous Task framework instead of blocking. All function that could potentially block
are instead asynchronous, and should be used with async/await.

See the `examples/` directory for examples of how to use RobotRaconteur.Sharp.

See the RobotRaconteurCore documentation for more information on Robot Raconteur and how to use the library:
https://github.com/robotraconteur/robotraconteur/wiki/Documentation

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

`RobotRaconteur.Sharp` can be easily installed using the NuGet package:

* NuGet RobotRaconteur.Sharp homepage: https://www.nuget.org/packages/RobotRaconteur.Sharp/

## Releasing

Publishing a GitHub release runs the [nuget-publish](.github/workflows/nuget-publish.yml) workflow. It checks the
versions, runs the tests, and pushes `RobotRaconteur.Sharp` and `RobotRaconteur.Sharp.Gen` to nuget.org.

To make a release:

1. Set the new version in `RobotRaconteur.Sharp/RobotRaconteur.Sharp.csproj` and
   `RobotRaconteur.Sharp.Gen/RobotRaconteur.Sharp.Gen.csproj` (`Version`, `AssemblyVersion` and `FileVersion`), and
   `RobotRaconteurNode.Version` in `RobotRaconteur.Sharp/RobotRaconteur.cs`.
2. Create a GitHub release with the tag `v<version>`, for example `v0.22.0`. The workflow fails without pushing if
   the tag does not match all three versions.

The workflow uses [nuget.org Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing),
so no API key is stored in the repository. One-time setup:

1. On nuget.org, open your username menu, choose **Trusted Publishing**, and add a policy with repository owner
   `robotraconteur`, repository `RobotRaconteur.Sharp`, workflow file `nuget-publish.yml`, and environment `nuget`.
   Packages published through the policy belong to the policy owner (a nuget.org user or organization).
2. In the GitHub repository settings, add a `NUGET_USER` Actions secret with the nuget.org profile name (not the
   email address) of the policy owner.
3. Optionally, add required reviewers to the `nuget` environment, so each publish waits for approval.

Copyright (C) 2024 Wason Technology, LLC

## Acknowledgment

This work was supported in part by the Advanced Robotics for Manufacturing ("ARM") Institute under Agreement Number W911NF-17-3-0004 sponsored by the Office of the Secretary of Defense. The views and conclusions contained in this document are those of the authors and should not be interpreted as representing the official policies, either expressed or implied, of either ARM or the Office of the Secretary of Defense of the U.S. Government. The U.S. Government is authorized to reproduce and distribute reprints for Government purposes, notwithstanding any copyright notation herein.

This work was supported in part by the New York State Empire State Development Division of Science, Technology and Innovation (NYSTAR) under contract C160142.

![](docs/figures/arm_logo.jpg) ![](docs/figures/nys_logo.jpg)
