using System;
using System.Threading.Tasks;
using H5;
using H5.Core;
using static H5.Core.es5;
using static H5.Core.dom;
using RobotRaconteurWeb;
using experimental.reynard_the_robot;

namespace h5_client
{
    class Program
    {
        static async void Main(string[] args)
        {
            var status_div = document.getElementById("status");
            status_div.innerHTML = "Connecting...";
            var error_log = document.getElementById("error_log");

            // Thunk source must me manually generated and registered when using H5
            RobotRaconteurNode.s.RegisterServiceType(new experimental__reynard_the_robotFactory());

            try
            {

                // Connect to Reynard the Robot service using websocket
                string url = "rr+tcp://localhost:29200?service=reynard";
                var c = (Reynard)await RobotRaconteurNode.s.ConnectService(url);

                var received_messages_div = document.getElementById("received_messages");


                // Connect a callback function to listen for new messages
                c.new_message += msg => received_messages_div.innerHTML += $"{msg}<br>";

                // Handle when the send_message button is clicked
                var send_button = document.getElementById("send_message");
                send_button.onclick = async delegate(MouseEvent ev)
                {
                    try
                    {
                        var message = document.getElementById("message").As<HTMLInputElement>().value;
                        await c.say(message);
                    }
                    catch (Exception e)
                    {
                        error_log.innerHTML += $"Error: {e.Message}<br>";
                    }
                };

                var teleport_button = document.getElementById("teleport");
                teleport_button.onclick = async delegate(MouseEvent ev)
                {
                    try
                    {
                        var x = double.Parse(document.getElementById("teleport_x").As<HTMLInputElement>().value) * 1e-3;
                        var y = double.Parse(document.getElementById("teleport_y").As<HTMLInputElement>().value) * 1e-3;
                        await c.teleport(x, y);
                    }
                    catch (Exception e)
                    {
                        error_log.innerHTML += $"Error: {e.Message}<br>";
                    }
                };


                status_div.innerHTML = "Connected";

                // Run loop to update Reynard position
                while (true)
                {
                    try
                    {
                        // Read the current state using a wire "peek". Can also "connect" to receive streaming updates.
                        var state = (await c.state.PeekInValue()).Item1;
                        document.getElementById("reynard_position").innerHTML = $"x: {state.robot_position[0]}, y: {state.robot_position[1]}";
                    }
                    catch (Exception e)
                    {
                        status_div.innerHTML = "Error";
                        error_log.innerHTML += $"Error: {e.Message}<br>";
                    }

                    await Task.Delay(100);
                }
            }
            catch (Exception e)
            {
                status_div.innerHTML = "Error";
                error_log.innerHTML += $"Error: {e.Message}<br>";
            }
        }
    }
}
