using System;
using RobotRaconteurWeb;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

// Initialize the client node
using (var node_setup = new ClientNodeSetup(args: args))
{
    // Connect to the Reynard service using a URL
    var c = (experimental.reynard_the_robot.Reynard)await RobotRaconteurNode.s.ConnectService(
        "rr+tcp://localhost:29200?service=reynard");

    // Connect a callback function to listen for new messages
    c.new_message += (msg) =>
    { Console.WriteLine(msg); };

    // Read the current state using a wire "peek". Can also "connect" to receive streaming updates.
    var state = (await c.state.PeekInValue()).Item1;
    Console.WriteLine(string.Join(",", state.robot_position.Select(x => x.ToString())));
    Console.WriteLine(string.Join(",", state.arm_position.Select(x => x.ToString())));

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

    // Drive the arm using timeout and wait
    await c.drive_arm(10.0 * (Math.PI / 180), -30 * (Math.PI / 180), -15 * (Math.PI / 180), 1.5, true);

    //  Set the color to red
    await c.set_color(new double[] { 1.0, 0.0, 0.0 });

    // Read the color
    var color_in = await c.get_color();
    Console.WriteLine(string.Join(",", color_in.Select(x => x.ToString())));

    await Task.Delay(1000);

    // Reset the color
    await c.set_color(new double[] { 0.929, 0.49, 0.192 });

    // Say hello
    await c.say("Hello, World From C#!");
}
