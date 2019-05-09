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
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using System.Globalization;

namespace RobotRaconteur
{
    public struct RobotRaconteurVersion
    {
        public uint major;
        public uint minor;
        public uint patch;
        public uint tweak;
                
        public RobotRaconteurVersion(uint major, uint minor, uint patch = 0, uint tweak = 0)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
            this.tweak = tweak;
        }

        public RobotRaconteurVersion(string v)
        {
            major = 0;
            minor = 0;
            patch = 0;
            tweak = 0;
            FromString(v);
        }

        public override string ToString()
        {
            if (patch == 0 && tweak == 0)
            {
                return String.Format("{0}.{1}", major, minor);
            }
            else if (tweak == 0)
            {
                return String.Format("{0}.{1}.{2}", major, minor, patch);
            }
            return String.Format("{0}.{1}.{2}.{3}", major, minor, patch, tweak);
        }

        public void FromString(string v)
        {
            var m = Regex.Match(v, "^(\\d+)\\.(\\d+)(?:\\.(\\d+)(?:\\.(\\d+))?)?$");
            if (!m.Success)
            {
                throw new RobotRaconteurParseException("Format error for version definition \"" + v + "\"");
            }

            major = UInt32.Parse(m.Groups[1].Value);
            minor = UInt32.Parse(m.Groups[2].Value);
            patch = m.Groups[3].Success ? UInt32.Parse(m.Groups[3].Value) : 0;
            tweak = m.Groups[4].Success ? UInt32.Parse(m.Groups[4].Value) : 0;
        }

        public static bool operator ==(RobotRaconteurVersion v1, RobotRaconteurVersion v2)
        {
            return (v1.major == v2.major) && (v1.minor == v2.minor) && (v1.patch == v2.patch) && (v1.tweak == v2.tweak);
        }

        public static bool operator !=(RobotRaconteurVersion v1, RobotRaconteurVersion v2)
        {
            return !((v1.major == v2.major) && (v1.minor == v2.minor) && (v1.patch == v2.patch) && (v1.tweak == v2.tweak));
        }

        public static bool operator >(RobotRaconteurVersion v1, RobotRaconteurVersion v2)
        {
            if (v1.major > v2.major) return true;
            if (v1.minor > v2.minor) return true;
            if (v1.patch > v2.patch) return true;
            if (v1.tweak > v2.tweak) return true;
            return false;
        }

        public static bool operator>=(RobotRaconteurVersion v1, RobotRaconteurVersion v2)
        {
            if ((v1.major == v2.major) && (v1.minor == v2.minor) && (v1.patch == v2.patch) && (v1.tweak == v2.tweak))
            {
                return true;
            }
            return v1 > v2;
        }

        public static bool operator <(RobotRaconteurVersion v1, RobotRaconteurVersion v2)
        {
            if (v1.major < v2.major) return true;
            if (v1.minor < v2.minor) return true;
            if (v1.patch < v2.patch) return true;
            if (v1.tweak < v2.tweak) return true;
            return false;
        }

        public static bool operator <=(RobotRaconteurVersion v1, RobotRaconteurVersion v2)
        {
            if ((v1.major == v2.major) && (v1.minor == v2.minor) && (v1.patch == v2.patch) && (v1.tweak == v2.tweak))
            {
                return true;
            }
            return v1 < v2;
        }

        public static explicit operator bool(RobotRaconteurVersion v1)
        {
            return !((v1.major == 0) && (v1.minor == 0) && (v1.patch == 0) && (v1.tweak == 0));
        }
    }

    namespace detail
    {
        static class ServiceDefinitionUtil
        {
            internal const string RR_NAME_REGEX = "[a-zA-Z](?:\\w*[a-zA-Z0-9])?";
            internal const string RR_TYPE_REGEX = "(?:[a-zA-Z](?:\\w*[a-zA-Z0-9])?)(?:\\.[a-zA-Z](?:\\w*[a-zA-Z0-9])?)*";
            internal const string RR_QUALIFIED_TYPE_REGEX = "(?:[a-zA-Z](?:\\w*[a-zA-Z0-9])?)(?:\\.[a-zA-Z](?:\\w*[a-zA-Z0-9])?)+";
            internal const string RR_TYPE2_REGEX = "(?:[a-zA-Z](?:\\w*[a-zA-Z0-9])?)(?:\\.[a-zA-Z](?:\\w*[a-zA-Z0-9])?)*(?:\\[[0-9\\,\\*\\-]*\\])?(?:\\{\\w{1,16}\\})?";
            internal const string RR_INT_REGEX = "[+\\-]?\\d+";
            internal const string RR_FLOAT_REGEX = "[+\\-]?(?:(?:0|[1-9]\\d*)(?:\\.\\d*)?|:\\.\\d+)(?:[eE][+\\-]?\\d+)?";

            internal static void ServiceDefinition_FromStringFormat_common(Regex r, string l, string keyword, ref List<string> vec)
            {
                var r_match = r.Match(l);
                if (!r_match.Success)
                {
                    throw new RobotRaconteurParseException("Format error for " + keyword + " definition \"" + l + "\"");
                }

                if (r_match.Groups[1].Value != keyword)
                {
                    throw new RobotRaconteurParseException("Format error for " + keyword + " definition \"" + l + "\"");
                }
                vec.Add(r_match.Groups[2].Value);
            }

            internal static void ServiceDefinition_FromStringImportFormat(string l, string keyword, ref List<string> vec)
            {
                var r = new Regex("^[ \\t]*(\\w{1,16})[ \\t]+(" + RR_TYPE_REGEX + ")[ \\t]*$");
                ServiceDefinition_FromStringFormat_common(r, l, keyword, ref vec);
            }

            internal static void ServiceDefinition_FromStringTypeFormat(string l, string keyword, ref List<string> vec)
            {
                var r = new Regex("^[ \\t]*(\\w{1,16})[ \\t]+(" + RR_TYPE_REGEX + ")[ \\t]*$");
                ServiceDefinition_FromStringFormat_common(r, l, keyword, ref vec);
            }

            internal static bool ServiceDefinition_GetLine(TextReader is_, ref string l, ref uint pos)
            {
                var r_comment = new Regex("^[ \\t]*#[ -~\\t]*$");
                var r_empty = new Regex("^[ \\t]*$");
                var r_valid = new Regex("^[ -~\\t]*$");

                string l2;

                while (true)
                {
                    l2 = is_.ReadLine();
                    if (l2 == null)
                        return false;
                    pos++;

                    l2 = l2.TrimEnd(new char[] { '\r' });

                    if (l2.Contains('\0'))
                        throw new RobotRaconteurParseException("Service definition must not contain null characters");

                    if (r_comment.IsMatch(l2))
                    {
                        continue;
                    }

                    if (r_empty.IsMatch(l2))
                    {
                        continue;
                    }

                    while (l2.EndsWith("\\"))
                    {
                        if (l2.Length <= 0) throw new InternalErrorException("Internal parsing error");
                        l2 = l2.Substring(0, l2.Length - 1) + ' ';
                        var l3 = is_.ReadLine();
                        if (l3 == null)
                            throw new RobotRaconteurParseException("Service definition line continuation must not be on last line");
                        l3 = l3.TrimEnd(new char[] { '\r' });

                        if (l3.Contains('\0'))
                            throw new RobotRaconteurParseException("Service definition must not contain null characters");
                        l2 += l3;
                    }

                    if (!r_valid.IsMatch(l2))
                    {
                        throw new RobotRaconteurParseException("Service definition must contain only ASCII characters");
                    }

                    l = l2;
                    return true;
                }
            }

            internal static void ServiceDefinition_FindBlock(string current_line, TextReader is_, TextWriter os, ref uint pos, ref uint init_pos)
            {
                var r_start = new Regex("^[ \\t]*(\\w{1,16})[ \\t]+(" + RR_NAME_REGEX + ")[ \\t]*$");
                var r_end = new Regex("^[ \\t]*end(?:[ \\t]+(\\w{1,16}))?[ \\t]*$");

                init_pos = pos;

                var r_start_match = r_start.Match(current_line);
                if (!r_start_match.Success)
                {
                    throw new RobotRaconteurParseException("Parse error near: " + current_line, (int)pos);
                }

                os.WriteLine(current_line);

                string block_type = r_start_match.Groups[1].Value;
                string l = null;

                uint last_pos = pos;

                while (ServiceDefinition_GetLine(is_, ref l, ref pos))
                {
                    last_pos++;
                    for (; last_pos < pos; last_pos++)
                    {
                        os.WriteLine("");
                    }

                    os.WriteLine(l);

                    var r_end_match = r_end.Match(l);
                    if (r_end_match.Success)
                    {
                        if (r_end_match.Groups[1].Success)
                        {
                            if (r_end_match.Groups[1].Value != block_type)
                            {
                                throw new RobotRaconteurParseException("Block end does not match start: " + l, (int)pos);
                            }
                        }

                        return;
                    }
                }
                throw new RobotRaconteurParseException("Block end not found: " + current_line, (int)init_pos);
            }
        }
    }

    public class ServiceDefinition
    {
        public string Name;

        public Dictionary<string, ServiceEntryDefinition> Structures = new Dictionary<string, ServiceEntryDefinition>();
        public Dictionary<string, ServiceEntryDefinition> Pods = new Dictionary<string, ServiceEntryDefinition>();
        public Dictionary<string, ServiceEntryDefinition> NamedArrays = new Dictionary<string, ServiceEntryDefinition>();
        public Dictionary<string, ServiceEntryDefinition> Objects = new Dictionary<string, ServiceEntryDefinition>();

        public List<string> Options = new List<string>();

        public List<string> Imports = new List<string>();

        public List<UsingDefinition> Using = new List<UsingDefinition>();

        public List<string> Exceptions = new List<string>();

        public Dictionary<string, ConstantDefinition> Constants = new Dictionary<string, ConstantDefinition>();
        public Dictionary<string, EnumDefinition> Enums = new Dictionary<string, EnumDefinition>();

        public RobotRaconteurVersion StdVer;

        public void ToWriter(TextWriter o)
        {
            o.WriteLine("service {0}", Name);
            o.WriteLine();

            if ((bool)StdVer)
            {
                bool version_found = false;
                foreach (var so in Options)
                {
                    var r_version = new Regex("^[ \\t]*version[ \\t]+(?:(\\d+(?:\\.\\d+)*)|[ -~\\t]*)$");
                    var r_version_match = r_version.Match(so);
                    if (r_version_match.Success)
                    {
                        if (version_found) throw new RobotRaconteurParseException("Robot Raconteur version already specified");
                        if (r_version_match.Groups[1].Success)
                        {
                            version_found = true;
                            break;
                        }
                        else
                        {
                            throw new RobotRaconteurParseException("Invalid Robot Raconteur version specified");
                        }
                    }
                }
                if (!version_found)
                {
                    if (StdVer < new RobotRaconteurVersion(0, 9))
                    {
                        o.WriteLine("option version {0}", StdVer.ToString());
                        o.WriteLine();
                    }
                    else
                    {
                        o.WriteLine("stdver {0}", StdVer.ToString());
                        o.WriteLine();
                    }
                }
            }

            foreach (var import in Imports)
            {
                o.WriteLine("import {0}", import);
            }
            if (Imports.Count != 0)
            {
                o.WriteLine();
            }

            foreach (var u in Using)
            {
                o.Write(u.ToString());
            }
            if (Using.Count != 0)
            {
                o.WriteLine();
            }

            foreach (var option in Options)
            {
                o.WriteLine("option {0}", option);
            }
            if (Options.Count!=0)
            {
                o.WriteLine();
            }

            foreach (var constant in Constants.Values)
            {
                o.WriteLine(constant.ToString());
            }
            if (Constants.Count != 0)
            {
                o.WriteLine();
            }

            foreach (var e in Enums.Values)
            {
                o.Write(e.ToString());
            }
            if (Enums.Count != 0)
            {
                o.WriteLine();
            }

            foreach (var e in Exceptions)
            {
                o.WriteLine("exception {0}", e);
            }
            if (Exceptions.Count != 0)
            {
                o.WriteLine();
            }

            foreach (var d in Structures.Values)
            {
                o.WriteLine(d.ToString());
            }

            foreach (var d in Pods.Values)
            {
                o.WriteLine(d.ToString());
            }

            foreach (var d in NamedArrays.Values)
            {
                o.WriteLine(d.ToString());
            }

            foreach (var d in Objects.Values)
            {
                o.WriteLine(d.ToString());
            }
        }


        public override string ToString()
        {
            var w = new StringWriter();
            w.NewLine = "\n";
            ToWriter(w);
            return w.ToString();
        }
                
        public void CheckVersion(RobotRaconteurVersion ver = default(RobotRaconteurVersion))
        {
            
            if (!(bool)StdVer)
                return;

            if (ver == new RobotRaconteurVersion(0,0))
            {
                ver = new RobotRaconteurVersion(RobotRaconteurNode.Version);
            }

            if (ver < StdVer)
            {
                throw new ServiceException("Service " + Name + " requires newer version of Robot Raconteur");
            }

        }

        public void FromString(string s)
        {
            var w = new List<Exception>();
            FromString(s, ref w);
        }

        public void FromReader(TextReader is_)
        {
            var w = new List<Exception>();
            FromReader(is_, ref w);
        }

        public void FromString(string s, ref List<Exception> warnings)
        {
            var is_ = new StringReader(s);
            FromReader(is_, ref warnings);
        }

        public void FromReader(TextReader is_, ref List<Exception> warnings)
        {
            var r_comment = new Regex("^[ \\t]*#[ -~\\t]*$");
            var r_empty = new Regex("^[ \\t]*$");
            var r_entry = new Regex("(?:^[ \\t]*(?:(service)|(stdver)|(option)|(import)|(using)|(exception)|(constant)|(enum)|(struct)|(object)|(pod)|(namedarray))[ \\t]+(\\w[^\\s]*(?:[ \\t]+[^\\s]+)*)[ \\t]*$)|(^[ \\t]*$)");

            bool service_name_found = false;

            uint pos = 0;
            string l = null;

            RobotRaconteurVersion stdver_version = new RobotRaconteurVersion();
            bool stdver_found = false;
            uint entry_key_max = 0;

            try
            {
                while (true)
                {
                    if (!detail.ServiceDefinitionUtil.ServiceDefinition_GetLine(is_, ref l, ref pos))
                    {
                        break;
                    }

                    var r_entry_match = r_entry.Match(l);
                    if (!r_entry_match.Success)
                    {
                        throw new RobotRaconteurParseException("Parse error near: " + l, (int)pos);
                    }

                    var r_entry_match_blank = r_entry_match.Groups[14];
                    if (r_entry_match_blank.Success) continue;

                    int entry_key = 1;
                    for (; entry_key < 12; entry_key++)
                    {
                        if (r_entry_match.Groups[entry_key].Success)
                            break;
                    }

                    var r_entry_match_remaining = r_entry_match.Groups[13];

                    if (entry_key != 1 && !service_name_found)
                        throw new RobotRaconteurParseException("service name must be first entry in service definition");

                    switch (entry_key)
                    {
                        //service name
                        case 1:
                            {
                                if (entry_key_max >= 1)
                                    throw new RobotRaconteurParseException("service name must be first entry in service definition");
                                if (service_name_found)
                                    throw new RobotRaconteurParseException("service name already specified");
                                var tmp_name = new List<string>();
                                detail.ServiceDefinitionUtil.ServiceDefinition_FromStringTypeFormat(l, "service", ref tmp_name);
                                Name = tmp_name[0];
                                entry_key_max = 1;
                                service_name_found = true;
                                continue;
                            }
                        //stdver
                        case 2:
                            {
                                if (entry_key_max >= 2)
                                    throw new RobotRaconteurParseException("service name must be first after service name");
                                stdver_version.FromString(r_entry_match_remaining.Value);
                                stdver_found = true;
                                if (stdver_version < new RobotRaconteurVersion(0, 9))
                                {
                                    throw new ServiceDefinitionException("Service definition standard version 0.9 or greater required for \"stdver\" keyword");
                                }
                                continue;
                            }
                        //option
                        case 3:
                            {
                                Options.Add(r_entry_match_remaining.Value);
                                continue;
                            }
                        //import
                        case 4:
                            {
                                if (entry_key_max > 4) throw new RobotRaconteurParseException("import must be before all but options");
                                detail.ServiceDefinitionUtil.ServiceDefinition_FromStringImportFormat(l, "import", ref Imports);
                                entry_key_max = 4;
                                continue;
                            }
                        //using
                        case 5:
                            {
                                if (entry_key_max > 5) throw new RobotRaconteurParseException("using must be after imports and before all others except options");
                                var using_def = new UsingDefinition(this);
                                using_def.FromString(l);
                                Using.Add(using_def);
                                entry_key_max = 5;
                                continue;
                            }
                        //exception
                        case 6:
                            {
                                if (entry_key_max >= 9) throw new RobotRaconteurParseException("exception must be before struct and object");
                                detail.ServiceDefinitionUtil.ServiceDefinition_FromStringTypeFormat(l, "exception", ref Exceptions);
                                entry_key_max = 6;
                                continue;
                            }
                        //constant
                        case 7:
                            {
                                if (entry_key_max >= 9) throw new RobotRaconteurParseException("exception must be before struct and object");
                                var constant_def = new ConstantDefinition(this);
                                constant_def.FromString(l);
                                Constants.Add(constant_def.Name, constant_def);
                                entry_key_max = 7;
                                continue;
                            }
                        //enum
                        case 8:
                            {
                                if (entry_key_max >= 9) throw new RobotRaconteurParseException("enum must be before struct and object");
                                uint init_pos = 0;
                                var block = new StringWriter();
                                detail.ServiceDefinitionUtil.ServiceDefinition_FindBlock(l, is_, block, ref pos, ref init_pos);
                                var enum_def = new EnumDefinition(this);
                                enum_def.FromString(block.ToString(), init_pos);
                                Enums.Add(enum_def.Name, enum_def);
                                entry_key_max = 8;
                                continue;
                            }
                        //struct
                        case 9:
                            {
                                uint init_pos = 0;
                                var block = new StringWriter();
                                detail.ServiceDefinitionUtil.ServiceDefinition_FindBlock(l, is_, block, ref pos, ref init_pos);
                                var struct_def = new ServiceEntryDefinition(this);
                                struct_def.FromString(block.ToString(), init_pos, ref warnings);
                                Structures.Add(struct_def.Name, struct_def);
                                entry_key_max = 9;
                                continue;
                            }
                        //object
                        case 10:
                            {
                                uint init_pos = 0;
                                var block = new StringWriter();
                                detail.ServiceDefinitionUtil.ServiceDefinition_FindBlock(l, is_, block, ref pos, ref init_pos);
                                var object_def = new ServiceEntryDefinition(this);
                                object_def.FromString(block.ToString(), init_pos, ref warnings);
                                Objects.Add(object_def.Name, object_def);
                                entry_key_max = 10;
                                continue;
                            }
                        //pod
                        case 11:
                            {
                                uint init_pos = 0;
                                var block = new StringWriter();
                                detail.ServiceDefinitionUtil.ServiceDefinition_FindBlock(l, is_, block, ref pos, ref init_pos);
                                var struct_def = new ServiceEntryDefinition(this);
                                struct_def.FromString(block.ToString(), init_pos, ref warnings);
                                Pods.Add(struct_def.Name, struct_def);
                                entry_key_max = 9;
                                continue;
                            }
                        //namedarray
                        case 12:
                            {
                                uint init_pos=0;
                                var block = new StringWriter();
                                detail.ServiceDefinitionUtil.ServiceDefinition_FindBlock(l, is_, block, ref pos, ref init_pos);
                                var struct_def = new ServiceEntryDefinition(this);
                                struct_def.FromString(block.ToString(), init_pos, ref warnings);
                                NamedArrays.Add(struct_def.Name,struct_def);
                                entry_key_max = 9;
                                continue;
                            }
                        default:
                            throw new RobotRaconteurParseException("Parse error near: " + l, (int)pos);
                    }

                }

                bool version_found = false;
                if (stdver_found)
                {
                    StdVer = stdver_version;
                    version_found = true;
                }
                foreach(var so in Options)

                {
                    var r_version= new Regex("^[ \\t]*version[ \\t]+(?:(\\d+(?:\\.\\d+)*)|[ -~\\t]*)$");
                    var r_version_match = r_version.Match(so);
                    if (r_version_match.Success)
                    {
                        if (version_found) throw new RobotRaconteurParseException("Robot Raconteur version already specified");
                        if (r_version_match.Groups[1].Success)
                        {
                            StdVer = new RobotRaconteurVersion(r_version_match.Groups[1].Value);
                        }
                        else
                        {
                            throw new RobotRaconteurParseException("Invalid Robot Raconteur version specified");
                        }
                        version_found = true;
                    }
                }
            }
            catch (RobotRaconteurParseException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new RobotRaconteurParseException("Parse error near: " + l, (int)pos);
            }
        }

        public void Reset()
        {
            Structures.Clear();
            Objects.Clear();
            Options.Clear();
            Imports.Clear();
            Using.Clear();
            Exceptions.Clear();
            Constants.Clear();
            Enums.Clear();
            Pods.Clear();
            NamedArrays.Clear();
        }
    }


    public abstract class NamedTypeDefinition
    {
        public string Name;
        public abstract DataTypes RRDataType { get; }
        public abstract string ResolveQualifiedName();
    }


    public class ServiceEntryDefinition : NamedTypeDefinition
    {   

        public Dictionary<string, MemberDefinition> Members = new Dictionary<string, MemberDefinition>();

        public DataTypes EntryType;

        public bool IsStructure = true;

        public List<string> Implements = new List<string>();
        public List<string> Options = new List<string>();
        public Dictionary<string, ConstantDefinition> Constants = new Dictionary<string, ConstantDefinition>();

        public ServiceDefinition ServiceDefinition;
                
        public ServiceEntryDefinition(ServiceDefinition def)
        {
            ServiceDefinition = def;
        }

        internal string UnqualifyTypeWithUsing(string s)
        {
            if (!s.Contains('.'))
            {
                return s;
            }

            foreach (var u in ServiceDefinition.Using)
            {
                if (u.QualifiedName == s)
                {
                    return u.UnqualifiedName;
                }
            }
            return s;
        }

        internal string QualifyTypeWithUsing(string s)
        {
            if (s.Contains('.'))
            {
                return s;
            }

            foreach (var u in ServiceDefinition.Using)
            {
                if (u.UnqualifiedName == s)
                {
                    return u.QualifiedName;
                }
            }
            return s;
        }

        public void ToWriter(TextWriter o)
        {
            switch (EntryType)
            {
                case DataTypes.structure_t:
                    o.WriteLine("struct {0}", Name);
                    break;
                case DataTypes.pod_t:
                    o.WriteLine("pod {0}", Name);
                    break;
                case DataTypes.namedarray_t:
                    o.WriteLine("namedarray {0}", Name);
                    break;
                case DataTypes.object_t:
                    o.WriteLine("object {0}", Name);
                    break;
                default:
                    throw new ServiceDefinitionException("Invalid ServiceEntryDefinition type in " + Name);
            }

            foreach (var imp in Implements)
            {
                o.WriteLine("    implements {0}", UnqualifyTypeWithUsing(imp));
            }

            foreach (var option in Options)
            {
                o.WriteLine("    option {0}", option);
            }

            foreach (var constant in Constants)
            {
                o.WriteLine("    {0}", constant.ToString());
            }

            foreach (var d in Members.Values)
            {
                var d1 = d.ToString();
                if (EntryType != DataTypes.object_t)
                {
                    var d2 = new Regex("property");
                    d1 = d2.Replace(d1, "field", 1);
                }
                o.WriteLine(d1);
            }

            o.WriteLine("end");
        }

        public override string ToString()
        {
            var w = new StringWriter();
            w.NewLine = "\n";
            ToWriter(w);
            return w.ToString();
        }

        public void FromString(string s)
        {
            FromString(s, 0);
        }

        public void FromString(string s, uint startline)
        {
            var w = new List<Exception>();
            FromString(s, startline, ref w);
        }

        public void FromString(string s, uint startline, ref List<Exception> warnings)
        {
            var is_ = new StringReader(s);
            FromStream(is_, startline, ref warnings);
        }

        public void FromStream(TextReader s)
        {
            FromStream(s, 0);
        }

        public void FromStream(TextReader s, uint startline)
        {
            var w = new List<Exception>();
            FromStream(s, startline, ref w);
        }

        public void FromStream(TextReader s, uint startline, ref List<Exception> warnings)
        {
            Reset();

            var start_struct_regex = new Regex("^[ \\t]*struct[ \\t]+(\\w+)[ \\t]*$");
            var start_pod_regex = new Regex("^[ \\t]*pod[ \\t]+(\\w+)[ \\t]*$");
            var start_namedarray_regex = new Regex("^[ \\t]*namedarray[ \\t]+(\\w+)[ \\t]*$");
            var start_object_regex = new Regex("^[ \\t]*object[ \\t]+(\\w+)[ \\t]*$");
            var end_struct_regex = new Regex("^[ \\t]*end[ \\t]+struct[ \\t]*$");
            var end_pod_regex = new Regex("^[ \\t]*end[ \\t]+pod[ \\t]*$");
            var end_namedarray_regex = new Regex("^[ \\t]*end[ \\t]+namedarray[ \\t]*$");
            var end_object_regex = new Regex("^[ \\t]*end[ \\t]+object[ \\t]*$");

            uint pos = startline - 1;
            string l = null;
            if (!detail.ServiceDefinitionUtil.ServiceDefinition_GetLine(s, ref l, ref pos))
            {
                throw new RobotRaconteurParseException("Invalid object member", (int)startline);
            }

            var start_struct_cmatch = start_struct_regex.Match(l);
            var start_pod_cmatch = start_pod_regex.Match(l);
            var start_namedarray_cmatch = start_namedarray_regex.Match(l);
            var start_object_cmatch = start_object_regex.Match(l);
            if (start_struct_cmatch.Success)
            {
                EntryType = DataTypes.structure_t;
                Name = start_struct_cmatch.Groups[1].Value;
            }
            else if (start_pod_cmatch.Success)
            {
                EntryType = DataTypes.pod_t;
                Name = start_pod_cmatch.Groups[1].Value;
            }
            else if (start_namedarray_cmatch.Success)
            {
                EntryType = DataTypes.namedarray_t;
                Name = start_namedarray_cmatch.Groups[1].Value;
            }
            else if (start_object_cmatch.Success)
            {
                EntryType = DataTypes.object_t;
                Name = start_object_cmatch.Groups[1].Value;
            }
            else
            {
                throw new RobotRaconteurParseException("Parse error", (int)startline);
            }

            try
            {
                var r_member = new Regex("(?:^[ \\t]*(?:(option)|(implements)|(constant)|(field)|(property)|(function)|(event)|(objref)|(pipe)|(callback)|(wire)|(memory)|(end))[ \\t]+(\\w[^\\s]*(?:[ \\t]+[^\\s]+)*)[ \\t]*$)|(^[ \\t]*$)");

                while (detail.ServiceDefinitionUtil.ServiceDefinition_GetLine(s, ref l, ref pos))
                {

                    try
                    {

                        var r_member_match = r_member.Match(l);
                        if (!r_member_match.Success)
                        {
                            if (l.Trim() == "end")
                            {
                                if (detail.ServiceDefinitionUtil.ServiceDefinition_GetLine(s, ref l, ref pos))
                                {
                                    throw new RobotRaconteurParseException("Parse error", (int)(pos));
                                }
                                return;
                            }
                            throw new RobotRaconteurParseException("Parse error near: " + l, (int)pos);
                        }

                        var r_member_match_blank = r_member_match.Groups[15];
                        if (r_member_match_blank.Success) continue;

                        int member_key = 1;
                        for (; member_key < 13; member_key++)
                        {
                            if (r_member_match.Groups[member_key].Success)
                                break;
                        }

                        var r_member_match_remaining = r_member_match.Groups[14];

                        if ((EntryType != DataTypes.object_t) && (member_key >= 5 && member_key != 13))
                        {
                            throw new RobotRaconteurParseException("Structures can only contain fields, constants, and options", (int)(pos));
                        }

                        switch (member_key)
                        {
                            //option
                            case 1:
                                {
                                    //TODO: look in to this
                                    //if (!Members.empty()) throw RobotRaconteurParseException("Structure option must come before members", (int32_t)(pos));
                                    Options.Add(r_member_match_remaining.Value);
                                    warnings.Add(new RobotRaconteurParseException("option keyword is deprecated", (int)pos));
                                    continue;
                                }
                            //implements
                            case 2:
                                {
                                    if (Members.Count != 0) throw new RobotRaconteurParseException("Structure implements must come before members", (int)(pos));
                                    if (EntryType != DataTypes.object_t) throw new RobotRaconteurParseException("Structures can only contain fields, constants, and options", (int)(pos));
                                    var implements1 = new List<string>();
                                    detail.ServiceDefinitionUtil.ServiceDefinition_FromStringTypeFormat(l, "implements", ref implements1);
                                    Implements.Add(QualifyTypeWithUsing(implements1[0]));
                                    continue;
                                }
                            //constant
                            case 3:
                                {
                                    if (Members.Count != 0) throw new RobotRaconteurParseException("Structure constants must come before members", (int)(pos));
                                    var constant_def = new ConstantDefinition(this);
                                    constant_def.FromString(l);
                                    Constants.Add(constant_def.Name, constant_def);
                                    continue;
                                }
                            //field
                            case 4:
                                {
                                    if (EntryType == DataTypes.object_t) throw new RobotRaconteurParseException("Objects cannot contain fields.  Use properties instead.", (int)(pos));
                                    var m = new PropertyDefinition(this);
                                    m.FromString(l);
                                    Members.Add(m.Name, m);
                                    continue;
                                }
                            //property
                            case 5:
                                {
                                    var m = new PropertyDefinition(this);
                                    m.FromString(l);
                                    Members.Add(m.Name, m);
                                    continue;
                                }
                            //function
                            case 6:
                                {
                                    var m = new FunctionDefinition(this);
                                    m.FromString(l);
                                    Members.Add(m.Name, m);
                                    continue;
                                }
                            //event
                            case 7:
                                {
                                    var m = new EventDefinition(this);
                                    m.FromString(l);
                                    Members.Add(m.Name, m);
                                    continue;
                                }
                            //objref
                            case 8:
                                {
                                    var m = new ObjRefDefinition(this);
                                    m.FromString(l);
                                    Members.Add(m.Name, m);
                                    continue;
                                }
                            //pipe
                            case 9:
                                {
                                    var m = new PipeDefinition(this);
                                    m.FromString(l);
                                    Members.Add(m.Name, m);
                                    continue;
                                }
                            //callback
                            case 10:
                                {
                                    var m = new CallbackDefinition(this);
                                    m.FromString(l);
                                    Members.Add(m.Name, m);
                                    continue;
                                }
                            //wire
                            case 11:
                                {
                                    var m = new WireDefinition(this);
                                    m.FromString(l);
                                    Members.Add(m.Name, m);
                                    continue;
                                }
                            //memory
                            case 12:
                                {
                                    var m = new MemoryDefinition(this);
                                    m.FromString(l);
                                    Members.Add(m.Name, m);
                                    continue;
                                }
                            //end
                            case 13:
                                {
                                    if (EntryType == DataTypes.structure_t)
                                    {
                                        if (!end_struct_regex.IsMatch(l))
                                        {
                                            throw new RobotRaconteurParseException("Parse error", (int)(pos));
                                        }
                                    }
                                    else if (EntryType == DataTypes.pod_t)
                                    {
                                        if (!end_pod_regex.IsMatch(l))
                                        {
                                            throw new RobotRaconteurParseException("Parse error", (int)(pos));
                                        }
                                    }
                                    else if (EntryType == DataTypes.namedarray_t)
                                    {
                                        if (!end_namedarray_regex.IsMatch(l))
                                        {
                                            throw new RobotRaconteurParseException("Parse error", (int)(pos));
                                        }
                                    }
                                    else
                                    {
                                        if (!end_object_regex.IsMatch(l))
                                        {
                                            throw new RobotRaconteurParseException("Parse error", (int)(pos));
                                        }
                                    }

                                    if (detail.ServiceDefinitionUtil.ServiceDefinition_GetLine(s, ref l, ref pos))
                                    {
                                        throw new RobotRaconteurParseException("Parse error", (int)(pos));
                                    }
                                    return;
                                }

                            default:
                                throw new RobotRaconteurParseException("Parse error", (int)(pos));
                                break;

                        }
                    }
                    catch (RobotRaconteurParseException e)
                    {
                        throw;
                    }
                }
            }
            catch (Exception exp)
            {
                throw new RobotRaconteurParseException("Parse error: " + exp.Message + " near: " + l, (int)(pos));
            }
        }

        public void Reset()
        {
            Name = "";
            Members.Clear();
            EntryType = DataTypes.structure_t;
            Implements.Clear();
            Options.Clear();
            Constants.Clear();
        }

        public override DataTypes RRDataType
        {
            get
            {

                switch (EntryType)
                {
                    case DataTypes.structure_t:
                    case DataTypes.pod_t:
                    case DataTypes.namedarray_t:
                    case DataTypes.object_t:
                        break;
                    default:
                        throw new ServiceDefinitionException("Invalid ServiceEntryDefinition type in " + Name);
                }

                return EntryType;
            }
        }

        public override string ResolveQualifiedName()
        { 
            return ServiceDefinition.Name + "." + Name;
        }

        
    }

    public enum MemberDefinition_Direction
    {
        readonly_=0,
        writeonly,
        both
    }

    public abstract class MemberDefinition
    {
        public string Name;
        public ServiceEntryDefinition ServiceEntry;
        public List<string> Modifiers;

        public MemberDefinition(ServiceEntryDefinition ServiceEntry)
        {
            this.ServiceEntry = ServiceEntry;
        }

        internal class MemberDefiniton_ParseResults
        {
            public string MemberType;
            public string Name;
            public string DataType;
            public List<string> Parameters = new List<string>();
            public List<string> Modifiers = new List<string>();
        }

        internal static bool MemberDefinition_ParseCommaList(Regex r, string s, ref List<string> res)
        {
            var r_empty = new Regex("^[ \\t]*$");
            if (r_empty.IsMatch(s))
            {
                return true;
            }

            var r_match = r.Match(s);
            if (!r_match.Success)
            {
                return false;
            }

            res.Add(r_match.Groups[1].Value);
            if (r_match.Groups[2].Success)
            {
                if (!MemberDefinition_ParseCommaList(r, r_match.Groups[2].Value, ref res))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool MemberDefinition_ParseParameters(string s, ref List<string> res)
        {
            var r_params = new Regex("^[ \\t]*(" + detail.ServiceDefinitionUtil.RR_TYPE_REGEX + "(?:\\[[0-9\\,\\*\\-]*\\])?(?:\\{\\w{1,16}\\})?[ \\t]+\\w+)(?:[ \\t]*,[ \\t]*([ -~\\t]*\\w[ -~\\t]*))?[ \\t]*$");

            return MemberDefinition_ParseCommaList(r_params, s, ref res);
        }

        internal static bool MemberDefinition_ParseModifiers(string s, ref List<string> res)
        {
            var r_modifier = new Regex("^[ \\t]*(" + detail.ServiceDefinitionUtil.RR_NAME_REGEX + "(?:\\([\\w\\-\\., \\t\\\"\\{\\}\\:]*\\))?)(?:[ \\t]*,([ -~\\t]*))?$");

            return MemberDefinition_ParseCommaList(r_modifier, s, ref res);
        }

        internal static string MemberDefinition_ModifiersToString(List<string> modifiers)
        {
            if (modifiers.Count == 0)
                return "";

            return " [" + String.Join(",", modifiers) + "]";
        }

        internal static bool MemberDefinition_ParseFormat_common(string s, out MemberDefiniton_ParseResults res)
        {
            var r = new Regex("^[ \\t]*([a-zA-Z]+)[ \\t]+(?:([a-zA-Z][\\w\\{\\}\\[\\]\\*\\,\\-\\.]*)[ \\t]+)?(\\w+)(?:[ \\t]*(\\(([^)]*)\\)))?(?:[ \\t]+\\[([^\\]]*)\\])?[ \\t]*$");
            var r_result = r.Match(s);
            if (!r_result.Success)
            {
                res = null;
                return false;
            }

            res = new MemberDefiniton_ParseResults();
            res.Modifiers.Clear();
            res.Parameters.Clear();

            var member_type_result = r_result.Groups[1];
            var data_type_result = r_result.Groups[2];
            var name_result = r_result.Groups[3];
            var params_present_result = r_result.Groups[4];
            var params_result = r_result.Groups[5];
            var modifiers_result = r_result.Groups[6];

            res.MemberType = member_type_result.Value;
            if (data_type_result.Success)
            {
                res.DataType = data_type_result.Value;
            }
            res.Name = name_result.Value;

            if (params_present_result.Success)
            {
                res.Parameters = new List<string>();
                if (!MemberDefinition_ParseParameters(params_result.Value, ref res.Parameters))
                {
                    return false;
                }
            }

            if (modifiers_result.Success)
            {
                res.Modifiers = new List<string>();
                if (!MemberDefinition_ParseModifiers(modifiers_result.Value, ref res.Modifiers))
                {
                    return false;
                }

                if (res.Modifiers.Count == 0)
                {
                    return false;
                }
            }
            return true;
        }

        internal static void MemberDefinition_FromStringFormat_common(ref MemberDefiniton_ParseResults parse_res, string s1, List<string> member_types, MemberDefinition def)
        {

            if (!MemberDefinition_ParseFormat_common(s1, out parse_res))
            {
                throw new RobotRaconteurParseException("Could not parse " + member_types[0] + " definition \"" + s1 + "\"");
            }

            string m = parse_res.MemberType;
            if (member_types.Find(x => x == m) == null)
            {
                throw new RobotRaconteurParseException("Format Error");
            }

            def.Reset();

            def.Name = parse_res.Name;
        }

        internal static void MemberDefinition_FromStringFormat1(string s1, List<string> member_types, MemberDefinition def, ref TypeDefinition type)
        {
            MemberDefiniton_ParseResults parse_res = new MemberDefiniton_ParseResults();
            MemberDefinition_FromStringFormat_common(ref parse_res, s1, member_types, def);

            if (parse_res.DataType == null || parse_res.Parameters.Count > 0) throw new RobotRaconteurParseException("Format error for " + member_types[0] + " definition \"" + s1 + "\"");
            type = new TypeDefinition(def);
            type.FromString(parse_res.DataType);
            type.Rename("value");
            type.QualifyTypeStringWithUsing();

            if (parse_res.Modifiers != null)
            {
                def.Modifiers = parse_res.Modifiers;
            }
        }

        internal static void MemberDefinition_FromStringFormat1(string s1, string member_type, MemberDefinition def, ref TypeDefinition type)
        {
            var member_types = new List<string>();
            member_types.Add(member_type);
            MemberDefinition_FromStringFormat1(s1, member_types, def, ref type);
        }

        internal static string MemberDefinition_ToStringFormat1(string member_type, MemberDefinition def, TypeDefinition data_type)
        {
            var t = new TypeDefinition();
            data_type.CopyTo(ref t);
            t.Rename(def.Name);
            t.UnqualifyTypeStringWithUsing();

            return member_type + " " + t.ToString() + MemberDefinition_ModifiersToString(def.Modifiers);
        }

        internal static void MemberDefinition_ParamatersFromStrings(List<string> s, ref List<TypeDefinition> params_, MemberDefinition def)
        {

            foreach (var s1 in s)
            {
                var tdef = new TypeDefinition(def);
                tdef.FromString(s1);
                tdef.QualifyTypeStringWithUsing();
                params_.Add(tdef);
            }
        }

        internal static string MemberDefinition_ParametersToString(List<TypeDefinition> params_)
        {
            var params2 = new List<string>();
            foreach (var p in params_)
            {
                TypeDefinition p2=new TypeDefinition();
                p.CopyTo(ref p2);
                p2.UnqualifyTypeStringWithUsing();
                params2.Add(p2.ToString());
            }

            return String.Join(", ", params2);
        }

        internal static void MemberDefinition_FromStringFormat2(string s1, string member_type, MemberDefinition def, ref TypeDefinition return_type, ref List<TypeDefinition> params_)
        {
            var member_types = new List<string>();
            member_types.Add(member_type);

            var parse_res = new MemberDefiniton_ParseResults();
            MemberDefinition_FromStringFormat_common(ref parse_res, s1, member_types, def);

            if (parse_res.DataType == null || parse_res.Parameters == null) throw new RobotRaconteurParseException("Format error for " + member_types[0] + " definition \"" + s1 + "\"");
            return_type = new TypeDefinition(def);
            return_type.FromString(parse_res.DataType);
            return_type.Rename("");
            return_type.QualifyTypeStringWithUsing();

            MemberDefinition_ParamatersFromStrings(parse_res.Parameters, ref params_, def);

            if (parse_res.Modifiers != null)
            {
                def.Modifiers = parse_res.Modifiers;
            }
        }

        internal static string MemberDefinition_ToStringFormat2(string member_type, MemberDefinition def, TypeDefinition return_type, List<TypeDefinition> params_)
        {
            TypeDefinition t=new TypeDefinition(def);
            return_type.CopyTo(ref t);
            t.Rename(def.Name);
            t.UnqualifyTypeStringWithUsing();

            return member_type + " " + t.ToString() + "(" + MemberDefinition_ParametersToString(params_) + ")" + MemberDefinition_ModifiersToString(def.Modifiers);
        }

        internal static void MemberDefinition_FromStringFormat3(string s1, string member_type, MemberDefinition def, ref List<TypeDefinition> params_)
        {
            var member_types = new List<string>();
            member_types.Add(member_type);

            var parse_res = new MemberDefiniton_ParseResults();
            MemberDefinition_FromStringFormat_common(ref parse_res, s1, member_types, def);

            if (parse_res.DataType != null || parse_res.Parameters == null) throw new RobotRaconteurParseException("Format error for " + member_types[0] + " definition \"" + s1 + "\"");

            MemberDefinition_ParamatersFromStrings(parse_res.Parameters, ref params_, def);

            if (parse_res.Modifiers != null)
            {
                def.Modifiers = parse_res.Modifiers;
            }
        }

        internal static string MemberDefinition_ToStringFormat3(string member_type, MemberDefinition def, List<TypeDefinition> params_)
        {
            return member_type + " " + def.Name + "(" + MemberDefinition_ParametersToString(params_) + ")" + MemberDefinition_ModifiersToString(def.Modifiers);
        }

        internal static MemberDefinition_Direction MemberDefinition_GetDirection(List<string> modifiers)
        {
            if (modifiers == null) return MemberDefinition_Direction.both;
            if (modifiers.Contains("readonly"))
            {
                return MemberDefinition_Direction.readonly_;
            }

            if (modifiers.Contains("writeonly"))
            {
                return MemberDefinition_Direction.writeonly;
            }

            return MemberDefinition_Direction.both;
        }

        public virtual void Reset()
        {
            Name = "";
            Modifiers = null;
        }
    }

    public class PropertyDefinition : MemberDefinition
    {
        public TypeDefinition Type;

        public PropertyDefinition(ServiceEntryDefinition ServiceEntry)
            : base(ServiceEntry)
        { }
        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool isstruct)
        {
            string member_type = isstruct ? "field" : "property";
            return MemberDefinition_ToStringFormat1(member_type, this, Type);
        }

        public void FromString(string s)
        {
            var member_types = new List<string>();
            member_types.Add("property");
            member_types.Add("field");
            MemberDefinition_FromStringFormat1(s, member_types, this, ref Type);
        }

        public override void Reset()
        {
            base.Reset();
            Type?.Reset();
        }

        public MemberDefinition_Direction Direction
        {
            get
            {
                return MemberDefinition_GetDirection(Modifiers);
            }
        }
    }

    public class FunctionDefinition : MemberDefinition
    {
        public TypeDefinition ReturnType;
        public List<TypeDefinition> Parameters = new List<TypeDefinition>();

        public FunctionDefinition(ServiceEntryDefinition ServiceEntry)
            : base(ServiceEntry)
        { }

        public override string ToString()
        {
            return MemberDefinition_ToStringFormat2("function", this, ReturnType, Parameters);
        }

        public void FromString(string s)
        {
            MemberDefinition_FromStringFormat2(s, "function", this, ref ReturnType, ref Parameters);
        }

        public override void Reset()
        {
            base.Reset();
            Parameters.Clear();
            ReturnType?.Reset();
        }

        public bool IsGenerator
        {
            get
            {
                if (ReturnType.ContainerType == DataTypes_ContainerTypes.generator)
                {
                    return true;
                }

                if (Parameters.Count != 0 && Parameters.Last().ContainerType == DataTypes_ContainerTypes.generator)
                {
                    return true;
                }

                return false;
            }
        }
    }

    public class EventDefinition : MemberDefinition
    {
        public List<TypeDefinition> Parameters = new List<TypeDefinition>();

        public EventDefinition(ServiceEntryDefinition ServiceEntry)
            : base(ServiceEntry)
        { }

        public override string ToString()
        {
            return MemberDefinition_ToStringFormat3("event", this, Parameters);
        }

        public void FromString(string s)
        {
            MemberDefinition_FromStringFormat3(s, "event", this, ref Parameters);
        }

        public override void Reset()
        {
            base.Reset();
            Parameters.Clear();
        }
    }

    public class ObjRefDefinition : MemberDefinition
    {
        public String ObjectType;
        public DataTypes_ArrayTypes ArrayType;
        public DataTypes_ContainerTypes ContainerType;

        public ObjRefDefinition(ServiceEntryDefinition ServiceEntry)
            : base(ServiceEntry)
        { }

        public override string ToString()
        {
            var t = new TypeDefinition(this);
            t.Name = Name;
            t.TypeString = ObjectType;
            t.Type = DataTypes.namedtype_t;

            switch (ArrayType)
            {
                case DataTypes_ArrayTypes.none:
                    {
                        switch (ContainerType)
                        {
                            case DataTypes_ContainerTypes.none:
                                break;
                            case DataTypes_ContainerTypes.map_int32:
                            case DataTypes_ContainerTypes.map_string:
                                t.ContainerType = ContainerType;
                                break;
                            default:
                                throw new ServiceDefinitionException("Invalid ObjRefDefinition for objref \"" + Name + "\"");
                        }
                        break;
                    }
                case DataTypes_ArrayTypes.array:
                    {
                        if (ContainerType != DataTypes_ContainerTypes.none)
                        {
                            throw new ServiceDefinitionException("Invalid ObjRefDefinition for objref \"" + Name + "\"");
                        }
                        t.ArrayType = ArrayType;
                        t.ArrayVarLength = true;
                        t.ArrayLength.Add(0);
                        break;
                    }
                default:
                    throw new ServiceDefinitionException("Invalid ObjRefDefinition for objref \"" + Name + "\"");
            }

            return MemberDefinition_ToStringFormat1("objref", this, t);
        }


        public void FromString(string s)
        {
            var t = new TypeDefinition();
            MemberDefinition_FromStringFormat1(s, "objref", this, ref t);

            switch (t.ArrayType)
            {
                case DataTypes_ArrayTypes.none:
                    {
                        switch (t.ContainerType)
                        {
                            case DataTypes_ContainerTypes.none:
                            case DataTypes_ContainerTypes.map_int32:
                            case DataTypes_ContainerTypes.map_string:
                                break;
                            default:
                                throw new RobotRaconteurParseException("Invalid objref definition \"" + s + "\"");
                        }
                        break;
                    }
                case DataTypes_ArrayTypes.array:
                    {
                        if (ContainerType != DataTypes_ContainerTypes.none)
                        {
                            throw new RobotRaconteurParseException("Invalid objref definition \"" + s + "\"");
                        }
                        if (!t.ArrayVarLength)
                        {
                            throw new RobotRaconteurParseException("Invalid objref definition \"" + s + "\"");
                        }
                        if (t.ArrayLength[0] != 0)
                        {
                            throw new RobotRaconteurParseException("Invalid objref definition \"" + s + "\"");
                        }
                        break;
                    }
                default:
                    throw new RobotRaconteurParseException("Invalid objref definition \"" + s + "\"");
            }

            if (!((!String.IsNullOrEmpty(t.TypeString) && t.Type == DataTypes.namedtype_t)
                || (t.Type == DataTypes.varobject_t)))
            {
                throw new RobotRaconteurParseException("Invalid objref definition \"" + s + "\"");
            }


            if (t.Type == DataTypes.namedtype_t)
            {
                ObjectType = t.TypeString;
            }
            else
            {
                ObjectType = "varobject";
            }

            ArrayType = t.ArrayType;
            ContainerType = t.ContainerType;
        }

        public override void Reset()
        {
            base.Reset();
            ObjectType = "";
            ArrayType = DataTypes_ArrayTypes.none;
            ContainerType = DataTypes_ContainerTypes.none;
        }
    }

    public class PipeDefinition : MemberDefinition
    {
        public TypeDefinition Type;

        public PipeDefinition(ServiceEntryDefinition ServiceEntry)
            : base(ServiceEntry)
        { }

        public override string ToString()
        {
            return MemberDefinition_ToStringFormat1("pipe", this, Type);
        }

        public void FromString(string s)
        {
            MemberDefinition_FromStringFormat1(s, "pipe", this, ref Type);
        }

        public override void Reset()
        {
            base.Reset();
            Type?.Reset();
        }

        public MemberDefinition_Direction Direction
        {
            get
            {
                return MemberDefinition_GetDirection(Modifiers);
            }
        }

        public bool IsUnreliable
        {
            get
            {
                if (Modifiers.Count(x => x=="unreliable") != 0)
                {
                    return true;
                }

                foreach (var o in ServiceEntry.Options)
                {
                    var r = new Regex("^[ \\t]*pipe[ \\t]+" + Name + "[ \\t]+unreliable[ \\t]*$");
                    if (r.IsMatch(o))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }

    public class CallbackDefinition : MemberDefinition
    {
        public TypeDefinition ReturnType;
        public List<TypeDefinition> Parameters = new List<TypeDefinition>();

        public CallbackDefinition(ServiceEntryDefinition ServiceEntry)
            : base(ServiceEntry)
        { }

        public override string ToString()
        {
            return MemberDefinition_ToStringFormat2("callback", this, ReturnType, Parameters);
        }

        public void FromString(string s)
        {
            MemberDefinition_FromStringFormat2(s, "callback", this, ref ReturnType, ref Parameters);
        }

        public override void Reset()
        {
            base.Reset();
            Parameters.Clear();
            ReturnType?.Reset();
        }


    }


    public class WireDefinition : MemberDefinition
    {
        public TypeDefinition Type;

        public WireDefinition(ServiceEntryDefinition ServiceEntry)
            : base(ServiceEntry)
        { }

        public override string ToString()
        {
            return MemberDefinition_ToStringFormat1("wire", this, Type);
        }

        public void FromString(string s)
        {
            MemberDefinition_FromStringFormat1(s, "wire", this, ref Type);
        }

        public override void Reset()
        {
            base.Reset();
            Type?.Reset();
        }

        public MemberDefinition_Direction Direction
        {
            get
            {
                return MemberDefinition_GetDirection(Modifiers);
            }
        }
    }

    public class MemoryDefinition : MemberDefinition
    {
        public TypeDefinition Type;

        public MemoryDefinition(ServiceEntryDefinition ServiceEntry)
            : base(ServiceEntry)
        { }

        public override string ToString()
        {
            return MemberDefinition_ToStringFormat1("memory", this, Type);
        }

        public void FromString(string s)
        {
            MemberDefinition_FromStringFormat1(s, "memory", this, ref Type);
        }

        public override void Reset()
        {
            base.Reset();
            Type?.Reset();
        }

        public MemberDefinition_Direction Direction
        {
            get
            {
                return MemberDefinition_GetDirection(Modifiers);
            }
        }
    }

    public class TypeDefinition
    {
        public string Name;

        public DataTypes Type;
        public string TypeString;

        public DataTypes_ArrayTypes ArrayType;
        public bool ArrayVarLength;
        public List<int> ArrayLength = new List<int>();

        public DataTypes_ContainerTypes ContainerType;

        public MemberDefinition member;

        internal NamedTypeDefinition ResolveNamedType_cache;

        public TypeDefinition()
        {

        }

        public TypeDefinition(MemberDefinition member)
        {
            this.member = member;
        }

        public override string ToString()
        {

            var o = new StringWriter();
            o.Write((Type >= DataTypes.namedtype_t && (!(Type == DataTypes.varvalue_t) && !(Type == DataTypes.varobject_t))) ? TypeString : StringFromDataType(Type));

            switch (ArrayType)
            {
                case DataTypes_ArrayTypes.none:
                    break;
                case DataTypes_ArrayTypes.array:
                    {
                        o.Write("[" + (ArrayLength[0] != 0 ? ((ArrayLength[0]) + (ArrayVarLength ? "-" : "")).ToString() : "") + "]");
                        break;
                    }
                case DataTypes_ArrayTypes.multidimarray:
                    {
                        if (ArrayVarLength)
                        {
                            o.Write("[*]");
                        }
                        else
                        {
                            if (ArrayLength.Count == 1)
                            {
                                o.Write("[{0}]", ArrayLength[0]);
                            }
                            else
                            {
                                var s3 = new List<string>();
                                foreach (var e in ArrayLength)

                                {
                                    s3.Add(e.ToString());
                                }
                                o.Write("[" + String.Join(",", s3) + "]");
                            }

                        }
                        break;
                    }
                default:
                    throw new ServiceDefinitionException("Invalid type definition \"" + Name + "\"");
            }

            switch (ContainerType)
            {
                case DataTypes_ContainerTypes.none:
                    break;
                case DataTypes_ContainerTypes.list:
                    o.Write("{list}");
                    break;
                case DataTypes_ContainerTypes.map_int32:
                    o.Write("{int32}");
                    break;
                case DataTypes_ContainerTypes.map_string:
                    o.Write("{string}");
                    break;
                case DataTypes_ContainerTypes.generator:
                    o.Write("{generator}");
                    break;
                default:
                    throw new ServiceDefinitionException("Invalid type definition \"" + Name + "\"");
            }

            o.Write(" " + Name);
            return o.ToString().Trim();
        }

        public void FromString(string s)
        {
            Reset();

            var r = new Regex("^[ \\t]*([a-zA-Z][\\w\\.]*)(?:(\\[\\])|\\[(([0-9]+)|([0-9]+)\\-|(\\*)|([0-9]+)\\,|([0-9\\,]+))\\])?(?:\\{(\\w{1,16})\\})?(?:[ \\t]+(\\w+))?[ \\t]*$");
            var r_result = r.Match(s);
            if (!r_result.Success)
            {
                throw new RobotRaconteurParseException("Could not parse type \"" + s.Trim() + "\"");
            }

            var type_result = r_result.Groups[1];
            var array_result = r_result.Groups[2];
            var array_result2 = r_result.Groups[3];
            var array_var_result = r_result.Groups[4];
            var array_max_var_result = r_result.Groups[5];
            var array_multi_result = r_result.Groups[6];
            var array_multi_single_fixed_result = r_result.Groups[7];
            var array_multi_fixed_result = r_result.Groups[8];
            var container_result = r_result.Groups[9];
            var name_result = r_result.Groups[10];

            Name = name_result.Success ? name_result.Value : "";

            if (container_result.Success)
            {
                if (container_result.Value == "list")
                {
                    ContainerType = DataTypes_ContainerTypes.list;
                }
                else if (container_result.Value == "int32")
                {
                    ContainerType = DataTypes_ContainerTypes.map_int32;
                }
                else if (container_result.Value == "string")
                {
                    ContainerType = DataTypes_ContainerTypes.map_string;
                }
                else if (container_result.Value == "generator")
                {
                    ContainerType = DataTypes_ContainerTypes.generator;
                }
                else
                {
                    throw new RobotRaconteurParseException("Could not parse type \"" + s.Trim() + "\": invalid container type");
                }
            }

            if (array_result.Success)
            {
                //variable array
                ArrayType = DataTypes_ArrayTypes.array;
                ArrayVarLength = true;
                ArrayLength = new List<int> { 0 };
            }
            if (array_result2.Success)
            {
                if (array_var_result.Success)
                {
                    //Fixed array
                    ArrayType = DataTypes_ArrayTypes.array;
                    ArrayLength.Clear();
                    ArrayLength.Add((int)UInt32.Parse(array_var_result.Value));
                    ArrayVarLength = false;
                }
                else if (array_max_var_result.Success)
                {
                    //variable array max sized
                    ArrayType = DataTypes_ArrayTypes.array;
                    ArrayLength.Clear();
                    ArrayLength.Add((int)UInt32.Parse(array_max_var_result.Value));
                    ArrayVarLength = true;
                }
                else if (array_multi_result.Success)
                {
                    //multidim array
                    ArrayType = DataTypes_ArrayTypes.multidimarray;
                    ArrayVarLength = true;
                }
                else if (array_multi_single_fixed_result.Success)
                {
                    //multidim single fixed array
                    ArrayType = DataTypes_ArrayTypes.multidimarray;
                    ArrayVarLength = false;
                    ArrayLength.Clear();
                    ArrayLength.Add((int)UInt32.Parse(array_multi_single_fixed_result.Value));
                }
                else if (array_multi_fixed_result.Success)
                {
                    //multidim fixed array
                    ArrayType = DataTypes_ArrayTypes.multidimarray;
                    ArrayVarLength = false;
                    var dims = array_multi_fixed_result.Value.Split(new char[] { ',' });
                    ArrayLength.Clear();
                    foreach (var d in dims)

                    {
                        ArrayLength.Add((int)UInt32.Parse(d));
                    }
                }
                else
                {
                    throw new RobotRaconteurParseException("Could not parse type \"" + s.Trim() + "\": array error");
                }
            }

            DataTypes t = DataTypeFromString(type_result.Value);
            if (t == DataTypes.namedtype_t)
            {
                Type = DataTypes.namedtype_t;
                TypeString = type_result.Value;
            }
            else
            {
                Type = t;
                TypeString = "";
            }
        }

        public static DataTypes DataTypeFromString(String d)
        {
            if (d == "void")
            {
                return DataTypes.void_t;
            }
            else if (d == "double")
            {
                return DataTypes.double_t;
            }
            else if (d == "single")
            {
                return DataTypes.single_t;
            }
            else if (d == "int8")
            {
                return DataTypes.int8_t;
            }
            else if (d == "uint8")
            {
                return DataTypes.uint8_t;
            }
            else if (d == "int16")
            {
                return DataTypes.int16_t;
            }
            else if (d == "uint16")
            {
                return DataTypes.uint16_t;
            }
            else if (d == "int32")
            {
                return DataTypes.int32_t;
            }
            else if (d == "uint32")
            {
                return DataTypes.uint32_t;
            }
            else if (d == "int64")
            {
                return DataTypes.int64_t;
            }
            else if (d == "uint64")
            {
                return DataTypes.uint64_t;
            }
            else if (d == "string")
            {
                return DataTypes.string_t;
            }
            else if (d == "cdouble")
            {
                return DataTypes.cdouble_t;
            }
            else if (d == "csingle")
            {
                return DataTypes.csingle_t;
            }
            else if (d == "bool")
            {
                return DataTypes.bool_t;
            }
            else if (d == "structure")
            {
                return DataTypes.structure_t;
            }
            else if (d == "object")
            {
                return DataTypes.object_t;
            }
            else if (d == "varvalue")
            {
                return DataTypes.varvalue_t;
            }
            else if (d == "varobject")
            {
                return DataTypes.varobject_t;

            }

            return DataTypes.namedtype_t;
        }

        public static string StringFromDataType(DataTypes d)
        {
            switch (d)
            {
                case DataTypes.void_t:
                    return "void";
                case DataTypes.double_t:
                    return "double";
                case DataTypes.single_t:
                    return "single";
                case DataTypes.int8_t:
                    return "int8";
                case DataTypes.uint8_t:
                    return "uint8";
                case DataTypes.int16_t:
                    return "int16";
                case DataTypes.uint16_t:
                    return "uint16";
                case DataTypes.int32_t:
                    return "int32";
                case DataTypes.uint32_t:
                    return "uint32";
                case DataTypes.int64_t:
                    return "int64";
                case DataTypes.uint64_t:
                    return "uint64";
                case DataTypes.string_t:
                    return "string";
                case DataTypes.cdouble_t:
                    return "cdouble";
                case DataTypes.csingle_t:
                    return "csingle";
                case DataTypes.bool_t:
                    return "bool";
                case DataTypes.structure_t:
                    return "structure";
                case DataTypes.object_t:
                    return "object";
                case DataTypes.varvalue_t:
                    return "varvalue";
                case DataTypes.varobject_t:
                    return "varobject";
                default:
                    throw new DataTypeException("Invalid data type");
            }

        }

        public static bool operator ==(TypeDefinition a, TypeDefinition b)
        {
            if (System.Object.ReferenceEquals(a, b)) return true;

            if (((object)a == null) || ((object)b == null)) return false;

            if (a.Name != b.Name) return false;
            if (a.TypeString != b.TypeString) return false;
            if (a.ArrayType != b.ArrayType) return false;
            if (a.ArrayVarLength != b.ArrayVarLength) return false;
            if (a.ArrayLength != b.ArrayLength) return false;
            if (a.ContainerType != b.ContainerType) return false;
            return true;
        }

        public static bool operator !=(TypeDefinition a, TypeDefinition b)
        {
            return !(a == b);
        }

        public override bool Equals(object o)
        {
            if (o == null) return false;
            return this == (TypeDefinition)o;
        }

        public void Reset()
        {
            ArrayType = DataTypes_ArrayTypes.none;
            ContainerType = DataTypes_ContainerTypes.none;

            ArrayVarLength = false;
            Type = DataTypes.void_t;
            ArrayLength.Clear();
            TypeString = null;
        }

        public void CopyTo(ref TypeDefinition def)
        {
            def.Name = Name;
            def.Type = Type;
            def.TypeString = TypeString;
            def.ArrayType = ArrayType;
            def.ContainerType = ContainerType;
            def.ArrayLength = ArrayLength.ToList();
            def.ArrayVarLength = ArrayVarLength;
            def.member = member;
            def.ResolveNamedType_cache = ResolveNamedType_cache;
        }

        public TypeDefinition Clone()
        {
            var def2 = new TypeDefinition();
            CopyTo(ref def2);
            return def2;
        }

        public void Rename(string name)
        {
            Name = name;
        }

        public void RemoveContainers()
        {
            ContainerType = DataTypes_ContainerTypes.none;
        }

        public void RemoveArray()
        {
            if (ContainerType != DataTypes_ContainerTypes.none) throw new InvalidOperationException("Remove containers first");

            ArrayType = DataTypes_ArrayTypes.none;
            ArrayLength.Clear();
            ArrayVarLength = false;
        }

        internal static List<UsingDefinition> TypeDefinition_GetServiceUsingDefinition(TypeDefinition def)
        {
            var member1 = def.member;
            if (member1 == null) throw new InvalidOperationException("Member not set for TypeDefinition");
            var entry1 = member1.ServiceEntry;
            if (entry1 == null) throw new InvalidOperationException("Object or struct not set for MemberDefinition " + member1.Name);
            var service1 = entry1.ServiceDefinition;
            if (service1 == null) throw new InvalidOperationException("ServiceDefinition or struct not set for Object or Structure " + entry1.Name);
            return service1.Using;
        }

        public void QualifyTypeStringWithUsing()
        {
            if (Type != DataTypes.namedtype_t)
                return;

            if (TypeString.Contains("."))
                return;

            List<UsingDefinition> using_ = TypeDefinition_GetServiceUsingDefinition(this);
            foreach (var u in using_)
            {
                if (u.UnqualifiedName == TypeString)
                {
                    TypeString = u.QualifiedName;
                    return;
                }
            }
        }

        public void UnqualifyTypeStringWithUsing()
        {
            if (Type != DataTypes.namedtype_t)
                return;

            if (!TypeString.Contains("."))
                return;

            var using_ = TypeDefinition_GetServiceUsingDefinition(this);
            foreach (var u in using_)
            {
                if (u.QualifiedName == TypeString)
                {
                    TypeString = u.UnqualifiedName;
                    return;
                }
            }
        }

        public NamedTypeDefinition ResolveNamedType(Dictionary<string, ServiceDefinition> imported_defs=null, RobotRaconteurNode node = null, object client = null)
        {
            NamedTypeDefinition o = ResolveNamedType_cache;
            if (o != null)
            {
                return o;
            }

            ServiceDefinition def = null;

            string entry_name;

            if (!TypeString.Contains("."))
            {
                entry_name = TypeString;
                //Assume not imported
                MemberDefinition m = member;
                if (m != null)
                {
                    ServiceEntryDefinition entry = m.ServiceEntry;
                    if (entry != null)
                    {
                        def = entry.ServiceDefinition;
                    }
                }
            }
            else
            {
                var s = ServiceDefinitionUtil.SplitQualifiedName(TypeString);
                string def_name = s.Item1;
                entry_name = s.Item2;

                if (imported_defs != null)
                {
                    imported_defs.TryGetValue(def_name, out def);
                }
                if (def == null)
                {
                    if (node == null) throw new ArgumentException("Node not specified for ResolveType");
                    try
                    {
                        if (client != null)
                        {
                            var client2 = ((ServiceStub)client).RRContext;
                            def = client2.GetPulledServiceType(def_name).ServiceDef();
                        }

                    }
                    catch (Exception) { }
                }
            }

            if (def == null)
            {
                throw new ServiceDefinitionException("Could not resolve named type " + TypeString);
            }

            ServiceEntryDefinition found_struct;
            if (def.Structures.TryGetValue(entry_name, out found_struct))
            {
                ResolveNamedType_cache = found_struct;
                return found_struct;
            }
            ServiceEntryDefinition found_pod;
            if (def.Pods.TryGetValue(entry_name, out found_pod))
            {
                ResolveNamedType_cache = found_pod;
                return found_pod;
            }
            ServiceEntryDefinition found_namedarray;
            if (def.NamedArrays.TryGetValue(entry_name, out found_namedarray))
            {
                ResolveNamedType_cache = found_namedarray;
                return found_namedarray;
            }
            ServiceEntryDefinition found_object;
            if (def.Objects.TryGetValue(entry_name, out found_object))
            {
                ResolveNamedType_cache = found_object;
                return found_object;
            }
            EnumDefinition found_enum;
            if (def.Enums.TryGetValue(entry_name, out found_enum))
            {
                ResolveNamedType_cache = found_enum;
                return found_enum;
            }

            throw new ServiceDefinitionException("Could not resolve named type " + def.Name + "." + entry_name);
        }

    }
    public class UsingDefinition
    {
        public string QualifiedName;
        public string UnqualifiedName;
        public ServiceDefinition service;

        public UsingDefinition(ServiceDefinition service)
        {
            this.service = service;
        }

        public override string ToString()
        {
            var s = ServiceDefinitionUtil.SplitQualifiedName(QualifiedName);
            string qualified_name_type = s.Item2;
            if (qualified_name_type == UnqualifiedName)
            {
                return "using " + QualifiedName + "\n";
            }
            else
            {
                return "using " + QualifiedName + " as " + UnqualifiedName + "\n";
            }
        }

        public void FromString(string s)
        {
            var r = new Regex("^[ \\t]*using[ \\t]+(" + detail.ServiceDefinitionUtil.RR_QUALIFIED_TYPE_REGEX + ")(?:[ \\t]+as[ \\t](" + detail.ServiceDefinitionUtil.RR_NAME_REGEX +  "))?[ \\t]*$");

            var r_match = r.Match(s);
            if (!r_match.Success)
            {
                throw new RobotRaconteurParseException("Format error for using  definition \"" + s + "\"");
            }

            if (!r_match.Groups[2].Success)
            {
                this.QualifiedName = r_match.Groups[1].Value;
                var s2 = ServiceDefinitionUtil.SplitQualifiedName(r_match.Groups[1].Value);
                this.UnqualifiedName = s2.Item2;                
            }
            else
            {
                this.QualifiedName = r_match.Groups[1].Value;
                this.UnqualifiedName = r_match.Groups[2].Value;
            }
        }
    }

    public class ConstantDefinition_StructField
    {
        public string Name;
        public string ConstantRefName;

    }

    public class ConstantDefinition
    {
        public string Name;
        public TypeDefinition Type;
        public string Value;

        public ServiceDefinition service;
        public ServiceEntryDefinition service_entry;

        public ConstantDefinition(ServiceDefinition service)
        {
            this.service = service;
        }

        public ConstantDefinition(ServiceEntryDefinition service_entry)
        {
            this.service_entry = service_entry;
        }

        public override string ToString()
        {
            return "constant " + Type.ToString() + " " + Name + " " + Value;
        }

        public void FromString(string s)
        {
            Reset();

            var r = new Regex("^[ \\t]*constant[ \\t]+(" + detail.ServiceDefinitionUtil.RR_TYPE2_REGEX + ")[ \\t]+(" + detail.ServiceDefinitionUtil.RR_NAME_REGEX + ")[ \\t]+([^\\s](?:[ -~\\t]*[^\\s])?)[ \\t]*$");
            var r_match = r.Match(s);
            if (!r_match.Success)
            {
                throw new RobotRaconteurParseException("Invalid constant definition: " + s.Trim());
            }

            var type_str = r_match.Groups[1].Value;
            var def = new TypeDefinition();
            def.FromString(type_str);
            if (!VerifyTypeAndValue(def, r_match.Groups[3].Value))
            {
                throw new RobotRaconteurParseException("Invalid constant definition: " + s.Trim());
            }

            Type = def;
            Name = r_match.Groups[2].Value;
            Value = r_match.Groups[3].Value;
        }

        static bool ConstantDefinition_CheckScalar(DataTypes t, string val)
        {

            switch (t)
            {
                case DataTypes.double_t:
                    double res1;
                    return Double.TryParse(val, out res1);
                case DataTypes.single_t:
                    float res2;
                    return Single.TryParse(val, out res2);
                case DataTypes.int8_t:
                    sbyte res3;
                    return SByte.TryParse(val, out res3);
                case DataTypes.uint8_t:
                    byte res4;
                    return Byte.TryParse(val, out res4);
                case DataTypes.int16_t:
                    short res5;
                    return Int16.TryParse(val, out res5);
                case DataTypes.uint16_t:
                    ushort res6;
                    return UInt16.TryParse(val, out res6);
                case DataTypes.int32_t:
                    int res7;
                    return Int32.TryParse(val, out res7);
                case DataTypes.uint32_t:
                    uint res8;
                    return UInt32.TryParse(val, out res8);
                case DataTypes.int64_t:
                    long res9;
                    return Int64.TryParse(val, out res9);
                case DataTypes.uint64_t:
                    ulong res10;
                    return UInt64.TryParse(val, out res10);
                default:
                    return false;
            }
        }

        public bool VerifyValue()
        {
            return VerifyTypeAndValue(Type, Value);
        }

        public bool VerifyTypeAndValue(TypeDefinition t, string value)
        {
            if (t.ArrayType == DataTypes_ArrayTypes.multidimarray) return false;
            if (DataTypeUtil.IsNumber(t.Type))
            {
                if (t.Type == DataTypes.cdouble_t || t.Type == DataTypes.csingle_t || t.Type == DataTypes.bool_t)
                    return false;
                if (t.ContainerType != DataTypes_ContainerTypes.none) return false;
                if (t.ArrayType == DataTypes_ArrayTypes.none)
                {
                    if (t.Type == DataTypes.double_t || t.Type == DataTypes.single_t)
                    {
                        var r_scalar = new Regex("^[ \\t]*" + detail.ServiceDefinitionUtil.RR_FLOAT_REGEX + "[ \\t]*$");
                        if (!r_scalar.IsMatch(value))
                            return false;
                    }
                    else
                    {
                        var r_scalar = new Regex("^[ \\t]*" + detail.ServiceDefinitionUtil.RR_INT_REGEX + "[ \\t]*$");
                        if (!r_scalar.IsMatch(value))
                            return false;
                    }
                    return ConstantDefinition_CheckScalar(t.Type, value);
                }
                else
                {
                    Match r_array_match;
                    if (t.Type == DataTypes.double_t || t.Type == DataTypes.single_t)
                    {
                        var r_array = new Regex("^[ \\t]*\\{[ \\t]*((?:" + detail.ServiceDefinitionUtil.RR_FLOAT_REGEX + ")(?:[ \\t]*,[ \\t]*(?:" + detail.ServiceDefinitionUtil.RR_FLOAT_REGEX + "))*)?[ \\t]*}[ \\t]*$");
                        r_array_match = r_array.Match(value);
                        if (!r_array_match.Success)
                            return false;
                    }
                    else
                    {
                        var r_array = new Regex("^[ \\t]*\\{[ \\t]*((?:" + detail.ServiceDefinitionUtil.RR_INT_REGEX + ")(?:[ \\t]*,[ \\t]*(?:" + detail.ServiceDefinitionUtil.RR_INT_REGEX + "))*)?[ \\t]*}[ \\t]*$");
                        r_array_match = r_array.Match(value);
                        if (!r_array_match.Success)
                            return false;
                    }

                    if (!r_array_match.Groups[1].Success)
                        return true;



                    foreach (var e in r_array_match.Groups[1].Value.Split(new char[] { ',' }))
                    {
                        if (!ConstantDefinition_CheckScalar(t.Type, e.Trim()))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            else
            {
                if (t.ArrayType != DataTypes_ArrayTypes.none) return false;
                if (t.Type == DataTypes.string_t)
                {
                    var r_string = new Regex("^[ \\t]*\"(?:(?:\\\\\"|\\\\\\\\|\\\\/|\\\\b|\\\\f|\\\\n|\\\\r|\\\\t|\\\\u[\\da-fA-F]{4})|[^\"\\\\])*\"[ \\t]*$");
                    if (!r_string.IsMatch(value))
                        return false;
                    return true;
                }
                else if (t.Type == DataTypes.namedtype_t)
                {
                    var r_struct = new Regex("^[ \\t]*\\{[ \\t]*(?:" + detail.ServiceDefinitionUtil.RR_NAME_REGEX + "[ \\t]*\\:[ \\t]*" + detail.ServiceDefinitionUtil.RR_NAME_REGEX + "(?:[ \\t]*,[ \\t]*" + detail.ServiceDefinitionUtil.RR_NAME_REGEX + "[ \\t]*\\:[ \\t]*" + detail.ServiceDefinitionUtil.RR_NAME_REGEX + ")*[ \\t]*)?\\}[ \\t]*$");
                    if (!r_struct.IsMatch(value))
                        return false;
                    return true;
                }
                return false;
            }
        }

        public void Reset()
        {
            Name = "";
            Type = null;
            Value = null;
        }
               
        public T ValueToScalar<T>()
        {
            return (T)Convert.ChangeType(Value.Trim(), typeof(T));
        }

        
        public T[] ValueToArray<T>()
        {
            string value1 = Value.Trim(' ', '\t', '{', '}').Trim();
            if (value1.Length == 0)
                return new T[0];

            return value1.Split(',').Select(x=> (T)Convert.ChangeType(x.Trim(), typeof(T))).ToArray();
        }

        public string ValueToString()
        {
            if (Type == null) throw new InvalidOperationException("Invalid operation");
            if (Type.Type != DataTypes.string_t) throw new InvalidOperationException("Invalid operation");

            var r_string = new Regex("^[ \\t]*\"((?:(?:\\\\\"|\\\\\\\\|\\\\/|\\\\b|\\\\f|\\\\n|\\\\r|\\\\t|\\\\u[\\da-fA-F]{4})|(?:(?![\"\\\\])[ -~]))*)\"[ \\t]*$");
            var r_string_match = r_string.Match(Value);
            if (!r_string_match.Success)
                throw new RobotRaconteurParseException("Invalid string constant format");

            string value2 = r_string_match.Groups[1].Value;
            return UnescapeString(value2);
        }

        static string ConstantDefinition_UnescapeString_Formatter(Match match)
        {
            string i = match.Groups[0].Value;
            if (i == "\\\"") return "\"";
            if (i == "\\\\") return "\\";
            if (i == "\\/") return "/";
            if (i == "\\b") return "\b";
            if (i == "\\f") return "\f";
            if (i == "\\n") return "\n";
            if (i == "\\r") return "\r";
            if (i == "\\t") return "\t";

            if (i.StartsWith("\\u"))
            {

                var v3 = new byte[i.Length / 3];
                for (int j = 0; j < v3.Length / 2; j++)
                {
                    string v = i.Substring(j * 6 + 2, 2);
                    string v2 = i.Substring(j * 6 + 4, 2);

                    v3[j * 2] = Convert.ToByte(v, 16);
                    v3[j * 2 + 1] = Convert.ToByte(v2, 16);
                }
                return UnicodeEncoding.Unicode.GetString(v3);
            }

            throw new InternalErrorException("Internal error");
        }

        public static string UnescapeString(string in_)
        {
            var r_string_expression = new Regex("(\\\\\"|\\\\\\\\|\\\\/|\\\\b|\\\\f|\\\\n|\\\\r|\\\\t|(?:\\\\u[\\da-fA-F]{4})+)");
            return r_string_expression.Replace(in_, ConstantDefinition_UnescapeString_Formatter);
        }

        static string ConstantDefinition_EscapeString_Formatter(Match match)
        {
            string i = match.Groups[0].Value;

            if (i == "\"") return "\\\"";
            if (i == "\\") return "\\\\";
            if (i == "/") return "\\/";
            if (i == "\b") return "\\b";
            if (i == "\f") return "\\f";
            if (i == "\n") return "\\n";
            if (i == "\r") return "\\r";
            if (i == "\t") return "\\t";

            var v = UnicodeEncoding.Unicode.GetBytes(i);
            var v2 = new StringWriter();
            for (int j = 0; j < v.Length; j += 2)
            {
                v2.Write("\\u{0:X02}{1:X02}", v[j], v[j + 1]);
            }
            return v2.ToString();
        }

        public static string EscapeString(string in_)
        {
            var r_replace = new Regex("(\"|\\\\|\\/|[\\x00-\\x1F]|\\x7F|[\\x80-\\xFF]+)");
            return r_replace.Replace(in_, ConstantDefinition_EscapeString_Formatter);            
        }

        public List<ConstantDefinition_StructField> ValueToStructFields()
        {
            var o = new List<ConstantDefinition_StructField>();
            var value1 = Value.Trim(new char[] { ' ', '\t', '{', '}' }).Trim();
            if (value1.Length == 0) return o;

            var r = new Regex("[ \\t]*(" + detail.ServiceDefinitionUtil.RR_NAME_REGEX + ")[ \\t]*\\:[ \\t]*(" + detail.ServiceDefinitionUtil.RR_NAME_REGEX + ")[ \\t]*");
            var s = value1.Split(new char[] { ',' });
            foreach (var e in s)
            {
                var r_match = r.Match(e);
                if (!r_match.Success)
                {
                    throw new RobotRaconteurParseException("Invalid struct constant format");
                }
                var f = new ConstantDefinition_StructField();
                f.Name = r_match.Groups[1].Value;
                f.ConstantRefName = r_match.Groups[2].Value;
                o.Add(f);
            }
            return o;
        }

    }


    public class EnumDefinition : NamedTypeDefinition
    {
        public List<EnumDefinitionValue> Values = new List<EnumDefinitionValue>();

        public ServiceDefinition service;

        public override DataTypes RRDataType => DataTypes.enum_t;

        public EnumDefinition(ServiceDefinition service)
        {
            this.service = service;
        }

        public override string ToString()
        {
            if (!VerifyValues())
            {
                throw new DataTypeException("Invalid enum: " + Name);
            }

            string s = "enum " + Name + "\n";

            var values = new List<string>();

            foreach (var e in Values)

            {
                if (e.ImplicitValue)
                {
                    values.Add("    " + e.Name);
                }
                else
                {
                    if (!e.HexValue)
                    {
                        values.Add("    " + e.Name + " = " + e.Value.ToString());
                    }
                    else
                    {
                        values.Add("    " + e.Name + " = 0x" + e.Value.ToString("x"));
                    }
                }
            }
            s += String.Join(",\n", values);
            s += "\nend enum\n";
            return s;
        }

        public void FromString(string s, uint startline)
        {
            Reset();

            string s2 = s.Trim();
            var lines = s2.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
                throw new RobotRaconteurParseException("Invalid enum", (int)startline);

            var r_start = new Regex("^[ \\t]*enum[ \\t]+([a-zA-Z]\\w*)[ \\t]*$");
            var r_end = new Regex("^[ \\t]*end(?:[ \\t]+enum)?[ \\t]*$");


            var r_start_match = r_start.Match(lines.First());
            if (!r_start_match.Success)
            {
                throw new RobotRaconteurParseException("Parse error near: " + lines.First(), (int)startline);
            }
            Name = r_start_match.Groups[1].Value;

            if (!r_end.IsMatch(lines.Last()))
            {
                throw new RobotRaconteurParseException("Parse error near: " + lines.Last(), (int)(startline + lines.Length - 1));
            }

            var values1 = String.Join(" ", lines.Skip(1).Take(lines.Length - 2));
            var values2 = values1.Split(new char[] { ',' });

            var r_value = new Regex("^[ \\t]*([A-Za-z]\\w*)(?:[ \\t]*=[ \\t]*(?:(0x\\d+)|(-?\\d+)))?[ \\t]*$");
            var values3 = new List<EnumDefinitionValue>();
            foreach (string l in values2)
            {
                var r_value_match = r_value.Match(l);
                if (!r_value_match.Success)
                {
                    throw new RobotRaconteurParseException("Enum value parse error near: " + lines.First(), (int)(startline + lines.Length - 1));
                }

                var enum_i = new EnumDefinitionValue();
                enum_i.Name = r_value_match.Groups[1].Value;

                if (r_value_match.Groups[2].Success)
                {
                    enum_i.ImplicitValue = false;
                    enum_i.Value = Convert.ToInt32(Regex.Match(r_value_match.Groups[2].Value,"0x([0-9a-fA-F]+)").Groups[1].Value, 16);
                    enum_i.HexValue = true;
                }
                else if (r_value_match.Groups[3].Success)
                {
                    enum_i.ImplicitValue = false;
                    enum_i.Value = Int32.Parse(r_value_match.Groups[3].Value);
                    enum_i.HexValue = false;
                }
                else
                {
                    if (Values.Count == 1)
                    {
                        throw new RobotRaconteurParseException("Enum first value must be specified: " + lines.First(), (int)(startline + lines.Length - 1));
                    }

                    enum_i.ImplicitValue = true;
                    enum_i.Value = (values3.Last()).Value + 1;
                    enum_i.HexValue = (values3.Last()).HexValue;
                }

                values3.Add(enum_i);
            }

            Values = values3;

            if (!VerifyValues())
            {
                throw new RobotRaconteurParseException("Enum names or values not unique: " + lines.First(), (int)(startline));
            }
        }

        public bool VerifyValues()
        {
            if (Values.Count == 1)
            {
                return true;
            }


            for (int i=0; i<Values.Count-1; i++)
            {
                var e = Values[i];                
                for (int j = i+1; j<Values.Count; j++)
                {
                    var e2 = Values[j];
                    if (e.Value == e2.Value)
                        return false;
                    if (e.Name == e2.Name)
                        return false;
                }
            }
            return true;
        }

        public void Reset()
        {
            Values.Clear();
        }

        public override string ResolveQualifiedName()
        {                        
            return service.Name + "." + Name;
        }
    }

    public class EnumDefinitionValue
    {
        public string Name;
        public int Value;
        public bool ImplicitValue;
        public bool HexValue;
    }

    public class RobotRaconteurParseException : Exception
    {

        public int LineNumber;

        public RobotRaconteurParseException(string e)
            : base(e)
        {

        }
        public RobotRaconteurParseException(string e, int line)
            : base(e)
        {
            LineNumber = line;

        }

        public override string ToString()
        {
            return "RobotRaconteur Parse Error On Line " + LineNumber + ": " + Message;
        }

    }

    public static class ServiceDefinitionUtil
    {
        public static Tuple<string, string> SplitQualifiedName(string name)
        {
            int pos = name.LastIndexOf('.');
            if (pos < 0) return Tuple.Create((string)null, name);
            return Tuple.Create(name.Substring(0, pos), name.Substring(pos + 1, name.Length - pos - 1));
        }

        internal static string[] SplitParameterString(string str)
        {
            List<string> o = new List<string>();

            bool inbracket = false;
            int pos = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (!inbracket && str[i] == '[') inbracket = true;
                if (inbracket && str[i] == ']') inbracket = false;

                if (!inbracket)
                {
                    if (str[i] == ',')
                    {
                        o.Add(str.Substring(pos, i - pos));
                        pos = i + 1;
                        if (pos > str.Length) break;
                    }
                }
            }

            if (pos < str.Length) o.Add(str.Substring(pos));
            return o.Select(x => x.Trim()).ToArray();
        }

        internal static void VerifyVersionSupport(ServiceDefinition def, uint major, uint minor, string msg)
        {
            var def_version = def.StdVer;
            if (def_version == null)
                return;

            if (def_version < new RobotRaconteurVersion(major, minor))
            {
                if (msg != null)
                {
                    throw new ServiceDefinitionException(msg);
                }
                else
                {
                    throw new ServiceDefinitionException("Newer service definition standard required for feature");
                }
            }
        }

        internal static void VerifyName(string name, ServiceDefinition def, bool allowdot = false, bool ignorereserved = false)
        {
            if (String.IsNullOrEmpty(name)) throw new ServiceDefinitionException("Empty name in service definition \"" + def.Name + "\"");

            string name2 = name.ToLower();

            if (!ignorereserved)
            {
                if (name == "this" || name == "self" || name == "Me") throw new ServiceDefinitionException("The names \"this\", \"self\", and \"Me\" are reserved, error in service definition \"" + def.Name + "\"");

                var reserved = new String[] { "object", "end", "option", "service", "object", "struct", "import", "implements", "field", "property", "function", "event", "objref", "pipe", "callback", "wire", "memory", "void", "int8", "uint8", "int16", "uint16", "int32", "uint32", "int64", "uint64", "single", "double", "varvalue", "varobject", "exception", "using", "constant", "enum", "pod", "namedarray", "cdouble", "csingle", "bool" };

                if (reserved.Contains(name))
                {
                    throw new ServiceDefinitionException("Name \"" + name + "\" is reserved in service definition\"" + def.Name + "\"");
                }

                if (name2.StartsWith("get_") || name2.StartsWith("set_") || name2.StartsWith("rr") || name2.StartsWith("robotraconteur") || name2.StartsWith("async_"))
                {
                    throw new ServiceDefinitionException("Name \"" + name + "\" is invalid in service definition \"" + def.Name + "\"");
                }
            }

            if (allowdot)
            {
                if (!Regex.IsMatch(name, "^" + detail.ServiceDefinitionUtil.RR_TYPE_REGEX + "$"))
                {
                    throw new ServiceDefinitionException("Name \"" + name + "\" is invalid in service definition \"" + def.Name + "\"");
                }
            }
            else
            {
                if (!Regex.IsMatch(name, "^" + detail.ServiceDefinitionUtil.RR_NAME_REGEX + "$"))
                {
                    throw new ServiceDefinitionException("Name \"" + name + "\" is invalid in service definition \"" + def.Name + "\"");
                }
            }
        }

        internal static string VerifyConstant(string constant, ServiceDefinition def)
        {
            var c = new ConstantDefinition(def);
            try
            {
                c.FromString(constant);
            }
            catch (Exception)
            {
                throw new ServiceDefinitionException("Error in constant in service definition \"" + def.Name + "\"");
            }

            if (!c.VerifyValue()) throw new ServiceDefinitionException("Error in constant " + c.Name + " in service definition \"" + def.Name + "\"");

            if (c.Type.Type == DataTypes.namedtype_t) throw new ServiceDefinitionException("Error in constant " + c.Name + " in service definition \"" + def.Name + "\"");

            VerifyName(c.Name, def);

            return c.Name;
        }

        internal static void VerifyConstantStruct(ConstantDefinition c, ServiceDefinition def, Dictionary<string, ConstantDefinition> constants, List<string> parent_types)
        {


            var fields = c.ValueToStructFields();
            parent_types.Add(c.Name);
            foreach (var e in fields)
            {
                VerifyName(e.Name, def);
                foreach (var name in parent_types)
                {
                    if (e.ConstantRefName == name)
                        throw new ServiceDefinitionException("Error in constant " + c.Name + " in service definition \"" + def.Name + "\": recursive struct not allowed");
                }
                bool found = false;
                foreach (var f in constants.Values)

                {
                    if (f.Name == e.ConstantRefName)
                    {
                        found = true;
                        if (f.Type.Type == DataTypes.namedtype_t)
                        {
                            VerifyConstantStruct(f, def, constants, parent_types);
                        }
                        break;
                    }
                }

                if (!found) throw new ServiceDefinitionException("Error in constant " + c.Name + " in service definition \"" + def.Name + "\": struct field " + e.ConstantRefName + " not found");
            }
        }

        internal static string VerifyConstant(ConstantDefinition c, ServiceDefinition def, Dictionary<string, ConstantDefinition> constants)
        {
            if (!c.VerifyValue()) throw new ServiceDefinitionException("Error in constant " + c.Name + " in service definition \"" + def.Name + "\"");
            VerifyName(c.Name, def);

            if (c.Type.Type == DataTypes.namedtype_t)
            {
                VerifyConstantStruct(c, def, constants, new List<string>());

            }

            return c.Name;
        }

        internal static void VerifyEnum(EnumDefinition e, ServiceDefinition def)
        {
            if (!e.VerifyValues())
            {
                throw new ServiceDefinitionException("Error in constant in enum definition \"" + def.Name + "\"");
            }

            VerifyName(e.Name, def);
            foreach (var e1 in e.Values)
            {
                VerifyName(e1.Name, def);
            }
        }

        internal static List<string> GetServiceNames(ServiceDefinition def)
        {
            var o = new List<string>();
            foreach (var n in def.Objects.Values) o.Add(n.Name);
            foreach (var n in def.Structures.Values) o.Add(n.Name);
            foreach (var n in def.Pods.Values) o.Add(n.Name);
            foreach (var n in def.NamedArrays.Values) o.Add(n.Name);
            foreach (var n in def.Constants.Values) o.Add(n.Name);
            foreach (var n in def.Enums.Values) o.Add(n.Name);
            foreach (var n in def.Exceptions) o.Add(n);
            return o;
        }

        internal static void VerifyUsing(UsingDefinition e, ServiceDefinition def, Dictionary<string,ServiceDefinition> importeddefs)
        {
            VerifyName(e.UnqualifiedName, def);
            var r = new Regex(detail.ServiceDefinitionUtil.RR_QUALIFIED_TYPE_REGEX);
            if (!r.IsMatch(e.QualifiedName))
            {
                throw new ServiceDefinitionException("Using \"" + e.QualifiedName + "\" is invalid in service definition \"" + def.Name + "\"");
            }

            var s1 = SplitQualifiedName(e.QualifiedName);

            foreach (var d1 in importeddefs.Values)

            {
                if (s1.Item1 == d1.Name)
                {
                    var importeddefs_names = GetServiceNames(d1);
                    if (!importeddefs_names.Contains(s1.Item2))
                    {
                        throw new ServiceDefinitionException("Using \"" + e.QualifiedName + "\" is invalid in service definition \"" + def.Name + "\"");
                    }
                    return;
                }
            }

            throw new ServiceDefinitionException("Using \"" + e.QualifiedName + "\" is invalid in service definition \"" + def.Name + "\"");
        }

        internal static void VerifyType(TypeDefinition t, ServiceDefinition def, Dictionary<string, ServiceDefinition> defs)
        {
            switch (t.ArrayType)
            {
                case DataTypes_ArrayTypes.none:
                case DataTypes_ArrayTypes.array:
                case DataTypes_ArrayTypes.multidimarray:
                    break;
                default:
                    throw new ServiceDefinitionException("Invalid Robot Raconteur data type \"" + t.ToString() + "\" type in service \"" + def.Name + "\"");
            }

            switch (t.ContainerType)
            {
                case DataTypes_ContainerTypes.none:
                case DataTypes_ContainerTypes.list:
                case DataTypes_ContainerTypes.map_int32:
                case DataTypes_ContainerTypes.map_string:
                    break;
                default:
                    throw new ServiceDefinitionException("Invalid Robot Raconteur data type \"" + t.ToString() + "\" type in service \"" + def.Name + "\"");
            }

            if (DataTypeUtil.IsNumber(t.Type))
            {
                return;

            }
            if (t.Type == DataTypes.string_t)
            {
                if (t.ArrayType != DataTypes_ArrayTypes.none) throw new ServiceDefinitionException("Invalid Robot Raconteur data type \"" + t.ToString() + "\" type in service \"" + def.Name + "\"");

                return;
            }
            if (t.Type == DataTypes.vector_t || t.Type == DataTypes.dictionary_t || t.Type == DataTypes.object_t || t.Type == DataTypes.varvalue_t || t.Type == DataTypes.varobject_t || t.Type == DataTypes.multidimarray_t) return;
            if (t.Type == DataTypes.namedtype_t)
            {
                var nt = t.ResolveNamedType(defs);
                DataTypes nt_type = nt.RRDataType;
                if ((nt_type != DataTypes.pod_t && nt_type != DataTypes.namedarray_t) && t.ArrayType != DataTypes_ArrayTypes.none) throw new ServiceDefinitionException("Invalid Robot Raconteur data type \"" + t.ToString() + "\" type in service \"" + def.Name + "\"");
                if (nt_type != DataTypes.structure_t && nt_type != DataTypes.pod_t && nt_type != DataTypes.namedarray_t && nt_type != DataTypes.enum_t) throw new ServiceDefinitionException("Invalid Robot Raconteur data type \"" + t.ToString() + "\" type in service \"" + def.Name + "\"");
                if (nt_type == DataTypes.pod_t)
                {

                }
                return;
            }
            throw new ServiceDefinitionException("Invalid Robot Raconteur data type \"" + t.ToString() + "\" type in service \"" + def.Name + "\"");
        }

        internal static void VerifyReturnType(TypeDefinition t, ServiceDefinition def, Dictionary<string, ServiceDefinition> defs)
        {
            if (t.Type == DataTypes.void_t)
            {
                if (t.ArrayType != DataTypes_ArrayTypes.none || t.ContainerType != DataTypes_ContainerTypes.none)
                {
                    throw new ServiceDefinitionException("Invalid Robot Raconteur data type \"" + t.ToString() + "\" type in service \"" + def.Name + "\"");
                }
                return;
            }
            else
            {
                VerifyType(t, def, defs);
            }
        }

        internal static void VerifyParameters(List<TypeDefinition> p, ServiceDefinition def, Dictionary<string, ServiceDefinition> defs)
        {

            var names = new List<string>();
            foreach (var t in p)

            {
                VerifyType(t, def, defs);
                if (names.Contains(t.Name))
                    throw new ServiceDefinitionException("Parameters must have unique names");
                names.Add(t.Name);
            }
        }

        internal static void VerifyModifiers(MemberDefinition m, bool readwrite, bool unreliable, ref List<Exception> warnings)
        {
            bool direction_found = false;
            bool unreliable_found = false;

            foreach (var s in m.Modifiers)

            {
                if (readwrite)
                {
                    if (s == "readonly" || s == "writeonly")
                    {
                        if (direction_found)
                        {
                            warnings.Add(new RobotRaconteurParseException("Invalid member modifier combination: [readonly,writeonly]"));
                        }
                        direction_found = true;
                        continue;
                    }
                }

                if (unreliable)
                {
                    if (s == "unreliable")
                    {
                        var obj = m.ServiceEntry;
                        if (obj != null)
                        {
                            foreach (var o in obj.Options)
                            {
                                var r = new Regex("^[ \\t]*pipe[ \\t]+" + m.Name + "[ \\t]+unreliable[ \\t]*$");
                                if (!r.IsMatch(o))
                                {
                                    warnings.Add(new RobotRaconteurParseException("Invalid member modifier combination: [unreliable]"));
                                }
                            }
                        }

                        if (unreliable_found)
                        {
                            warnings.Add(new RobotRaconteurParseException("Invalid member modifier combination: [unreliable]"));
                        }
                        unreliable_found = true;
                        continue;
                    }
                }

                warnings.Add(new RobotRaconteurParseException("Unknown member modifier: [" + s + "]"));
            }
        }

        internal static string VerifyMember(MemberDefinition m, ServiceDefinition def, Dictionary<string, ServiceDefinition> defs, ref List<Exception> warnings)
        {
            VerifyName(m.Name, def);

            if (m.Modifiers.Count > 0)
            {
                VerifyVersionSupport(def, 0, 9, "Service definition standard version 0.9 or greater required for Member Modifiers");
            }

            var p = m as PropertyDefinition;
            if (p != null)
            {
                VerifyType(p.Type, def, defs);
                VerifyModifiers(m, true, false, ref warnings);
                return p.Name;
            }
            var f = m as FunctionDefinition;
            if (f != null)
            {
                if (!f.IsGenerator)
                {
                    VerifyParameters(f.Parameters, def, defs);
                    VerifyReturnType(f.ReturnType, def, defs);
                    VerifyModifiers(m, false, false, ref warnings);
                    return f.Name;
                }
                else
                {
                    bool generator_found = false; ;
                    if (f.ReturnType.ContainerType == DataTypes_ContainerTypes.generator)
                    {
                        if (f.ReturnType.Type == DataTypes.void_t)
                        {
                            throw new ServiceDefinitionException("Generator return must not be void");
                        }

                        var ret2 = f.ReturnType.Clone();
                        ret2.RemoveContainers();
                        VerifyType(ret2, def, defs);
                        if (f.ReturnType.Type == DataTypes.namedtype_t) f.ReturnType.ResolveNamedType(defs);
                        generator_found = true;
                    }
                    else
                    {
                        if (f.ReturnType.Type != DataTypes.void_t)
                        {
                            throw new ServiceDefinitionException("Generator return must use generator container");
                        }
                        //VerifyReturnType(f.ReturnType, def, defs);
                    }

                    if (f.Parameters.Count > 0 && f.Parameters.Last().ContainerType == DataTypes_ContainerTypes.generator)
                    {
                        var p3 = f.Parameters.Last().Clone();
                        p3.RemoveContainers();
                        VerifyType(p3, def, defs);
                        if (f.Parameters.Last().Type == DataTypes.namedtype_t) f.Parameters.Last().ResolveNamedType(defs);
                        if (f.Parameters.Count > 1)
                        {
                            var params2 = new List<TypeDefinition>((TypeDefinition[])f.Parameters.Take(f.Parameters.Count - 1).ToArray());
                            VerifyParameters(params2, def, defs);
                        }
                        generator_found = true;
                    }
                    else
                    {
                        VerifyParameters(f.Parameters, def, defs);
                    }

                    if (!generator_found)
                    {
                        throw new ServiceDefinitionException("Generator return or parameter not found");
                    }

                    return f.Name;
                }
            }

            var e = m as EventDefinition;
            if (e != null)
            {
                VerifyParameters(e.Parameters, def, defs);
                VerifyModifiers(m, false, false, ref warnings);
                return e.Name;
            }

            var o = m as ObjRefDefinition;
            if (o != null)
            {
                VerifyModifiers(m, false, false, ref warnings);

                if (o.ObjectType == "varobject") return o.Name;

                if (!o.ObjectType.Contains('.'))
                {
                    foreach (var ee in def.Objects.Values)

                    {
                        if (ee.Name == o.ObjectType) return o.Name;
                    }
                }
                else
                {
                    var s1 = SplitQualifiedName(o.ObjectType);

                    var defname = s1.Item1;
                    ServiceDefinition def2;
                    if (defs.TryGetValue(defname, out def2))
                    {
                        foreach (var ee in def2.Objects.Values)

                        {
                            if (ee.Name == s1.Item2)
                            {
                                return o.Name;
                            }
                        }
                    }
                }
                throw new ServiceDefinitionException("Unknown object type \"" + o.ObjectType + "\" in member \"" + m.Name + "\" in  service \"" + def.Name + "\"");
            }

            var p2 = m as PipeDefinition;
            if (p2 != null)
            {
                VerifyType(p2.Type, def, defs);
                VerifyModifiers(m, true, true, ref warnings);
                return p2.Name;
            }

            var c = m as CallbackDefinition;
            if (c != null)
            {
                VerifyParameters(c.Parameters, def, defs);
                VerifyReturnType(c.ReturnType, def, defs);
                VerifyModifiers(m, false, false, ref warnings);
                return c.Name;
            }

            var w = m as WireDefinition;
            if (w != null)
            {
                VerifyType(w.Type, def, defs);
                VerifyModifiers(m, true, false, ref warnings);
                return w.Name;
            }

            var m2 = m as MemoryDefinition;
            if (m2 != null)
            {
                VerifyType(m2.Type, def, defs);
                VerifyModifiers(m, true, false, ref warnings);
                if (!DataTypeUtil.IsNumber(m2.Type.Type))
                {
                    if (m2.Type.Type != DataTypes.namedtype_t)
                    {
                        throw new ServiceDefinitionException("Memory member must be numeric or pod");
                    }
                    var nt = m2.Type.ResolveNamedType(defs);
                    if (nt.RRDataType != DataTypes.pod_t && nt.RRDataType != DataTypes.namedarray_t)
                    {
                        throw new ServiceDefinitionException("Memory member must be numeric or pod");
                    }
                }
                switch (m2.Type.ArrayType)
                {
                    case DataTypes_ArrayTypes.array:
                    case DataTypes_ArrayTypes.multidimarray:
                        break;
                    default:
                        throw new ServiceDefinitionException("Memory member must be numeric or pod");
                }

                if (!m2.Type.ArrayVarLength)
                {
                    throw new ServiceDefinitionException("Memory member must not be fixed size");
                }

                if (m2.Type.ArrayLength.Count != 0)
                {
                    var array_count = m2.Type.ArrayLength.Aggregate(1, (x, y) => x * y);
                    if (array_count != 0)
                    {
                        throw new ServiceDefinitionException("Memory member must not be fixed size");
                    }
                }

                return m2.Name;
            }

            throw new ServiceDefinitionException("Invalid member \"" + m.Name + "\" type in service \"" + def.Name + "\"");
        }

        internal class rrimplements
        {
            public string name;
            public ServiceEntryDefinition obj;
            public List<rrimplements> implements = new List<rrimplements>();
        };

        internal static rrimplements get_implements(ServiceEntryDefinition obj, ServiceDefinition def, Dictionary<string, ServiceDefinition> defs, string rootobj = "")
        {
            var out_ = new rrimplements();
            out_.obj = obj;
            out_.name = def.Name + "." + obj.Name;

            if (rootobj == "") rootobj = out_.name;

            foreach (var e in obj.Implements)

            {
                if (!e.Contains("."))
                {


                    ServiceEntryDefinition obj2;
                    if (!def.Objects.TryGetValue(e, out obj2))
                        throw new ServiceDefinitionException("Object \"" + def.Name + "." + e + " not found");

                    if (rootobj == (def.Name + "." + obj2.Name)) throw new ServiceDefinitionException("Recursive implements between \"" + rootobj + "\" and \"" + def.Name + "." + obj2.Name + "\"");

                    rrimplements imp2 = get_implements(obj2, def, defs, rootobj);
                    out_.implements.Add(imp2);
                }
                else
                {
                    var s1 = SplitQualifiedName(e);

                    ServiceDefinition def2;
                    if (!defs.TryGetValue(s1.Item1, out def2))
                        throw new ServiceDefinitionException("Service definition \"" + e + "\" not found.");

                    ServiceEntryDefinition obj2;
                    if (!def2.Objects.TryGetValue(s1.Item2, out obj2))
                        throw new ServiceDefinitionException("Object \"" + e + " not found");

                    if (rootobj == (def2.Name + "." + obj2.Name)) throw new ServiceDefinitionException("Recursive implements between \"" + rootobj + "\" and \"" + def2.Name + "." + obj2.Name + "\"");

                    rrimplements imp2 = get_implements(obj2, def2, defs, rootobj);
                    out_.implements.Add(imp2);
                }




            }

            foreach (var r in out_.implements)
            {
                foreach (var r2 in r.implements)
                {
                    bool found = false;
                    foreach (var r3 in out_.implements)
                    {
                        if (r2.name == r3.name)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        throw new ServiceDefinitionException("Object \"" + out_.name + "\" does not implement inherited type \"" + r2.name + "\"");
                    }
                }
            }
            return out_;

        }

        internal static bool CompareTypeDefinition(ServiceDefinition d1, TypeDefinition t1, ServiceDefinition d2, TypeDefinition t2)
        {
            if (t1.Name != t2.Name) return false;
            //if (t1.ImportedType!=t2.ImportedType) return false;
            if (t1.ArrayType != t2.ArrayType) return false;
            if (t1.ArrayType != DataTypes_ArrayTypes.none)
            {
                if (t1.ArrayVarLength != t2.ArrayVarLength) return false;
                if (t1.ArrayLength.Count != t2.ArrayLength.Count) return false;
                if (!t1.ArrayLength.SequenceEqual(t2.ArrayLength)) return false;
            }

            if (t1.ContainerType != t2.ContainerType) return false;

            if (t1.Type != t2.Type) return false;
            if (t1.Type != DataTypes.namedtype_t && t1.Type != DataTypes.object_t) return true;

            if (t1.TypeString == "varvalue" && t2.TypeString == "varvalue") return true;


            string st1 = "";
            string st2 = "";

            if (!t1.TypeString.Contains('.'))
            {
                st1 = d1.Name + "." + t1.TypeString;
            }
            else
            {
                st1 = t1.TypeString;
            }

            if (!t2.TypeString.Contains('.'))
            {
                st2 = d2.Name + "." + t2.TypeString;
            }
            else
            {
                st2 = t2.TypeString;
            }

            return st1 == st2;
        }

        internal static bool CompareTypeDefinitions(ServiceDefinition d1, List<TypeDefinition> t1, ServiceDefinition d2, List<TypeDefinition> t2)
        {
            if (t1.Count != t2.Count) return false;
            for (int i = 0; i < t1.Count; i++)
            {
                if (!CompareTypeDefinition(d1, t1[i], d2, t2[i])) return false;
            }

            return true;
        }

        internal static bool CompareMember(MemberDefinition m1, MemberDefinition m2)
        {
            if (m1.Name != m2.Name) return false;
            if (!m1.Modifiers.SequenceEqual(m2.Modifiers)) return false;

            var e1 = m1.ServiceEntry;
            var e2 = m2.ServiceEntry;
            if ((e1 == null) || (e2 == null)) return false;

            var d1 = e1.ServiceDefinition;
            var d2 = e2.ServiceDefinition;
            if ((d1 == null) || (d2 == null)) return false;

            var p1 = m1 as PropertyDefinition;
            var p2 = m2 as PropertyDefinition;
            if (p2 != null)
            {
                if (p2 == null) return false;
                return CompareTypeDefinition(d1, p1.Type, d2, p2.Type);
            }

            var f1 = m1 as FunctionDefinition;
            var f2 = m2 as FunctionDefinition;
            if (f1 != null)
            {
                if (f2 == null) return false;
                if (!CompareTypeDefinition(d1, f1.ReturnType, d2, f2.ReturnType)) return false;
                return CompareTypeDefinitions(d1, f1.Parameters, d2, f2.Parameters);
            }

            var ev1 = m1 as EventDefinition;
            var ev2 = m2 as EventDefinition;
            if (ev2 != null)
            {
                if (ev2 == null) return false;

                return CompareTypeDefinitions(d1, ev1.Parameters, d2, ev2.Parameters);
            }

            var o1 = m1 as ObjRefDefinition;
            var o2 = m2 as ObjRefDefinition;
            if (o1 != null)
            {
                if (o2 == null) return false;

                if (o1.ArrayType != o2.ArrayType) return false;
                if (o1.ContainerType != o2.ContainerType) return false;

                if (o1.ObjectType == "varobject" && o2.ObjectType == "varobject") return true;


                string st1 = "";
                string st2 = "";

                if (!o1.ObjectType.Contains('.'))
                {
                    st1 = d1.Name + "." + o1.ObjectType;
                }
                else
                {
                    st1 = o1.ObjectType;
                }


                if (!o2.ObjectType.Contains('.'))
                {
                    st2 = d2.Name + "." + o2.ObjectType;
                }
                else
                {
                    st2 = o2.ObjectType;
                }

                return st1 == st2;

            }

            var pp1 = m1 as PipeDefinition;
            var pp2 = m2 as PipeDefinition;
            if (pp1 != null)
            {
                if (pp2 == null) return false;

                return CompareTypeDefinition(d1, pp1.Type, d2, pp2.Type);
            }

            var c1 = m1 as CallbackDefinition;
            var c2 = m2 as CallbackDefinition;
            if (c1 != null)
            {
                if (c2 == null) return false;
                if (!CompareTypeDefinition(d1, c1.ReturnType, d2, c2.ReturnType)) return false;
                return CompareTypeDefinitions(d1, c1.Parameters, d2, c2.Parameters);
            }

            var w1 = m1 as WireDefinition;
            var w2 = m2 as WireDefinition;
            if (w1 != null)
            {
                if (w2 == null) return false;

                return CompareTypeDefinition(d1, w1.Type, d2, w2.Type);
            }

            var mem1 = m1 as MemoryDefinition;
            var mem2 = m2 as MemoryDefinition;
            if (mem1 != null)
            {
                if (mem2 == null) return false;

                return CompareTypeDefinition(d1, mem1.Type, d2, mem2.Type);
            }

            return false;

        }

        internal static void VerifyObject(ServiceEntryDefinition obj, ServiceDefinition def, Dictionary<string, ServiceDefinition> defs, ref List<Exception> warnings)
        {
            if (obj.EntryType != DataTypes.object_t) throw new ServiceDefinitionException("Invalid EntryType in " + obj.Name);

            VerifyName(obj.Name, def);

            var membernames = new List<string>();

            foreach (var e in obj.Options)
            {
                var s1 = e.Split(null);
                if (s1[0] == "constant")
                {
                    string membername = VerifyConstant(e, def);
                    if (membernames.Contains(membername)) throw new ServiceDefinitionException("Object \"" + obj.Name + "\" in service definition \"" + def.Name + "\" contains multiple members named \"" + membername + "\"");
                    membernames.Add(membername);
                }
            }

            if (obj.Constants.Count != 0)
            {
                VerifyVersionSupport(def, 0, 9, "Service definition standard version 0.9 or greater required for \"constant\" keyword");
            }

            foreach (var e in obj.Constants.Values)
            {
                string membername = VerifyConstant(e, def, obj.Constants);
                if (membernames.Contains(membername)) throw new ServiceDefinitionException("Object \"" + obj.Name + "\" in service definition \"" + def.Name + "\" contains multiple members named \"" + membername + "\"");
                membernames.Add(membername);
            }

            foreach (var e in obj.Members.Values)
            {

                string membername = VerifyMember(e, def, defs, ref warnings);
                if (membernames.Contains(membername)) throw new ServiceDefinitionException("Object \"" + obj.Name + "\" in service definition \"" + def.Name + "\" contains multiple members named \"" + membername + "\"");
                membernames.Add(membername);
            }

            rrimplements r = get_implements(obj, def, defs);

            foreach (var e in r.implements)
            {
                foreach (var ee in e.obj.Members.Values)
                {
                    MemberDefinition m2;
                    if (!obj.Members.TryGetValue(ee.Name, out m2))
                        throw new ServiceDefinitionException("Object \"" + obj.Name + "\" in service definition \"" + def.Name + "\" does not implement required member \"" + ee.Name + "\"");


                    if (!CompareMember(m2, ee)) throw new ServiceDefinitionException("Member \"" + ee.Name + "\" in object \"" + obj.Name + "\" in service definition \"" + def.Name + "\" does not match implemented member " + m2.ToString());
                }

            }

        }


        internal static void VerifyStructure_check_recursion(ServiceEntryDefinition strut, ref HashSet<string>  names, DataTypes entry_type)
        {
            if (strut.EntryType != entry_type && strut.EntryType != DataTypes.namedarray_t)
            {
                throw new InternalErrorException("");
            }

            names.Add(strut.Name);

            foreach (MemberDefinition e in strut.Members.Values)

            {
                var p = e as PropertyDefinition;
                if (p == null) throw new InternalErrorException("");

                if (p.Type.Type == DataTypes.namedtype_t)
                {
                    var nt_def = p.Type.ResolveNamedType();
                    var et_def = nt_def as ServiceEntryDefinition;
                    if (et_def == null) throw new InternalErrorException("");
                    if (et_def.EntryType != entry_type && et_def.EntryType != DataTypes.namedarray_t) throw new InternalErrorException("");

                    if (names.Contains(et_def.Name))
                    {
                        throw new ServiceDefinitionException("Recursive namedarray/pod detected in " + strut.Name);
                    }

                    var names2 = new HashSet<string>(names);
                    VerifyStructure_check_recursion(et_def, ref names2, entry_type);
                }
            }
        }

        internal static void VerifyStructure_common(ServiceEntryDefinition strut, ServiceDefinition def, Dictionary<string,ServiceDefinition> defs, ref List<Exception> warnings, DataTypes entry_type)
        {
            if (strut.EntryType != entry_type) throw new ServiceDefinitionException("Invalid EntryType in " + strut.Name);

            VerifyName(strut.Name, def);
            var membernames = new List<string>();

            foreach(var e in strut.Options)

        {
                var s1 = e.Split((string[])null, StringSplitOptions.RemoveEmptyEntries);                
                if (s1[0] == "constant")
                {
                    var membername = VerifyConstant(e, def);
                    if (membernames.Contains( membername)) throw new ServiceDefinitionException("Structure \"" + strut.Name + "\" in service definition \"" + def.Name + "\" contains multiple members named \"" + membername + "\"");
                    membernames.Add(membername);
                }
            }

            foreach(var e in strut.Constants.Values)

        {
                string membername = VerifyConstant(e, def, strut.Constants);
                if (membernames.Contains(membername)) throw new ServiceDefinitionException("Struct \"" + strut.Name + "\" in service definition \"" + def.Name + "\" contains multiple members named \"" + membername + "\"");
                membernames.Add(membername);
            }

            DataTypes namedarray_element_type = DataTypes.void_t;

            foreach(var e in strut.Members.Values)

        {
                var p = e as PropertyDefinition;
                if (p == null) throw new ServiceDefinitionException("Structure \"" + strut.Name + "\" can only contain fields in service definition \"" + def.Name + "\"");

                string membername = VerifyMember(p, def, defs, ref warnings);

                if (entry_type == DataTypes.pod_t)
                {
                    var t = p.Type;
                    if (!DataTypeUtil.IsNumber(t.Type) && t.Type != DataTypes.namedtype_t)
                    {
                        throw new ServiceDefinitionException("Pods must only contain numeric, pod, and namedarray types");
                    }

                    if (t.Type == DataTypes.namedtype_t)
                    {
                        NamedTypeDefinition tt = t.ResolveNamedType();
                        if (tt.RRDataType != DataTypes.pod_t && tt.RRDataType != DataTypes.namedarray_t)
                        {
                            throw new ServiceDefinitionException("Pods must only contain numeric, custruct, pod types");
                        }
                    }

                    if (t.ContainerType != DataTypes_ContainerTypes.none)
                    {
                        throw new ServiceDefinitionException("Pods may not use containers");
                    }

                    if (t.ArrayLength.Contains(0)
                        || (t.ArrayType == DataTypes_ArrayTypes.multidimarray && t.ArrayLength.Count==0))
                    {
                        throw new ServiceDefinitionException("Pods must have fixed or finite length arrays");
                    }

                    var n = new HashSet<string>();
                    VerifyStructure_check_recursion(strut, ref n, DataTypes.pod_t);
                }

                if (entry_type == DataTypes.namedarray_t)
                {
                    TypeDefinition t = p.Type;
                    if (!DataTypeUtil.IsNumber(t.Type) && t.Type != DataTypes.namedtype_t)
                    {
                        throw new ServiceDefinitionException("NamedArrays must only contain numeric and namedarray types");
                    }

                    if (t.Type == DataTypes.namedtype_t)
                    {
                        NamedTypeDefinition tt = t.ResolveNamedType();
                        if (tt.RRDataType != DataTypes.namedarray_t)
                        {
                            throw new ServiceDefinitionException("NamedArrays must only contain numeric and namedarray types");
                        }
                    }

                    if (t.ContainerType != DataTypes_ContainerTypes.none)
                    {
                        throw new ServiceDefinitionException("NamedArrays may not use containers");
                    }

                    switch (t.ArrayType)
                    {
                        case DataTypes_ArrayTypes.none:
                            break;
                        case DataTypes_ArrayTypes.array:
                            if (t.ArrayVarLength) throw new ServiceDefinitionException("NamedArray fields must be scalars or fixed arrays");
                            break;
                        default:
                            throw new ServiceDefinitionException("NamedArray fields must be scalars or fixed arrays");
                    }
                                        
                    GetNamedArrayElementTypeAndCount(strut, defs);
                }

                if (membernames.Contains(membername)) throw new ServiceDefinitionException("Structure \"" + strut.Name + "\" in service definition \"" + def.Name + "\" contains multiple members named \"" + membername + "\"");
                membernames.Add(membername);
            }

        }

        internal static void VerifyStructure(ServiceEntryDefinition strut, ServiceDefinition def, Dictionary<string,ServiceDefinition> defs, ref List<Exception> warnings)
        {
            VerifyStructure_common(strut, def, defs, ref warnings, DataTypes.structure_t);
        }

        internal static void VerifyPod(ServiceEntryDefinition strut, ServiceDefinition def, Dictionary<string, ServiceDefinition> defs, ref List<Exception> warnings)
        {
            VerifyStructure_common(strut, def, defs, ref warnings, DataTypes.pod_t);
        }

        internal static void VerifyNamedArray(ServiceEntryDefinition strut, ServiceDefinition def, Dictionary<string, ServiceDefinition> defs, ref List<Exception> warnings)
        {
            VerifyStructure_common(strut, def, defs, ref warnings, DataTypes.namedarray_t);
        }

        internal class rrimports
        {
            public ServiceDefinition def;
            public List<rrimports> imported=new List<rrimports>();
        };

        internal static rrimports get_imports(ServiceDefinition def, Dictionary<string, ServiceDefinition> defs, ServiceDefinition rootdef = null)
        {
            rrimports out_ = new rrimports();
            out_.def = def;
            if (def.Imports.Count == 0) return out_;

            if (rootdef == null) rootdef = def;

            foreach (var e in def.Imports)

            {
                ServiceDefinition def2;                
                if (!defs.TryGetValue(e, out def2)) throw new ServiceDefinitionException("Service definition \"" + e + "\" not found.");
                if (def2.Name == rootdef.Name) throw new ServiceDefinitionException("Recursive imports between \"" + def.Name + "\" and \"" + rootdef.Name + "\"");
                rrimports imp2 = get_imports(def2, defs, rootdef);
                out_.imported.Add(imp2);
            }

            return out_;
        }

        internal static void VerifyImports(ServiceDefinition def, Dictionary<string,ServiceDefinition> defs)
        {
            rrimports c = get_imports(def, defs);
        }

        public static void VerifyServiceDefinitions(Dictionary<string, ServiceDefinition> def, ref List<Exception> warnings)
        {
            foreach (var e in def.Values)

            {
                e.CheckVersion();

                if (!e.Name.StartsWith("RobotRaconteurTestService") && !e.Name.StartsWith("RobotRaconteurServiceIndex"))
                    VerifyName(e.Name, e, true);

                if (e.Name.EndsWith("_signed")) throw new ServiceDefinitionException("Service definition names ending with \"_signed\" are reserved");

                VerifyImports(e, def);

                var names = new HashSet<string>();
                foreach (var ee in e.Options)

                {
                    var s1 = ee.Split((string[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (s1[0] == "constant")
                    {
                        var name = VerifyConstant(ee, e);
                        if (names.Contains(name)) throw new ServiceDefinitionException("Service definition \"" + e.Name + "\" contains multiple high level names \"" + name + "\"");
                        names.Add(name);
                    }
                }

                if (e.Constants.Count != 0)
                {
                    VerifyVersionSupport(e, 0, 9, "Service definition standard version 0.9 or greater required for \"constant\" keyword");
                }

                foreach (var ee in e.Constants.Values)

                {
                    string name = VerifyConstant(ee, e, e.Constants);
                    if (names.Contains(name)) throw new ServiceDefinitionException("Service definition \"" + e.Name + "\" contains multiple high level names \"" + name + "\"");
                    names.Add(name);
                }

                foreach (var ee in e.Exceptions)

                {
                    VerifyName(ee, e);
                    string name = ee;
                    if (names.Contains(name)) throw new ServiceDefinitionException("Service definition \"" + e.Name + "\" contains multiple high level names \"" + name + "\"");
                    names.Add(name);
                }

                var importeddefs = new Dictionary<string, ServiceDefinition>();
                foreach (var ee in e.Imports)

                {
                    foreach (var ee2 in def.Values)

                    {
                        if (ee == ee2.Name)
                        {
                            importeddefs.Add(ee2.Name, ee2);
                        }

                    }
                }

                if ((bool)e.StdVer)
                {
                    foreach (var ee in importeddefs.Values)

                    {
                        if (!(bool)ee.StdVer || ee.StdVer > e.StdVer)
                        {
                            throw new ServiceDefinitionException("Imported service definition " + ee.Name + " has a higher Service Definition standard version than " + e.Name);
                        }
                    }
                }

                if (e.Using.Count > 0)
                {
                    VerifyVersionSupport(e, 0, 9, "Service definition standard version 0.9 or greater required for \"using\" keyword");
                }

                foreach (var ee in e.Using)
                {
                    string name = ee.UnqualifiedName;
                    if (names.Contains(name)) throw new ServiceDefinitionException("Service definition \"" + e.Name + "\" contains multiple high level names \"" + name + "\"");
                    VerifyUsing(ee, e, importeddefs);
                    names.Add(name);

                }

                if (e.Enums.Count > 0)
                {
                    VerifyVersionSupport(e, 0, 9, "Service definition standard version 0.9 or greater required for \"enum\" keyword");
                }

                foreach (var ee in e.Enums.Values)
                {
                    VerifyEnum(ee, e);
                    string name = ee.Name;
                    if (names.Contains(name)) throw new ServiceDefinitionException("Service definition \"" + e.Name + "\" contains multiple high level names \"" + name + "\"");
                    names.Add(name);
                }

                foreach (var ee in e.Structures.Values)
                {
                    VerifyStructure(ee, e, importeddefs, ref warnings);

                    string name = ee.Name;
                    if (names.Contains(name)) throw new ServiceDefinitionException("Service definition \"" + e.Name + "\" contains multiple high level names \"" + name + "\"");
                    names.Add(name);
                }

                foreach (var ee in e.Pods.Values)
                {
                    VerifyPod(ee, e, importeddefs, ref warnings);

                    string name = ee.Name;
                    if (names.Contains(name)) throw new ServiceDefinitionException("Service definition \"" + e.Name + "\" contains multiple high level names \"" + name + "\"");
                    names.Add(name);
                }

                foreach (var ee in e.NamedArrays.Values)
                {
                    VerifyNamedArray(ee, e, importeddefs, ref warnings);

                    string name = ee.Name;
                    if (names.Contains(name)) throw new ServiceDefinitionException("Service definition \"" + e.Name + "\" contains multiple high level names \"" + name + "\"");
                    names.Add(name);
                }

                foreach (var ee in e.Objects.Values)
                {
                    VerifyObject(ee, e, importeddefs, ref warnings);

                    string name = ee.Name;
                    if (names.Contains(name)) throw new ServiceDefinitionException("Service definition \"" + e.Name + "\" contains multiple high level names \"" + name + "\"");
                    names.Add(name);

                }

            }

        }

        public static void VerifyServiceDefinitions(Dictionary<string,ServiceDefinition> def)
        {
            var warnings = new List<Exception>();
            VerifyServiceDefinitions(def, ref warnings);
        }

        internal static bool CompareConstantDefinition(ServiceDefinition service1, ConstantDefinition d1, ServiceDefinition service2, ConstantDefinition d2)
        {
            if (d1.Name != d2.Name) return false;
            if (!CompareTypeDefinition(service1, d1.Type, service2, d2.Type)) return false;
            if (d1.Value.Trim() != d2.Value.Trim()) return false;
            return true;
        }

        public static bool CompareServiceEntryDefinition(ServiceDefinition service1, ServiceEntryDefinition d1, ServiceDefinition service2, ServiceEntryDefinition d2)
        {
            if (d1.Name != d2.Name) return false;
            if (d1.EntryType != d2.EntryType) return false;
            if (!d1.Implements.SequenceEqual(d2.Implements)) return false;
            if (!d1.Options.SequenceEqual(d2.Options)) return false;
            if (d1.Constants.Count != d2.Constants.Count) return false;
            string[] constant_keys = d1.Constants.Keys.ToArray();
            for (int i = 0; i < d1.Constants.Count; i++)
            {
                if (!CompareConstantDefinition(service1, d1.Constants[constant_keys[i]], service2, d2.Constants[constant_keys[i]]))
                    return false;
            }

            if (d1.Members.Count != d2.Members.Count) return false;
            string[] member_keys = d1.Members.Keys.ToArray();
            for (int i = 0; i < d1.Members.Count; i++)
            {
                if (!CompareMember(d1.Members[member_keys[i]], d2.Members[member_keys[i]]))
                    return false;
            }
            return true;
        }

        internal static bool CompareUsingDefinition(UsingDefinition u1, UsingDefinition u2)
        {
            if (u1.QualifiedName != u2.QualifiedName) return false;
            if (u1.UnqualifiedName != u2.UnqualifiedName) return false;
            return true;
        }

        internal static bool CompareEnumDefinition(EnumDefinition enum1, EnumDefinition enum2)
        {
            if (enum1.Name != enum2.Name) return false;
            if (enum1.Values.Count != enum2.Values.Count) return false;
            for (int i = 0; i < enum1.Values.Count; i++)
            {
                if (enum1.Values[i].Name != enum2.Values[i].Name) return false;
                if (enum1.Values[i].Value != enum2.Values[i].Value) return false;
                if (enum1.Values[i].ImplicitValue != enum2.Values[i].ImplicitValue) return false;
                if (enum1.Values[i].HexValue != enum2.Values[i].HexValue) return false;
            }
            return true;
        }

        public static bool CompareServiceDefinitions(ServiceDefinition service1, ServiceDefinition service2)
        {
            if (service1.Name != service2.Name) return false;
            if (!service1.Imports.SequenceEqual(service2.Imports)) return false;
            if (!service1.Options.SequenceEqual(service2.Options)) return false;

            if (service1.Using.Count != service2.Using.Count) return false;
            for (int i = 0; i < service1.Using.Count; i++)
            {
                if (!CompareUsingDefinition(service1.Using[i], service2.Using[i]))
                    return false;
            }

            if (service1.Constants.Count != service2.Constants.Count) return false;
            foreach (var i in service1.Constants.Keys)
            {
                if (!CompareConstantDefinition(service1, service1.Constants[i], service2, service2.Constants[i]))
                    return false;
            }

            if (service1.Enums.Count != service2.Enums.Count) return false;
            foreach (var i in service1.Enums.Keys)
            {
                if (!CompareEnumDefinition(service1.Enums[i], service2.Enums[i]))
                    return false;
            }

            if (service1.StdVer != service2.StdVer) return false;

            if (service1.Objects.Count != service2.Objects.Count) return false;
            foreach (var i in service1.Objects.Keys)
            {
                if (!CompareServiceEntryDefinition(service1, service1.Objects[i], service2, service2.Objects[i]))
                    return false;
            }

            if (service1.Structures.Count != service2.Structures.Count) return false;
            foreach (var i in service1.Structures.Keys)
            {
                if (!CompareServiceEntryDefinition(service1, service1.Structures[i], service2, service2.Structures[i]))
                    return false;
            }

            if (service1.Pods.Count != service2.Pods.Count) return false;
            foreach (var i in service1.Pods.Keys)
            {
                if (!CompareServiceEntryDefinition(service1, service1.Pods[i], service2, service2.Pods[i]))
                    return false;
            }

            return true;
        }

        public static int EstimatePodPackedElementSize(ServiceEntryDefinition def, Dictionary<string,ServiceDefinition> other_defs=null, RobotRaconteurNode node = null, object client = null)
        {
            int s = 16;
            s += ArrayBinaryWriter.GetStringByteCount8(def.Name);
            foreach( var m in def.Members.Values)

        {
                var p = m as PropertyDefinition;
                if (DataTypeUtil.IsNumber(p.Type.Type))
                {
                    s += 16;
                    s += ArrayBinaryWriter.GetStringByteCount8(p.Name);
                    int array_count;
                    if (p.Type.ArrayType == DataTypes_ArrayTypes.none)
                    {
                        array_count = 1;
                    }
                    else
                    {
                        array_count = (int)p.Type.ArrayLength.Aggregate(1, (x, y) => x * y);;
                    }
                    s += (int)DataTypeUtil.size(p.Type.Type) * array_count;
                }
                else
                {
                    var nt = (ServiceEntryDefinition)(p.Type.ResolveNamedType(other_defs, node, client));
                    s += 16;
                    s += ArrayBinaryWriter.GetStringByteCount8(p.Name);
                    s += ArrayBinaryWriter.GetStringByteCount8(nt.ResolveQualifiedName());
                    int array_count;
                    if (p.Type.ArrayType == DataTypes_ArrayTypes.none)
                    {
                        array_count = 1;
                    }
                    else
                    {
                        array_count = (int)p.Type.ArrayLength.Aggregate(1,(x,y) => x*y);
                    }
                    s += EstimatePodPackedElementSize(nt, other_defs, node, client) * array_count;
                }
            }
            return s;
        }

        public static Tuple<DataTypes, int> GetNamedArrayElementTypeAndCount(ServiceEntryDefinition def, Dictionary<string,ServiceDefinition> other_defs=null, RobotRaconteurNode node = null, object client = null, HashSet<string> n = null)
        {
            if (n == null) n = new HashSet<string>();

            if (def.EntryType != DataTypes.namedarray_t)
            {
                throw new InvalidOperationException("Argument must be an namedarray");
            }

            n.Add(def.Name);

            DataTypes element_type = DataTypes.void_t;
            int element_count = 0;

            if (def.Members.Count==0)
            {
                throw new ServiceDefinitionException("namedarray must not be empty");
            }

            foreach(var e in def.Members.Values)
    
        {
                int field_element_count = 1;

                var p = e as PropertyDefinition;
                if (p == null) throw new ServiceDefinitionException("Invalid member type in namedarray: " + def.Name);

                if (p.Type.ContainerType != DataTypes_ContainerTypes.none)
                {
                    throw new ServiceDefinitionException("namedarray must not contain containers: " + def.Name);
                }

                if (p.Type.ArrayType != DataTypes_ArrayTypes.none && p.Type.ArrayVarLength)
                {
                    throw new ServiceDefinitionException("namedarray must not contain variable length arrays: " + def.Name);
                }

                if (p.Type.ArrayType != DataTypes_ArrayTypes.none)
                {
                    field_element_count = (int)p.Type.ArrayLength.Aggregate(1, (x, y) => x * y);
                }

                if (DataTypeUtil.IsNumber(p.Type.Type))
                {
                    if (element_type == DataTypes.void_t)
                    {
                        element_type = p.Type.Type;
                    }
                    else
                    {
                        if (element_type != p.Type.Type) throw new ServiceDefinitionException("namedarray must contain same numeric type: " + def.Name);
                    }

                    element_count += field_element_count;
                }
                else if (p.Type.Type == DataTypes.namedtype_t)
                {
                    var nt_def = p.Type.ResolveNamedType();
                    var et_def = nt_def as ServiceEntryDefinition;
                    if (et_def == null) throw new InternalErrorException("");
                    if (et_def.EntryType != DataTypes.namedarray_t) throw new InternalErrorException("");

                    if (n.Contains(et_def.Name))
                    {
                        throw new ServiceDefinitionException("Recursive namedarray detected in " + def.Name);
                    }

                    var n2 = new HashSet<string>(n);
                    var v = GetNamedArrayElementTypeAndCount(et_def, other_defs, node, client, n2);
                    if (element_type == DataTypes.void_t)
                    {
                        element_type = v.Item1;
                    }
                    else
                    {
                        if (element_type != v.Item1) throw new ServiceDefinitionException("namedarray must contain same numeric type: " + def.Name);
                    }

                    element_count += field_element_count * v.Item2;

                }
                else
                {
                    throw new ServiceDefinitionException("Invalid namedarray field in " + def.Name);
                }
            }

            return Tuple.Create(element_type, element_count);

        }

        public static Type FindParentInterface(Type objtype)
        {
            List<Type> interfaces = new List<Type>(objtype.GetInterfaces());
            interfaces.RemoveAll(x => (x.GetCustomAttributes(typeof(RobotRaconteurServiceObjectInterface), true).Length == 0));

            if (interfaces.Count == 0) throw new DataTypeException("Object not a Robot Raconteur type");
            if (interfaces.Count == 1) return interfaces[0];

            List<Type> parentinterfaces = new List<Type>();

            for (int i = 0; i < interfaces.Count; i++)
            {
                bool parent = true;
                for (int j = 0; j < interfaces.Count; j++)
                {
                    if (i != j)
                        if (interfaces[j].GetInterface(interfaces[i].ToString()) != null) parent = false;
                }

                if (parent)
                    parentinterfaces.Add(interfaces[i]);

            }

            if (parentinterfaces.Count != 1)
                throw new DataTypeException("Robot Raconteur types can only directly inheret one Robot Raconteur interface type");

            return parentinterfaces[0];

        }

        public static string FindObjectRRType(object obj)
        {
            var i = FindParentInterface(obj.GetType());
            return ((RobotRaconteurServiceObjectInterface)Attribute.GetCustomAttribute(i, typeof(RobotRaconteurServiceObjectInterface))).RRType;
        }

        public static string FindStructRRType(Type s)
        {
            if (s.IsArray)
            {
                s = s.GetElementType();
            }

            var t1 = ((RobotRaconteurServiceStruct)Attribute.GetCustomAttribute(s, typeof(RobotRaconteurServiceStruct)));
            if (t1 != null)
            {
                return t1.RRType;
            }

            var t2 = ((RobotRaconteurNamedArrayElementTypeAndCount)Attribute.GetCustomAttribute(s, typeof(RobotRaconteurNamedArrayElementTypeAndCount)));
            if (t2 != null)
            {
                return t2.RRType;
            }

            var t3 = ((RobotRaconteurServicePod)Attribute.GetCustomAttribute(s, typeof(RobotRaconteurServicePod)));
            if (t3 != null)
            {
                return t3.RRType;
            }

            throw new ArgumentException("Invalid Robot Raconteur structure");
        }

    }
}
