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

namespace RobotRaconteurWebGen
{
    class Program
    {
        internal class Source
        {
            internal string filename;
            internal bool is_import;
            internal string full_text;
            internal ServiceDefinition service_def;
        }

        static int Main(string[] args)
        {
            try
            {

                bool show_help = false;
                bool show_version = false;
                bool thunksource = false;
                string lang = null;
                var sources = new List<Source>();                
                string outfile = null;
                var include_dirs = new List<string>();
                string output_dir = ".";

                var p = new OptionSet()
            {
                {"thunksource", "generate thunk source code", v=> thunksource = v!=null},
                {"version", "print program version", v => show_version = v != null},
                {"output-dir=", "directory for output", v => output_dir = v },
                {"lang=", "language to generate sources for for (only csharp currently supported)", v=> lang=v},
                {"import=", "input file for use in imports", v=> sources.Add(new Source {filename = v, is_import = true }) },
                {"I|include-path=", "include path", v=> include_dirs.Add(v) },
                {"outfile=", "unified output file (csharp only)", v=> outfile = v },
                {"h|help", "show this message and exit", v=> show_help = v!=null},
            };

                try
                {
                    foreach (var s in p.Parse(args))
                    {
                        sources.Add(new Source { filename = s, is_import = false });
                    }
                }
                catch (OptionException e)
                {
                    Console.Write("RobotRaconteurWebGen: fatal error: ");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try 'RobotRaconteurWebGen --help' for more information.");
                    return 1;
                }


                if (show_help)
                {
                    ShowHelp(p);
                    return 0;
                }

                if (show_version)
                {
                    ShowVersion(p);
                    return 0;
                }

                if (!thunksource)
                {
                    Console.WriteLine("RobotRaconteurWebGen: fatal error: no command specified");
                    Console.WriteLine("Try 'RobotRaconteurWebGen --help' for more information.");
                    return 1;
                }

                if (sources.Count(x => x.is_import == false) == 0)
                {
                    Console.WriteLine("RobotRaconteurWebGen: fatal error: no files specified for thunksource");                    
                    return 1001;
                }

                if (lang != "csharp")
                {
                    Console.WriteLine("RobotRaconteurWebGen: fatal error: unknown or no language specified");                    
                    return 1012;
                }

                var robdef_path = Environment.GetEnvironmentVariable("ROBOTRACONTEUR_ROBDEF_PATH");
                if (robdef_path != null)
                {
                    robdef_path = robdef_path.Trim();
                    var env_dirs = robdef_path.Split(Path.PathSeparator);
                    include_dirs.AddRange(env_dirs);
                }

                foreach (var s in sources)
                {
                    if (!File.Exists(s.filename) && !Path.IsPathFullyQualified(s.filename))
                    {
                        foreach (var inc in include_dirs)
                        {
                            string s3 = Path.Join(inc, s.filename);
                            if (File.Exists(s3))
                            {
                                s.filename = s3;
                                break;
                            }                            
                        }
                    }
                }

                foreach (var s in sources)
                {
                    if (!File.Exists(s.filename))
                    {
                        Console.WriteLine("RobotRaconteurWebGen: fatal error: input file not found {0}", s.filename);                        
                        return 1002;
                    }
                }

                if (!Directory.Exists(output_dir))
                {
                    Console.WriteLine("RobotRaconteurWebGen: fatal error: output director not found {0}", output_dir);
                    return 1003;
                }
                                
                foreach (var s in sources)
                {
                    ServiceDefinition d = null;
                    string def;
                    try
                    {
                        StreamReader sr = new StreamReader(s.filename);
                        def = sr.ReadToEnd();
                        sr.Close();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("RobotRaconteurWebGen: fatal error: could not open file {0}", s.filename);
                        return 1004;
                    }

                    try
                    {
                        d = new ServiceDefinition();
                        ServiceDefinitionParseInfo parse_info = default(ServiceDefinitionParseInfo);
                        parse_info.RobDefFilePath = s.filename;
                        d.FromString(def, parse_info);
                        string a = d.ToString();
                        s.full_text = def;
                        s.service_def = d;
                    }
                    catch (ServiceDefinitionParseException ee)
                    {
                        Console.WriteLine("{0}({1}): error: {2}",s.filename, ee.ParseInfo.LineNumber, ee.ShortMessage);
                        return 1005;
                    }
                }

                try
                {
                    ServiceDefinitionUtil.VerifyServiceDefinitions(sources.Select(x => x.service_def).ToDictionary(x => x.Name));
                }
                catch (ServiceDefinitionParseException ee)
                {
                    Console.WriteLine("{0}({1}): error: {2}", ee.ParseInfo.RobDefFilePath, ee.ParseInfo.LineNumber, ee.ShortMessage);
                    return 1007;
                }
                catch (ServiceDefinitionVerifyException ee)
                {
                    Console.WriteLine("{0}({1}): error: {2}", ee.ParseInfo.RobDefFilePath, ee.ParseInfo.LineNumber, ee.ShortMessage);
                    return 1008;
                }
                catch (Exception ee)
		        {
                    Console.WriteLine("RobotRaconteurWebGen: fatal error: could not verify service definition set {0}", ee.Message);
                    return 1009;
                }

                if (outfile == null)
                {                   
                    foreach (var s in sources)
                    {
                        try
                        {
                            CSharpServiceLangGen.GenerateFiles(s.service_def, s.full_text, output_dir);
                        }
                        catch (Exception ee)
                        {
                            Console.WriteLine("{0}: error: could not generate thunksource files {1}", s.filename, ee.Message);
                            return 1010;
                        }
                    }
                    return 0;
                    
                }
                else
                {
                    StreamWriter f2;
                    try
                    {
                        f2 = new StreamWriter(outfile);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("RobotRaconteurWebGen: fatal error: could not open outfile for writing {0}", outfile);
                        return 1015;
                    }

                    using (f2)
                    {
                        CSharpServiceLangGen.GenerateOneFileHeader(f2);

                        foreach (var s in sources)
                        {
                            try
                            {
                                CSharpServiceLangGen.GenerateOneFilePart(s.service_def, s.full_text, f2);
                            }
                            catch (Exception ee)
                            {
                                Console.WriteLine("{0}: error: could not generate thunksource files {1}", s.filename, ee.Message);
                                return 1010;
                            }
                        }
                    }
                    return 0;
                }
            }
            catch (Exception e)
            {
                Console.Write("RobotRaconteurWebGen: error: ");
                Console.WriteLine(e.Message);
                return 1;
            }

            Console.WriteLine("RobotRaconteurGen: error: unknown internal error");

            return 7;
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: RobotRaconteurWebGen [Options+] sources");
            Console.WriteLine("Generates sources and provides utitilies for the RobotRaconteurWeb library.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        static void ShowVersion(OptionSet p)
        {
            Console.WriteLine("RobotRaconteurWebGen version {0}", RobotRaconteurNode.Version);
            Console.WriteLine();
        }
    }
}
