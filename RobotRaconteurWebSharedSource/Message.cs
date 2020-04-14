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

#pragma warning disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RobotRaconteurWeb
{

    public enum DataTypes
    {
        void_t = 0,
        double_t,
        single_t,
        int8_t,
        uint8_t,
        int16_t,
        uint16_t,
        int32_t,
        uint32_t,
        int64_t,
        uint64_t,
        string_t,
        cdouble_t,
        csingle_t,
        bool_t,
        structure_t = 101,
        vector_t,
        dictionary_t,
        object_t,
        varvalue_t,
        varobject_t,
        list_t = 108,
        pod_t,
        pod_array_t,
        pod_multidimarray_t,
        enum_t,
        namedtype_t,
        namedarray_t,
        namedarray_array_t,
        namedarray_multidimarray_t,
        multidimarray_t
    }

    public enum DataTypes_ArrayTypes
    {
        none = 0,
        array,
        multidimarray
    }

    public enum DataTypes_ContainerTypes
    {
        none = 0,
        list,
        map_int32,
        map_string,
        generator
    }

    public enum MessageEntryType
    {
        Null = 0,
        StreamOp = 1,
        StreamOpRet,
        StreamCheckCapability,
        StreamCheckCapabilityRet,
        StringTableOp,
        StringTableOpRet,
        GetServiceDesc = 101,
        GetServiceDescRet,
        ObjectTypeName,
        ObjectTypeNameRet,
        ServiceClosed,
        ServiceClosedRet,
        ConnectClient,
        ConnectClientRet,
        DisconnectClient,
        DisconnectClientRet,
        ConnectionTest,
        ConnectionTestRet,
        GetNodeInfo,
        GetNodeInfoRet,
        ReconnectClient,
        ReconnectClientRet,
        NodeCheckCapability,
        NodeCheckCapabilityRet,
        GetServiceAttributes,
        GetServiceAttributesRet,
        ConnectClientCombined,
        ConnectClientCombinedRet,
        EndpointCheckCapability = 501,
        EndpointCheckCapabilityRet,
        ServiceCheckCapabilityReq = 1101,
        ServiceCheckCapabilityRet,
        ClientKeepAliveReq = 1105,
        ClientKeepAliveRet,
        ClientSessionOpReq = 1107,
        ClientSessionOpRet,
        ServicePathReleasedReq,
        ServicePathReleasedRet,
        PropertyGetReq = 1111,
        PropertyGetRes,
        PropertySetReq,
        PropertySetRes,
        FunctionCallReq = 1121,
        FunctionCallRes,
        GeneratorNextReq,
        GeneratorNextRes,
        EventReq = 1131,
        EventRes,
        PipePacket = 1141,
        PipePacketRet,
        PipeConnectReq,
        PipeConnectRet,
        PipeDisconnectReq,
        PipeDisconnectRet,
        PipeClosed,
        PipeClosedRet,
        CallbackCallReq = 1151,
        CallbackCallRet,
        WirePacket = 1161,
        WirePacketRet,
        WireConnectReq,
        WireConnectRet,
        WireDisconnectReq,
        WireDisconnectRet,
        WireClosed,
        WireClosedRet,
        MemoryRead = 1171,
        MemoryReadRet,
        MemoryWrite,
        MemoryWriteRet,
        MemoryGetParam,
        MemoryGetParamRet,
        WirePeekInValueReq = 1181,
        WirePeekInValueRet,
        WirePeekOutValueReq,
        WirePeekOutValueRet,
        WirePokeOutValueReq,
        WirePokeOutValueRet,
        WireTransportOpReq = 11161,
        WireTransportOpRet,
        WireTransportEvent,
        WireTransportEventRet
    }

    public enum MessageErrorType
    {
        None = 0,
        ConnectionError = 1,
        ProtocolError,
        ServiceNotFound,
        ObjectNotFound,
        InvalidEndpoint,
        EndpointCommunicationFatalError,
        NodeNotFound,
        ServiceError,
        MemberNotFound,
        MemberFormatMismatch,
        DataTypeMismatch,
        DataTypeError,
        DataSerializationError,
        MessageEntryNotFound,
        MessageElementNotFound,
        UnknownError,
        InvalidOperation,
        InvalidArgument,
        OperationFailed,
        NullValue,
        InternalError,
        SystemResourcePermissionDenied,
        OutOfSystemResource,
        SystemResourceError,
        ResourceNotFound,
        IOError,
        BufferLimitViolation,
        ServiceDefinitionError,
        OutOfRange,
        KeyNotFound,
        RemoteError = 100,
        RequestTimeout,
        ReadOnlyMember,
        WriteOnlyMember,
        NotImplementedError,
        MemberBusy,
        ValueNotSet,
        AbortOperation,
        OperationAborted,
        StopIteration,
        AuthenticationError = 150,
        ObjectLockedError,
        PermissionDenied      
    }

    public struct CDouble
    {
        public double Real;
        public double Imag;

        public CDouble(double real, double imag)
        {
            Real = real;
            Imag = imag;
        }

        public static bool operator ==(CDouble a, CDouble b)
        {
            return (a.Real == b.Real) && (a.Imag == b.Imag);
        }

        public static bool operator !=(CDouble a, CDouble b)
        {
            return !((a.Real == b.Real) && (a.Imag == b.Imag));
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is CDouble)) return false;
            return ((CDouble)obj) == this;
        }

        public override int GetHashCode()
        {
            return (int)(Real % 1e7 + Imag % 1e7);
        }
    }

    public struct CSingle
    {
        public float Real;
        public float Imag;

        public CSingle(float real, float imag)
        {
            Real = real;
            Imag = imag;
        }

        public static bool operator ==(CSingle a, CSingle b)
        {
            return (a.Real == b.Real) && (a.Imag == b.Imag);
        }

        public static bool operator !=(CSingle a, CSingle b)
        {
            return !((a.Real == b.Real) && (a.Imag == b.Imag));
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is CSingle)) return false;
            return ((CSingle)obj) == this;
        }

        public override int GetHashCode()
        {
            return (int)(Real % 1e7 + Imag % 1e7);
        }

    }


    public static class DataTypeUtil
    {
        public static uint size(DataTypes type)
        {
            switch (type)
            {
                case DataTypes.double_t:
                    return 8;

                case DataTypes.single_t:
                    return 4;
                case DataTypes.int8_t:
                case DataTypes.uint8_t:
                    return 1;
                case DataTypes.int16_t:
                case DataTypes.uint16_t:
                    return 2;
                case DataTypes.int32_t:
                case DataTypes.uint32_t:
                    return 4;
                case DataTypes.int64_t:
                case DataTypes.uint64_t:
                    return 8;
                case DataTypes.string_t:
                    return 1;
                case DataTypes.cdouble_t:
                    return 16;
                case DataTypes.csingle_t:
                    return 8;
                case DataTypes.bool_t:
                    return 1;
                default:
                    throw new DataTypeException("Invalid data type");                    
            }
        }

        public static DataTypes TypeIDFromString(string stype)
        {
            switch (stype)
            {
                case "null":
                    return DataTypes.void_t;
                case "System.Double":
                    return DataTypes.double_t;
                case "System.Single":
                    return DataTypes.single_t;
                case "System.SByte":
                    return DataTypes.int8_t;
                case "System.Byte":
                    return DataTypes.uint8_t;
                case "System.Int16":
                    return DataTypes.int16_t;
                case "System.UInt16":
                    return DataTypes.uint16_t;
                case "System.Int32":
                    return DataTypes.int32_t;
                case "System.UInt32":
                    return DataTypes.uint32_t;
                case "System.Int64":
                    return DataTypes.int64_t;
                case "System.UInt64":
                    return DataTypes.uint64_t;
                case "System.String":
                    return DataTypes.string_t;
                case "System.Boolean":                
                    return DataTypes.bool_t;
                case "RobotRaconteurWeb.CDouble":
                    return DataTypes.cdouble_t;
                case "RobotRaconteurWeb.CSingle":
                    return DataTypes.csingle_t;
                case "RobotRaconteurWeb.MessageElementStructure":
                    return DataTypes.structure_t;
                case "RobotRaconteurWeb.MessageElementMap<int>":
                    return DataTypes.vector_t;
                case "RobotRaconteurWeb.MessageElementMap<string>":
                    return DataTypes.dictionary_t;
                case "RobotRaconteurWeb.MessageElementMultiDimArray":
                    return DataTypes.multidimarray_t;
                case "RobotRaconteurWeb.MessageElementList":
                    return DataTypes.list_t;
                case "RobotRaconteurWeb.MessageElementPod":
                    return DataTypes.pod_t;
                case "RobotRaconteurWeb.MessageElementPodArray":
                    return DataTypes.pod_array_t;
                case "RobotRaconteurWeb.MessageElementPodMultiDimArray":
                    return DataTypes.pod_multidimarray_t;
                case "RobotRaconteurWeb.MessageElementNamedArray":
                    return DataTypes.namedarray_array_t;
                case "RobotRaconteurWeb.MessageElementNamedMultiDimArray":
                    return DataTypes.namedarray_multidimarray_t;
                case "System.Object":
                    return DataTypes.varvalue_t;

            }

            

            throw new DataTypeException("Unknown data type");
        }

        public static bool TypeIDFromString_known(string stype)
        {
            switch (stype)
            {
                case "null":
                    
                case "System.Double":
                    
                case "System.Single":
                    
                case "System.SByte":
                   
                case "System.Byte":
                    
                case "System.Int16":
                    
                case "System.UInt16":
                    
                case "System.Int32":
                    
                case "System.UInt32":
                    
                case "System.Int64":
                    
                case "System.UInt64":
                    
                case "System.String":

                case "System.Boolean":

                case "RobotRaconteurWeb.CDouble":

                case "RobotRaconteurWeb.CSingle":

                case "RobotRaconteurWeb.MessageElementStructure":
                    
                case "RobotRaconteurWeb.MessageElementIndexedSet<int>":
                    
                case "RobotRaconteurWeb.MessageElementIndexedSet<string>":
                    
                case "RobotRaconteurWeb.MessageElementMultiDimArray":
                    
                case "System.Object":
                    return true;

            }



            return false;
        }

        public static bool IsNumber(DataTypes t)
        {
            switch (t)
            {
                case DataTypes.double_t:
                case DataTypes.single_t:
                case DataTypes.int8_t:
                case DataTypes.uint8_t:
                case DataTypes.int16_t:
                case DataTypes.uint16_t:
                case DataTypes.int32_t:
                case DataTypes.uint32_t:
                case DataTypes.int64_t:
                case DataTypes.uint64_t:
                case DataTypes.cdouble_t:
                case DataTypes.csingle_t:
                case DataTypes.bool_t:
                    return true;
                default:
                    return false;
            }
        }

        public static Array ArrayFromDataType(DataTypes t, uint length)
        {
            switch (t)
            {
                
                case DataTypes.double_t:
                    return new double[length];
                case DataTypes.single_t:
                    return new float[length];
                case  DataTypes.int8_t:
                    return new sbyte[length];
                case DataTypes.uint8_t:
                    return new byte[length];
                case  DataTypes.int16_t:
                    return new short[length];
                case  DataTypes.uint16_t:
                    return new ushort[length];
                case  DataTypes.int32_t:
                    return new int[length];
                case  DataTypes.uint32_t:
                    return new uint[length];
                case  DataTypes.int64_t:
                    return new long[length];
                case  DataTypes.uint64_t:
                    return new ulong[length];
                case  DataTypes.string_t:
                    return null;
                case DataTypes.cdouble_t:
                    return new CDouble[length];
                case DataTypes.csingle_t:
                    return new CSingle[length];
                case DataTypes.bool_t:
                    return new bool[length];
                case  DataTypes.structure_t:
                    return null;
            }

            throw new DataTypeException("Could not create array for data type");
        }

        public static Object ArrayFromScalar(Object inv)
        {

            if (inv is Array) return inv;

            string stype = inv.GetType().ToString();
            switch (stype)
            {
                case "System.Double":
                    return new double[] {((double)inv)};
                case "System.Single":
                    return new float[] {((float)inv)};
                case "System.SByte":
                    return new sbyte[] { ((sbyte)inv) };
                case "System.Byte":
                    return new byte[] { ((byte)inv) };
                case "System.Int16":
                    return new short[] { ((short)inv) };
                case "System.UInt16":
                    return new ushort[] { ((ushort)inv) };
                case "System.Int32":
                    return new int[] { ((int)inv) };
                case "System.UInt32":
                    return new uint[] { ((uint)inv) };
                case "System.Int64":
                    return new long[] { ((long)inv) };
                case "System.UInt64":
                    return new ulong[] { ((ulong)inv) };
                case "RobotRaconteurWeb.CDouble":
                    return new CDouble[] { ((CDouble)inv) };
                case "RobotRaconteurWeb.CSingle":
                    return new CSingle[] { ((CSingle)inv) };
                case "System.Boolean":
                    return new bool[] { ((bool)inv) };
            }

            throw new DataTypeException("Could not create array for data");
        }

        public static DataTypes TypeIDFromType(Type stype)
        {
            switch (Type.GetTypeCode(stype))
            {                
                case TypeCode.Double:
                    return DataTypes.double_t;
                case TypeCode.Single:
                    return DataTypes.single_t;
                case TypeCode.SByte:
                    return DataTypes.int8_t;
                case TypeCode.Byte:
                    return DataTypes.uint8_t;
                case TypeCode.Int16:
                    return DataTypes.int16_t;
                case TypeCode.UInt16:
                    return DataTypes.uint16_t;
                case TypeCode.Int32:
                    return DataTypes.int32_t;
                case TypeCode.UInt32:
                    return DataTypes.uint32_t;
                case TypeCode.Int64:
                    return DataTypes.int64_t;
                case TypeCode.UInt64:
                    return DataTypes.uint64_t;
                case TypeCode.String:
                    return DataTypes.string_t;
                case TypeCode.Boolean:
                    return DataTypes.bool_t;
                /*case "RobotRaconteurWeb.CDouble":
                    return DataTypes.cdouble_t;
                case "RobotRaconteurWeb.CSingle":
                    return DataTypes.csingle_t;
                case "RobotRaconteurWeb.MessageElementStructure":
                    return DataTypes.structure_t;
                case "RobotRaconteurWeb.MessageElementMap<int>":
                    return DataTypes.vector_t;
                case "RobotRaconteurWeb.MessageElementMap<string>":
                    return DataTypes.dictionary_t;
                case "RobotRaconteurWeb.MessageElementMultiDimArray":
                    return DataTypes.multidimarray_t;
                case "RobotRaconteurWeb.MessageElementList":
                    return DataTypes.list_t;*/
                case TypeCode.Object:
                    {
                        if (stype == typeof(CDouble))
                        {
                            return DataTypes.cdouble_t;
                        }
                        if (stype == typeof(CSingle))
                        {
                            return DataTypes.csingle_t;
                        }
                        return DataTypes.varvalue_t;
                    }
                default:
                    throw new DataTypeException("Unknown data type");
            }
            
        }

        public static T[] VerifyArrayLength<T>(T[] a, int len, bool varlength) where T : struct
        {
            if (a == null) throw new NullReferenceException();
            if (len != 0)
            {
                if (varlength && a.Length > len)
                {
                    throw new DataTypeException("Array dimension mismatch");
                }
                if (!varlength && a.Length != len)
                {
                    throw new DataTypeException("Array dimension mismatch");
                }
            }
            return a;
        }


        public static MultiDimArray VerifyArrayLength(MultiDimArray a, int n_elems, uint[] len)
        {
            if (a.Dims.Length != len.Length)
            {
                throw new DataTypeException("Array dimension mismatch");
            }

            for (int i = 0; i < len.Length; i++)
            {
                if (a.Dims[i] != len[i])
                {
                    throw new DataTypeException("Array dimension mismatch");
                }
            }
            return a;
        }

        public static PodMultiDimArray VerifyArrayLength(PodMultiDimArray a, int n_elems, uint[] len)
        {
            if (a.Dims.Length != len.Length)
            {
                throw new DataTypeException("Array dimension mismatch");
            }

            for (int i = 0; i < len.Length; i++)
            {
                if (a.Dims[i] != len[i])
                {
                    throw new DataTypeException("Array dimension mismatch");
                }
            }
            return a;
        }

        public static NamedMultiDimArray VerifyArrayLength(NamedMultiDimArray a, int n_elems, uint[] len)
        {
            if (a.Dims.Length != len.Length)
            {
                throw new DataTypeException("Array dimension mismatch");
            }

            for (int i = 0; i < len.Length; i++)
            {
                if (a.Dims[i] != len[i])
                {
                    throw new DataTypeException("Array dimension mismatch");
                }
            }
            return a;
        }

        public static List<T[]> VerifyArrayLength<T>(List<T[]> a, int len, bool varlength) where T : struct
        {
            if (a == null) return a;
            foreach (T[] aa in a)
            {
                VerifyArrayLength(aa, len, varlength);
            }

            return a;
        }

        public static Dictionary<K, T[]> VerifyArrayLength<K, T>(Dictionary<K, T[]> a, int len, bool varlength) where T : struct
        {
            if (a == null) return a;
            foreach (T[] aa in a.Values)
            {
                VerifyArrayLength(aa, len, varlength);
            }

            return a;
        }

        public static List<MultiDimArray> VerifyArrayLength(List<MultiDimArray> a, int n_elems, uint[] len)
        {
            if (a == null) return a;
            foreach (MultiDimArray aa in a)
            {
                VerifyArrayLength(aa, n_elems, len);
            }

            return a;
        }

        public static Dictionary<K, MultiDimArray> VerifyArrayLength<K>(Dictionary<K, MultiDimArray> a, int n_elems, uint[] len)
        {
            if (a == null) return a;
            foreach (MultiDimArray aa in a.Values)
            {
                VerifyArrayLength(aa, n_elems, len);
            }

            return a;
        }

        public static List<PodMultiDimArray> VerifyArrayLength(List<PodMultiDimArray> a, int n_elems, uint[] len)
        {
            if (a == null) return a;
            foreach (PodMultiDimArray aa in a)
            {
                VerifyArrayLength(aa, n_elems, len);
            }

            return a;
        }

        public static Dictionary<K, PodMultiDimArray> VerifyArrayLength<K>(Dictionary<K, PodMultiDimArray> a, int n_elems, uint[] len)
        {
            if (a == null) return a;
            foreach (PodMultiDimArray aa in a.Values)
            {
                VerifyArrayLength(aa, n_elems, len);
            }

            return a;
        }

        public static List<NamedMultiDimArray> VerifyArrayLength(List<NamedMultiDimArray> a, int n_elems, uint[] len)
        {
            if (a == null) return a;
            foreach (NamedMultiDimArray aa in a)
            {
                VerifyArrayLength(aa, n_elems, len);
            }

            return a;
        }

        public static Dictionary<K, NamedMultiDimArray> VerifyArrayLength<K>(Dictionary<K, NamedMultiDimArray> a, int n_elems, uint[] len)
        {
            if (a == null) return a;
            foreach (NamedMultiDimArray aa in a.Values)
            {
                VerifyArrayLength(aa, n_elems, len);
            }

            return a;
        }
    }    
    

    public class Message
    {
        public MessageHeader header;
        public List<MessageEntry> entries;

        public Message()
        {
            entries=new List<MessageEntry>();
        }

        public uint ComputeSize()
        {
            uint s=header.ComputeSize();
            foreach (MessageEntry e in entries)
            {
                s += e.ComputeSize();
            }
            return s;
        }

        public void Write(ArrayBinaryWriter w)
        {
            w.PushRelativeLimit(ComputeSize());
            header.UpdateHeader(ComputeSize(), (ushort)entries.Count);
            header.Write(w);
            foreach (MessageEntry e in entries)
            {
                e.Write(w);
            }
            if (w.DistanceFromLimit != 0) throw new IOException("Message write error");
            w.PopLimit();

        }

        public MessageEntry FindEntry(string name)
        {

            if (entries == null) return null;

            foreach (MessageEntry m in entries)
            {
                if (m.MemberName == name)
                    return m;
            }

            throw new MessageEntryNotFoundException("Element " + name + " not found.");

        }

        public MessageEntry AddEntry(MessageEntryType t,string name)
        {
            MessageEntry m = new MessageEntry();
            m.MemberName = name;
            m.EntryType = t;

            if (entries == null) entries = new List<MessageEntry>();
            entries.Add(m);

            return m;
        }

        public void Read(ArrayBinaryReader r)
        {
            header = new MessageHeader();
            header.Read(r);

            r.PushRelativeLimit(header.MessageSize - header.HeaderLength);

            ushort s = header.EntryCount;
            entries=new List<MessageEntry>(s);
            for (int i=0; i<s; i++) 
            {
                MessageEntry e=new MessageEntry();
                e.Read(r);
                entries.Add(e);
            }

            r.PopLimit();

        }
    }

    public class MessageHeader
    {
        public ushort HeaderLength;
        public uint SenderEndpoint;
        public uint ReceiverEndpoint;
        public string SenderNodeName="";
        public string ReceiverNodeName = "";
        public NodeID SenderNodeID=new NodeID(new byte[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0});
        public NodeID ReceiverNodeID = new NodeID(new byte[] { 0, 0, 0, 0,0,0,0,0,0,0,0,0,0,0,0,0 });
        public string MetaData="";
        public ushort EntryCount;
        public ushort MessageID;
        
        public ushort MessageResID;
        
        public uint MessageSize;

        public ushort ComputeSize()
        {
            return (ushort)(64 + ArrayBinaryWriter.GetStringByteCount8(SenderNodeName) + ArrayBinaryWriter.GetStringByteCount8(ReceiverNodeName) + ArrayBinaryWriter.GetStringByteCount8(MetaData));
        }

        public void UpdateHeader(uint message_size, ushort entry_count)
        {
            HeaderLength = ComputeSize();
            MessageSize = message_size;
            EntryCount = entry_count;
        }

        public void Write(ArrayBinaryWriter w)
        {
            w.PushRelativeLimit(HeaderLength);
            w.WriteString8("RRAC");
            w.Write(MessageSize);
            w.Write((ushort)2);
            
            w.Write(HeaderLength);

            byte[] bSenderNodeID = SenderNodeID.ToByteArray();
            byte[] bReceiverNodeID = ReceiverNodeID.ToByteArray();
            for (int i = 0; i < 16; i++) { w.Write(bSenderNodeID[i]); };
            for (int i = 0; i < 16; i++) { w.Write(bReceiverNodeID[i]); };
            w.Write(SenderEndpoint);
            w.Write(ReceiverEndpoint);
            w.Write((ushort)ArrayBinaryWriter.GetStringByteCount8(SenderNodeName));
            w.WriteString8(SenderNodeName);
            w.Write((ushort)ArrayBinaryWriter.GetStringByteCount8(ReceiverNodeName));
            w.WriteString8(ReceiverNodeName);
            w.Write((ushort)ArrayBinaryWriter.GetStringByteCount8(MetaData));
            w.WriteString8(MetaData);
            w.Write((ushort)EntryCount);
            w.Write(MessageID);
            w.Write(MessageResID);

            if (w.DistanceFromLimit != 0) throw new IOException("Message write error");
            w.PopLimit();

        }

        public void Read(ArrayBinaryReader r)
        {
            string seed = r.ReadString8(4);
            if (seed !="RRAC")
                throw new ProtocolException("Incorrect message seed");
            MessageSize = r.ReadUInt32();
            ushort version = r.ReadUInt16();
            if (version != 2)
                throw new ProtocolException("Uknown protocol version");
            
            HeaderLength = r.ReadUInt16();

            r.PushRelativeLimit((uint)(HeaderLength - 12));

            byte[] bSenderNodeID = new byte[] { 0, 0, 0, 0,0,0,0,0,0,0,0,0,0,0,0,0 };
            for (int i = 0; i < 16; i++) { bSenderNodeID[i] = r.ReadByte(); };
            SenderNodeID = new NodeID(bSenderNodeID);
            byte[] bReceiverNodeID = new byte[] { 0, 0, 0, 0,0,0,0,0,0,0,0,0,0,0,0,0 };
            for (int i = 0; i < 16; i++) { bReceiverNodeID[i] = r.ReadByte(); };
            ReceiverNodeID = new NodeID(bReceiverNodeID);
            SenderEndpoint = r.ReadUInt32();
            ReceiverEndpoint = r.ReadUInt32();
            ushort pname_s= r.ReadUInt16();
            SenderNodeName = r.ReadString8(pname_s);
            ushort pname_r = r.ReadUInt16();
            ReceiverNodeName = r.ReadString8(pname_r);
            ushort meta_s = r.ReadUInt16();
            MetaData = r.ReadString8(meta_s);
            
            EntryCount = r.ReadUInt16();
            MessageID = r.ReadUInt16();
            MessageResID = r.ReadUInt16();
            if (r.DistanceFromLimit != 0) throw new IOException("Error reading message");
            r.PopLimit();
            
        }
    }


    public class MessageEntry
    {
        public uint EntrySize;

        public MessageEntryType EntryType;

        public string ServicePath="";

        public string MemberName="";

        public uint RequestID;

        public MessageErrorType Error;

        public string MetaData="";

        public List<MessageElement> elements;

        public MessageEntry() {
            elements =new List<MessageElement>();
        }

        public MessageEntry(MessageEntryType t, string n)
        {
            elements = new List<MessageElement>();
            EntryType = t;
            MemberName = n;
        }

        public uint ComputeSize()
        {
            uint s = 22;
            foreach (MessageElement e in elements)
            {
                s += e.ComputeSize();
            }

            s += (uint)ArrayBinaryWriter.GetStringByteCount8(ServicePath);
            s += (uint)ArrayBinaryWriter.GetStringByteCount8(MemberName);
            s += (uint)ArrayBinaryWriter.GetStringByteCount8(MetaData);

            return s;
        }

        public MessageElement FindElement(string name)
        {

            if (elements == null) return null;

            foreach (MessageElement m in elements)
            {
                if (m.ElementName == name)
                    return m;
            }

            throw new MessageElementNotFoundException("Element " + name + " not found.");

        }

        public bool TryFindElement(string name, out MessageElement m)
        {
            if (elements != null)
            {
                foreach (MessageElement m1 in elements)
                {
                    if (m1.ElementName == name)
                    {
                        m = m1;
                        return false;
                    }                    
                }
            }
            m = null;
            return false;
        }

        public MessageElement AddElement(string name, Object data)
        {
            MessageElement m = new MessageElement();
            m.ElementName = name;
            m.Data = data;

            if (elements == null) elements = new List<MessageElement>();
            elements.Add(m);

            return m;
        }

        public MessageElement AddElement(MessageElement m)
        {
            if (elements == null) elements = new List<MessageElement>();
            elements.Add(m);

            return m;
        }

        public void Write(ArrayBinaryWriter w)
        {
            
            EntrySize = ComputeSize();
            w.PushRelativeLimit(EntrySize);
            w.Write(EntrySize);
            w.Write((ushort)EntryType);
            w.Write((ushort)0);

            w.Write((ushort)ArrayBinaryWriter.GetStringByteCount8(ServicePath));
            w.WriteString8(ServicePath);
            w.Write((ushort)ArrayBinaryWriter.GetStringByteCount8(MemberName));
            w.WriteString8(MemberName);
            w.Write(RequestID);
            w.Write((ushort)Error);
             w.Write((ushort)ArrayBinaryWriter.GetStringByteCount8(MetaData));
            w.WriteString8(MetaData);
            w.Write((ushort)elements.Count);

            foreach (MessageElement e in elements)
            {
                e.Write(w);
            }

            if (w.DistanceFromLimit != 0) throw new IOException("Message write error");
            w.PopLimit();
        }

        public void Read(ArrayBinaryReader r)
        {
            EntrySize = r.ReadUInt32();

            r.PushRelativeLimit(EntrySize - 4);

            EntryType = (MessageEntryType)r.ReadUInt16();
            r.ReadUInt16();
            
            ushort sname_s = r.ReadUInt16();
            ServicePath = r.ReadString8(sname_s);
            ushort mname_s = r.ReadUInt16();
            MemberName = r.ReadString8(mname_s);
            RequestID = r.ReadUInt32();
            Error = (MessageErrorType)r.ReadUInt16();

            ushort metadata_s = r.ReadUInt16();
            MetaData = r.ReadString8(metadata_s);

            ushort ecount = r.ReadUInt16();

            

            elements=new List<MessageElement>(ecount);
            for (int i = 0; i < ecount; i++)
            {
                MessageElement e = new MessageElement();
                e.Read(r);
                elements.Add(e);
            }

            if (r.DistanceFromLimit != 0) throw new IOException("Error reading message");
            r.PopLimit();

        }


    }

    public class MessageElement
    {
        public uint ElementSize;

        public string ElementName="";

        public DataTypes ElementType;

        public string ElementTypeName = "";

        public string MetaData="";

        public uint DataCount;

        private object dat;

        public MessageElement()
        {
        }

        public MessageElement(String name, Object datin)
        {
            ElementName = name;
            Data = datin;
            //UpdateData();
        }

        public Object Data
        {
            get
            {
                return dat;
            }
            set
            {
                dat = value;
               
                UpdateData();
            }

        }
        

        public uint ComputeSize()
        {
            uint s = 16 + (uint)ArrayBinaryWriter.GetStringByteCount8(ElementName)+(uint)ArrayBinaryWriter.GetStringByteCount8(ElementTypeName) + (uint)ArrayBinaryWriter.GetStringByteCount8(MetaData);

            if (ElementType == DataTypes.structure_t)
            {

                MessageElementStructure d = (MessageElementStructure)Data;

                
                foreach (MessageElement e in d.Elements)
                {
                    s += e.ComputeSize();
                }

                
            }
            else if (ElementType==DataTypes.vector_t)
            {
                MessageElementMap<int> d = (MessageElementMap<int>)Data;


                foreach (MessageElement e in d.Elements)
                {
                    s += e.ComputeSize();
                }
            }
            else if (ElementType == DataTypes.dictionary_t)
            {
                MessageElementMap<string> d = (MessageElementMap<string>)Data;


                foreach (MessageElement e in d.Elements)
                {
                    s += e.ComputeSize();
                }
            }
            else if (ElementType == DataTypes.list_t)
            {
                MessageElementList d = (MessageElementList)Data;


                foreach (MessageElement e in d.Elements)
                {
                    s += e.ComputeSize();
                }
            }
            else if (ElementType == DataTypes.multidimarray_t)
            {
                MessageElementMultiDimArray d = (MessageElementMultiDimArray)Data;                
                foreach (MessageElement e in d.Elements)
                {
                    s += e.ComputeSize();
                }
            }
            else if (ElementType == DataTypes.pod_t)
            {
                MessageElementPod d = (MessageElementPod)Data;
                foreach (MessageElement e in d.Elements)
                {
                    s += e.ComputeSize();
                }
            }
            else if (ElementType == DataTypes.pod_array_t)
            {
                MessageElementPodArray d = (MessageElementPodArray)Data;
                foreach (MessageElement e in d.Elements)
                {
                    s += e.ComputeSize();
                }
            }
            else if (ElementType == DataTypes.pod_multidimarray_t)
            {
                MessageElementPodMultiDimArray d = (MessageElementPodMultiDimArray)Data;
                foreach (MessageElement e in d.Elements)
                {
                    s += e.ComputeSize();
                }
            }
            else if (ElementType == DataTypes.namedarray_array_t)
            {
                MessageElementNamedArray d = (MessageElementNamedArray)Data;
                foreach (MessageElement e in d.Elements)
                {
                    s += e.ComputeSize();
                }
            }
            else if (ElementType == DataTypes.namedarray_multidimarray_t)
            {
                MessageElementNamedMultiDimArray d = (MessageElementNamedMultiDimArray)Data;
                foreach (MessageElement e in d.Elements)
                {
                    s += e.ComputeSize();
                }
            }
            else if (ElementType == DataTypes.void_t)
            {
                s += 0;
            }
            else
            {

                s += DataCount * DataTypeUtil.size(ElementType);
            }

            return s; 
        }


        public void UpdateData()
        {
            string datatype;

            if (dat == null)
            {
                datatype = "null";
            }
            else if (dat is Array)
            {
                datatype = dat.GetType().GetElementType().ToString();
                DataCount = (uint)((Array)dat).Length;
            }
            else if (dat is MessageElementStructure) {
                DataCount = (uint)((MessageElementStructure)dat).Elements.Count;
                ElementTypeName = ((MessageElementStructure)dat).Type;
                datatype = "RobotRaconteurWeb.MessageElementStructure";
            }
            else if (dat is MessageElementMap<int>)
            {
                DataCount = (uint)((MessageElementMap<int>)dat).Elements.Count;
                datatype = "RobotRaconteurWeb.MessageElementMap<int>";
            }
            else if (dat is MessageElementMap<string>)
            {
                DataCount = (uint)((MessageElementMap<string>)dat).Elements.Count;
                datatype = "RobotRaconteurWeb.MessageElementMap<string>";
            }
            else if (dat is MessageElementList)
            {
                DataCount = (uint)((MessageElementList)dat).Elements.Count;
                datatype = "RobotRaconteurWeb.MessageElementList";
            }
            else if (dat is MessageElementMultiDimArray)
            {
                DataCount = (uint)((MessageElementMultiDimArray)dat).Elements.Count;
                datatype = "RobotRaconteurWeb.MessageElementMultiDimArray";
            }
            else if (dat is MessageElementPod)
            {
                DataCount = (uint)((MessageElementPod)dat).Elements.Count;
                datatype = "RobotRaconteurWeb.MessageElementPod";
            }
            else if (dat is MessageElementPodArray)
            {
                DataCount = (uint)((MessageElementPodArray)dat).Elements.Count;
                ElementTypeName = ((MessageElementPodArray)dat).Type;
                datatype = "RobotRaconteurWeb.MessageElementPodArray";
            }
            else if (dat is MessageElementPodMultiDimArray)
            {
                DataCount = (uint)((MessageElementPodMultiDimArray)dat).Elements.Count;
                ElementTypeName = ((MessageElementPodMultiDimArray)dat).Type;
                datatype = "RobotRaconteurWeb.MessageElementPodMultiDimArray";
            }
            else if (dat is MessageElementNamedArray)
            {
                DataCount = (uint)((MessageElementNamedArray)dat).Elements.Count;
                ElementTypeName = ((MessageElementNamedArray)dat).Type;
                datatype = "RobotRaconteurWeb.MessageElementNamedArray";
            }
            else if (dat is MessageElementNamedMultiDimArray)
            {
                DataCount = (uint)((MessageElementNamedMultiDimArray)dat).Elements.Count;
                ElementTypeName = ((MessageElementNamedMultiDimArray)dat).Type;
                datatype = "RobotRaconteurWeb.MessageElementNamedMultiDimArray";
            }
            else if (dat is string) 
            {
                datatype = "System.String";
                DataCount = (uint)UTF8Encoding.UTF8.GetByteCount((string)dat);

            }
            else
            {
                
                DataCount = 1;
                datatype = dat.GetType().ToString();
            }

            ElementType = DataTypeUtil.TypeIDFromString(datatype);

            if (ElementType != DataTypes.void_t && ElementType < DataTypes.string_t && !(dat is Array))
            {
                dat = DataTypeUtil.ArrayFromScalar(dat);
            }

            ElementSize = ComputeSize();

        }

        public void Write(ArrayBinaryWriter w)
        {            
            UpdateData();
            w.PushRelativeLimit(ElementSize);
            w.Write(ElementSize);
            w.Write((ushort)ArrayBinaryWriter.GetStringByteCount8(ElementName));
            w.WriteString8(ElementName);
            w.Write((ushort)ElementType);
            w.Write((ushort)ArrayBinaryWriter.GetStringByteCount8(ElementTypeName));
            w.WriteString8(ElementTypeName);
            w.Write((ushort)ArrayBinaryWriter.GetStringByteCount8(MetaData));
            w.WriteString8(MetaData);
            w.Write((uint)DataCount);
            if (dat == null)
            {

            }
            else if (dat.GetType().IsArray)
            {
                w.WriteArray((Array)dat);
            }
            else if (dat is MessageElementStructure)
            {
                List<MessageElement> l = ((MessageElementStructure)dat).Elements;
                foreach (MessageElement e in l) e.Write(w);
                 
            }
            else if (dat is MessageElementMap<int>)
            {
                List<MessageElement> l = ((MessageElementMap<int>)dat).Elements;
                foreach (MessageElement e in l) e.Write(w);

            }
            else if (dat is MessageElementMap<string>)
            {
                List<MessageElement> l = ((MessageElementMap<string>)dat).Elements;
                foreach (MessageElement e in l) e.Write(w);

            }
            else if (dat is MessageElementList)
            {
                List<MessageElement> l = ((MessageElementList)dat).Elements;
                foreach (MessageElement e in l) e.Write(w);

            }
            else if (dat is string)
            {
                w.WriteString8((string)dat);
            }
            else if (dat is MessageElementMultiDimArray)
            {
                List<MessageElement> l = ((MessageElementMultiDimArray)dat).Elements;
                foreach (MessageElement e in l) e.Write(w);
            }
            else if (dat is MessageElementPod)
            {
                List<MessageElement> l = ((MessageElementPod)dat).Elements;
                foreach (MessageElement e in l) e.Write(w);
            }
            else if (dat is MessageElementPodArray)
            {
                List<MessageElement> l = ((MessageElementPodArray)dat).Elements;
                foreach (MessageElement e in l) e.Write(w);
            }
            else if (dat is MessageElementPodMultiDimArray)
            {
                List<MessageElement> l = ((MessageElementPodMultiDimArray)dat).Elements;
                foreach (MessageElement e in l) e.Write(w);
            }
            else if (dat is MessageElementNamedArray)
            {
                List<MessageElement> l = ((MessageElementNamedArray)dat).Elements;
                foreach (MessageElement e in l) e.Write(w);
            }
            else if (dat is MessageElementNamedMultiDimArray)
            {
                List<MessageElement> l = ((MessageElementNamedMultiDimArray)dat).Elements;
                foreach (MessageElement e in l) e.Write(w);
            }
            else
            {
                w.WriteNumber(dat, ElementType);
            }

            if (w.DistanceFromLimit != 0) throw new IOException("Message write error");
            w.PopLimit();

        }

        public void Read(ArrayBinaryReader r)
        {
            ElementSize=r.ReadUInt32();
            r.PushRelativeLimit(ElementSize - 4);
            ushort name_s = r.ReadUInt16();
            ElementName = r.ReadString8(name_s);
            ElementType = (DataTypes)r.ReadUInt16();
            ushort nametype_s = r.ReadUInt16();
            ElementTypeName = r.ReadString8(nametype_s);
            ushort metadata_s = r.ReadUInt16();
            MetaData = r.ReadString8(metadata_s);
            DataCount = r.ReadUInt32();

            if (ElementType == DataTypes.void_t)
            {
            }
            else if (ElementType == DataTypes.structure_t)
            {
                List<MessageElement> l = new List<MessageElement>((int)DataCount);
                for (int i = 0; i < DataCount; i++)
                {
                    MessageElement m=new MessageElement();
                    m.Read(r);
                    l.Add(m);
                    
                }

                dat = new MessageElementStructure(ElementTypeName,l);
            } else
            if (ElementType == DataTypes.vector_t)
            {
                List<MessageElement> l = new List<MessageElement>((int)DataCount);
                for (int i = 0; i < DataCount; i++)
                {
                    MessageElement m = new MessageElement();
                    m.Read(r);
                    l.Add(m);

                }

                dat = new MessageElementMap<int>(l);
            }
            else
            if (ElementType == DataTypes.dictionary_t)
            {
                List<MessageElement> l = new List<MessageElement>((int)DataCount);
                for (int i = 0; i < DataCount; i++)
                {
                    MessageElement m = new MessageElement();
                    m.Read(r);
                    l.Add(m);

                }

                dat = new MessageElementMap<string>(l);
            }
            else
            if (ElementType == DataTypes.list_t)
            {
                List<MessageElement> l = new List<MessageElement>((int)DataCount);
                for (int i = 0; i < DataCount; i++)
                {
                    MessageElement m = new MessageElement();
                    m.Read(r);
                    l.Add(m);

                }

                dat = new MessageElementList(l);
            }
            else if (ElementType == DataTypes.string_t)
            {
                if (DataCount > r.DistanceFromLimit) throw new IOException("Error reading message");
                dat = r.ReadString8(DataCount);
            }
            else
            if (ElementType == DataTypes.multidimarray_t)
            {
                List<MessageElement> l = new List<MessageElement>((int)DataCount);
                for (int i = 0; i < DataCount; i++)
                {
                    MessageElement m = new MessageElement();
                    m.Read(r);
                    l.Add(m);

                }
                dat = new MessageElementMultiDimArray(l);
            }
            else
            if (ElementType == DataTypes.pod_t)
            {
                List<MessageElement> l = new List<MessageElement>((int)DataCount);
                for (int i = 0; i < DataCount; i++)
                {
                    MessageElement m = new MessageElement();
                    m.Read(r);
                    l.Add(m);

                }
                dat = new MessageElementPod(l);
            }
            else
            if (ElementType == DataTypes.pod_array_t)
            {
                List<MessageElement> l = new List<MessageElement>((int)DataCount);
                for (int i = 0; i < DataCount; i++)
                {
                    MessageElement m = new MessageElement();
                    m.Read(r);
                    l.Add(m);

                }
                dat = new MessageElementPodArray(ElementTypeName, l);
            }
            else
            if (ElementType == DataTypes.pod_multidimarray_t)
            {
                List<MessageElement> l = new List<MessageElement>((int)DataCount);
                for (int i = 0; i < DataCount; i++)
                {
                    MessageElement m = new MessageElement();
                    m.Read(r);
                    l.Add(m);

                }
                dat = new MessageElementPodMultiDimArray(ElementTypeName, l);
            }
            else
            if (ElementType == DataTypes.pod_t)
            {
                List<MessageElement> l = new List<MessageElement>((int)DataCount);
                for (int i = 0; i < DataCount; i++)
                {
                    MessageElement m = new MessageElement();
                    m.Read(r);
                    l.Add(m);

                }
                dat = new MessageElementPod(l);
            }
            else
            if (ElementType == DataTypes.namedarray_array_t)
            {
                List<MessageElement> l = new List<MessageElement>((int)DataCount);
                for (int i = 0; i < DataCount; i++)
                {
                    MessageElement m = new MessageElement();
                    m.Read(r);
                    l.Add(m);

                }
                dat = new MessageElementNamedArray(ElementTypeName, l);
            }
            else
            if (ElementType == DataTypes.namedarray_multidimarray_t)
            {
                List<MessageElement> l = new List<MessageElement>((int)DataCount);
                for (int i = 0; i < DataCount; i++)
                {
                    MessageElement m = new MessageElement();
                    m.Read(r);
                    l.Add(m);

                }
                dat = new MessageElementNamedMultiDimArray(ElementTypeName, l);
            }
            else
            {
                if (DataCount*DataTypeUtil.size(ElementType) > r.DistanceFromLimit) throw new IOException("Error reading message");
                Array d=DataTypeUtil.ArrayFromDataType(ElementType,DataCount);
                r.ReadArray(d);
                dat = (Object)d;

            }

            if (r.DistanceFromLimit != 0) throw new IOException("Error reading message");
            r.PopLimit();

        }

        public static MessageElement FindElement(List<MessageElement> m, string name)
        {
            foreach (MessageElement e in m) {
                if (e.ElementName == name) {
                    return e;
                }
            }
            throw new MessageElementNotFoundException("Could not find element " + name);
        }

        public static MessageElement FindElement(MessageEntry m, string name)
        {
            return FindElement(m.elements, name);
        }

        public T CastData<T>()
        {
            if (Data == null) return default(T);
            if (Data is T)
                return (T)Data;
            throw new DataTypeException("Could not cast data to type " + typeof(T).ToString());
        }

        public static T CastData<T>(object Data)
        {
            if (Data == null) return default(T);
            if (Data is T)
                return (T)Data;
            throw new DataTypeException("Could not cast data to type " + typeof(T).ToString());
        }
    }

    public class MessageElementStructure
    {
        public MessageElementStructure(string type_, List<MessageElement> elements_)
        {
            Elements = elements_;
            Type = type_;
        }
        public List<MessageElement> Elements;

        public string Type;

    }

    public class MessageElementMap<T>
    {
        public MessageElementMap(List<MessageElement> e)
        {
            Elements = e;
        }
        public List<MessageElement> Elements;
    }

    public class MessageElementList
    {
        public MessageElementList(List<MessageElement> e)
        {
            Elements = e;
        }
        public List<MessageElement> Elements;
    }
    

    public class MessageElementMultiDimArray
    {
         public MessageElementMultiDimArray(List<MessageElement> e)
        {
            Elements = e;
        }

        public List<MessageElement> Elements;
    }

    public class MessageElementPod
    {        
        public MessageElementPod(List<MessageElement> e)
        {
            Elements = e;
        }        
        public List<MessageElement> Elements;
    }

    public class MessageElementPodArray
    {
        public MessageElementPodArray(string type, List<MessageElement> e)
        {
            Elements = e;
            this.Type = type;
        }
        public string Type;
        public List<MessageElement> Elements;
    }

    public class MessageElementPodMultiDimArray
    {
        public MessageElementPodMultiDimArray(string type, List<MessageElement> e)
        {
            Elements = e;
            this.Type = type;
        }
        public string Type;
        public List<MessageElement> Elements;
    }

    public class MessageElementNamedArray
    {
        public MessageElementNamedArray(string type, List<MessageElement> e)
        {
            Elements = e;
            this.Type = type;
        }
        public string Type;
        public List<MessageElement> Elements;
    }

    public class MessageElementNamedMultiDimArray
    {
        public MessageElementNamedMultiDimArray(string type, List<MessageElement> e)
        {
            Elements = e;
            this.Type = type;
        }
        public string Type;
        public List<MessageElement> Elements;
    }

    public static class MessageElementUtil
    {
        public static MessageElement NewMessageElement(string name, object data)
        {            
            var m = new MessageElement();
            m.ElementName = name;
            m.Data = data;
            return m;
        }

        public static void AddMessageElement(List<MessageElement> vct, string name, object data)
        {
            var m = NewMessageElement(name, data);
            vct.Add(m);
        }

        public static void AddMessageElement(MessageEntry m, string name, object data)
        {
            var m1 = NewMessageElement(name, data);
            m.AddElement(m1);
        }

        public static MessageElement NewMessageElement(int i, object data)
        {
            var m = new MessageElement();
            m.ElementName = i.ToString();
            m.Data = data;
            return m;
        }

        public static void AddMessageElement(List<MessageElement> vct, int i, object data)
        {
            var m = NewMessageElement(i, data);            
            vct.Add(m);           
        }

        public static void AddMessageElement(MessageEntry m, int i, object data)
        {
            var m1 = NewMessageElement(i, data);
            m.AddElement(m1);
        }

        public static void AddMessageElement(List<MessageElement> vct, MessageElement m)
        {            
             vct.Add(m);
        }

        public static void AddMessageElement(MessageEntry m, MessageElement mm)
        {
            m.AddElement(mm);
        }

        public static T FindElementAndCast<T>(List<MessageElement> elems, string name)
        {
            var e = MessageElement.FindElement(elems, name);
            
            return e.CastData<T>();            
        }

        public static MessageElement FindElement(List<MessageElement> elems, string name)
        {
            return MessageElement.FindElement(elems, name);
        }

        public static MessageElement FindElement(MessageEntry m, string name)
        {
            return MessageElement.FindElement(m, name);
        }

        public static T CastData<T>(MessageElement m)
        {            
            return m.CastData<T>();
        }

        public static int GetMessageElementNumber(MessageElement e)
        {
            int res;
            if(!Int32.TryParse(e.ElementName, out res))
            { 
                throw new ProtocolException("Could not determine Element Number");
            }
            return res;
        }

        public static MessageElement PackScalar<T>(string name, T val) where T : struct
        {
            return NewMessageElement(name, new T[] { val });
        }

        public static MessageElement PackArray<T>(string name, T[] val) where T : struct
        {
            if (val == null)
            {
                throw new NullReferenceException();
            }
            return NewMessageElement(name, val);
        }

        public static MessageElement PackMultiDimArray(RobotRaconteurNode node, string name, MultiDimArray val)
        {
            if (val == null)
            {
                throw new NullReferenceException();
            }

            return NewMessageElement(name, node.PackMultiDimArray(val));
        }

        public static MessageElement PackString(string name, string val)
        {
            if (val == null)
            {
                throw new NullReferenceException();
            }
            return NewMessageElement(name, val);
        }

        public static MessageElement PackStructure(RobotRaconteurNode node, ClientContext client, string name, object val)
        {
            return NewMessageElement(name, node.PackStructure(val,client));
        }

        public static MessageElement PackVarType(RobotRaconteurNode node, ClientContext client, string name, object val)
        {
            return NewMessageElement(name, node.PackVarType(val,client));
        }

        public static MessageElement PackAnyType<T>(RobotRaconteurNode node, ClientContext client, string name, ref T val)
        {
            return node.PackAnyType<T>(name, ref val, client);
        }

        public static MessageElement PackMapType<K, T>(RobotRaconteurNode node, ClientContext client, string name, Dictionary<K, T> val)
        {
            return NewMessageElement(name, node.PackMapType<K, T>(val,client));
        }

        public static MessageElement PackListType<T>(RobotRaconteurNode node, ClientContext client, string name, List<T> val)
        {
            return NewMessageElement(name, node.PackListType<T>(val, client));
        }

        public static MessageElement PackEnum<T>(string name, T val)
        {
            return NewMessageElement(name, new int[] { (int)(object)val });
        }

        public static MessageElement PackPodToArray<T>(RobotRaconteurNode node, ClientContext client, string name, ref T val) where T : struct
        {
            return NewMessageElement(name, node.PackPodToArray<T>(ref val, client));
        }

        public static MessageElement PackPodArray<T>(RobotRaconteurNode node, ClientContext client, string name, T[] val) where T : struct
        {
            return NewMessageElement(name, node.PackPodArray<T>(val, client));
        }

        public static MessageElement PackPodMultiDimArray<T>(RobotRaconteurNode node, ClientContext client, string name, PodMultiDimArray val) where T : struct
        {
            return NewMessageElement(name, node.PackPodMultiDimArray<T>(val, client));
        }

        public static MessageElement PackNamedArrayToArray<T>(RobotRaconteurNode node, ClientContext client, string name, ref T val) where T : struct
        {
            return NewMessageElement(name, node.PackNamedArrayToArray<T>(ref val, client));
        }

        public static MessageElement PackNamedArray<T>(RobotRaconteurNode node, ClientContext client, string name, T[] val) where T : struct
        {
            return NewMessageElement(name, node.PackNamedArray<T>(val, client));
        }

        public static MessageElement PackNamedMultiDimArray<T>(RobotRaconteurNode node, ClientContext client, string name, NamedMultiDimArray val) where T : struct
        {
            return NewMessageElement(name, node.PackNamedMultiDimArray<T>(val, client));
        }

        public static T UnpackScalar<T>(MessageElement m) where T : struct
        {
            T[] a = CastData<T[]>(m);
            if (a.Length != 1) throw new DataTypeException("Invalid scalar");
            return a[0];
        }

        public static T[] UnpackArray<T>(MessageElement m) where T : struct
        {
            T[] a = CastData<T[]>(m);
            if (a == null) throw new NullReferenceException();
            return a;
        }

        public static MultiDimArray UnpackMultiDimArray(RobotRaconteurNode node, MessageElement m)
        {
            MultiDimArray a = node.UnpackMultiDimArray(MessageElementUtil.CastData<MessageElementMultiDimArray>(m));
            if (a == null) throw new NullReferenceException();
            return a;
        }

        public static string UnpackString(MessageElement m)
        {
            string s = MessageElementUtil.CastData<string>(m);
            if (s == null) throw new NullReferenceException();
            return s;
        }

        public static T UnpackStructure<T>(RobotRaconteurNode node, ClientContext client, MessageElement m)
        {
            return node.UnpackStructure<T>(MessageElementUtil.CastData<MessageElementStructure>(m), client);
        }

        public static object UnpackVarType(RobotRaconteurNode node, ClientContext client, MessageElement m)
        {
            return node.UnpackVarType(m, client);
        }

        public static T UnpackAnyType<T>(RobotRaconteurNode node, ClientContext client, MessageElement m)
        {
            return node.UnpackAnyType<T>(m, client);
        }

        public static Dictionary<K, T> UnpackMap<K, T>(RobotRaconteurNode node, ClientContext client, MessageElement m)
        {
            return (Dictionary<K, T>)node.UnpackMapType<K, T>(m.Data, client);
        }

        public static List<T> UnpackList<T>(RobotRaconteurNode node, ClientContext client, MessageElement m)
        {
            return (List<T>)node.UnpackListType<T>(m.Data, client);
        }

        public static T UnpackEnum<T>(MessageElement m)
        {
            int[] a = CastData<int[]>(m);
            if (a.Length != 1) throw new DataTypeException("Invalid enum");
            return (T)(object)a[0];
        }

        public static T UnpackPodFromArray<T>(RobotRaconteurNode node, ClientContext client, MessageElement m) where T : struct
        {
            return node.UnpackPodFromArray<T>(CastData<MessageElementPodArray>(m), client);
        }

        public static T[] UnpackPodArray<T>(RobotRaconteurNode node, ClientContext client, MessageElement m) where T : struct
        {
            return node.UnpackPodArray<T>(CastData<MessageElementPodArray>(m), client);
        }

        public static PodMultiDimArray UnpackPodMultiDimArray<T>(RobotRaconteurNode node, ClientContext client, MessageElement m) where T : struct
        {
            return node.UnpackPodMultiDimArray<T>(MessageElementUtil.CastData<MessageElementPodMultiDimArray>(m), client);
        }

        public static T UnpackNamedArrayFromArray<T>(RobotRaconteurNode node, ClientContext client, MessageElement m) where T : struct
        {
            return node.UnpackNamedArrayFromArray<T>(CastData<MessageElementNamedArray>(m), client);
        }

        public static T[] UnpackNamedArray<T>(RobotRaconteurNode node, ClientContext client, MessageElement m) where T : struct
        {
            return node.UnpackNamedArray<T>(CastData<MessageElementNamedArray>(m), client);
        }

        public static NamedMultiDimArray UnpackNamedMultiDimArray<T>(RobotRaconteurNode node, ClientContext client, MessageElement m) where T : struct
        {
            return node.UnpackNamedMultiDimArray<T>(MessageElementUtil.CastData<MessageElementNamedMultiDimArray>(m), client);
        }

        public static string GetMessageElementDataTypeString(object o)
        {
            var f = o.GetType().GetField("Type");
            return (string)f.GetValue(o);
        }
    }
    

}
