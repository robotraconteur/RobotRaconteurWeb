using System;
using RobotRaconteurWeb;
using experimental.reynard_the_robot;
using System.Collections.Generic;
using DrekarLaunchProcess;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

class Reynard_impl : Reynard_default_impl, IDisposable
{

    IPEndPoint _socket_ep;
    Timer _state_timer;

    public Reynard_impl(string host, int port)
    {
        var host_entry = Dns.GetHostEntry(host, AddressFamily.InterNetwork);
        _socket_ep = new IPEndPoint(host_entry.AddressList[0], port);
    }

    public async Task<string[]> _communicate(string text_request, string expected_response_op)
    {
        using (var client = new System.Net.Sockets.TcpClient())
        {
            await client.ConnectAsync(_socket_ep);
            using (var stream = client.GetStream())
            {
                var writer = new System.IO.StreamWriter(stream);
                await writer.WriteLineAsync(text_request);
                await writer.FlushAsync();

                var reader = new System.IO.StreamReader(stream);
                var response = await reader.ReadLineAsync();
                if (response == null)
                {
                    throw new System.IO.IOException("Connection closed");
                }

                if (response.StartsWith("ERROR"))
                {
                    throw new System.IO.IOException("Error from robot: " + response);
                }

                var response_parts = response.Split(' ');
                if (response_parts.Length < 1)
                {
                    throw new System.IO.IOException("Invalid response");
                }

                if (expected_response_op == "OK")
                {
                    if (response_parts[0] != "OK")
                    {
                        throw new System.IO.IOException("Unexpected response from robot: " + response);
                    }
                    return new string[] {};
                }

                if (response_parts[0] != expected_response_op)
                {
                    throw new System.IO.IOException("Invalid response from robot: " + response);
                }

                return response_parts.Skip(1).ToArray();
            }
        }
    }

    public override async Task<double[]> get_robot_position(CancellationToken cancel=default(CancellationToken))
    {
        var socket_res = await _communicate("STATE", "STATE");
        var ret = new double[2];
        ret[0] = double.Parse(socket_res[0]) * 1e-3;
        ret[1] = double.Parse(socket_res[1]) * 1e-3;
        return ret;
    }

    public override async Task<double[]> get_color(CancellationToken cancel=default(CancellationToken))
    {
        var socket_res = await _communicate("COLORGET", "COLOR");
        return new double[] { double.Parse(socket_res[0]), double.Parse(socket_res[1]),
                                double.Parse(socket_res[2]) };
    }
    public override async Task set_color(double[] value, CancellationToken cancel=default(CancellationToken))
    {
        string text_request =
            "COLORSET " + value[0].ToString() + " " + value[1].ToString() + " " + value[2].ToString();
        await _communicate(text_request, "OK");
    }

    public override async Task teleport(double x, double y, CancellationToken cancel=default(CancellationToken))

    {
        string text_request = "TELEPORT " + (x * 1e3).ToString() + " " + (y * 1e3).ToString();
        await _communicate(text_request, "OK");
    }
    public override async Task setf_arm_position(double q1, double q2, double q3, CancellationToken cancel=default(CancellationToken))
    {
        string text_request = "SETARM " + (q1 * (180.0 / Math.PI)).ToString() + " " +
                              (q2 * (180.0 / Math.PI)).ToString() + " " + (q3 * (180.0 / Math.PI)).ToString();
        await _communicate(text_request, "OK");
    }
    public override async Task<double[]> getf_arm_position(CancellationToken cancel=default(CancellationToken))

    {
        var socket_res = await _communicate("STATE", "STATE");

        return new double[] { double.Parse(socket_res[2]) * (Math.PI / 180.0),
                              double.Parse(socket_res[3]) * (Math.PI / 180.0),
                              double.Parse(socket_res[4]) * (Math.PI / 180.0) };
    }
    public override async Task drive_robot(double vel_x, double vel_y, double timeout, bool wait, CancellationToken cancel=default(CancellationToken))

    {
        string text_request = "DRIVE " + (vel_x * 1e3).ToString() + " " + (vel_y * 1e3).ToString() + " " +
                              timeout.ToString() + " " + (wait ? "1" : "0");
        await _communicate(text_request, "OK");
    }
    public override async Task drive_arm(double q1, double q2, double q3, double timeout, bool wait, CancellationToken cancel=default(CancellationToken))

    {
        string text_request = "DRIVEARM " + (q1 * (180.0 / Math.PI)).ToString() + " " +
                              (q2 * (180.0 / Math.PI)).ToString() + " " + (q3 * (180.0 / Math.PI)).ToString() + " " +
                              timeout.ToString() + " " + (wait ? "1" : "0");
        await _communicate(text_request, "OK");
    }
    public override async Task say(string message, CancellationToken cancel=default(CancellationToken))
    {
        string text_request = "SAY \"" + message + "\"";
        await _communicate(text_request, "OK");
    }

    private  async Task timer_task(CancellationToken cancel)
    {
        while (!cancel.IsCancellationRequested)
        {
            try
            {
                var socket_res = await _communicate("STATE", "STATE");

                var state =
                    new ReynardState() { time = double.Parse(socket_res[0]),
                                        robot_position = new double[] { double.Parse(socket_res[1]) * 1e-3,
                                                                        double.Parse(socket_res[2]) * 1e-3 },
                                        arm_position = new double[] { double.Parse(socket_res[3]) * (Math.PI / 180.0),
                                                                    double.Parse(socket_res[4]) * (Math.PI / 180.0),
                                                                    double.Parse(socket_res[5]) * (Math.PI / 180.0) },
                                        robot_velocity = new double[] {}, arm_velocity = new double[] {} };

                if (rrvar_state != null)
                {
                    rrvar_state.OutValue = state;
                }
            }
            catch (Exception e)
            {
                RRLogFuncs.LogWarning("Error updating state: " + e.Message);
            }
            await Task.Delay(250);
        }
    }

    CancellationTokenSource _timer_cancel = new CancellationTokenSource();
    Task _timer_task;

    public void _start()
    {
        _timer_task = Task.Run(() => timer_task(_timer_cancel.Token));
    }

    public void Dispose()
    {
        _timer_cancel.Cancel();
    }
}

class Program
{
    static int Main(string[] args)
    {
        var reynard = new Reynard_impl("localhost", 29202);
        using (reynard) using (var node_setup =
                                   new ServerNodeSetup("experimental.reynard_the_robot_csharp_socket", 59201, args: args))
        {
            var ctx = RobotRaconteurNode.s.RegisterService("reynard", "experimental.reynard_the_robot", reynard);

            reynard._start();

            Console.WriteLine("Reynard the Robot C# Service Started");
            Console.WriteLine();
            Console.WriteLine("Press Ctrl-C to quit");

            using (var wait_exit = new CWaitForExit())
            {
                wait_exit.WaitForExit();
            }
        }

        return 0;
    }
}
