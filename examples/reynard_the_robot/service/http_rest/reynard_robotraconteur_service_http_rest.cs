using System;
using RobotRaconteurWeb;
using experimental.reynard_the_robot;
using RestSharp;
using Newtonsoft.Json;
using System.Collections.Generic;
using DrekarLaunchProcess;
using System.Threading.Tasks;
using System.Threading;

class Reynard_impl : Reynard_default_impl, IDisposable
{

    string _base_url;
    Timer _state_timer;

    public Reynard_impl(string base_url)
    {
        _base_url = base_url;
    }

    public async Task<dynamic> _get_json(string path)
    {
        using (var client = new RestClient(_base_url))
        {
            var request = new RestRequest(path, Method.Get);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                throw new OperationFailedException("HTTP request failed: " + response.Content);
            }
            var content = response.Content;
            return JsonConvert.DeserializeObject(content);
        }
    }

    public async Task<dynamic> _post_json(string path, object args)
    {
        using (var client = new RestClient(_base_url))
        {
            var request = new RestRequest(path, Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(args);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                throw new OperationFailedException("HTTP request failed: " + response.Content);
            }
            var content = response.Content;
            if (content != null)
            {
                return JsonConvert.DeserializeObject(content);
            }
            else
            {
                return null;
            }
        }
    }

    public override async Task<double[]> get_robot_position(CancellationToken cancel=default(CancellationToken))
    {
        var json = await _get_json("/api/state");
        var ret = new double[2];
        ret[0] = json.x * 1e-3;
        ret[1] = json.y * 1e-3;
        return ret;
    }

    public override async Task<double[]> get_color(CancellationToken cancel=default(CancellationToken))
    {
        var json = await _get_json("/api/color");
        return new double[] { json.r, json.g, json.b };
    }
    public override async Task set_color(double[] value, CancellationToken cancel=default(CancellationToken))
    {
        var args = new Dictionary<string, double>() { { "r", value[0] }, { "g", value[1] }, { "b", value[2] } };
        await _post_json("/api/color", args);
    }

    public override async Task teleport(double x, double y, CancellationToken cancel=default(CancellationToken))
    {
        var args = new Dictionary<string, double>() { { "x", x * 1e3 }, { "y", y * 1e3 } };
        await _post_json("/api/teleport", args);
    }
    public override async Task setf_arm_position(double q1, double q2, double q3, CancellationToken cancel=default(CancellationToken))
    {
        var args = new Dictionary<string, double>() { { "q1", q1 * (180.0 / Math.PI) },
                                                      { "q2", q2 * (180.0 / Math.PI) },
                                                      { "q3", q3 * (180.0 / Math.PI) } };
        await _post_json("/api/arm", args);
    }
    public override async Task<double[]> getf_arm_position(CancellationToken cancel=default(CancellationToken))
    {
        var json = await _get_json("/api/arm");
        return new double[] { json.q1 * (Math.PI / 180.0), json.q2 * (Math.PI / 180.0), json.q3 * (Math.PI / 180.0) };
    }
    public override async Task drive_robot(double vel_x, double vel_y, double timeout, bool wait, CancellationToken cancel=default(CancellationToken))
    {
        var args = new Dictionary<string, double>() {
            { "vel_x", vel_x * 1e3 }, { "vel_y", vel_y * 1e3 }, { "timeout", timeout }, { "wait", wait ? 1 : 0 }
        };
        await _post_json("/api/drive_robot", args);
    }
    public override async Task drive_arm(double q1, double q2, double q3, double timeout, bool wait, CancellationToken cancel=default(CancellationToken))
    {
        var args = new Dictionary<string, double>() { { "q1", q1 * (180.0 / Math.PI) },
                                                      { "q2", q2 * (180.0 / Math.PI) },
                                                      { "q3", q3 * (180.0 / Math.PI) },
                                                      { "timeout", timeout },
                                                      { "wait", wait ? 1 : 0 } };
        await _post_json("/api/drive_arm", args);
    }
    public override async Task say(string message, CancellationToken cancel=default(CancellationToken))
    {
        var args = new Dictionary<string, string>() { { "message", message } };
        await _post_json("api/say", args);
    }

    private  async Task timer_task(CancellationToken cancel)
    {
        while (!cancel.IsCancellationRequested)
        {
            try
            {
                var json = await _get_json("/api/state");

                var state = new ReynardState() {
                    time = json.time,
                    robot_position = new double[] { json.x * 1e-3, json.y * 1e-3 },
                    arm_position = new double[] { json.q1 * (Math.PI / 180.0), json.q2 * (Math.PI / 180.0),
                                                json.q3 * (Math.PI / 180.0) },
                    robot_velocity = new double[] { json.vel_x * 1e-3, json.vel_y * 1e-3 },
                    arm_velocity = new double[] { json.vel_q1 * (Math.PI / 180.0), json.vel_q2 * (Math.PI / 180.0),
                                                json.vel_q3 * (Math.PI / 180.0) }
                };

                if (rrvar_state != null)
                {
                    rrvar_state.OutValue = state;
                }

                var message_json = await _get_json("/api/messages");
                foreach (string message in message_json)
                {
                    new_message?.Invoke(message);
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

    public override event Action<string> new_message;
}

class Program
{
    static int Main(string[] args)
    {
        var reynard = new Reynard_impl("http://localhost:29201");
        using (reynard) using (var node_setup =
                                   new ServerNodeSetup("experimental.reynard_the_robot_csharp_rest", 59201, args: args))
        {
            var ctx = RobotRaconteurNode.s.RegisterService("reynard", "experimental.reynard_the_robot", reynard);

            reynard._start();

            Console.WriteLine("Reynard the Robot C# Service Started");
            Console.WriteLine();
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
