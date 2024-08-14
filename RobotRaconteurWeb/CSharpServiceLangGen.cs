// Copyright 2011-2024 Wason Technology, LLC
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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

#pragma warning disable 1591

namespace RobotRaconteurWeb
{
    public class CSharpServiceLangGen
    {
        public ServiceDefinition def;

        public class convert_type_result
        {
            public string name;
            public string cs_type;
            public string cs_arr_type;
        }

        public static convert_type_result convert_type(TypeDefinition tdef)
        {

            var o = new convert_type_result();
            DataTypes t = tdef.Type;
            o.name = FixName(tdef.Name);
            o.cs_arr_type = tdef.ArrayType == DataTypes_ArrayTypes.array ? "[]" : "";
            switch (t)
            {
                case DataTypes.void_t:
                    o.cs_type = "void";
                    break;
                case DataTypes.double_t:
                    o.cs_type = "double";
                    break;
                case DataTypes.single_t:
                    o.cs_type = "float";
                    break;
                case DataTypes.int8_t:
                    o.cs_type = "sbyte";
                    break;
                case DataTypes.uint8_t:
                    o.cs_type = "byte";
                    break;
                case DataTypes.int16_t:
                    o.cs_type = "short";
                    break;
                case DataTypes.uint16_t:
                    o.cs_type = "ushort";
                    break;
                case DataTypes.int32_t:
                    o.cs_type = "int";
                    break;
                case DataTypes.uint32_t:
                    o.cs_type = "uint";
                    break;
                case DataTypes.int64_t:
                    o.cs_type = "long";
                    break;
                case DataTypes.uint64_t:
                    o.cs_type = "ulong";
                    break;
                case DataTypes.string_t:
                    o.cs_type = "string";
                    break;
                case DataTypes.cdouble_t:
                    o.cs_type = "CDouble";
                    break;
                case DataTypes.csingle_t:
                    o.cs_type = "CSingle";
                    break;
                case DataTypes.bool_t:
                    o.cs_type = "bool";
                    break;
                case DataTypes.namedtype_t:
                case DataTypes.object_t:
                    o.cs_type = FixName(tdef.TypeString);
                    break;
                case DataTypes.varvalue_t:
                    o.cs_type = "object";
                    break;
                default:
                    throw new DataTypeException("Unknown data type");
            }

            var nt = tdef.ResolveNamedType_cache;

            if (tdef.ArrayType == DataTypes_ArrayTypes.multidimarray)
            {
                if (DataTypeUtil.IsNumber(tdef.Type))
                {
                    o.cs_type = "MultiDimArray";
                    o.cs_arr_type = "";
                }
                else if (tdef.Type == DataTypes.namedtype_t)
                {
                    if (nt == null) throw new DataTypeException("Data type not resolved");
                    switch (nt.RRDataType)
                    {
                        case DataTypes.pod_t:
                            o.cs_type = "PodMultiDimArray";
                            o.cs_arr_type = "";
                            break;
                        case DataTypes.namedarray_t:
                            o.cs_type = "NamedMultiDimArray";
                            o.cs_arr_type = "";
                            break;
                        default:
                            throw new ArgumentException("Invalid multidimarray type");
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid multidimarray type");
                }
            }

            switch (tdef.ContainerType)
            {
                case DataTypes_ContainerTypes.none:
                    break;
                case DataTypes_ContainerTypes.list:
                    o.cs_type = "List<" + o.cs_type + o.cs_arr_type + ">";
                    o.cs_arr_type = "";
                    break;
                case DataTypes_ContainerTypes.map_int32:
                    o.cs_type = "Dictionary<int," + o.cs_type + o.cs_arr_type + ">";
                    o.cs_arr_type = "";
                    break;
                case DataTypes_ContainerTypes.map_string:
                    o.cs_type = "Dictionary<string," + o.cs_type + o.cs_arr_type + ">";
                    o.cs_arr_type = "";
                    break;
                default:
                    throw new DataTypeException("Invalid container type");
            }

            return o;
        }

        public class convert_generator_result
        {
            public string generator_csharp_type;
            public string generator_csharp_base_type;
            public string generator_csharp_template_params;
            public List<TypeDefinition> params_;
        }

        public static convert_generator_result convert_generator(FunctionDefinition f)
        {
            if (!f.IsGenerator)
            {
                throw new InternalErrorException("");
            }

            var o = new convert_generator_result();

            bool return_generator = f.ReturnType.ContainerType == DataTypes_ContainerTypes.generator;
            bool param_generator = (f.Parameters.Count != 0) && f.Parameters.Last().ContainerType == DataTypes_ContainerTypes.generator;

            if (return_generator && param_generator)
            {
                var r_type = f.ReturnType.Clone();
                r_type.RemoveContainers();
                var t = convert_type(r_type);
                var p_type = f.Parameters.Last().Clone();
                p_type.RemoveContainers();
                var t2 = convert_type(p_type);
                o.params_ = f.Parameters.Take(f.Parameters.Count - 1).ToList();
                o.generator_csharp_base_type = "Generator1";
                o.generator_csharp_template_params = t.cs_type + t.cs_arr_type + "," + t2.cs_type + t2.cs_arr_type;
                o.generator_csharp_type = o.generator_csharp_base_type + "<" + o.generator_csharp_template_params + ">";
                return o;
            }

            if (param_generator)
            {
                var p_type = f.Parameters.Last().Clone();
                p_type.RemoveContainers();
                var t2 = convert_type(p_type);
                o.params_ = f.Parameters.Take(f.Parameters.Count - 1).ToList();
                o.generator_csharp_base_type = "Generator3";
                o.generator_csharp_template_params = t2.cs_type + t2.cs_arr_type;
                o.generator_csharp_type = o.generator_csharp_base_type + "<" + o.generator_csharp_template_params + ">";
                return o;
            }
            else
            {
                var r_type = f.ReturnType.Clone();
                r_type.RemoveContainers();
                var t = convert_type(r_type);
                o.params_ = f.Parameters.ToList();
                o.generator_csharp_base_type = "Generator2";
                o.generator_csharp_template_params = t.cs_type + t.cs_arr_type;
                o.generator_csharp_type = o.generator_csharp_base_type + "<" + o.generator_csharp_template_params + ">";
                return o;
            }

        }

        public static string str_pack_parameters(List<TypeDefinition> l, bool inclass)
        {
            string[] o = new string[l.Count];

            for (int i = 0; i < o.Length; i++)
            {
                var t = convert_type(l[i]);
                if (inclass)
                {
                    o[i] = String.Format("{0}{1} {2}", t.cs_type, t.cs_arr_type, t.name);
                }
                else
                {
                    o[i] = t.name;
                }
            }

            return String.Join(", ", o);

        }

        public static string str_pack_delegate(List<TypeDefinition> parameters, TypeDefinition rettype, bool isasync)
        {
            if (rettype == null || rettype.Type == DataTypes.void_t)
            {
                if (parameters.Count == 0)
                {
                    return isasync ? "Func<CancellationToken, Task>" : "Action";
                }
                else
                {
                    string[] paramtypes = new string[parameters.Count];
                    for (int i = 0; i < paramtypes.Length; i++)
                    {
                        var t = convert_type(parameters[i]);
                        paramtypes[i] = String.Format("{0}{1}", t.cs_type, t.cs_arr_type);

                    }
                    if (isasync)
                    {
                        return "Func<" + String.Join(", ", paramtypes) + ", CancellationToken, Task>";
                    }
                    else
                    {
                        return "Action<" + String.Join(", ", paramtypes) + ">";
                    }
                }
            }

            else
            {
                string[] paramtypes = new string[parameters.Count + 1];

                var t1 = convert_type(rettype);
                paramtypes[paramtypes.Length - 1] = String.Format("{0}{1}", t1.cs_type, t1.cs_arr_type);
                for (int i = 0; i < parameters.Count; i++)
                {
                    var t = convert_type(parameters[i]);
                    paramtypes[i] = String.Format("{0}{1}", t.cs_type, t.cs_arr_type);

                }
                if (isasync)
                {
                    return "Func<" + String.Join(", ", paramtypes.Take(paramtypes.Length - 1).Concat(new string[] { "CancellationToken" })) + ", Task<" + paramtypes.Last() + ">>";
                }
                else
                {
                    return "Func<" + String.Join(", ", paramtypes) + ">";
                }

            }

        }

        public static string str_pack_async_delegate(List<TypeDefinition> parameters, TypeDefinition rettype)
        {
            if (rettype == null || rettype.Type == DataTypes.void_t)
            {
                if (parameters.Count == 0)
                {
                    return "Func<CancellationToken, Task>";
                }
                else
                {
                    string[] paramtypes = new string[parameters.Count];
                    for (int i = 0; i < paramtypes.Length; i++)
                    {
                        var t = convert_type(parameters[i]);
                        paramtypes[i] = String.Format("{0}{1}", t.cs_type, t.cs_arr_type);

                    }
                    return "Func<" + String.Join(", ", paramtypes) + ", CancellationToken, Task>";
                }
            }

            else
            {
                string[] paramtypes = new string[parameters.Count + 2];

                var t1 = convert_type(rettype);
                paramtypes[paramtypes.Length - 1] = String.Format("Task<{1}{2}>", t1);
                paramtypes[paramtypes.Length - 2] = "CancellationToken";
                for (int i = 0; i < parameters.Count; i++)
                {
                    var t = convert_type(parameters[i]);
                    paramtypes[i] = String.Format("{0}{1}", t.cs_type, t.cs_arr_type);

                }
                return "Func<" + String.Join(", ", paramtypes) + ">";

            }

        }

        public static string VerifyArrayLength(TypeDefinition t, string varname)
        {
            if (t.ArrayType == DataTypes_ArrayTypes.array && t.ArrayLength[0] != 0)
            {
                return "DataTypeUtil.VerifyArrayLength(" + varname + ", " + t.ArrayLength[0] + ", " + (t.ArrayVarLength ? "true" : "false") + ")";
            }
            if (t.ArrayType == DataTypes_ArrayTypes.multidimarray && t.ArrayLength.Count != 0 && !t.ArrayVarLength)
            {
                int n_elems = t.ArrayLength.Aggregate(1, (x, y) => x * y);
                return "DataTypeUtil.VerifyArrayLength(" + varname + "," + n_elems.ToString() + ",new uint[] {" + string.Join(", ", t.ArrayLength.Select(x => x.ToString())) + "})";
            }
            return varname;
        }

        public static string str_pack_message_element(string elementname, string varname, TypeDefinition t)
        {
            var t1 = new TypeDefinition();
            t.CopyTo(ref t1);
            t1.RemoveContainers();
            var tt1 = convert_type(t1);

            switch (t.ContainerType)
            {
                case DataTypes_ContainerTypes.none:
                    {
                        if (DataTypeUtil.IsNumber(t.Type))
                        {
                            switch (t.ArrayType)
                            {
                                case DataTypes_ArrayTypes.none:
                                    {
                                        var ts = convert_type(t);
                                        return "MessageElementUtil.PackScalar<" + ts.cs_type + ">(\"" + elementname + "\"," + varname + ")";
                                    }
                                case DataTypes_ArrayTypes.array:
                                    {
                                        var ts = convert_type(t);
                                        return "MessageElementUtil.PackArray<" + ts.cs_type + ">(\"" + elementname + "\"," + VerifyArrayLength(t, varname) + ")";
                                    }
                                case DataTypes_ArrayTypes.multidimarray:
                                    {
                                        var ts = convert_type(t);
                                        return "MessageElementUtil.PackMultiDimArray(rr_node, \"" + elementname + "\",(MultiDimArray)" + VerifyArrayLength(t, varname) + ")";
                                    }
                                default:
                                    throw new DataTypeException("Invalid array type");
                            }
                        }
                        else if (t.Type == DataTypes.string_t)
                        {
                            return "MessageElementUtil.PackString(\"" + elementname + "\"," + varname + ")";
                        }
                        else if (t.Type == DataTypes.varvalue_t)
                        {
                            return "MessageElementUtil.PackVarType(rr_node, rr_context, \"" + elementname + "\"," + varname + ")";
                        }
                        else if (t.Type == DataTypes.namedtype_t)
                        {
                            var nt = t.ResolveNamedType();
                            switch (nt.RRDataType)
                            {
                                case DataTypes.structure_t:
                                    return "MessageElementUtil.PackStructure(rr_node, rr_context, \"" + elementname + "\"," + varname + ")";
                                case DataTypes.enum_t:
                                    return "MessageElementUtil.PackEnum<" + FixName(t.TypeString) + ">(\"" + elementname + "\"," + varname + ")";
                                case DataTypes.pod_t:
                                    switch (t.ArrayType)
                                    {
                                        case DataTypes_ArrayTypes.none:
                                            return "MessageElementUtil.PackPodToArray<" + FixName(t.TypeString) + ">(rr_node, rr_context, \"" + elementname + "\",ref " + varname + ")";
                                        case DataTypes_ArrayTypes.array:
                                            return "MessageElementUtil.PackPodArray<" + FixName(t.TypeString) + ">(rr_node, rr_context, \"" + elementname + "\"," + VerifyArrayLength(t, varname) + ")";
                                        case DataTypes_ArrayTypes.multidimarray:
                                            return "MessageElementUtil.PackPodMultiDimArray<" + FixName(t.TypeString) + ">(rr_node, rr_context, \"" + elementname + "\"," + VerifyArrayLength(t, varname) + ")";
                                        default:
                                            throw new DataTypeException("Invalid array type");
                                    }
                                case DataTypes.namedarray_t:
                                    switch (t.ArrayType)
                                    {
                                        case DataTypes_ArrayTypes.none:
                                            return "MessageElementUtil.PackNamedArrayToArray<" + FixName(t.TypeString) + ">(rr_node, rr_context, \"" + elementname + "\",ref " + varname + ")";
                                        case DataTypes_ArrayTypes.array:
                                            return "MessageElementUtil.PackNamedArray<" + FixName(t.TypeString) + ">(rr_node, rr_context, \"" + elementname + "\"," + VerifyArrayLength(t, varname) + ")";
                                        case DataTypes_ArrayTypes.multidimarray:
                                            return "MessageElementUtil.PackNamedMultiDimArray<" + FixName(t.TypeString) + ">(rr_node, rr_context, \"" + elementname + "\"," + VerifyArrayLength(t, varname) + ")";
                                        default:
                                            throw new DataTypeException("Invalid array type");
                                    }
                                default:
                                    throw new DataTypeException("Unknown named type id");
                            }
                        }
                        else
                        {
                            throw new DataTypeException("Unknown type");
                        }
                    }
                case DataTypes_ContainerTypes.list:
                    return "MessageElementUtil.PackListType<" + tt1.cs_type + tt1.cs_arr_type + ">(rr_node, rr_context, \"" + elementname + "\"," + VerifyArrayLength(t, varname) + ")";
                case DataTypes_ContainerTypes.map_int32:
                    return "MessageElementUtil.PackMapType<int," + tt1.cs_type + tt1.cs_arr_type + ">(rr_node, rr_context, \"" + elementname + "\"," + VerifyArrayLength(t, varname) + ")";
                case DataTypes_ContainerTypes.map_string:
                    return "MessageElementUtil.PackMapType<string," + tt1.cs_type + tt1.cs_arr_type + ">(rr_node, rr_context, \"" + elementname + "\"," + VerifyArrayLength(t, varname) + ")";
                default:
                    throw new DataTypeException("Invalid container type");
            }

        }

        public static string str_unpack_message_element(string varname, TypeDefinition t)
        {

            var t1 = new TypeDefinition();
            t.CopyTo(ref t1);
            t1.RemoveContainers();
            convert_type_result tt = convert_type(t1);
            if (t1.ArrayType == DataTypes_ArrayTypes.array)
                tt.cs_arr_type = "[]";
            string structunpackstring = "";

            convert_type_result tt1 = convert_type(t1);

            if (DataTypeUtil.IsNumber(t.Type))
            {
                switch (t.ArrayType)
                {
                    case DataTypes_ArrayTypes.none:
                        structunpackstring = "(MessageElementUtil.UnpackScalar<" + tt.cs_type + tt.cs_arr_type + ">(" + varname + "))";
                        break;
                    case DataTypes_ArrayTypes.array:
                        structunpackstring = VerifyArrayLength(t, "MessageElementUtil.UnpackArray<" + tt.cs_type + ">(" + varname + ")");
                        break;
                    case DataTypes_ArrayTypes.multidimarray:
                        structunpackstring = VerifyArrayLength(t, "MessageElementUtil.UnpackMultiDimArray(rr_node, " + varname + ")");
                        break;
                    default:
                        throw new DataTypeException("Invalid array type");
                }
            }
            else if (t.Type == DataTypes.string_t)
            {
                structunpackstring = "MessageElementUtil.UnpackString(" + varname + ")";
            }
            else if (t.Type == DataTypes.namedtype_t)
            {
                var nt = t.ResolveNamedType();
                switch (nt.RRDataType)
                {
                    case DataTypes.structure_t:
                        structunpackstring = "MessageElementUtil.UnpackStructure<" + tt.cs_type + ">(rr_node, rr_context, " + varname + ")";
                        break;
                    case DataTypes.enum_t:
                        structunpackstring = "MessageElementUtil.UnpackEnum<" + tt.cs_type + ">(" + varname + ")";
                        break;
                    case DataTypes.pod_t:
                        switch (t.ArrayType)
                        {
                            case DataTypes_ArrayTypes.none:
                                {
                                    structunpackstring = "MessageElementUtil.UnpackPodFromArray<" + FixName(t.TypeString) + ">(rr_node, rr_context, " + varname + ")";
                                    break;
                                }
                            case DataTypes_ArrayTypes.array:
                                {
                                    structunpackstring = VerifyArrayLength(t, "MessageElementUtil.UnpackPodArray<" + FixName(t.TypeString) + ">(rr_node, rr_context, " + varname + ")");
                                    break;
                                }
                            case DataTypes_ArrayTypes.multidimarray:
                                {
                                    structunpackstring = VerifyArrayLength(t, "MessageElementUtil.UnpackPodMultiDimArray<" + FixName(t.TypeString) + ">(rr_node, rr_context, " + varname + ")");
                                    break;
                                }
                            default:
                                throw new DataTypeException("Invalid array type");
                        }
                        break;
                    case DataTypes.namedarray_t:
                        switch (t.ArrayType)
                        {
                            case DataTypes_ArrayTypes.none:
                                {
                                    structunpackstring = "MessageElementUtil.UnpackNamedArrayFromArray<" + FixName(t.TypeString) + ">(rr_node, rr_context, " + varname + ")";
                                    break;
                                }
                            case DataTypes_ArrayTypes.array:
                                {
                                    structunpackstring = VerifyArrayLength(t, "MessageElementUtil.UnpackNamedArray<" + FixName(t.TypeString) + ">(rr_node, rr_context, " + varname + ")");
                                    break;
                                }
                            case DataTypes_ArrayTypes.multidimarray:
                                {
                                    structunpackstring = VerifyArrayLength(t, "MessageElementUtil.UnpackNamedMultiDimArray<" + FixName(t.TypeString) + ">(rr_node, rr_context, " + varname + ")");
                                    break;
                                }
                            default:
                                throw new DataTypeException("Invalid array type");
                        }
                        break;
                    default:
                        throw new DataTypeException("Unknown named type id");
                }
            }

            else if (t.Type == DataTypes.varvalue_t)
            {
                structunpackstring = "MessageElementUtil.UnpackVarType(rr_node, rr_context, " + varname + ")";
            }
            else
            {
                throw new ArgumentException("Unknown type");
            }

            switch (t.ContainerType)
            {
                case DataTypes_ContainerTypes.none:
                    return structunpackstring;
                case DataTypes_ContainerTypes.list:
                    return VerifyArrayLength(t, "MessageElementUtil.UnpackList<" + tt.cs_type + tt.cs_arr_type + ">(rr_node, rr_context, " + varname + ")");
                case DataTypes_ContainerTypes.map_int32:
                    return VerifyArrayLength(t, "MessageElementUtil.UnpackMap<int," + tt.cs_type + tt.cs_arr_type + ">(rr_node, rr_context, " + varname + ")");
                case DataTypes_ContainerTypes.map_string:
                    return VerifyArrayLength(t, "MessageElementUtil.UnpackMap<string," + tt.cs_type + tt.cs_arr_type + ">(rr_node, rr_context, " + varname + ")");
                default:
                    throw new DataTypeException("Invalid container type");
            }
        }

        public static TypeDefinition RemoveMultiDimArray(TypeDefinition t)
        {
            var t2 = new TypeDefinition();
            t.CopyTo(ref t2);

            if (t.ArrayType != DataTypes_ArrayTypes.multidimarray)
                return t2;

            t2.ArrayType = DataTypes_ArrayTypes.array;
            t2.ArrayLength.Clear();
            t2.ArrayLength.Add(t.ArrayLength.Aggregate(1, (x, y) => x * y));
            return t2;
        }

        public static string FixName(string str)
        {
            if (str == null) return null;
            if (str == "") return "";
            if (str.Contains('.'))
            {
                return String.Join(".", str.Split(new char[] { '.' }).Select(x => FixName(x)));
            }


            var res_str = new string[] {"abstract","as","async","await","base","bool","break","byte","case","catch","char","checked","class",
            "const","continue","decimal","default","delegate","do","double","dynamic","else","enum","event","explicit",
            "extern","false","finally","fixed","float","for","foreach","goto","if","implicit","in","int","interface",
            "internal","is","lock","long","namespace","new","null","object","operator","out","override","params",
            "private","protected","public","readonly","ref","return","sbyte","sealed","short","sizeof","stackalloc",
            "static","string","struct","switch","this","throw","true","try","typeof","uint","ulong","unchecked",
            "unsafe","ushort","using","virtual","void","volatile","while","value"};


            if (res_str.Contains(str)) return str + "_";

            return str;
        }

        static IEnumerable<T> MemberIter<T>(ServiceEntryDefinition e) where T : MemberDefinition
        {
            foreach (var m in e.Members)
            {
                T m2 = m.Value as T;
                if (m2 != null)
                    yield return m2;
            }
        }

        public static void GenerateStructure(ServiceEntryDefinition e, TextWriter w2)
        {
            w2.WriteLine("[RobotRaconteurServiceStruct(\"" + e.ServiceDefinition.Name + "." + e.Name + "\")]");
            w2.WriteLine("public class " + FixName(e.Name));
            w2.WriteLine("{");

            foreach (var m in MemberIter<PropertyDefinition>(e))
            {

                convert_type_result t = convert_type(m.Type);
                t.name = FixName(m.Name);
                w2.WriteLine("    public " + t.cs_type + t.cs_arr_type + " " + t.name + ";");
            }

            w2.WriteLine("}");
            w2.WriteLine();
        }

        public static void GeneratePod(ServiceEntryDefinition e, TextWriter w2)
        {

            if (e.EntryType == DataTypes.namedarray_t)
            {
                var t4 = ServiceDefinitionUtil.GetNamedArrayElementTypeAndCount(e);
                var t5 = new TypeDefinition();
                t5.Type = t4.Item1;
                convert_type_result t6 = convert_type(t5);
                w2.WriteLine("[RobotRaconteurNamedArrayElementTypeAndCount(\"" + e.ServiceDefinition.Name + "." + e.Name + "\",typeof(" + t6.cs_type + "), " + t4.Item2 + ")]");
            }
            else
            {
                w2.WriteLine("[RobotRaconteurServicePod(\"" + e.ServiceDefinition.Name + "." + e.Name + "\")]");
            }

            w2.WriteLine("public struct " + FixName(e.Name));
            w2.WriteLine("{");

            foreach (var m in MemberIter<PropertyDefinition>(e))
            {
                TypeDefinition t2 = RemoveMultiDimArray(m.Type);
                convert_type_result t = convert_type(t2);
                t.name = FixName(m.Name);
                w2.WriteLine("    public " + t.cs_type + t.cs_arr_type + " " + t.name + ";");
            }


            if (e.EntryType == DataTypes.namedarray_t)
            {
                var t4 = ServiceDefinitionUtil.GetNamedArrayElementTypeAndCount(e);
                var t5 = new TypeDefinition();
                t5.Type = t4.Item1;
                convert_type_result t6 = convert_type(t5);

                w2.WriteLine("    public " + t6.cs_type + "[] GetNumericArray()");
                w2.WriteLine("    {");
                w2.WriteLine("    var a=new ArraySegment<" + t6.cs_type + ">(new " + t6.cs_type + "[" + t4.Item2.ToString() + "]);");
                w2.WriteLine("    GetNumericArray(ref a);");
                w2.WriteLine("    return a.Array;");
                w2.WriteLine("    }");

                w2.WriteLine("    public void GetNumericArray(ref ArraySegment<" + t6.cs_type + "> a)");
                w2.WriteLine("    {");
                {
                    w2.WriteLine("    if(a.Count < " + t4.Item2 + ") throw new ArgumentException(\"ArraySegment invalid length\");");
                    int i = 0;
                    foreach (var m in MemberIter<PropertyDefinition>(e))
                    {
                        var t7 = RemoveMultiDimArray(m.Type);
                        convert_type_result t8 = convert_type(t7);
                        t8.name = FixName(m.Name);
                        if (DataTypeUtil.IsNumber(m.Type.Type))
                        {
                            if (m.Type.ArrayType == DataTypes_ArrayTypes.none)
                            {
                                w2.WriteLine("    a.Array[a.Offset + " + i + "] = this." + t8.name + ";");
                                i++;
                            }
                            else
                            {
                                w2.WriteLine("    Array.Copy(this." + t8.name + ", 0, a.Array, a.Offset + " + i + ", " + t7.ArrayLength[0] + ");");
                                i += t7.ArrayLength[0];
                            }
                        }
                        else
                        {
                            var e2 = (ServiceEntryDefinition)(m.Type.ResolveNamedType());
                            var t9 = ServiceDefinitionUtil.GetNamedArrayElementTypeAndCount(e2);
                            int e2_count = m.Type.ArrayType == DataTypes_ArrayTypes.none ? 1 : t7.ArrayLength[0];

                            w2.WriteLine("    var a" + i + " = new ArraySegment<" + t6.cs_type + ">(a.Array, a.Offset + " + i + ", " + t9.Item2 * e2_count + ");");
                            w2.WriteLine("    this." + t8.name + ".GetNumericArray(ref a" + i + ");");
                            i += t9.Item2 * e2_count;

                        }
                        //w2 << "    public " + t8.cs_type + t8.cs_arr_type + " " + t8.name + ";");
                    }

                }
                w2.WriteLine("    }");

                w2.WriteLine("    public void AssignFromNumericArray(ref ArraySegment<" + t6.cs_type + "> a)");
                w2.WriteLine("    {");
                {
                    w2.WriteLine("    if(a.Count < " + t4.Item2 + ") throw new ArgumentException(\"ArraySegment invalid length\");");
                    int i = 0;
                    foreach (var m in MemberIter<PropertyDefinition>(e))
                    {

                        TypeDefinition t7 = RemoveMultiDimArray(m.Type);
                        convert_type_result t8 = convert_type(t7);
                        t8.name = FixName(m.Name);
                        if (DataTypeUtil.IsNumber(m.Type.Type))
                        {
                            if (m.Type.ArrayType == DataTypes_ArrayTypes.none)
                            {
                                w2.WriteLine("    this." + t8.name + " = a.Array[a.Offset + " + i + "]" + ";");
                                i++;
                            }
                            else
                            {
                                w2.WriteLine("    Array.Copy(a.Array, a.Offset + " + i + ", this." + t8.name + ", 0, " + t7.ArrayLength[0] + ");");
                                i += t7.ArrayLength[0];
                            }
                        }
                        else
                        {
                            var e2 = (ServiceEntryDefinition)(m.Type.ResolveNamedType());
                            var t9 = ServiceDefinitionUtil.GetNamedArrayElementTypeAndCount(e2);
                            int e2_count = m.Type.ArrayType == DataTypes_ArrayTypes.none ? 1 : t7.ArrayLength[0];

                            w2.WriteLine("    var a" + i + " = new ArraySegment<" + t6.cs_type + ">(a.Array, a.Offset + " + i + ", " + t9.Item2 * e2_count + ");");
                            w2.WriteLine("    this." + t8.name + ".AssignFromNumericArray(ref a" + i + ");");
                            i += t9.Item2 * e2_count;

                        }
                        //w2 << "    public " + t8.cs_type + t8.cs_arr_type + " " + t8.name + ";");
                    }

                    w2.WriteLine("    }");
                }
            }

            w2.WriteLine("}");
            w2.WriteLine();
        }

        public static void GenerateNamedArrayExtensions(ServiceEntryDefinition e, TextWriter w2)
        {

            var t1 = ServiceDefinitionUtil.GetNamedArrayElementTypeAndCount(e);
            var t2 = new TypeDefinition();
            t2.Type = t1.Item1;
            convert_type_result t3 = convert_type(t2);

            w2.WriteLine("    public static " + t3.cs_type + "[] GetNumericArray(this " + FixName(e.Name) + "[] s)");
            w2.WriteLine("    {");
            w2.WriteLine("    var a=new ArraySegment<" + t3.cs_type + ">(new " + t3.cs_type + "[" + t1.Item2.ToString() + " * s.Length]);");
            w2.WriteLine("    s.GetNumericArray(ref a);");
            w2.WriteLine("    return a.Array;");
            w2.WriteLine("    }");

            w2.WriteLine("    public static void GetNumericArray(this " + FixName(e.Name) + "[] s, ref ArraySegment<" + t3.cs_type + "> a)");
            w2.WriteLine("    {");
            w2.WriteLine("    if(a.Count < " + t1.Item2 + " * s.Length) throw new ArgumentException(\"ArraySegment invalid length\");");
            w2.WriteLine("    for (int i=0; i<s.Length; i++)");
            w2.WriteLine("    {");
            w2.WriteLine("    var a1 = new ArraySegment<" + t3.cs_type + ">(a.Array, a.Offset + " + t1.Item2 + "*i," + t1.Item2 + ");");
            w2.WriteLine("    s[i].GetNumericArray(ref a1);");
            w2.WriteLine("    }");
            w2.WriteLine("    }");

            w2.WriteLine("    public static void AssignFromNumericArray(this " + FixName(e.Name) + "[] s, ref ArraySegment<" + t3.cs_type + "> a)");
            w2.WriteLine("    {");
            w2.WriteLine("    if(a.Count < " + t1.Item2 + " * s.Length) throw new ArgumentException(\"ArraySegment invalid length\");");

            w2.WriteLine("    for (int i=0; i<s.Length; i++)");
            w2.WriteLine("    {");
            w2.WriteLine("    var a1 = new ArraySegment<" + t3.cs_type + ">(a.Array, a.Offset + " + t1.Item2 + "*i," + t1.Item2 + ");");
            w2.WriteLine("    s[i].AssignFromNumericArray(ref a1);");
            w2.WriteLine("    }");

            w2.WriteLine("    }");
        }

        public static bool GetObjRefIndType(ObjRefDefinition m, out string indtype)
        {
            switch (m.ArrayType)
            {
                case DataTypes_ArrayTypes.none:
                    switch (m.ContainerType)
                    {
                        case DataTypes_ContainerTypes.none:
                            indtype = "";
                            return false;
                        case DataTypes_ContainerTypes.map_int32:
                            indtype = "int";
                            return true;
                        case DataTypes_ContainerTypes.map_string:
                            indtype = "string";
                            return true;
                        default:
                            throw new DataTypeException("Unknown object container type");
                    }
                case DataTypes_ArrayTypes.array:
                    {
                        if (m.ContainerType != DataTypes_ContainerTypes.none)
                        {
                            throw new DataTypeException("Invalid object container type");
                        }
                        indtype = "int";
                        return true;
                    }
                default:
                    throw new DataTypeException("Invalid object array type");
            }
        }

        public static void GenerateInterface(ServiceEntryDefinition e, TextWriter w2)
        {
            w2.WriteLine("[RobotRaconteurServiceObjectInterface(\"" + e.ServiceDefinition.Name + "." + e.Name + "\")]");

            var implements2 = new List<string>();

            foreach (var ee in e.Implements)
            {
                implements2.Add(FixName(ee));
            }

            string implements = string.Join(", ", implements2);
            if (e.Implements.Count > 0) implements = " : " + implements;

            w2.WriteLine("public interface " + FixName(e.Name) + implements);
            w2.WriteLine("{");

            foreach (var m in MemberIter<PropertyDefinition>(e))
            {
                convert_type_result t = convert_type(m.Type);
                t.name = FixName(m.Name);
                w2.WriteLine(String.Format("    Task<{0}{1}> get_{2}(CancellationToken cancel=default(CancellationToken));", t.cs_type, t.cs_arr_type, t.name));
                w2.WriteLine(String.Format("    Task set_{2}({0}{1} value, CancellationToken cancel=default(CancellationToken));", t.cs_type, t.cs_arr_type, t.name));
            }


            foreach (var m in MemberIter<FunctionDefinition>(e))
            {

                if (!m.IsGenerator)
                {
                    convert_type_result t = convert_type(m.ReturnType);
                    string p = str_pack_parameters(m.Parameters, true);
                    if (p.Length == 0)
                    {
                        p = "CancellationToken rr_cancel=default(CancellationToken)";
                    }
                    else
                    {
                        p = String.Join(",", new string[] { p, "CancellationToken rr_cancel=default(CancellationToken)" });
                    }
                    if (m.ReturnType.Type == DataTypes.void_t)
                    {
                        w2.WriteLine("    Task " + FixName(m.Name) + "(" + p + ");");
                    }
                    else
                    {
                        w2.WriteLine("    Task<" + t.cs_type + t.cs_arr_type + "> " + FixName(m.Name) + "(" + p + ");");
                    }
                }
                else
                {
                    convert_generator_result t = convert_generator(m);
                    string p = str_pack_parameters(t.params_, true);
                    if (p.Length == 0)
                    {
                        p = "CancellationToken rr_cancel=default(CancellationToken)";
                    }
                    else
                    {
                        p = String.Join(",", new string[] { p, "CancellationToken rr_cancel=default(CancellationToken)" });
                    }
                    w2.WriteLine("    Task<" + t.generator_csharp_type + "> " + FixName(m.Name) + "(" + p + ");");
                }
            }


            foreach (var m in MemberIter<EventDefinition>(e))
            {
                w2.WriteLine("    event " + str_pack_delegate(m.Parameters, null, false) + " " + FixName(m.Name) + ";");
            }


            foreach (var m in MemberIter<ObjRefDefinition>(e))
            {

                string objtype = FixName(m.ObjectType);
                if (objtype == "varobject") objtype = "object";
                string indtype;
                if (GetObjRefIndType(m, out indtype))
                {
                    w2.WriteLine("    Task<" + objtype + "> get_" + FixName(m.Name) + "(" + indtype + " ind, CancellationToken rr_cancel=default(CancellationToken));");
                }
                else
                {
                    w2.WriteLine("    Task<" + objtype + "> get_" + FixName(m.Name) + "(CancellationToken rr_cancel=default(CancellationToken));");
                }
            }


            foreach (var m in MemberIter<PipeDefinition>(e))
            {

                convert_type_result t = convert_type(m.Type);
                w2.WriteLine("    Pipe<" + t.cs_type + t.cs_arr_type + "> " + FixName(m.Name) + "{ get; set; }");

            }


            foreach (var m in MemberIter<CallbackDefinition>(e))
            {
                w2.WriteLine("    Callback<" + str_pack_delegate(m.Parameters, m.ReturnType, true) + "> " + FixName(m.Name) + " {get; set;}");
            }


            foreach (var m in MemberIter<WireDefinition>(e))
            {
                convert_type_result t = convert_type(m.Type);
                w2.WriteLine("    Wire<" + t.cs_type + t.cs_arr_type + "> " + FixName(m.Name) + " { get; set; }");
            }


            foreach (var m in MemberIter<MemoryDefinition>(e))
            {

                var t2 = new TypeDefinition();
                m.Type.CopyTo(ref t2);
                t2.RemoveArray();
                convert_type_result t = convert_type(t2);
                string c = "";
                if (!DataTypeUtil.IsNumber(m.Type.Type))
                {
                    DataTypes entry_type = m.Type.ResolveNamedType().RRDataType;
                    if (entry_type != DataTypes.namedarray_t)
                    {
                        c = "Pod";
                    }
                    else
                    {
                        c = "Named";
                    }
                }
                switch (m.Type.ArrayType)
                {
                    case DataTypes_ArrayTypes.array:
                        w2.WriteLine("    " + c + "ArrayMemory<" + t.cs_type + "> " + FixName(m.Name) + " { get; }");
                        break;
                    case DataTypes_ArrayTypes.multidimarray:
                        w2.WriteLine("    " + c + "MultiDimArrayMemory<" + t.cs_type + "> " + FixName(m.Name) + " { get; }");
                        break;
                    default:
                        throw new DataTypeException("Invalid memory definition");
                }
            }
            w2.WriteLine("}");
            w2.WriteLine();

        }

        public static void GenerateInterfaceFile(ServiceDefinition d, TextWriter w2, bool onefile)
        {
            if (!onefile)
            {
                w2.WriteLine("//This file is automatically generated. DO NOT EDIT!");
                w2.WriteLine("using System;");
                w2.WriteLine("using RobotRaconteurWeb;");
                w2.WriteLine("using RobotRaconteurWeb.Extensions;");
                w2.WriteLine("using System.Collections.Generic;");
                w2.WriteLine("using System.Threading;");
                w2.WriteLine("using System.Threading.Tasks;");
                w2.WriteLine();
                w2.WriteLine("#pragma warning disable 0108");
            }
            w2.WriteLine();
            w2.WriteLine("namespace " + FixName(d.Name));
            w2.WriteLine("{");
            foreach (var e in d.Structures)
            {
                GenerateStructure(e.Value, w2);
            }
            foreach (var e in d.NamedArrays)
            {
                GeneratePod(e.Value, w2);
            }
            foreach (var e in d.Pods)
            {
                GeneratePod(e.Value, w2);
            }
            foreach (var e in d.Objects)
            {
                GenerateInterface(e.Value, w2);
            }

            GenerateConstants(d, w2);

            foreach (var e in d.Exceptions)
            {
                w2.WriteLine("public class " + FixName(e) + " : RobotRaconteurRemoteException");
                w2.WriteLine("{");

                w2.WriteLine("    public " + FixName(e) + "(string message) : base(\"" + d.Name + "." + e + "\",message) {}");
                w2.WriteLine("};");
            }

            w2.WriteLine("}");
        }

        public static void GenerateStubSkelFile(ServiceDefinition d, string defstring, TextWriter w2, bool onefile)
        {
            if (!onefile)
            {
                w2.WriteLine("//This file is automatically generated. DO NOT EDIT!");
                w2.WriteLine("using System;");
                w2.WriteLine("using RobotRaconteurWeb;");
                w2.WriteLine("using RobotRaconteurWeb.Extensions;");
                w2.WriteLine("using System.Collections.Generic;");
                w2.WriteLine("using System.Threading;");
                w2.WriteLine("using System.Threading.Tasks;");
                w2.WriteLine("#pragma warning disable 0108");
            }
            w2.WriteLine();
            w2.WriteLine("namespace " + FixName(d.Name));
            w2.WriteLine("{");
            GenerateServiceFactory(d, defstring, w2);
            w2.WriteLine();
            foreach (var e in d.Structures)
            {
                GenerateStructureStub(e.Value, w2);
                w2.WriteLine();
            }
            foreach (var e in d.Pods)
            {
                GeneratePodStub(e.Value, w2);
                w2.WriteLine();
            }
            foreach (var e in d.NamedArrays)
            {
                GenerateNamedArrayStub(e.Value, w2);
                w2.WriteLine();
            }
            foreach (var e in d.Objects)
            {
                GenerateStub(e.Value, w2);
            }

            foreach (var e in d.Objects)
            {
                GenerateSkel(e.Value, w2);
            }

            foreach (var e in d.Objects)
            {
                GenerateDefaultImpl(e.Value, w2);
            }

            w2.WriteLine("public static class RRExtensions");
            w2.WriteLine("{");
            foreach (var e in d.NamedArrays)
            {
                GenerateNamedArrayExtensions(e.Value, w2);
            }

            w2.WriteLine("}");
            w2.WriteLine("}");
        }

        public static void GenerateServiceFactory(ServiceDefinition d, string defstring, TextWriter w2)
        {

            w2.WriteLine("public class " + FixName(d.Name).Replace(".", "__") + "Factory : ServiceFactory");
            w2.WriteLine("{");
            w2.WriteLine("    public override string DefString()");
            w2.WriteLine("{");
            w2.Write("    const string s=\"");
            var lines = defstring.Split('\n');

            foreach (var e in lines)
            {
                var l = e.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "").Trim();
                w2.Write(l);
                w2.Write("\\n");
            }
            w2.WriteLine("\";");
            w2.WriteLine("    return s;");
            w2.WriteLine("    }");
            w2.WriteLine("    public override string GetServiceName() {return \"" + d.Name + "\";}");
            foreach (var e in d.Structures)
            {
                w2.WriteLine("    public " + FixName(e.Value.Name) + "_stub " + FixName(e.Value.Name) + "_stubentry;");
            }
            foreach (var e in d.Pods)
            {
                w2.WriteLine("    public " + FixName(e.Value.Name) + "_stub " + FixName(e.Value.Name) + "_stubentry;");
            }
            foreach (var e in d.NamedArrays)
            {
                w2.WriteLine("    public " + FixName(e.Value.Name) + "_stub " + FixName(e.Value.Name) + "_stubentry;");
            }
            w2.WriteLine("    public " + FixName(d.Name).Replace(".", "__") + "Factory() : this(null,null) {}");
            w2.WriteLine("    public " + FixName(d.Name).Replace(".", "__") + "Factory(RobotRaconteurNode node = null, ClientContext context = null) : base(node,context)");
            w2.WriteLine("    {");
            foreach (var e in d.Structures)
            {
                w2.WriteLine("    " + FixName(e.Value.Name) + "_stubentry=new " + FixName(e.Value.Name) + "_stub(this,this.node,this.context);");
            }
            foreach (var e in d.Pods)
            {
                w2.WriteLine("    " + FixName(e.Value.Name) + "_stubentry=new " + FixName(e.Value.Name) + "_stub(this,this.node,this.context);");
            }
            foreach (var e in d.NamedArrays)
            {
                w2.WriteLine("    " + FixName(e.Value.Name) + "_stubentry=new " + FixName(e.Value.Name) + "_stub();");
            }
            w2.WriteLine("    }");

            w2.WriteLine("    public override IStructureStub FindStructureStub(string objecttype)");
            w2.WriteLine("    {");
            //w2 << "    string objshort=RemovePath(objecttype);");

            foreach (var e in d.Structures)
            {
                w2.WriteLine("    if (objecttype==\"" + e.Value.Name + "\")");
                w2.WriteLine("    return " + FixName(e.Value.Name) + "_stubentry;");
            }
            w2.WriteLine("    throw new DataTypeException(\"Cannot find appropriate structure stub\");");
            w2.WriteLine("    }");

            w2.WriteLine("    public override IPodStub FindPodStub(string objecttype)");
            w2.WriteLine("    {");
            //w2 << "    string objshort=RemovePath(objecttype);");

            foreach (var e in d.Pods)
            {
                w2.WriteLine("    if (objecttype==\"" + e.Value.Name + "\")");
                w2.WriteLine("    return " + FixName(e.Value.Name) + "_stubentry;");
            }
            w2.WriteLine("    throw new DataTypeException(\"Cannot find appropriate pod stub\");");
            w2.WriteLine("    }");

            w2.WriteLine("    public override INamedArrayStub FindNamedArrayStub(string objecttype)");
            w2.WriteLine("    {");
            //w2 << "    string objshort=RemovePath(objecttype);");

            foreach (var e in d.NamedArrays)
            {
                w2.WriteLine("    if (objecttype==\"" + e.Value.Name + "\")");
                w2.WriteLine("    return " + FixName(e.Value.Name) + "_stubentry;");
            }
            w2.WriteLine("    throw new DataTypeException(\"Cannot find appropriate pod stub\");");
            w2.WriteLine("    }");

            w2.WriteLine("    public override ServiceStub CreateStub(string objecttype, string path, ClientContext context) {");
            w2.WriteLine("    string objshort;");
            w2.WriteLine("    if (CompareNamespace(objecttype, out objshort)) {");
            w2.WriteLine("    switch (objshort) {");
            foreach (var e in d.Objects)
            {
                string objname = e.Value.Name;
                w2.WriteLine("    case \"" + objname + "\":");
                w2.WriteLine("    return new " + FixName(objname) + "_stub(path, context);");
            }
            w2.WriteLine("    default:");
            w2.WriteLine("    break;");
            w2.WriteLine("    }");
            w2.WriteLine("    } else {");
            w2.WriteLine("    return base.CreateStub(objecttype,path,context);");
            w2.WriteLine("    }");
            w2.WriteLine("    throw new ServiceException(\"Could not create stub\");");
            w2.WriteLine("    }");

            w2.WriteLine("    public override ServiceSkel CreateSkel(string path,object obj,ServerContext context) {");
            w2.WriteLine("    string objtype=ServiceDefinitionUtil.FindObjectRRType(obj);");
            w2.WriteLine("    string objshort;");

            w2.WriteLine("    if (CompareNamespace(objtype, out objshort)) {");

            w2.WriteLine("    switch(objshort) {");
            foreach (ServiceEntryDefinition e in d.Objects.Values)
            {
                string objname = e.Name;
                w2.WriteLine("    case \"" + objname + "\":");
                w2.WriteLine("    return new " + objname + "_skel(path,(" + objname + ")obj,context);");
            }
            w2.WriteLine("    default:");
            w2.WriteLine("    break;");
            w2.WriteLine("    }");
            w2.WriteLine("    } else {");
            w2.WriteLine("    return base.CreateSkel(path,obj,context);");
            w2.WriteLine("    }");
            w2.WriteLine("    throw new ServiceException(\"Could not create skel\");");
            w2.WriteLine("    }");

            w2.WriteLine("    public override RobotRaconteurException DownCastException(RobotRaconteurException rr_exp)");
            w2.WriteLine("    {");
            w2.WriteLine("    if (rr_exp==null) return rr_exp;");
            w2.WriteLine("    string rr_type=rr_exp.Error;");
            w2.WriteLine("    if (!rr_type.Contains(\".\")) return rr_exp;");
            w2.WriteLine("    string rr_stype;");
            w2.WriteLine("    if (CompareNamespace(rr_type, out rr_stype)) {");
            foreach (var e in d.Exceptions)
            {
                w2.WriteLine("    if (rr_stype==\"" + e + "\") return new " + FixName(e) + "(rr_exp.Message);");
            }
            w2.WriteLine("    } else {");
            w2.WriteLine("    return base.DownCastException(rr_exp); ");
            w2.WriteLine("    }");
            w2.WriteLine("    return rr_exp;");
            w2.WriteLine("    }");
            w2.WriteLine("}");
        }

        public static void GenerateStructureStub(ServiceEntryDefinition e, TextWriter w2)
        {
            w2.WriteLine("public class " + FixName(e.Name) + "_stub : IStructureStub {");
            w2.WriteLine("    public " + FixName(e.Name) + "_stub(" + FixName(e.ServiceDefinition.Name).Replace(".", "__") + "Factory d, RobotRaconteurNode node, ClientContext context) {def=d; rr_node=node; rr_context=context;}");
            w2.WriteLine("    private " + FixName(e.ServiceDefinition.Name).Replace(".", "__") + "Factory def;");
            w2.WriteLine("    private RobotRaconteurNode rr_node;");
            w2.WriteLine("    private ClientContext rr_context;");
            w2.WriteLine("    public MessageElementNestedElementList PackStructure(object s1) {");

            w2.WriteLine("    List<MessageElement> m=new List<MessageElement>();");
            w2.WriteLine("    if (s1 ==null) return null;");
            w2.WriteLine("    " + FixName(e.Name) + " s = (" + FixName(e.Name) + ")s1;");
            foreach (var m in MemberIter<PropertyDefinition>(e))
            {

                w2.WriteLine("    MessageElementUtil.AddMessageElement(m," + str_pack_message_element(m.Name, "s." + FixName(m.Name), m.Type) + ");");
            }

            w2.WriteLine("    return new MessageElementNestedElementList(DataTypes.structure_t,\"" + e.ServiceDefinition.Name + "." + e.Name + "\",m);");
            w2.WriteLine("    }");

            //Write Read
            w2.WriteLine("    public T UnpackStructure<T>(MessageElementNestedElementList m) {");
            w2.WriteLine("    if (m == null ) return default(T);");
            w2.WriteLine("    " + FixName(e.Name) + " s=new " + FixName(e.Name) + "();");
            foreach (var m in MemberIter<PropertyDefinition>(e))
            {
                convert_type_result t = convert_type(m.Type);
                t.name = m.Name;
                w2.WriteLine("    s." + FixName(t.name) + " =" + str_unpack_message_element("MessageElement.FindElement(m.Elements,\"" + t.name + "\")", m.Type) + ";");
            }
            //w2.WriteLine("    if ((s as T)==null) throw new DataTypeException(\"Incorrect structure cast\");");
            w2.WriteLine("    T st; try {st=(T)((object)s);} catch (InvalidCastException) {throw new DataTypeMismatchException(\"Wrong structuretype\");}");
            w2.WriteLine("    return st;");
            w2.WriteLine("    }");

            w2.WriteLine("}");
        }

        public static void GeneratePodStub(ServiceEntryDefinition e, TextWriter w2)
        {
            w2.WriteLine("public class " + FixName(e.Name) + "_stub : PodStub<" + FixName(e.Name) + "> {");
            w2.WriteLine("    public " + FixName(e.Name) + "_stub(" + FixName(e.ServiceDefinition.Name).Replace(".", "__") + "Factory d, RobotRaconteurNode node, ClientContext context) {def=d; rr_node=node; rr_context=context;}");
            w2.WriteLine("    private " + FixName(e.ServiceDefinition.Name).Replace(".", "__") + "Factory def;");
            w2.WriteLine("    private RobotRaconteurNode rr_node;");
            w2.WriteLine("    private ClientContext rr_context;");
            w2.WriteLine("    public override MessageElementNestedElementList PackPod(ref " + FixName(e.Name) + " s1) {");
            w2.WriteLine("    List<MessageElement> m=new List<MessageElement>();");
            w2.WriteLine("    " + FixName(e.Name) + " s = (" + FixName(e.Name) + ")s1;");
            foreach (var m in MemberIter<PropertyDefinition>(e))
            {
                var t2 = RemoveMultiDimArray(m.Type);
                w2.WriteLine("    MessageElementUtil.AddMessageElement(m," + str_pack_message_element(m.Name, "s." + FixName(m.Name), t2) + ");");
            }
            w2.WriteLine("    return new MessageElementNestedElementList(DataTypes.pod_t, \"\", m);");
            w2.WriteLine("    }");

            //Write Read
            w2.WriteLine("    public override " + FixName(e.Name) + " UnpackPod(MessageElementNestedElementList m) {");

            w2.WriteLine("    if (m == null ) throw new NullReferenceException(\"Pod must not be null\");");
            w2.WriteLine("    " + FixName(e.Name) + " s = new " + FixName(e.Name) + "();");
            foreach (var m in MemberIter<PropertyDefinition>(e))
            {
                convert_type_result t = convert_type(m.Type);
                t.name = m.Name;
                var t2 = RemoveMultiDimArray(m.Type);
                w2.WriteLine("    s." + FixName(t.name) + " =" + str_unpack_message_element("MessageElement.FindElement(m.Elements,\"" + t.name + "\")", t2) + ";");
            }
            w2.WriteLine("    return s;");
            w2.WriteLine("    }");
            w2.WriteLine("    public override string TypeName { get { return \"" + e.ServiceDefinition.Name + "." + e.Name + "\"; } }");
            w2.WriteLine("}");
        }

        public static void GenerateNamedArrayStub(ServiceEntryDefinition e, TextWriter w2)
        {
            var t4 = ServiceDefinitionUtil.GetNamedArrayElementTypeAndCount(e);
            var t5 = new TypeDefinition();
            t5.Type = t4.Item1;
            convert_type_result t6 = convert_type(t5);

            w2.WriteLine("public class " + FixName(e.Name) + "_stub : NamedArrayStub<" + FixName(e.Name) + "," + t6.cs_type + "> {");
            w2.WriteLine("    public override " + t6.cs_type + "[] GetNumericArrayFromNamedArrayStruct(ref " + FixName(e.Name) + " s) {");
            w2.WriteLine("    return s.GetNumericArray();");
            w2.WriteLine("    }");
            w2.WriteLine("    public override " + FixName(e.Name) + " GetNamedArrayStructFromNumericArray(" + t6.cs_type + "[] m) {");
            w2.WriteLine("    if (m.Length != " + t4.Item2 + ") throw new DataTypeException(\"Invalid namedarray array\");");
            w2.WriteLine("    var s = new " + FixName(e.Name) + "();");
            w2.WriteLine("    var a = new ArraySegment<" + t6.cs_type + ">(m);");
            w2.WriteLine("    s.AssignFromNumericArray(ref a);");
            w2.WriteLine("    return s;");
            w2.WriteLine("    }");
            w2.WriteLine("    public override " + t6.cs_type + "[] GetNumericArrayFromNamedArray(" + FixName(e.Name) + "[] s) {");
            w2.WriteLine("    return s.GetNumericArray();");
            w2.WriteLine("    }");
            w2.WriteLine("    public override " + FixName(e.Name) + "[] GetNamedArrayFromNumericArray(" + t6.cs_type + "[] m) {");
            w2.WriteLine("    if (m.Length % " + t4.Item2 + " != 0) throw new DataTypeException(\"Invalid namedarray array\");");
            w2.WriteLine("    " + FixName(e.Name) + "[] s = new " + FixName(e.Name) + "[m.Length / " + t4.Item2 + "];");
            w2.WriteLine("    var a = new ArraySegment<" + t6.cs_type + ">(m);");
            w2.WriteLine("    s.AssignFromNumericArray(ref a);");
            w2.WriteLine("    return s;");
            w2.WriteLine("    }");
            w2.WriteLine("    public override string TypeName { get { return \"" + e.ServiceDefinition.Name + "." + e.Name + "\"; } }");

            w2.WriteLine("}");
        }

        public static void GenerateStub(ServiceEntryDefinition e, TextWriter w2)
        {

            w2.WriteLine("public class " + FixName(e.Name) + "_stub : ServiceStub , " + FixName(e.Name) + " {");

            foreach (var m in MemberIter<CallbackDefinition>(e))
            {

                w2.WriteLine("    private CallbackClient<" + str_pack_delegate(m.Parameters, m.ReturnType, true) + "> rr_" + FixName(m.Name) + ";");
            }


            foreach (var m in MemberIter<PipeDefinition>(e))
            {

                convert_type_result t = convert_type(m.Type);
                w2.WriteLine("    private Pipe<" + t.cs_type + t.cs_arr_type + "> rr_" + FixName(m.Name) + ";");
            }


            foreach (var m in MemberIter<WireDefinition>(e))
            {

                convert_type_result t = convert_type(m.Type);
                w2.WriteLine("    private Wire<" + t.cs_type + t.cs_arr_type + "> rr_" + FixName(m.Name) + ";");
            }


            foreach (var m in MemberIter<MemoryDefinition>(e))
            {

                var t2 = new TypeDefinition();
                m.Type.CopyTo(ref t2);
                t2.RemoveArray();
                convert_type_result t = convert_type(t2);
                string c = "";
                if (!DataTypeUtil.IsNumber(m.Type.Type))
                {
                    DataTypes entry_type = m.Type.ResolveNamedType().RRDataType;
                    if (entry_type != DataTypes.namedarray_t)
                    {
                        c = "Pod";
                    }
                    else
                    {
                        c = "Named";
                    }
                }
                switch (m.Type.ArrayType)
                {
                    case DataTypes_ArrayTypes.array:
                        w2.WriteLine("    private " + c + "ArrayMemory<" + t.cs_type + "> rr_" + FixName(m.Name) + ";");
                        break;
                    case DataTypes_ArrayTypes.multidimarray:
                        w2.WriteLine("    private " + c + "MultiDimArrayMemory<" + t.cs_type + "> rr_" + FixName(m.Name) + ";");
                        break;
                    default:
                        throw new DataTypeException("Invalid memory definition");
                }

            }

            w2.WriteLine("    public " + FixName(e.Name) + "_stub(string path, ClientContext c) : base(path, c) {");
            foreach (var m in MemberIter<CallbackDefinition>(e))
            {

                w2.WriteLine("    rr_" + FixName(m.Name) + "=new CallbackClient<" + str_pack_delegate(m.Parameters, m.ReturnType, true) + ">(\"" + m.Name + "\");");
            }

            foreach (var m in MemberIter<PipeDefinition>(e))
            {

                convert_type_result t = convert_type(m.Type);
                w2.WriteLine("    rr_" + FixName(m.Name) + "=new PipeClient<" + t.cs_type + t.cs_arr_type + ">(\"" + m.Name + "\", this);");
            }


            foreach (var m in MemberIter<WireDefinition>(e))
            {

                convert_type_result t = convert_type(m.Type);
                w2.WriteLine("    rr_" + FixName(m.Name) + "=new WireClient<" + t.cs_type + t.cs_arr_type + ">(\"" + m.Name + "\", this);");
            }


            foreach (var m in MemberIter<MemoryDefinition>(e))
            {

                var t2 = new TypeDefinition();
                m.Type.CopyTo(ref t2);
                t2.RemoveArray();
                convert_type_result t = convert_type(t2);
                string c = "";
                if (DataTypeUtil.IsNumber(m.Type.Type))
                {
                    switch (m.Type.ArrayType)
                    {
                        case DataTypes_ArrayTypes.array:
                            w2.WriteLine("    rr_" + FixName(m.Name) + "=new ArrayMemoryClient<" + t.cs_type + ">(\"" + m.Name + "\",this, " + DirectionStr(m.Direction) + ");");
                            break;
                        case DataTypes_ArrayTypes.multidimarray:
                            w2.WriteLine("    rr_" + FixName(m.Name) + "=new MultiDimArrayMemoryClient<" + t.cs_type + ">(\"" + m.Name + "\",this," + DirectionStr(m.Direction) + ");");
                            break;
                        default:
                            throw new DataTypeException("Invalid memory definition");
                    }
                }
                else
                {
                    DataTypes entry_type = m.Type.ResolveNamedType().RRDataType;
                    int elem_size = 0;
                    if (entry_type != DataTypes.namedarray_t)
                    {
                        c = "Pod";
                        elem_size = ServiceDefinitionUtil.EstimatePodPackedElementSize((ServiceEntryDefinition)t2.ResolveNamedType());
                    }
                    else
                    {
                        elem_size = ServiceDefinitionUtil.GetNamedArrayElementTypeAndCount((ServiceEntryDefinition)t2.ResolveNamedType()).Item2;
                        c = "Named";
                    }

                    switch (m.Type.ArrayType)
                    {
                        case DataTypes_ArrayTypes.array:
                            w2.WriteLine("    rr_" + FixName(m.Name) + "=new " + c + "ArrayMemoryClient<" + t.cs_type + ">(\"" + m.Name + "\",this," + elem_size + "," + DirectionStr(m.Direction) + ");");
                            break;
                        case DataTypes_ArrayTypes.multidimarray:
                            w2.WriteLine("    rr_" + FixName(m.Name) + "=new " + c + "MultiDimArrayMemoryClient<" + t.cs_type + ">(\"" + m.Name + "\",this," + elem_size + "," + DirectionStr(m.Direction) + ");");
                            break;
                        default:
                            throw new DataTypeException("Invalid memory definition");
                    }
                }
            }


            w2.WriteLine("    }");

            foreach (var m in MemberIter<PropertyDefinition>(e))
            {

                convert_type_result t = convert_type(m.Type);
                t.name = FixName(m.Name);
                w2.WriteLine(String.Format("    public async Task<{0}{1}> get_{2}(CancellationToken cancel=default(CancellationToken))", t.cs_type, t.cs_arr_type, t.name) + " {");
                w2.WriteLine("        MessageEntry m = new MessageEntry(MessageEntryType.PropertyGetReq, \"" + m.Name + "\");");
                w2.WriteLine("        MessageEntry mr=await ProcessRequest(m, cancel).ConfigureAwait(false);");
                w2.WriteLine("        MessageElement me=mr.FindElement(\"value\");");
                w2.WriteLine("        return " + str_unpack_message_element("me", m.Type) + ";");
                w2.WriteLine("        }");
                w2.WriteLine(String.Format("    public async Task set_{2}({0}{1} value, CancellationToken cancel=default(CancellationToken))", t.cs_type, t.cs_arr_type, t.name) + " {");
                w2.WriteLine(String.Format("        MessageEntry m=new MessageEntry(MessageEntryType.PropertySetReq,\"{0}\");", m.Name));
                w2.WriteLine("        MessageElementUtil.AddMessageElement(m," + str_pack_message_element("value", "value", m.Type) + ");");
                w2.WriteLine(String.Format("        MessageEntry mr=await ProcessRequest(m, cancel).ConfigureAwait(false);", t));
                w2.WriteLine("        }");
            }


            foreach (var m in MemberIter<FunctionDefinition>(e))
            {
                if (!m.IsGenerator)
                {
                    convert_type_result t = convert_type(m.ReturnType);
                    string params_ = str_pack_parameters(m.Parameters, true);
                    if (params_.Length == 0)
                    {
                        params_ = "CancellationToken cancel = default(CancellationToken)";
                    }
                    else
                    {
                        params_ += ", CancellationToken cancel = default(CancellationToken)";
                    }

                    string ret_type = (m.ReturnType.Type == DataTypes.void_t) ? "Task" : "Task<" + t.cs_type + t.cs_arr_type + ">";

                    w2.WriteLine("    public async " + ret_type + " " + FixName(m.Name) + "(" + params_ + ") {");
                    w2.WriteLine(String.Format("        MessageEntry rr_m=new MessageEntry(MessageEntryType.FunctionCallReq,\"{0}\");", m.Name));
                    foreach (var p in m.Parameters)
                    {
                        w2.WriteLine("    MessageElementUtil.AddMessageElement(rr_m," + str_pack_message_element(p.Name, FixName(p.Name), p) + ");");
                    }
                    w2.WriteLine(String.Format("        MessageEntry rr_me=await ProcessRequest(rr_m, cancel).ConfigureAwait(false);", t));
                    if (m.ReturnType.Type != DataTypes.void_t)
                    {
                        w2.WriteLine("    return " + str_unpack_message_element("rr_me.FindElement(\"return\")", m.ReturnType) + ";");
                    }
                    w2.WriteLine("    }");
                }
                else
                {
                    convert_generator_result t = convert_generator(m);
                    string params_ = str_pack_parameters(t.params_, true);
                    if (params_.Length == 0)
                    {
                        params_ = "CancellationToken cancel = default(CancellationToken)";
                    }
                    else
                    {
                        params_ += ", CancellationToken cancel = default(CancellationToken)";
                    }
                    w2.WriteLine("    public async Task<" + t.generator_csharp_type + "> " + FixName(m.Name) + "(" + params_ + ") {");
                    w2.WriteLine(String.Format("        MessageEntry rr_m=new MessageEntry(MessageEntryType.FunctionCallReq,\"{0}\");", m.Name));
                    foreach (var p in t.params_)
                    {
                        w2.WriteLine("    MessageElementUtil.AddMessageElement(rr_m," + str_pack_message_element(p.Name, FixName(p.Name), p) + ");");
                    }
                    w2.WriteLine(String.Format("        MessageEntry rr_me=await ProcessRequest(rr_m, cancel).ConfigureAwait(false);", t));
                    w2.WriteLine("    return new " + t.generator_csharp_base_type + "Client<" + t.generator_csharp_template_params + ">(\"" + m.Name + "\",this,rr_me.FindElement(\"index\").CastData<int[]>()[0]);");
                    w2.WriteLine("    }");
                }
            }


            foreach (var m in MemberIter<EventDefinition>(e))
            {

                string params_ = str_pack_parameters(m.Parameters, true);
                w2.WriteLine("    public event " + str_pack_delegate(m.Parameters, null, false) + " " + FixName(m.Name) + ";");
            }


            w2.WriteLine("    protected override void DispatchEvent(MessageEntry rr_m) {");
            w2.WriteLine("    switch (rr_m.MemberName) {");
            foreach (var m in MemberIter<EventDefinition>(e))
            {

                string params_ = str_pack_parameters(m.Parameters, false);
                w2.WriteLine("    case \"" + m.Name + "\":");
                w2.WriteLine("    {");
                w2.WriteLine("    if (" + FixName(m.Name) + " != null) { ");
                foreach (var p in m.Parameters)
                {
                    convert_type_result t3 = convert_type(p);
                    w2.WriteLine("    " + t3.cs_type + t3.cs_arr_type + " " + FixName(p.Name) + "=" + str_unpack_message_element("rr_m.FindElement(\"" + p.Name + "\")", p) + ";");

                }
                w2.WriteLine("    " + FixName(m.Name) + "(" + params_ + ");");
                w2.WriteLine("    }");
                w2.WriteLine("    return;");
                w2.WriteLine("    }");
            }

            w2.WriteLine("    default:");
            w2.WriteLine("    break;");
            w2.WriteLine("    }");
            w2.WriteLine("    }");


            foreach (var m in MemberIter<ObjRefDefinition>(e))
            {

                string objtype = FixName(m.ObjectType);
                if (objtype == "varobject")
                {
                    objtype = "object";
                    string indtype;
                    if (GetObjRefIndType(m, out indtype))
                    {
                        w2.WriteLine("    public async Task<" + objtype + "> get_" + FixName(m.Name) + "(" + indtype + " ind, CancellationToken cancel=default(CancellationToken)) {");
                        w2.WriteLine("    return (" + objtype + ")await FindObjRef(\"" + m.Name + "\",ind.ToString(),cancel).ConfigureAwait(false);");
                        w2.WriteLine("    }");
                    }
                    else
                    {
                        w2.WriteLine("    public async Task<" + objtype + "> get_" + FixName(m.Name) + "(CancellationToken cancel=default(CancellationToken)) {");
                        w2.WriteLine("    return (" + objtype + ")await FindObjRef(\"" + m.Name + "\", cancel).ConfigureAwait(false);");
                        w2.WriteLine("    }");
                    }
                }
                else
                {

                    var d = e.ServiceDefinition;
                    if (d == null) throw new DataTypeException("Invalid object type name");

                    string objecttype2 = "";

                    string s2 = FixName(m.ObjectType);

                    if (!s2.Contains('.'))
                    {
                        objecttype2 = FixName(d.Name) + "." + s2;
                    }
                    else
                    {
                        objecttype2 = s2;
                    }


                    string indtype;
                    if (GetObjRefIndType(m, out indtype))
                    {
                        w2.WriteLine("    public async Task<" + objtype + "> get_" + FixName(m.Name) + "(" + indtype + " ind, CancellationToken cancel=default(CancellationToken)) {");
                        w2.WriteLine("    return (" + objtype + ")await FindObjRefTyped(\"" + FixName(m.Name) + "\",ind.ToString(),\"" + objecttype2 + "\",cancel).ConfigureAwait(false);");
                        w2.WriteLine("    }");
                    }
                    else
                    {
                        w2.WriteLine("    public async Task<" + objtype + "> get_" + FixName(m.Name) + "(CancellationToken cancel=default(CancellationToken)) {");
                        w2.WriteLine("    return (" + objtype + ")await FindObjRefTyped(\"" + m.Name + "\",\"" + objecttype2 + "\",cancel).ConfigureAwait(false);");
                        w2.WriteLine("    }");
                    }
                }
            }



            foreach (var m in MemberIter<PipeDefinition>(e))
            {

                convert_type_result t = convert_type(m.Type);
                w2.WriteLine("    public Pipe<" + t.cs_type + t.cs_arr_type + "> " + FixName(m.Name) + " {");
                w2.WriteLine("    get { return rr_" + FixName(m.Name) + ";  }");
                w2.WriteLine("    set { throw new InvalidOperationException();}");
                w2.WriteLine("    }");
            }


            foreach (var m in MemberIter<CallbackDefinition>(e))
            {

                w2.WriteLine("    public Callback<" + str_pack_delegate(m.Parameters, m.ReturnType, true) + "> " + FixName(m.Name) + " {");
                w2.WriteLine("    get { return rr_" + FixName(m.Name) + ";  }");
                w2.WriteLine("    set { throw new InvalidOperationException();}");
                w2.WriteLine("    }");
            }


            foreach (var m in MemberIter<WireDefinition>(e))
            {

                convert_type_result t = convert_type(m.Type);
                w2.WriteLine("    public Wire<" + t.cs_type + t.cs_arr_type + "> " + FixName(m.Name) + " {");
                w2.WriteLine("    get { return rr_" + FixName(m.Name) + ";  }");
                w2.WriteLine("    set { throw new InvalidOperationException();}");
                w2.WriteLine("    }");
            }






            foreach (var m in MemberIter<MemoryDefinition>(e))
            {

                var t2 = new TypeDefinition();
                m.Type.CopyTo(ref t2);
                t2.RemoveArray();
                convert_type_result t = convert_type(t2);

                string c = "";
                if (!DataTypeUtil.IsNumber(m.Type.Type))
                {
                    DataTypes entry_type = m.Type.ResolveNamedType().RRDataType;
                    if (entry_type != DataTypes.namedarray_t)
                    {
                        c = "Pod";
                    }
                    else
                    {
                        c = "Named";
                    }
                }
                switch (m.Type.ArrayType)
                {
                    case DataTypes_ArrayTypes.array:
                        w2.WriteLine("    public " + c + "ArrayMemory<" + t.cs_type + "> " + FixName(m.Name) + " { ");
                        break;
                    case DataTypes_ArrayTypes.multidimarray:

                        w2.WriteLine("    public " + c + "MultiDimArrayMemory<" + t.cs_type + "> " + FixName(m.Name) + " {");
                        break;
                    default:
                        throw new DataTypeException("Invalid memory definition");
                }
                w2.WriteLine("    get { return rr_" + FixName(m.Name) + "; }");

                w2.WriteLine("    }");
            }



            w2.WriteLine("    protected override void DispatchPipeMessage(MessageEntry m)");
            w2.WriteLine("    {");
            w2.WriteLine("    switch (m.MemberName) {");
            foreach (var m in MemberIter<PipeDefinition>(e))
            {
                w2.WriteLine("    case \"" + m.Name + "\":");
                w2.WriteLine("    this.rr_" + FixName(m.Name) + ".PipePacketReceived(m);");
                w2.WriteLine("    break;");
            }
            w2.WriteLine("    default:");
            w2.WriteLine("    throw new Exception();");
            w2.WriteLine("    }");
            w2.WriteLine("    }");


            w2.WriteLine("    protected override async Task<MessageEntry> CallbackCall(MessageEntry rr_m) {");
            w2.WriteLine("    string rr_ename=rr_m.MemberName;");
            w2.WriteLine("    MessageEntry rr_mr=new MessageEntry(MessageEntryType.CallbackCallRet, rr_ename);");
            w2.WriteLine("    rr_mr.ServicePath=rr_m.ServicePath;");
            w2.WriteLine("    rr_mr.RequestID=rr_m.RequestID;");
            w2.WriteLine("    switch (rr_ename) {");

            foreach (var m in MemberIter<CallbackDefinition>(e))
            {
                var t = convert_type(m.ReturnType);


                string[] pars = new string[m.Parameters.Count + 1];
                for (int i = 0; i < pars.Length - 1; i++) pars[i] = FixName(m.Parameters[i].Name);
                pars[pars.Length - 1] = "default(CancellationToken)";

                var params_ = String.Join(", ", pars);
                //w.WriteLine(String.Format("    public {1}{2} {0}({3}) {{", t2));

                w2.WriteLine("    case \"" + FixName(m.Name) + "\": {");

                foreach (TypeDefinition pt in m.Parameters)
                {
                    var t3 = convert_type(pt);

                    w2.WriteLine(String.Format("    {0}{1} {2}=" + str_unpack_message_element("rr_m.FindElement(\"" + pt.Name + "\")", pt) + ";", t3.cs_type, t3.cs_arr_type, t3.name));
                }

                if (m.ReturnType.Type != DataTypes.void_t)
                {
                    w2.WriteLine(String.Format("    var rr_ret=await {0}.Function({1}).ConfigureAwait(false);", FixName(m.Name), params_));
                    w2.WriteLine("    MessageElementUtil.AddMessageElement(rr_mr," + str_pack_message_element("return", "rr_ret", m.ReturnType) + ");");
                }
                else
                {
                    TypeDefinition tvoid = new TypeDefinition();
                    tvoid.Type = DataTypes.int32_t;
                    w2.WriteLine(String.Format("    await this.{0}.Function({1}).ConfigureAwait(false);", FixName(m.Name), params_));
                    w2.WriteLine("    MessageElementUtil.AddMessageElement(rr_mr," + str_pack_message_element("return", "0", tvoid) + ");");
                }
                w2.WriteLine("    break;");

                w2.WriteLine("    }");
            }
            w2.WriteLine("    default:");
            w2.WriteLine("    throw new MemberNotFoundException(\"Member not found\");");
            w2.WriteLine("    }");
            w2.WriteLine("    return rr_mr;");
            w2.WriteLine("    }");

            w2.WriteLine("    protected override void DispatchWireMessage(MessageEntry m)");
            w2.WriteLine("    {");
            w2.WriteLine("    switch (m.MemberName) {");
            foreach (var m in MemberIter<WireDefinition>(e))
            {
                w2.WriteLine("    case \"" + m.Name + "\":");
                w2.WriteLine("    this.rr_" + FixName(m.Name) + ".WirePacketReceived(m);");
                w2.WriteLine("    break;");
            }
            w2.WriteLine("    default:");
            w2.WriteLine("    throw new Exception();");
            w2.WriteLine("    }");
            w2.WriteLine("    }");

            w2.WriteLine("}");

        }

        protected static string DirectionStr(MemberDefinition_Direction dir)
        {
            switch (dir)
            {
                case MemberDefinition_Direction.both:
                    return "MemberDefinition_Direction.both";
                case MemberDefinition_Direction.readonly_:
                    return "MemberDefinition_Direction.readonly_";
                case MemberDefinition_Direction.writeonly:
                    return "MemberDefinition_Direction.writeonly";
                default:
                    throw new ArgumentException("Invalid direction");

            }
        }

        public static void GenerateSkel(ServiceEntryDefinition e, TextWriter w2)
        {
            w2.WriteLine("public class " + FixName(e.Name) + "_skel : ServiceSkel {");
            w2.WriteLine("    protected " + FixName(e.Name) + " obj;");
            w2.WriteLine("    public " + e.Name + "_skel(string p," + FixName(e.Name) + " o,ServerContext c) : base(p,o,c) { obj=(" + FixName(e.Name) + ")o; }");
            w2.WriteLine("    public override void ReleaseCastObject() { ");
            w2.WriteLine("    }");

            w2.WriteLine("    public override async Task<MessageEntry> CallGetProperty(MessageEntry m) {");
            w2.WriteLine("    string ename=m.MemberName;");
            w2.WriteLine("    MessageEntry mr=new MessageEntry(MessageEntryType.PropertyGetRes, ename);");
            w2.WriteLine("    switch (ename) {");
            foreach (var m in MemberIter<PropertyDefinition>(e))
            {
                w2.WriteLine("    case \"" + m.Name + "\":");
                w2.WriteLine("    {");
                convert_type_result t = convert_type(m.Type);
                w2.WriteLine("    " + t.cs_type + t.cs_arr_type + " ret=await obj.get_" + FixName(m.Name) + "().ConfigureAwait(false);");
                w2.WriteLine("    mr.AddElement(" + str_pack_message_element("value", "ret", m.Type) + ");");
                w2.WriteLine("    break;");
                w2.WriteLine("    }");
            }

            w2.WriteLine("    default:");
            w2.WriteLine("    throw new MemberNotFoundException(\"Member not found\");");
            w2.WriteLine("    }");
            w2.WriteLine("    return mr;");
            w2.WriteLine("    }");

            w2.WriteLine("    public override async Task<MessageEntry> CallSetProperty(MessageEntry m) {");
            w2.WriteLine("    string ename=m.MemberName;");
            w2.WriteLine("    MessageElement me=m.FindElement(\"value\");");
            w2.WriteLine("    MessageEntry mr=new MessageEntry(MessageEntryType.PropertySetRes, ename);");
            w2.WriteLine("    switch (ename) {");

            foreach (var m in MemberIter<PropertyDefinition>(e))
            {


                w2.WriteLine("    case \"" + m.Name + "\":");
                w2.WriteLine("    {");

                w2.WriteLine("    await obj.set_" + FixName(m.Name) + "(" + str_unpack_message_element("me", m.Type) + ").ConfigureAwait(false);");
                w2.WriteLine("    break;");
                w2.WriteLine("    }");
            }

            w2.WriteLine("    default:");
            w2.WriteLine("    throw new MemberNotFoundException(\"Member not found\");");
            w2.WriteLine("    }");
            w2.WriteLine("    return mr;");
            w2.WriteLine("    }");

            w2.WriteLine("    public override async Task<MessageEntry> CallFunction(MessageEntry rr_m) {");
            w2.WriteLine("    string rr_ename=rr_m.MemberName;");
            w2.WriteLine("    MessageEntry rr_mr=new MessageEntry(MessageEntryType.FunctionCallRes, rr_ename);");
            w2.WriteLine("    switch (rr_ename) {");

            foreach (var m in MemberIter<FunctionDefinition>(e))
            {

                if (!m.IsGenerator)
                {
                    w2.WriteLine("    case \"" + m.Name + "\":");
                    w2.WriteLine("    {");

                    string params_ = str_pack_parameters(m.Parameters, false);
                    if (params_.Length == 0)
                    {
                        params_ = "default(CancellationToken)";
                    }
                    else
                    {
                        params_ += ", default(CancellationToken)";
                    }
                    foreach (var p in m.Parameters)
                    {
                        convert_type_result t3 = convert_type(p);
                        w2.WriteLine("    " + t3.cs_type + t3.cs_arr_type + " " + FixName(p.Name) + "=" + str_unpack_message_element("MessageElementUtil.FindElement(rr_m,\"" + p.Name + "\")", p) + ";");

                    }
                    if (m.ReturnType.Type == DataTypes.void_t)
                    {
                        w2.WriteLine("    await this.obj." + FixName(m.Name) + "(" + params_ + ").ConfigureAwait(false);");
                        w2.WriteLine("    rr_mr.AddElement(\"return\",(int)0);");
                    }
                    else
                    {
                        convert_type_result t = convert_type(m.ReturnType);
                        w2.WriteLine("    " + t.cs_type + t.cs_arr_type + " rr_ret=await this.obj." + FixName(m.Name) + "(" + params_ + ").ConfigureAwait(false);");
                        w2.WriteLine("    rr_mr.AddElement(" + str_pack_message_element("return", "rr_ret", m.ReturnType) + ");");
                    }
                    w2.WriteLine("    break;");
                    w2.WriteLine("    }");
                }
                else
                {
                    w2.WriteLine("    case \"" + m.Name + "\":");
                    w2.WriteLine("    {");
                    convert_generator_result t4 = convert_generator(m);
                    string params_ = str_pack_parameters(t4.params_, false);
                    foreach (var p in t4.params_)
                    {
                        convert_type_result t3 = convert_type(p);
                        w2.WriteLine("    " + t3.cs_type + t3.cs_arr_type + " " + FixName(p.Name) + "=" + str_unpack_message_element("MessageElementUtil.FindElement(rr_m,\"" + p.Name + "\")", p) + ";");

                    }
                    w2.WriteLine("    var rr_ep = ServerEndpoint.CurrentEndpoint;");
                    w2.WriteLine("    " + t4.generator_csharp_type + " rr_ret=await this.obj." + FixName(m.Name) + "(" + params_ + ").ConfigureAwait(false);");
                    w2.WriteLine("    lock(generators) {");
                    w2.WriteLine("    int rr_index = GetNewGeneratorIndex();");
                    w2.WriteLine("    generators.Add(rr_index, new " + t4.generator_csharp_base_type + "Server<" + t4.generator_csharp_template_params + ">(rr_ret,\"" + m.Name + "\",rr_index, this, rr_ep));");
                    w2.WriteLine("    rr_mr.AddElement(\"index\",rr_index);");
                    w2.WriteLine("    }");
                    w2.WriteLine("    break;");
                    w2.WriteLine("    }");
                }
            }

            w2.WriteLine("    default:");
            w2.WriteLine("    throw new MemberNotFoundException(\"Member not found\");");
            w2.WriteLine("    }");
            w2.WriteLine("    return rr_mr;");
            w2.WriteLine("    }");

            w2.WriteLine("    public override async Task<object> GetSubObj(string name, string ind) {");
            w2.WriteLine("    switch (name) {");
            foreach (var m in MemberIter<ObjRefDefinition>(e))
            {

                w2.WriteLine("    case \"" + m.Name + "\": {");
                string indtype;
                if (GetObjRefIndType(m, out indtype))
                {
                    if (indtype == "int")
                    {
                        w2.WriteLine("    return await obj.get_" + FixName(m.Name) + "(Int32.Parse(ind)).ConfigureAwait(false);");
                    }
                    else
                    {
                        w2.WriteLine("    return await obj.get_" + FixName(m.Name) + "(ind).ConfigureAwait(false);");
                    }
                }
                else
                {
                    w2.WriteLine("    return await obj.get_" + FixName(m.Name) + "().ConfigureAwait(false);");
                }
                w2.WriteLine("    }");
            }

            w2.WriteLine("    default:");
            w2.WriteLine("    break;");
            w2.WriteLine("    }");
            w2.WriteLine("    throw new MemberNotFoundException(\"\");");
            w2.WriteLine("    }");

            w2.WriteLine("    public override void RegisterEvents(object rrobj1) {");
            w2.WriteLine("    obj=(" + FixName(e.Name) + ")rrobj1;");
            foreach (var m in MemberIter<EventDefinition>(e))
            {

                w2.WriteLine("    obj." + FixName(m.Name) + "+=rr_" + FixName(m.Name) + ";");
            }

            w2.WriteLine("    }");

            w2.WriteLine("    public override void UnregisterEvents(object rrobj1) {");
            w2.WriteLine("    obj=(" + FixName(e.Name) + ")rrobj1;");
            foreach (var m in MemberIter<EventDefinition>(e))
            {

                w2.WriteLine("    obj." + FixName(m.Name) + "-=rr_" + FixName(m.Name) + ";");
            }

            w2.WriteLine("    }");

            foreach (var m in MemberIter<EventDefinition>(e))
            {


                string params_ = str_pack_parameters(m.Parameters, true);
                w2.WriteLine("    public void rr_" + FixName(m.Name) + "(" + params_ + ") {");
                w2.WriteLine("    MessageEntry rr_mm=new MessageEntry(MessageEntryType.EventReq,\"" + m.Name + "\");");
                foreach (var p in m.Parameters)
                {
                    w2.WriteLine("    MessageElementUtil.AddMessageElement(rr_mm," + str_pack_message_element(p.Name, FixName(p.Name), p) + ");");
                }
                w2.WriteLine("    this.SendEvent(rr_mm);");

                w2.WriteLine("    }");
            }


            w2.WriteLine("    public override object GetCallbackFunction(uint rr_endpoint, string rr_membername) {");
            w2.WriteLine("    switch (rr_membername) {");
            foreach (var m in MemberIter<CallbackDefinition>(e))
            {

                convert_type_result t = convert_type(m.ReturnType);
                string params_ = str_pack_parameters(m.Parameters, true);
                if (params_.Length == 0)
                {
                    params_ = "CancellationToken rr_cancel";
                }
                else
                {
                    params_ += ", CancellationToken rr_cancel";
                }
                w2.WriteLine("    case \"" + m.Name + "\": {");
                w2.WriteLine("    return new " + str_pack_delegate(m.Parameters, m.ReturnType, true) + "( async delegate(" + params_ + ") {"); ;
                w2.WriteLine("    MessageEntry rr_mm=new MessageEntry(MessageEntryType.CallbackCallReq,\"" + m.Name + "\");");
                w2.WriteLine("    rr_mm.ServicePath=m_ServicePath;");

                foreach (var p in m.Parameters)
                {
                    w2.WriteLine("    MessageElementUtil.AddMessageElement(rr_mm," + str_pack_message_element(p.Name, FixName(p.Name), p) + ");");
                }

                w2.WriteLine("    MessageEntry rr_mr=await RRContext.ProcessCallbackRequest(rr_mm,rr_endpoint,rr_cancel).ConfigureAwait(false);");
                w2.WriteLine("    MessageElement rr_me = rr_mr.FindElement(\"return\");");
                if (m.ReturnType.Type != DataTypes.void_t)
                {
                    w2.WriteLine("    return " + str_unpack_message_element("rr_me", m.ReturnType) + ";");
                }

                w2.WriteLine("    });");
                w2.WriteLine("    }");
            }

            w2.WriteLine("    default:");
            w2.WriteLine("    break;");
            w2.WriteLine("    }");
            w2.WriteLine("    throw new MemberNotFoundException(\"Member not found\");");
            w2.WriteLine("    }");

            foreach (var m in MemberIter<PipeDefinition>(e))
            {
                var t = convert_type(m.Type);
                t.name = FixName(m.Name);
                w2.WriteLine(String.Format("    private PipeServer<{0}{1}> rr_{2};", t.cs_type, t.cs_arr_type, t.name));
            }

            foreach (var m in MemberIter<WireDefinition>(e))
            {
                var t = convert_type(m.Type);
                t.name = FixName(m.Name);
                w2.WriteLine(String.Format("    private WireServer<{0}{1}> rr_{2};", t.cs_type, t.cs_arr_type, t.name));
            }


            w2.WriteLine("    private bool rr_InitPipeServersRun=false;");
            w2.WriteLine("    public override void InitPipeServers(object o) {");
            w2.WriteLine("    if (this.rr_InitPipeServersRun) return;");
            w2.WriteLine("    this.rr_InitPipeServersRun=true;");
            w2.WriteLine("    " + e.Name + " castobj=(" + e.Name + ")o;");

            foreach (var m in MemberIter<PipeDefinition>(e))
            {
                var t = convert_type(m.Type);
                t.name = FixName(m.Name);
                w2.WriteLine(String.Format("    this.rr_{2}=new PipeServer<{0}{1}>(\"" + m.Name + "\",this);", t.cs_type, t.cs_arr_type, t.name));
            }

            foreach (var m in MemberIter<WireDefinition>(e))
            {
                var t = convert_type(m.Type);
                t.name = FixName(m.Name);
                w2.WriteLine(String.Format("    this.rr_{2}=new WireServer<{0}{1}>(\"" + m.Name + "\",this);", t.cs_type, t.cs_arr_type, t.name));
            }

            foreach (var m in MemberIter<PipeDefinition>(e))
            {
                var t = convert_type(m.Type);
                t.name = FixName(m.Name);
                w2.WriteLine(String.Format("    castobj.{0}=this.rr_{0};", t.name));
            }

            foreach (var m in MemberIter<WireDefinition>(e))
            {
                var t = convert_type(m.Type);
                t.name = FixName(m.Name);
                w2.WriteLine(String.Format("    castobj.{0}=this.rr_{0};", t.name));
            }
            w2.WriteLine("    }");


            w2.WriteLine("    public override void InitCallbackServers(object rrobj1) {");
            w2.WriteLine("    obj=(" + FixName(e.Name) + ")rrobj1;");
            foreach (var m in MemberIter<CallbackDefinition>(e))
            {


                w2.WriteLine("    obj." + FixName(m.Name) + "=new CallbackServer<" + str_pack_delegate(m.Parameters, m.ReturnType, true) + ">(\"" + m.Name + "\",this);");
            }

            w2.WriteLine("    }");

            w2.WriteLine("    public override async Task<MessageEntry> CallPipeFunction(MessageEntry m,Endpoint e) {");
            w2.WriteLine("    string ename=m.MemberName;");
            w2.WriteLine("    switch (ename) {");
            foreach (var m in MemberIter<PipeDefinition>(e))
            {
                w2.WriteLine("    case \"" + m.Name + "\":");
                w2.WriteLine("    return await this.rr_" + FixName(m.Name) + ".PipeCommand(m,e).ConfigureAwait(false);");
            }

            w2.WriteLine("    default:");
            w2.WriteLine("    throw new MemberNotFoundException(\"Member not found\");");
            w2.WriteLine("    }");
            w2.WriteLine("    }");

            w2.WriteLine("    public override async Task<MessageEntry> CallWireFunction(MessageEntry m,Endpoint e) {");
            w2.WriteLine("    string ename=m.MemberName;");
            w2.WriteLine("    switch (ename) {");
            foreach (var m in MemberIter<WireDefinition>(e))
            {
                w2.WriteLine("    case \"" + m.Name + "\":");
                w2.WriteLine("    return await this.rr_" + FixName(m.Name) + ".WireCommand(m,e).ConfigureAwait(false);");
            }
            w2.WriteLine("    default:");
            w2.WriteLine("    throw new MemberNotFoundException(\"Member not found\");");
            w2.WriteLine("    }");
            w2.WriteLine("    }");

            w2.WriteLine("    public override void DispatchPipeMessage(MessageEntry m, Endpoint e)");
            w2.WriteLine("    {");
            w2.WriteLine("    switch (m.MemberName) {");
            foreach (var m in MemberIter<PipeDefinition>(e))
            {
                w2.WriteLine("    case \"" + m.Name + "\":");
                w2.WriteLine("    this.rr_" + FixName(m.Name) + ".PipePacketReceived(m,e);");
                w2.WriteLine("    break;");
            }
            w2.WriteLine("    default:");
            w2.WriteLine("    throw new MemberNotFoundException(\"Member not found\");");
            w2.WriteLine("    }");
            w2.WriteLine("    }");

            w2.WriteLine("    public override void DispatchWireMessage(MessageEntry m, Endpoint e)");
            w2.WriteLine("    {");
            w2.WriteLine("    switch (m.MemberName) {");
            foreach (var m in MemberIter<WireDefinition>(e))
            {
                w2.WriteLine("    case \"" + m.Name + "\":");
                w2.WriteLine("    this.rr_" + FixName(m.Name) + ".WirePacketReceived(m,e);");
                w2.WriteLine("    break;");
            }

            w2.WriteLine("    default:");
            w2.WriteLine("    throw new MemberNotFoundException(\"Member not found\");");
            w2.WriteLine("    }");
            w2.WriteLine("    }");


            w2.WriteLine("    public override async Task<MessageEntry> CallMemoryFunction(MessageEntry m,Endpoint e) {");
            w2.WriteLine("    string ename=m.MemberName;");
            w2.WriteLine("    switch (ename) {");
            foreach (var m in MemberIter<MemoryDefinition>(e))
            {
                var t2 = new TypeDefinition();
                m.Type.CopyTo(ref t2);
                t2.RemoveArray();
                var t = convert_type(t2);
                t.name = FixName(m.Name);
                w2.WriteLine(String.Format("    case \"{0}\":", m.Name));
                if (DataTypeUtil.IsNumber(m.Type.Type))
                {
                    if (m.Type.ArrayType == DataTypes_ArrayTypes.array)
                    {
                        w2.WriteLine(String.Format("     return await (new ArrayMemoryServiceSkel<{1}>(\"{0}\",this," + DirectionStr(m.Direction) + ")).CallMemoryFunction(m,e,obj.{2}).ConfigureAwait(false);", m.Name, t.cs_type, t.name));
                    }
                    else
                    {
                        w2.WriteLine(String.Format("     return await (new MultiDimArrayMemoryServiceSkel<{1}>(\"{0}\",this," + DirectionStr(m.Direction) + ")).CallMemoryFunction(m,e,obj.{2}).ConfigureAwait(false);", m.Name, t.cs_type, t.name));
                    }
                }
                else
                {
                    string c;
                    DataTypes entry_type = m.Type.ResolveNamedType().RRDataType;
                    int elem_size = 0;
                    if (entry_type != DataTypes.namedarray_t)
                    {
                        c = "Pod";
                        elem_size = ServiceDefinitionUtil.EstimatePodPackedElementSize((ServiceEntryDefinition)t2.ResolveNamedType());
                    }
                    else
                    {
                        elem_size = ServiceDefinitionUtil.GetNamedArrayElementTypeAndCount((ServiceEntryDefinition)t2.ResolveNamedType()).Item2;
                        c = "Named";
                    }

                    if (m.Type.ArrayType == DataTypes_ArrayTypes.array)
                    {
                        w2.WriteLine(String.Format("     return await (new " + c + "ArrayMemoryServiceSkel<{1}>(\"{0}\",this," + elem_size + "," + DirectionStr(m.Direction) + ")).CallMemoryFunction(m,e,obj.{2}).ConfigureAwait(false);", m.Name, t.cs_type, t.name));
                    }
                    else
                    {
                        w2.WriteLine(String.Format("     return await (new " + c + "MultiDimArrayMemoryServiceSkel<{1}>(\"{0}\",this," + elem_size + "," + DirectionStr(m.Direction) + ")).CallMemoryFunction(m,e,obj.{2}).ConfigureAwait(false);", m.Name, t.cs_type, t.name));
                    }
                }
                w2.WriteLine("    break;");

            }
            w2.WriteLine("    default:");
            w2.WriteLine("    throw new MemberNotFoundException(\"Member not found\");");
            w2.WriteLine("    }");
            w2.WriteLine("    }");

            w2.WriteLine("    public override bool IsRequestNoLock(MessageEntry m) {");

            foreach (var m in MemberIter<MemberDefinition>(e))
            {
                if (m.NoLock == MemberDefinition_NoLock.all)
                {
                    w2.WriteLine("    if (m.MemberName == \"" + m.Name + "\") return true;");
                }

                if (m.NoLock == MemberDefinition_NoLock.read)
                {
                    if (m is PropertyDefinition)
                    {
                        w2.WriteLine("    if (m.MemberName == \"" + m.Name + "\" && m.EntryType == MessageEntryType.PropertyGetReq) return true;");
                    }
                    if (m is MemoryDefinition)
                    {
                        w2.WriteLine("    if (m.MemberName == \"" + m.Name + "\" && (m.EntryType == MessageEntryType.MemoryRead || m.EntryType == MessageEntryType.MemoryGetParam)) return true;");
                    }
                }
            }

            w2.WriteLine("    return false;");
            w2.WriteLine("    }");

            w2.WriteLine("}");
        }

        public static void GenerateDefaultImpl(ServiceEntryDefinition e, TextWriter w2)
        {
            w2.WriteLine("public class " + FixName(e.Name) + "_default_impl : " + FixName(e.Name) + "{");

            foreach (var m in MemberIter<CallbackDefinition>(e))
            {

                w2.WriteLine("    protected Callback<" + str_pack_delegate(m.Parameters, m.ReturnType, true) + "> rrvar_" + FixName(m.Name) + ";");
            }


            foreach (var m in MemberIter<PipeDefinition>(e))
            {

                if (m.Direction == MemberDefinition_Direction.readonly_)
                {
                    convert_type_result t = convert_type(m.Type);
                    w2.WriteLine("    protected PipeBroadcaster<" + t.cs_type + t.cs_arr_type + "> rrvar_" + FixName(m.Name) + ";");
                }
            }


            foreach (var m in MemberIter<WireDefinition>(e))
            {

                if (m.Direction == MemberDefinition_Direction.readonly_)
                {
                    convert_type_result t = convert_type(m.Type);
                    w2.WriteLine("    protected WireBroadcaster<" + t.cs_type + t.cs_arr_type + "> rrvar_" + FixName(m.Name) + ";");
                }
                if (m.Direction == MemberDefinition_Direction.writeonly)
                {
                    convert_type_result t = convert_type(m.Type);
                    w2.WriteLine("    protected WireUnicastReceiver<" + t.cs_type + t.cs_arr_type + "> rrvar_" + FixName(m.Name) + ";");
                }
            }


            foreach (var m in MemberIter<PropertyDefinition>(e))
            {

                convert_type_result t = convert_type(m.Type);
                t.name = FixName(m.Name);
                w2.WriteLine(String.Format("    public virtual Task<{0}{1}> get_{2}(CancellationToken cancel=default(CancellationToken)) {{", t.cs_type, t.cs_arr_type, t.name));
                w2.WriteLine("    throw new NotImplementedException();");
                w2.WriteLine("    }");
                w2.WriteLine(String.Format("    public virtual Task set_{2}({0}{1} value, CancellationToken cancel=default(CancellationToken)) {{", t.cs_type, t.cs_arr_type, t.name));
                w2.WriteLine("    throw new NotImplementedException();");
                w2.WriteLine("    }");
            }


            foreach (var m in MemberIter<FunctionDefinition>(e))
            {

                if (!m.IsGenerator)
                {
                    convert_type_result t = convert_type(m.ReturnType);
                    string p = str_pack_parameters(m.Parameters, true);
                    if (p.Length == 0)
                    {
                        p = "CancellationToken rr_cancel=default(CancellationToken)";
                    }
                    else
                    {
                        p = String.Join(",", new string[] { p, "CancellationToken rr_cancel=default(CancellationToken)" });
                    }
                    if (m.ReturnType.Type == DataTypes.void_t)
                    {
                        w2.WriteLine("    public virtual Task " + FixName(m.Name) + "(" + p + ") {");
                    }
                    else
                    {
                        w2.WriteLine("    public virtual Task<" + t.cs_type + t.cs_arr_type + "> " + FixName(m.Name) + "(" + p + ") {");
                    }
                }
                else
                {
                    convert_generator_result t = convert_generator(m);
                    string p = str_pack_parameters(t.params_, true);
                    if (p.Length == 0)
                    {
                        p = "CancellationToken rr_cancel=default(CancellationToken)";
                    }
                    else
                    {
                        p = String.Join(",", new string[] { p, "CancellationToken rr_cancel=default(CancellationToken)" });
                    }
                    w2.WriteLine("    public virtual Task<" + t.generator_csharp_type + "> " + FixName(m.Name) + "(" + p + ") {");
                }
                w2.WriteLine("    throw new NotImplementedException();");
                w2.WriteLine("    }");
            }


            foreach (var m in MemberIter<EventDefinition>(e))
            {

                string params_ = str_pack_parameters(m.Parameters, true);
                w2.WriteLine("    public virtual event " + str_pack_delegate(m.Parameters, null, false) + " " + FixName(m.Name) + ";");
            }

            foreach (var m in MemberIter<ObjRefDefinition>(e))
            {

                string objtype = FixName(m.ObjectType);
                if (objtype == "varobject") objtype = "object";
                string indtype;
                if (GetObjRefIndType(m, out indtype))
                {
                    w2.WriteLine("    public virtual Task<" + objtype + "> get_" + FixName(m.Name) + "(" + indtype + " ind, CancellationToken cancel=default(CancellationToken)) {");
                    w2.WriteLine("    throw new NotImplementedException();");
                    w2.WriteLine("    }");
                }
                else
                {
                    w2.WriteLine("    public virtual Task<" + objtype + "> get_" + FixName(m.Name) + "(CancellationToken cancel=default(CancellationToken)) {");
                    w2.WriteLine("    throw new NotImplementedException();");
                    w2.WriteLine("    }");
                }
            }



            foreach (var m in MemberIter<PipeDefinition>(e))
            {

                convert_type_result t = convert_type(m.Type);
                w2.WriteLine("    public virtual Pipe<" + t.cs_type + t.cs_arr_type + "> " + FixName(m.Name) + " {");
                if (m.Direction == MemberDefinition_Direction.readonly_)
                {
                    w2.WriteLine("    get { return rrvar_" + FixName(m.Name) + ".Pipe;  }");
                }
                else
                {
                    w2.WriteLine("    get { throw new NotImplementedException(); }");
                }
                if (m.Direction == MemberDefinition_Direction.readonly_)
                {
                    w2.WriteLine("    set {");
                    w2.WriteLine("    if (rrvar_" + FixName(m.Name) + "!=null) throw new InvalidOperationException(\"Pipe already set\");");
                    w2.WriteLine("    rrvar_" + FixName(m.Name) + "= new PipeBroadcaster<" + t.cs_type + t.cs_arr_type + ">(value);");
                    w2.WriteLine("    }");
                }
                else
                {
                    w2.WriteLine("    set { throw new InvalidOperationException();}");
                }
                w2.WriteLine("    }");
            }


            foreach (var m in MemberIter<CallbackDefinition>(e))
            {

                w2.WriteLine("    public virtual Callback<" + str_pack_delegate(m.Parameters, m.ReturnType, true) + "> " + FixName(m.Name) + " {");
                w2.WriteLine("    get { return rrvar_" + FixName(m.Name) + ";  }");
                w2.WriteLine("    set {");
                w2.WriteLine("    if (rrvar_" + FixName(m.Name) + "!=null) throw new InvalidOperationException(\"Callback already set\");");
                w2.WriteLine("    rrvar_" + FixName(m.Name) + "= value;");
                w2.WriteLine("    }");
                w2.WriteLine("    }");
            }


            foreach (var m in MemberIter<WireDefinition>(e))
            {

                convert_type_result t = convert_type(m.Type);
                w2.WriteLine("    public virtual Wire<" + t.cs_type + t.cs_arr_type + "> " + FixName(m.Name) + " {");
                if (m.Direction == MemberDefinition_Direction.readonly_ || m.Direction == MemberDefinition_Direction.writeonly)
                {
                    w2.WriteLine("    get { return rrvar_" + FixName(m.Name) + ".Wire;  }");
                }
                else
                {
                    w2.WriteLine("    get { throw new NotImplementedException(); }");
                }
                if (m.Direction == MemberDefinition_Direction.readonly_)
                {
                    w2.WriteLine("    set {");
                    w2.WriteLine("    if (rrvar_" + FixName(m.Name) + "!=null) throw new InvalidOperationException(\"Pipe already set\");");
                    w2.WriteLine("    rrvar_" + FixName(m.Name) + "= new WireBroadcaster<" + t.cs_type + t.cs_arr_type + ">(value);");
                    w2.WriteLine("    }");
                }
                else if (m.Direction == MemberDefinition_Direction.writeonly)
                {
                    w2.WriteLine("    set {");
                    w2.WriteLine("    if (rrvar_" + FixName(m.Name) + "!=null) throw new InvalidOperationException(\"Pipe already set\");");
                    w2.WriteLine("    rrvar_" + FixName(m.Name) + "= new WireUnicastReceiver<" + t.cs_type + t.cs_arr_type + ">(value);");
                    w2.WriteLine("    }");
                }
                else
                {
                    w2.WriteLine("    set { throw new NotImplementedException();}");
                }
                w2.WriteLine("    }");
            }



            foreach (var m in MemberIter<MemoryDefinition>(e))
            {

                var t2 = new TypeDefinition();
                m.Type.CopyTo(ref t2);
                t2.RemoveArray();
                convert_type_result t = convert_type(t2);

                string c = "";
                if (!DataTypeUtil.IsNumber(m.Type.Type))
                {
                    DataTypes entry_type = m.Type.ResolveNamedType().RRDataType;
                    if (entry_type != DataTypes.namedarray_t)
                    {
                        c = "Pod";
                    }
                    else
                    {
                        c = "Named";
                    }
                }
                switch (m.Type.ArrayType)
                {
                    case DataTypes_ArrayTypes.array:
                        w2.WriteLine("    public virtual " + c + "ArrayMemory<" + t.cs_type + "> " + FixName(m.Name) + " { ");
                        break;
                    case DataTypes_ArrayTypes.multidimarray:

                        w2.WriteLine("    public virtual " + c + "MultiDimArrayMemory<" + t.cs_type + "> " + FixName(m.Name) + " {");
                        break;
                    default:
                        throw new DataTypeException("Invalid memory definition");
                }
                w2.WriteLine("    get { throw new NotImplementedException(); }");
                w2.WriteLine("    }");
            }


            w2.WriteLine("}");

        }

        static string EscapeString_Formatter(Match match)
        {
            string i = match.Groups[0].Value;

            if (i == "\"") return "\\\"";
            if (i == "\\") return "\\\\";
            if (i == "/") return "/";
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

        public static string convert_constant(ConstantDefinition c, Dictionary<string, ConstantDefinition> c2, ServiceDefinition def)
        {

            var t = c.Type;
            if (t.ContainerType != DataTypes_ContainerTypes.none) throw new DataTypeException("Only numbers, primitive number arrays, and strings can be constants");
            switch (t.ArrayType)
            {
                case DataTypes_ArrayTypes.none:
                    break;
                case DataTypes_ArrayTypes.array:
                    if (!t.ArrayVarLength)
                        throw new DataTypeException("Only numbers, primitive number arrays, and strings can be constants");
                    break;
                default:
                    throw new DataTypeException("Only numbers, primitive number arrays, and strings can be constants");
            }

            if (t.Type == DataTypes.namedtype_t)
            {
                var f = c.ValueToStructFields();

                var o = "public static class " + FixName(c.Name) + " { ";

                foreach (var f2 in f)

                {
                    ConstantDefinition c3;
                    if (!c2.TryGetValue(f2.ConstantRefName, out c3))
                        throw new ServiceException("Invalid structure cosntant " + c.Name);
                    o += convert_constant(c3, c2, def) + " ";
                }

                o += "}";
                return o;
            }

            convert_type_result c1 = convert_type(t);
            if (t.Type == DataTypes.string_t)
            {
                var r_replace = new Regex("(\"|\\\\|\\/|[\\x00-\\x1F]|\\x7F|[\\x80-\\xFF]+)");


                var str_value = c.ValueToString();

                var str_res = r_replace.Replace(str_value, EscapeString_Formatter);

                return "public const string " + FixName(c.Name) + "=\"" + str_res + "\";";
            }

            if (t.ArrayType == DataTypes_ArrayTypes.none)
            {
                return "public const " + c1.cs_type + " " + FixName(c.Name) + "=" + c.Value + ";";
            }
            else
            {
                return "public static readonly " + c1.cs_type + "[] " + FixName(c.Name) + "=" + c.Value + ";";
            }
        }

        public static void GenerateConstants(ServiceDefinition d, TextWriter w2)
        {
            bool hasconstants = false;

            foreach (var e in d.Options)
            {
                if (e.StartsWith("constant")) hasconstants = true;
            }

            if (d.Enums.Count != 0 || d.Constants.Count != 0) hasconstants = true;

            foreach (var ee in d.Objects)
            {
                foreach (var e in ee.Value.Options)
                {
                    if (e.StartsWith("constant")) hasconstants = true;
                }

                if (ee.Value.Constants.Count != 0) hasconstants = true;
            }

            if (!hasconstants) return;


            w2.WriteLine("public static class " + FixName(d.Name).Replace(".", "__") + "Constants " + " {");


            foreach (var e in d.Options)
            {
                if (e.StartsWith("constant"))
                {
                    var c = new ConstantDefinition(d);
                    c.FromString(e);
                    w2.WriteLine("    " + convert_constant(c, d.Constants, d));
                }
            }

            foreach (var c in d.Constants)

            {
                w2.WriteLine("    " + convert_constant(c.Value, d.Constants, d));
            }

            foreach (var ee in d.Objects)
            {
                bool objhasconstants = false;

                foreach (var e in ee.Value.Options)
                {
                    if (e.StartsWith("constant")) objhasconstants = true;
                }

                if (ee.Value.Constants.Count != 0) objhasconstants = true;

                if (objhasconstants)
                {
                    w2.WriteLine("    public static class " + FixName(ee.Value.Name));
                    w2.WriteLine("    {");
                    foreach (var e in ee.Value.Options)
                    {
                        if (e.StartsWith("constant"))
                        {
                            var c = new ConstantDefinition(d);
                            c.FromString(e);
                            w2.WriteLine("    " + convert_constant(c, ee.Value.Constants, d));
                        }
                    }

                    foreach (var c in ee.Value.Constants)

                    {
                        w2.WriteLine("    " + convert_constant(c.Value, ee.Value.Constants, d));
                    }

                    w2.WriteLine("    }");
                }
            }

            w2.WriteLine("}");

            foreach (var e in d.Enums)

            {
                w2.WriteLine("    public enum " + FixName(e.Value.Name));
                w2.WriteLine("    {");
                for (int i = 0; i < e.Value.Values.Count; i++)
                {
                    var v = e.Value.Values[i];
                    if (!v.HexValue)
                    {
                        w2.Write("    " + FixName(v.Name) + " = " + v.Value);
                    }
                    else
                    {
                        if (v.Value >= 0)
                        {
                            w2.Write("    " + FixName(v.Name) + " = 0x" + v.Value.ToString("x"));
                        }
                        else
                        {
                            w2.Write("    " + FixName(v.Name) + " = " + v.Value);
                        }
                    }
                    if (i + 1 < e.Value.Values.Count)
                    {
                        w2.WriteLine(",");
                    }
                    else
                    {
                        w2.WriteLine();
                    }
                }
                w2.WriteLine("    };");
            }

        }

        public static string GetDefaultValue(TypeDefinition tdef)
        {

            convert_type_result tt = convert_type(tdef);
            return "default(" + tt.cs_type + tt.cs_arr_type + ")";
        }

        public static string GetDefaultInitializedValue(TypeDefinition tdef)
        {
            if (tdef.Type == DataTypes.void_t) throw new InternalErrorException("Internal error");

            if (tdef.ContainerType == DataTypes_ContainerTypes.none)
            {
                if (DataTypeUtil.IsNumber(tdef.Type))
                {
                    switch (tdef.ArrayType)
                    {
                        case DataTypes_ArrayTypes.none:
                            {
                                return GetDefaultValue(tdef);
                            }
                        case DataTypes_ArrayTypes.array:
                            {
                                var tdef2 = tdef.Clone();
                                tdef2.RemoveContainers();
                                tdef2.RemoveArray();
                                convert_type_result t = convert_type(tdef2);
                                if (tdef.ArrayVarLength)
                                {
                                    return "new " + t.cs_type + "[0]";
                                }
                                else
                                {
                                    return "new " + t.cs_type + "[" + tdef.ArrayLength[0].ToString() + "]";
                                }
                            }
                        case DataTypes_ArrayTypes.multidimarray:
                            {
                                var tdef2 = tdef.Clone();
                                tdef2.RemoveContainers();
                                tdef2.RemoveArray();
                                convert_type_result t = convert_type(tdef2);
                                if (tdef.ArrayVarLength)
                                {
                                    return "new MultiDimArray(new uint[] {1,0}, new " + t.cs_type + "[0])";
                                }
                                else
                                {
                                    int n_elems = tdef.ArrayLength.Aggregate(1, (x, y) => x * y);
                                    return "new MultiDimArray(new uint[] {" + String.Join(",", tdef.ArrayLength.Select(x => x.ToString())) + "}, new " + t.cs_type + "[" + n_elems.ToString() + "])";

                                }
                            }
                        default:
                            throw new ArgumentException("Invalid array type");
                    }
                }
                if (tdef.Type == DataTypes.string_t)
                {
                    return "\"\"";
                }
                if (tdef.Type == DataTypes.namedtype_t)
                {
                    var tdef2 = tdef.Clone();
                    tdef2.RemoveContainers();
                    tdef2.RemoveArray();

                    if (tdef2.ResolveNamedType().RRDataType == DataTypes.pod_t)
                    {
                        switch (tdef.ArrayType)
                        {
                            case DataTypes_ArrayTypes.none:
                                {
                                    return GetDefaultValue(tdef);
                                }
                            case DataTypes_ArrayTypes.array:
                                {
                                    convert_type_result t = convert_type(tdef2);
                                    if (tdef.ArrayVarLength)
                                    {
                                        return "new " + t.cs_type + "[0]";
                                    }
                                    else
                                    {
                                        return "new " + t.cs_type + "[" + tdef.ArrayLength[0].ToString() + "]";
                                    }
                                }
                            case DataTypes_ArrayTypes.multidimarray:
                                {
                                    convert_type_result t = convert_type(tdef2);
                                    if (tdef.ArrayVarLength)
                                    {
                                        return "new PodMultiDimArray(new uint[] {1,0}, new " + t.cs_type + "[0])";
                                    }
                                    else
                                    {
                                        int n_elems = tdef.ArrayLength.Aggregate(1, (x, y) => x * y);
                                        return "new PodMultiDimArray(new uint[] {" + String.Join(",", tdef.ArrayLength.Select(x => x.ToString())) + "}, new " + t.cs_type + "[" + n_elems.ToString() + "])";
                                    }
                                }
                            default:
                                throw new ArgumentException("Invalid array type");
                        }
                    }

                    if (tdef2.ResolveNamedType().RRDataType == DataTypes.namedarray_t)
                    {
                        switch (tdef.ArrayType)
                        {
                            case DataTypes_ArrayTypes.none:
                                {
                                    return GetDefaultValue(tdef);
                                }
                            case DataTypes_ArrayTypes.array:
                                {
                                    convert_type_result t = convert_type(tdef2);
                                    if (tdef.ArrayVarLength)
                                    {
                                        return "new " + t.cs_type + "[0]";
                                    }
                                    else
                                    {
                                        return "new " + t.cs_type + "[" + tdef.ArrayLength[0].ToString() + "]";
                                    }
                                }
                            case DataTypes_ArrayTypes.multidimarray:
                                {
                                    convert_type_result t = convert_type(tdef2);
                                    if (tdef.ArrayVarLength)
                                    {
                                        return "new NamedMultiDimArray(new uint[] {1,0}, new " + t.cs_type + "[0])";
                                    }
                                    else
                                    {
                                        int n_elems = tdef.ArrayLength.Aggregate(1, (x, y) => x * y);
                                        return "new NamedMultiDimArray(new uint[] {" + String.Join(",", tdef.ArrayLength.Select(x => x.ToString())) + "}, new " + t.cs_type + "[" + n_elems.ToString() + "])";
                                    }
                                }
                            default:
                                throw new ArgumentException("Invalid array type");
                        }
                    }
                }
            }

            return GetDefaultValue(tdef);
        }

        public static void GenerateFiles(ServiceDefinition d, string servicedef, string path)
        {

            string fname = Path.Combine(path, d.Name + ".cs");
            using (var f1 = new StreamWriter(fname))
            {
                GenerateInterfaceFile(d, f1, false);
            }


            string fname2 = Path.Combine(path, d.Name + "_stubskel.cs");
            using (var f2 = new StreamWriter(fname2))
            {
                GenerateStubSkelFile(d, servicedef, f2, false);
            }

        }

        public static void GenerateOneFileHeader(TextWriter w2)
        {
            w2.WriteLine("//This file is automatically generated. DO NOT EDIT!");
            w2.WriteLine("using System;");
            w2.WriteLine("using RobotRaconteurWeb;");
            w2.WriteLine("using RobotRaconteurWeb.Extensions;");
            w2.WriteLine("using System.Collections.Generic;");
            w2.WriteLine("using System.Threading;");
            w2.WriteLine("using System.Threading.Tasks;");
            w2.WriteLine();
            w2.WriteLine("#pragma warning disable 0108");
            w2.WriteLine();
        }

        public static void GenerateOneFilePart(ServiceDefinition d, string servicedef, TextWriter w2)
        {
            GenerateInterfaceFile(d, w2, true);
            GenerateStubSkelFile(d, servicedef, w2, true);
        }

    }
}
