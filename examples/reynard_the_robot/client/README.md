# Reynard C\# Robot Raconteur Client Example

This example demonstrates controlling Reynard using C\# as a Robot Raconteur client.

dotnet core 7.0 SDK is required. See https://dotnet.microsoft.com/en-us/download .

The example requires the `RobotRaconteurWebGen` dotnet tool to be installed to generate
the Robot Raconteur thunk code.

```
dotnet tool install -g RobotRaconteurWebGen
```

## Run Example

Start Reynard the Robot server example in a separate terminal. See
[Reynard the Robot](https://github.com/robotraconteur/reynard-the-robot).

The `dotnet run` command will install the RobotRaconteurWeb library and compile the example.

```
dotnet run --framework net7.0
```
