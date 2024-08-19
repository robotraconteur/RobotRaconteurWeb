# Reynard C\# Robot Raconteur Service Example ASCII Socket Backend

This example demonstrates a Robot Raconteur service to control Reynard using C\# with ASCII Socket communication
with the robot. This example is intended to be representative of the types of drivers that are used with real
industrial robots and devices.

## Setup

These setup steps only need to be run once, however other examples may require additional packages to be installed.
Check the instructions for the example for additional setup steps.

Install the "dotnet" SDK from https://dotnet.microsoft.com/download. In this example, dotnet 7 is used.

The `reynard-the-robot` Python package must be installed. See
[Reynard the Robot](https://github.com/robotraconteur/reynard-the-robot) for instructions.

The example requires the `RobotRaconteurWebGen` dotnet tool to be installed to generate
the Robot Raconteur thunk code.

```
dotnet tool install -g RobotRaconteurWebGen
```

## Run Example

Start the Reynard the Robot server:

```cmd
python -m reynard_the_robot --disable-robotraconteur
```

On Linux, `python3` may need to be used instead of `python`.

Open a second command prompt and navigate to the `examples/reynard_the_robot/service/ascii_socket` directory.
Run the following command:

```cmd
dotnet run --framework net7.0
```

The example Robot Raconteur service listens on port 59201. Python can be used to connect to the service:

```python
from RobotRaconteur.Client import *
c = RRN.ConnectService('rr+tcp://localhost:59201?service=reynard')
c.say('Hello World!')
```
