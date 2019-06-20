// Copyright 2011-2019 Wason Technology, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RobotRaconteurWeb;
using Mono.Options;
using System.IO;

namespace RobotRaconteurNETGen
{
    class Program
    {
        static int Main(string[] args)
        {

            bool show_help = false;
            bool thunksource=false;
            string lang=null;

            var p = new OptionSet()
            {
                {"thunksource", "generate thunk source code", v=> thunksource = v!=null},
                {"lang=", "language to generate sources for for", v=> lang=v},
                { "h|help", "show this message and exit", v=> show_help = v!=null},
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("RobotRaconteurGenNETCLI: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try 'RobotRaconteurGenNETCLI --help' for more information.");
                return 1;
            }
                       

            if (show_help)
            {
                ShowHelp(p);
                return 0;
            }


            if (!thunksource)
            {
                Console.WriteLine("RobotRaconteurGenNETCLI: no command specified");
                Console.WriteLine("Try 'RobotRaconteurGenNETCLI --help' for more information.");
                return 1;
            }

            if (lang != "csharp")
            {
                Console.WriteLine("RobotRaconteurGenNETCLI: unknown or no language specified");
                Console.WriteLine("Try 'RobotRaconteurGenNETCLI --help' for more information.");
                return 1;
            }

            
            try
            {
                List<Tuple<ServiceDefinition,string>> defs = new List<Tuple<ServiceDefinition,string>>();
                foreach (string f in extra)
                {


                    ServiceDefinition d = null;
                    
                    StreamReader sr = new StreamReader(f);
                    string def = sr.ReadToEnd();
                    sr.Close();

                    d = new ServiceDefinition();
                    d.FromString(def);
                    string a = d.ToString();
                    defs.Add(Tuple.Create(d,def));
                    
                }

                ServiceDefinitionUtil.VerifyServiceDefinitions(defs.Select(x=>x.Item1).ToDictionary(x=>x.Name));

                foreach (var d in defs)
                {
                    CSharpServiceLangGen.GenerateFiles(d.Item1, d.Item2, ".");
                }

            }
            catch (Exception e)
            {
                Console.Write("RobotRaconteurGenNETCLI: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try 'RobotRaconteurGenNETCLI --help' for more information.");
                return 1;
            }

            return 0;


        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: RobotRaconteurGenNETCLI [Options+] sources");
            Console.WriteLine("Generates sources and provides utitilies for the RobotRaconteurNETCLI library.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }
}
