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

#pragma warning disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RobotRaconteurWeb
{

    /// <summary>
    /// Type codes for types supported by Robot Raconteur
    /// </summary>
    /// <remarks>
    /// <para>
    /// Data type codes are used in messages and service definition parsers.
    /// </para>
    /// <para> Data is always stored as little-endian, except for UUID which are big endian
    /// </para>
    /// </remarks>
    [PublicApi]
    public enum DataTypes
    {
        /// <summary>void or null type</summary>
        void_t = 0,

        /// <summary>IEEE-754 64-bit floating point number</summary>
        double_t,

        /// <summary>IEEE-754 32-bit floating point number</summary>
        single_t,

        /// <summary>8-bit signed integer</summary>
        int8_t,

        /// <summary>8-bit unsigned integer</summary>
        uint8_t,

        /// <summary>16-bit signed integer</summary>
        int16_t,

        /// <summary>16-bit unsigned integer</summary>
        uint16_t,

        /// <summary>32-bit signed integer</summary>
        int32_t,

        /// <summary>32-bit unsigned integer</summary>
        uint32_t,

        /// <summary>64-bit signed integer</summary>
        int64_t,

        /// <summary>64-bit unsigned integer</summary>
        uint64_t,

        /// <summary>UTF-8 string</summary>
        string_t,

        /// <summary>128-bit complex double (real,imag)</summary>
        cdouble_t,

        /// <summary>64-bit complex float (real,imag)</summary>
        csingle_t,

        /// <summary>8-bit boolean</summary>
        bool_t,

        /// <summary>structure (nested message type)</summary>
        structure_t = 101,

        /// <summary>map with int32 key (nested message type)</summary>
        vector_t,

        /// <summary>map with string key (nested message type)</summary>
        dictionary_t,

        /// <summary>object type (not serializable)</summary>
        object_t,

        /// <summary>varvalue type (not serializable)</summary>
        varvalue_t,

        /// <summary>varobject type (not serializable)</summary>
        varobject_t,

        /// <summary>list type (nested message type)</summary>
        list_t = 108,

        /// <summary>pod type (nested message type)</summary>
        pod_t,

        /// <summary>pod array type (nested message type)</summary>
        pod_array_t,

        /// <summary>pod multidimarray type (nested message type)</summary>
        pod_multidimarray_t,

        /// <summary>enum type (not serializable uses int32 for messages)</summary>
        enum_t,

        /// <summary>namedtype definition (not serializable)</summary>
        namedtype_t,

        /// <summary>namedarray type (not serializable)</summary>
        namedarray_t,

        /// <summary>namedarray array type (nested message type)</summary>
        namedarray_array_t,

        /// <summary>namedarray multidimarray type (nested message type)</summary>
        namedarray_multidimarray_t,

        /// <summary>multi-dimensional numeric array (nested message type)</summary>
        multidimarray_t
    }

    /// <summary>
    /// Array type enum for TypeDefinition parser class
    /// </summary>
    [PublicApi]
    public enum DataTypes_ArrayTypes
    {
        /// <summary>type is not an array</summary>
        none = 0,

        /// <summary>type is a single dimensional array</summary>
        array,

        /// <summary>type is a multidimensional array</summary>
        multidimarray
    }

    /// <summary>
    /// Container type enum for TypeDefinition parser class
    /// </summary>
    [PublicApi]
    public enum DataTypes_ContainerTypes
    {
        /// <summary>type does not have a container</summary>
        none = 0,

        /// <summary>type has a list container</summary>
        list,

        /// <summary>type has a map with int32 keys container</summary>
        map_int32,

        /// <summary>type has a map with string keys container</summary>
        map_string,

        /// <summary>type has a generator container. Only valid for use with function generator members</summary>
        generator
    }


    /// <summary>
    /// Message entry type codes
    /// </summary>
    /// <remarks>
    /// <para>
    /// Message entries are sent between nodes stored in messages, and represent
    /// requests, responses, or packets. The type of the entry is specified through
    /// the message entry type code. These type codes are similar to op-codes. This
    /// enum contains the defined entry type codes.
    /// </para>
    /// <para>
    /// Odd codes represent requests or packets, even codes
    /// represent responses.
    /// </para>
    /// <para>
    /// Entry types less than 500 are considered "special requests" that can be used
    /// before a session is established.
    /// </para>
    /// </remarks>
    [PublicApi]
    public enum MessageEntryType
    {
        /// <summary>no-op</summary>
        Null = 0,

        /// <summary>Stream operation request (transport only)</summary>
        StreamOp = 1,

        /// <summary>Stream operation response (transport only)</summary>
        StreamOpRet,

        /// <summary>Stream check capability request (transport only)</summary>
        StreamCheckCapability,

        /// <summary>Stream check capability response (transport only)</summary>
        StreamCheckCapabilityRet,

        /// <summary>Get service definition request</summary>
        GetServiceDesc = 101,

        /// <summary>Get service definition response</summary>
        GetServiceDescRet,

        /// <summary>Get object qualified type name request</summary>
        ObjectTypeName,

        /// <summary>Get object qualified type name response</summary>
        ObjectTypeNameRet,

        /// <summary>Service closed notification packet</summary>
        ServiceClosed,

        /// <summary>(reserved)</summary>
        ServiceClosedRet,

        /// <summary>Connect client request</summary>
        ConnectClient,

        /// <summary>Connect client response</summary>
        ConnectClientRet,

        /// <summary>Disconnect client request</summary>
        DisconnectClient,

        /// <summary>Disconnect client response</summary>
        DisconnectClientRet,

        /// <summary>Ping request</summary>
        ConnectionTest,

        /// <summary>Pong response</summary>
        ConnectionTestRet,

        /// <summary>Get node information request (NodeID and NodeName)</summary>
        GetNodeInfo,

        /// <summary>Get node information response</summary>
        GetNodeInfoRet,

        /// <summary>(reserved)</summary>
        ReconnectClient,

        /// <summary>(reserved)</summary>
        ReconnectClientRet,

        /// <summary>Get node capability request</summary>
        NodeCheckCapability,

        /// <summary>Get node capability response</summary>
        NodeCheckCapabilityRet,

        /// <summary>Get service attributes request</summary>
        GetServiceAttributes,

        /// <summary>Get service attributes response</summary>
        GetServiceAttributesRet,

        /// <summary>Connect client combined operation request</summary>
        ConnectClientCombined,

        /// <summary>Connect client combined operation response</summary>
        ConnectClientCombinedRet,

        /// <summary>Get endpoint capability request</summary>
        EndpointCheckCapability = 501,

        /// <summary>Get endpoint capability response</summary>
        EndpointCheckCapabilityRet,

        /// <summary>Get service capability request</summary>
        ServiceCheckCapabilityReq = 1101,

        /// <summary>Get service capability response</summary>
        ServiceCheckCapabilityRet,

        /// <summary>Client keep alive request</summary>
        ClientKeepAliveReq = 1105,

        /// <summary>Client keep alive response</summary>
        ClientKeepAliveRet,

        /// <summary>Client session management operation request</summary>
        ClientSessionOpReq = 1107,

        /// <summary>Client session management operation response</summary>
        ClientSessionOpRet,

        /// <summary>Service path released event notification packet</summary>
        ServicePathReleasedReq,

        /// <summary>(reserved)</summary>
        ServicePathReleasedRet,

        /// <summary>Property member get request</summary>
        PropertyGetReq = 1111,

        /// <summary>Property member get response</summary>
        PropertyGetRes,

        /// <summary>Property member set request</summary>
        PropertySetReq,

        /// <summary>Property member set response</summary>
        PropertySetRes,

        /// <summary>Function member call request</summary>
        FunctionCallReq = 1121,

        /// <summary>Function member call response</summary>
        FunctionCallRes,

        /// <summary>Generator next call request</summary>
        GeneratorNextReq,

        /// <summary>Generator next call response</summary>
        GeneratorNextRes,

        /// <summary>Event member notification</summary>
        EventReq = 1131,

        /// <summary>(reserved)</summary>
        EventRes,

        /// <summary>Pipe member packet</summary>
        PipePacket = 1141,

        /// <summary>Pipe member packet ack</summary>
        PipePacketRet,

        /// <summary>Pipe member connect request</summary>
        PipeConnectReq,

        /// <summary>Pipe member connect response</summary>
        PipeConnectRet,

        /// <summary>Pipe member close request</summary>
        PipeDisconnectReq,

        /// <summary>Pipe member close response</summary>
        PipeDisconnectRet,

        /// <summary>Pipe member closed event notification packet</summary>
        PipeClosed,

        /// <summary>(reserved)</summary>
        PipeClosedRet,

        /// <summary>Callback member call request</summary>
        CallbackCallReq = 1151,

        /// <summary>Callback member call response</summary>
        CallbackCallRet,

        /// <summary>Wire member value packet</summary>
        WirePacket = 1161,

        /// <summary>(reserved)</summary>
        WirePacketRet,

        /// <summary>Wire member connect request</summary>
        WireConnectReq,

        /// <summary>Wire member connect response</summary>
        WireConnectRet,

        /// <summary>Wire member close request</summary>
        WireDisconnectReq,

        /// <summary>Wire member close response</summary>
        WireDisconnectRet,

        /// <summary>Wire member closed event notification packet</summary>
        WireClosed,

        /// <summary>(reserved)</summary>
        WireClosedRet,

        /// <summary>Memory member read request</summary>
        MemoryRead = 1171,

        /// <summary>Memory member read response</summary>
        MemoryReadRet,

        /// <summary>Memory member write request</summary>
        MemoryWrite,

        /// <summary>Memory member write response</summary>
        MemoryWriteRet,

        /// <summary>Memory member get param request</summary>
        MemoryGetParam,

        /// <summary>Memory member get param response</summary>
        MemoryGetParamRet,

        /// <summary>Wire member peek InValue request</summary>
        WirePeekInValueReq = 1181,

        /// <summary>Wire member peek InValue response</summary>
        WirePeekInValueRet,

        /// <summary>Wire member peek OutValue request</summary>
        WirePeekOutValueReq,

        /// <summary>Wire member peek OutValue response</summary>
        WirePeekOutValueRet,

        /// <summary>Wire member poke OutValue request</summary>
        WirePokeOutValueReq,

        /// <summary>Wire member poke OutValue response</summary>
        WirePokeOutValueRet,

        /// <summary>Wire transport operation request</summary>
        WireTransportOpReq = 11161,

        /// <summary>Wire transport operation response</summary>
        WireTransportOpRet,

        /// <summary>Wire transport event</summary>
        WireTransportEvent,

        /// <summary>Wire transport event response</summary>
        WireTransportEventRet
    }


    /// <summary>
    /// Message error type codes enum
    /// </summary>
    [PublicApi]
    public enum MessageErrorType
    {
        /// <summary>success</summary>
        None = 0,

        /// <summary>connection error</summary>
        ConnectionError = 1,

        /// <summary>protocol error serializing messages</summary>
        ProtocolError,

        /// <summary>specified service not found</summary>
        ServiceNotFound,

        /// <summary>specified object not found</summary>
        ObjectNotFound,

        /// <summary>specified endpoint not found</summary>
        InvalidEndpoint,

        /// <summary>communication with specified endpoint failed</summary>
        EndpointCommunicationFatalError,

        /// <summary>specified node not found</summary>
        NodeNotFound,

        /// <summary>service error</summary>
        ServiceError,

        /// <summary>specified member not found</summary>
        MemberNotFound,

        /// <summary>message format incompatible with specified member</summary>
        MemberFormatMismatch,

        /// <summary>data type did not match expected type</summary>
        DataTypeMismatch,

        /// <summary>data type failure</summary>
        DataTypeError,

        /// <summary>failure serializing data type</summary>
        DataSerializationError,

        /// <summary>specified message entry not found</summary>
        MessageEntryNotFound,

        /// <summary>specified message element not found</summary>
        MessageElementNotFound,

        /// <summary>unknown exception occurred check `error name`</summary>
        UnknownError,

        /// <summary>invalid operation attempted</summary>
        InvalidOperation,

        /// <summary>argument is invalid</summary>
        InvalidArgument,

        /// <summary>the requested operation failed</summary>
        OperationFailed,

        /// <summary>invalid null value</summary>
        NullValue,

        /// <summary>internal error</summary>
        InternalError,

        /// <summary>permission denied to a system resource</summary>
        SystemResourcePermissionDenied,

        /// <summary>system resource has been exhausted</summary>
        OutOfSystemResource,

        /// <summary>system resource error</summary>
        SystemResourceError,

        /// <summary>a required resource was not found</summary>
        ResourceNotFound,

        /// <summary>input/output error</summary>
        IOError,

        /// <summary>a buffer underrun/overrun has occurred</summary>
        BufferLimitViolation,

        /// <summary>service definition parse or validation error</summary>
        ServiceDefinitionError,

        /// <summary>attempt to access an out of range element</summary>
        OutOfRange,

        /// <summary>key not found</summary>
        KeyNotFound,

        /// <summary>error occurred on remote node</summary>
        RemoteError = 100,

        /// <summary>request timed out</summary>
        RequestTimeout,

        /// <summary>attempt to write to a read only member</summary>
        ReadOnlyMember,

        /// <summary>attempt to read a write only member</summary>
        WriteOnlyMember,

        /// <summary>member not implemented</summary>
        NotImplementedError,

        /// <summary>member is busy try again</summary>
        MemberBusy,

        /// <summary>value has not been set</summary>
        ValueNotSet,

        /// <summary>abort operation (generator only)</summary>
        AbortOperation,

        /// <summary>the operation has been aborted</summary>
        OperationAborted,

        /// <summary>stop generator iteration (generator only)</summary>
        StopIteration,

        /// <summary>authentication has failed</summary>
        AuthenticationError = 150,

        /// <summary>the object is locked by another user or session</summary>
        ObjectLockedError,

        /// <summary>permission to service object or resource denied</summary>
        PermissionDenied
    }

    /// <summary>
    /// Flags for MessageFlags entry in MessageHeader
    /// </summary>
    [Flags, PublicApi]
    public enum MessageFlags
    {
        /** <summary> Message contains ROUTING_INFO section </summary> */
        RoutingInfo = 0x01,
        /** <summary>Message contains ENDPOINT_INFO section</summary> */
        EndpointInfo = 0x02,
        /** <summary> Message contains PRIORITY section</summary> */
        Priority = 0x04,
        /** <summary>Message is unreliable and may be dropped</summary> */
        Unreliable = 0x08,
        /** <summary>Message contains META_INFO section </summary> */
        MetaInfo = 0x10,
        /** <summary> Message contains STRING_TABLE section</summary> */
        StringTable = 0x20,
        /** <summary>Message contains MULTIPLE_ENTRIES section. If unset, message contains one entry</summary> */
        MultipleEntries = 0x40,
        /** <summary>Message contains EXTENDED section</summary> */
        Extended = 0x80,

        /** <summary>Message flags for compatibility with Message Format Version 2 </summary>*/
        Version2Compat =
            RoutingInfo | EndpointInfo | MetaInfo | MultipleEntries
    }

    /// <summary>
    /// Flags for EntryFlags in MessageEntry
    /// </summary>
    [Flags, PublicApi]
    public enum MessageEntryFlags
    {
        /** <summary>MessageEntry contains SERVICE_PATH_STR section</summary> */
        ServicePathStr = 0x01,
        /** <summary>MessageEntry contains SERVICE_PATH_CODE section</summary> */
        ServicePathCode = 0x02,
        /** <summary>MessageEntry contains MEMBER_NAME_STR section</summary> */
        MemberNameStr = 0x04,
        /** <summary>MessageEntry contains MEMBER_NAME_CODE section</summary> */
        MemberNameCode = 0x08,
        /** <summary>MessageEntry contains REQUEST_ID section</summary> */
        RequestID = 0x10,
        /** <summary>MessageEntry contains ERROR section</summary> */
        Error = 0x20,
        /** <summary>MessageEntry contains META_INFO section</summary> */
        MetaInfo = 0x40,
        /** <summary>MessageEntry contains EXTENDED section</summary> */
        Extended = 0x80,

        /** <summary>MessageEntry flags for compatibility with Message Format Version 2</summary> */
        Version2Compat = ServicePathStr |
                                                         MemberNameStr | RequestID |
                                                         Error | MetaInfo
    }

    ///<summary>
    /// Flags for ElementFlags in MessageElement
    /// </summary>
    [Flags, PublicApi]
    public enum MessageElementFlags
    {
        /** <summary>MessageElement contains ELEMENT_NAME_STR section</summary> */
        ElementNameStr = 0x01,
        /** <summary>MessageElement contains ELEMENT_NAME_CODE section</summary> */
        ElementNameCode = 0x02,
        /** <summary>MessageElement contains ELEMENT_NUMBER section</summary> */
        ElementNumber = 0x04,
        /** <summary>MessageElement contains ELEMENT_TYPE_NAME_STR section</summary> */
        ElementTypeNameStr = 0x08,
        /** <summary>MessageElement contains ELEMENT_TYPE_NAME_CODE section</summary> */
        ElementTypeNameCode = 0x10,
        /** <summary>MessageElement contains META_INFO section</summary> */
        MetaInfo = 0x20,
        /** <summary>MessageElement contains EXTENDED section</summary> */
        Extended = 0x80,

        /** <summary>MessageElement flags for compatibility with Message Format Version 2</summary> */
        Version2Compat =
            ElementNameStr | ElementTypeNameStr | MetaInfo
    }


    /// <summary>
    /// Transport capability codes
    /// </summary>
    [Flags, PublicApi]
    public enum TransportCapabilityCode : uint
    {
        /** <summary>Page mask for transport capability code</summary> */
        PageMask = 0xFFF00000,
        /** <summary>Message Version 2 transport capability page code</summar> */
        Message2BasicPage = 0x02000000,
        /** <summary>Enable Message Version 2 transport capability flag</summary> */
        Message2BasicEnable = 0x00000001,
        /** <summary>Enable Message Version 2 connect combined transport capability flag</summary> */
        Message2BasicConnectCombined = 0x00000002,
        /** <summary>Message Version 4 transport capability page code</summary> */
        Message4BasicPage = 0x04000000,
        /** <summary>Enable Message Version 4 transport capability flag</summary> */
        Message4BasicEnable = 0x00000001,
        /** <summary>Enable Message Version 4 connect combine transport capability flag</summary> */
        Message4BasicConnectCombined = 0x00000002,
        /** <summary>Message Version 4 String Table capability page code</summary> */
        Message4StringTablePage = 0x04100000,
        /** <summary>Enable Message Version 4 String Table transport capability code</summary> */
        Message4StringTableEnable = 0x00000001,
        /** <summary>Enable Message Version 4 local String Table capability code</summary> */
        Message4StringTableMessageLocal = 0x00000002,
        /** <summary>Enable Message Version 4 standard String Table capability code</summary> */
        Message4StringTableStandardTable = 0x00000004
    }
    /// <summary>
    /// Represents a complex number using double precision.
    /// </summary>
    [PublicApi]
    public struct CDouble
    {
        /// <summary>
        /// The real component of the complex number.
        /// </summary>
        [PublicApi]
        public double Real;

        /// <summary>
        /// The imaginary component of the complex number.
        /// </summary>
        [PublicApi]
        public double Imag;

        /// <summary>
        /// Initializes a new instance of the <see cref="CDouble"/> struct.
        /// </summary>
        /// <param name="real">The real component of the complex number.</param>
        /// <param name="imag">The imaginary component of the complex number.</param>
        [PublicApi]
        public CDouble(double real, double imag)
        {
            Real = real;
            Imag = imag;
        }

        /// <summary>
        /// Determines whether two <see cref="CDouble"/> instances are equal.
        /// </summary>
        [PublicApi]
        public static bool operator ==(CDouble a, CDouble b)
        {
            return (a.Real == b.Real) && (a.Imag == b.Imag);
        }

        /// <summary>
        /// Determines whether two <see cref="CDouble"/> instances are not equal.
        /// </summary>
        [PublicApi]
        public static bool operator !=(CDouble a, CDouble b)
        {
            return !((a.Real == b.Real) && (a.Imag == b.Imag));
        }

        /// <summary>
        /// Determines whether this instance is equal to the specified object.
        /// </summary>
        [PublicApi]
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is CDouble)) return false;
            return ((CDouble)obj) == this;
        }

        /// <summary>
        /// Returns the hash code for this <see cref="CDouble"/> instance.
        /// </summary>
        [PublicApi]
        public override int GetHashCode()
        {
            return (int)(Real % 1e7 + Imag % 1e7);
        }
    }

    /// <summary>
    /// Represents a complex number using single precision.
    /// </summary>
    [PublicApi]
    public struct CSingle
    {
        /// <summary>
        /// The real component of the complex number.
        /// </summary>
        [PublicApi]
        public float Real;

        /// <summary>
        /// The imaginary component of the complex number.
        /// </summary>
        [PublicApi]
        public float Imag;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSingle"/> struct.
        /// </summary>
        /// <param name="real">The real component of the complex number.</param>
        /// <param name="imag">The imaginary component of the complex number.</param>
        [PublicApi]
        public CSingle(float real, float imag)
        {
            Real = real;
            Imag = imag;
        }

        /// <summary>
        /// Determines whether two <see cref="CSingle"/> instances are equal.
        /// </summary>
        [PublicApi]
        public static bool operator ==(CSingle a, CSingle b)
        {
            return (a.Real == b.Real) && (a.Imag == b.Imag);
        }

        /// <summary>
        /// Determines whether two <see cref="CSingle"/> instances are not equal.
        /// </summary>
        [PublicApi]
        public static bool operator !=(CSingle a, CSingle b)
        {
            return !((a.Real == b.Real) && (a.Imag == b.Imag));
        }

        /// <summary>
        /// Determines whether this instance is equal to the specified object.
        /// </summary>
        [PublicApi]
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is CSingle)) return false;
            return ((CSingle)obj) == this;
        }

        /// <summary>
        /// Returns the hash code for this <see cref="CSingle"/> instance.
        /// </summary>
        [PublicApi]
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
                case DataTypes.int8_t:
                    return new sbyte[length];
                case DataTypes.uint8_t:
                    return new byte[length];
                case DataTypes.int16_t:
                    return new short[length];
                case DataTypes.uint16_t:
                    return new ushort[length];
                case DataTypes.int32_t:
                    return new int[length];
                case DataTypes.uint32_t:
                    return new uint[length];
                case DataTypes.int64_t:
                    return new long[length];
                case DataTypes.uint64_t:
                    return new ulong[length];
                case DataTypes.string_t:
                    return null;
                case DataTypes.cdouble_t:
                    return new CDouble[length];
                case DataTypes.csingle_t:
                    return new CSingle[length];
                case DataTypes.bool_t:
                    return new bool[length];
                case DataTypes.structure_t:
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
                    return new double[] { ((double)inv) };
                case "System.Single":
                    return new float[] { ((float)inv) };
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
            entries = new List<MessageEntry>();
        }

        public uint ComputeSize()
        {
            uint s = header.ComputeSize();
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

        public MessageEntry AddEntry(MessageEntryType t, string name)
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
            entries = new List<MessageEntry>(s);
            for (int i = 0; i < s; i++)
            {
                MessageEntry e = new MessageEntry();
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
        public string SenderNodeName = "";
        public string ReceiverNodeName = "";
        public NodeID SenderNodeID = new NodeID(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        public NodeID ReceiverNodeID = new NodeID(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        public string MetaData = "";
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
            if (seed != "RRAC")
                throw new ProtocolException("Incorrect message seed");
            MessageSize = r.ReadUInt32();
            ushort version = r.ReadUInt16();
            if (version != 2)
                throw new ProtocolException("Uknown protocol version");

            HeaderLength = r.ReadUInt16();

            r.PushRelativeLimit((uint)(HeaderLength - 12));

            byte[] bSenderNodeID = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < 16; i++) { bSenderNodeID[i] = r.ReadByte(); };
            SenderNodeID = new NodeID(bSenderNodeID);
            byte[] bReceiverNodeID = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < 16; i++) { bReceiverNodeID[i] = r.ReadByte(); };
            ReceiverNodeID = new NodeID(bReceiverNodeID);
            SenderEndpoint = r.ReadUInt32();
            ReceiverEndpoint = r.ReadUInt32();
            ushort pname_s = r.ReadUInt16();
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

        public string ServicePath = "";

        public string MemberName = "";

        public uint RequestID;

        public MessageErrorType Error;

        public string MetaData = "";

        public List<MessageElement> elements;

        public MessageEntry()
        {
            elements = new List<MessageElement>();
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



            elements = new List<MessageElement>(ecount);
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

        public string ElementName = "";

        public DataTypes ElementType;

        public string ElementTypeName = "";

        public string MetaData = "";

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
            uint s = 16 + (uint)ArrayBinaryWriter.GetStringByteCount8(ElementName) + (uint)ArrayBinaryWriter.GetStringByteCount8(ElementTypeName) + (uint)ArrayBinaryWriter.GetStringByteCount8(MetaData);

            switch (ElementType)
            {
                case DataTypes.void_t:
                    break;
                case DataTypes.structure_t:
                case DataTypes.vector_t:
                case DataTypes.dictionary_t:
                case DataTypes.multidimarray_t:
                case DataTypes.list_t:
                case DataTypes.pod_t:
                case DataTypes.pod_array_t:
                case DataTypes.pod_multidimarray_t:
                case DataTypes.namedarray_array_t:
                case DataTypes.namedarray_multidimarray_t:
                    {
                        MessageElementNestedElementList d = (MessageElementNestedElementList)Data;


                        foreach (MessageElement e in d.Elements)
                        {
                            s += e.ComputeSize();
                        }
                    }
                    break;
                default:
                    {
                        s += DataCount * DataTypeUtil.size(ElementType);
                        break;
                    }
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
            else if (dat is MessageElementNestedElementList)
            {
                var dat2 = (MessageElementNestedElementList)dat;
                DataCount = (uint)dat2.Elements.Count;
                ElementTypeName = dat2.TypeName ?? "";
                switch (dat2.Type)
                {
                    case DataTypes.vector_t:
                        datatype = "RobotRaconteurWeb.MessageElementMap<int>";
                        break;
                    case DataTypes.dictionary_t:
                        datatype = "RobotRaconteurWeb.MessageElementMap<string>";
                        break;
                    case DataTypes.list_t:
                        datatype = "RobotRaconteurWeb.MessageElementList";
                        break;
                    case DataTypes.multidimarray_t:
                        datatype = "RobotRaconteurWeb.MessageElementMultiDimArray";
                        break;
                    case DataTypes.pod_t:
                        datatype = "RobotRaconteurWeb.MessageElementPod";
                        break;
                    case DataTypes.pod_array_t:
                        datatype = "RobotRaconteurWeb.MessageElementPodArray";
                        break;
                    case DataTypes.pod_multidimarray_t:
                        datatype = "RobotRaconteurWeb.MessageElementPodMultiDimArray";
                        break;
                    case DataTypes.namedarray_array_t:
                        datatype = "RobotRaconteurWeb.MessageElementNamedArray";
                        break;
                    case DataTypes.namedarray_multidimarray_t:
                        datatype = "RobotRaconteurWeb.MessageElementNamedMultiDimArray";
                        break;
                    default:
                        datatype = "RobotRaconteurWeb.MessageElementStructure";
                        break;
                }
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
            else if (dat is MessageElementNestedElementList)
            {
                List<MessageElement> l = ((MessageElementNestedElementList)dat).Elements;
                foreach (MessageElement e in l) e.Write(w);
            }
            else if (dat is string)
            {
                w.WriteString8((string)dat);
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
            ElementSize = r.ReadUInt32();
            r.PushRelativeLimit(ElementSize - 4);
            ushort name_s = r.ReadUInt16();
            ElementName = r.ReadString8(name_s);
            ElementType = (DataTypes)r.ReadUInt16();
            ushort nametype_s = r.ReadUInt16();
            ElementTypeName = r.ReadString8(nametype_s);
            ushort metadata_s = r.ReadUInt16();
            MetaData = r.ReadString8(metadata_s);
            DataCount = r.ReadUInt32();

            switch (ElementType)
            {
                case DataTypes.void_t:
                    break;
                case DataTypes.structure_t:
                case DataTypes.vector_t:
                case DataTypes.dictionary_t:
                case DataTypes.multidimarray_t:
                case DataTypes.list_t:
                case DataTypes.pod_t:
                case DataTypes.pod_array_t:
                case DataTypes.pod_multidimarray_t:
                case DataTypes.namedarray_array_t:
                case DataTypes.namedarray_multidimarray_t:
                    {
                        List<MessageElement> l = new List<MessageElement>((int)DataCount);
                        for (int i = 0; i < DataCount; i++)
                        {
                            MessageElement m = new MessageElement();
                            m.Read(r);
                            l.Add(m);
                        }

                        dat = new MessageElementNestedElementList(ElementType, ElementTypeName, l);
                        break;
                    }

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
                    {
                        if (DataCount * DataTypeUtil.size(ElementType) > r.DistanceFromLimit) throw new IOException("Error reading message");
                        Array d = DataTypeUtil.ArrayFromDataType(ElementType, DataCount);
                        r.ReadArray(d);
                        dat = (Object)d;
                    }
                    break;
                case DataTypes.string_t:
                    {
                        if (DataCount > r.DistanceFromLimit) throw new IOException("Error reading message");
                        dat = r.ReadString8(DataCount);
                        break;
                    }
                default:
                    throw new DataTypeException("Unknown data type");
            }


            if (r.DistanceFromLimit != 0) throw new IOException("Error reading message");
            r.PopLimit();

        }

        public static MessageElement FindElement(List<MessageElement> m, string name)
        {
            foreach (MessageElement e in m)
            {
                if (e.ElementName == name)
                {
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

        public string CastDataToString()
        {
            return CastData<string>();
        }

        public MessageElementNestedElementList CastDataToNestedList()
        {
            return CastData<MessageElementNestedElementList>();
        }

        public MessageElementNestedElementList CastDataToNestedList(DataTypes expected_type)
        {
            var l = CastData<MessageElementNestedElementList>();

            if (l != null && l.Type != expected_type)
            {
                throw new DataTypeMismatchException("Unexpected MessageElementNestedElementList type");
            }
            return l;
        }

        public static T CastData<T>(object Data)
        {
            if (Data == null) return default(T);
            if (Data is T)
                return (T)Data;
            throw new DataTypeException("Could not cast data to type " + typeof(T).ToString());
        }
    }

    public class MessageElementNestedElementList
    {
        public MessageElementNestedElementList(DataTypes type_, string type_name_, List<MessageElement> elements_)
        {
            Elements = elements_;
            Type = type_;
            TypeName = type_name_;
        }
        public List<MessageElement> Elements;

        public DataTypes Type;
        public string TypeName;

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

        public static string CastDataToString(MessageElement m)
        {
            return m.CastDataToString();
        }

        public static MessageElementNestedElementList CastDataToNestedList(MessageElement m)
        {
            return m.CastDataToNestedList();
        }

        public static MessageElementNestedElementList CastDataToNestedList(MessageElement m, DataTypes expected_type)
        {
            return m.CastDataToNestedList(expected_type);
        }

        public static int GetMessageElementNumber(MessageElement e)
        {
            int res;
            if (!Int32.TryParse(e.ElementName, out res))
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
            return NewMessageElement(name, node.PackStructure(val, client));
        }

        public static MessageElement PackVarType(RobotRaconteurNode node, ClientContext client, string name, object val)
        {
            return NewMessageElement(name, node.PackVarType(val, client));
        }

        public static MessageElement PackAnyType<T>(RobotRaconteurNode node, ClientContext client, string name, ref T val)
        {
            return node.PackAnyType<T>(name, ref val, client);
        }

        public static MessageElement PackMapType<K, T>(RobotRaconteurNode node, ClientContext client, string name, Dictionary<K, T> val)
        {
            return NewMessageElement(name, node.PackMapType<K, T>(val, client));
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
            MultiDimArray a = node.UnpackMultiDimArray(MessageElementUtil.CastDataToNestedList(m, DataTypes.multidimarray_t));
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
            return node.UnpackStructure<T>(MessageElementUtil.CastDataToNestedList(m, DataTypes.structure_t), client);
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
            return node.UnpackPodFromArray<T>(CastDataToNestedList(m, DataTypes.pod_array_t), client);
        }

        public static T[] UnpackPodArray<T>(RobotRaconteurNode node, ClientContext client, MessageElement m) where T : struct
        {
            return node.UnpackPodArray<T>(CastDataToNestedList(m, DataTypes.pod_array_t), client);
        }

        public static PodMultiDimArray UnpackPodMultiDimArray<T>(RobotRaconteurNode node, ClientContext client, MessageElement m) where T : struct
        {
            return node.UnpackPodMultiDimArray<T>(MessageElementUtil.CastDataToNestedList(m, DataTypes.pod_multidimarray_t), client);
        }

        public static T UnpackNamedArrayFromArray<T>(RobotRaconteurNode node, ClientContext client, MessageElement m) where T : struct
        {
            return node.UnpackNamedArrayFromArray<T>(CastDataToNestedList(m, DataTypes.namedarray_array_t), client);
        }

        public static T[] UnpackNamedArray<T>(RobotRaconteurNode node, ClientContext client, MessageElement m) where T : struct
        {
            return node.UnpackNamedArray<T>(CastDataToNestedList(m, DataTypes.namedarray_array_t), client);
        }

        public static NamedMultiDimArray UnpackNamedMultiDimArray<T>(RobotRaconteurNode node, ClientContext client, MessageElement m) where T : struct
        {
            return node.UnpackNamedMultiDimArray<T>(MessageElementUtil.CastDataToNestedList(m, DataTypes.namedarray_multidimarray_t), client);
        }

        public static string GetMessageElementDataTypeString(object o)
        {
            var f = o.GetType().GetField("Type");
            return (string)f.GetValue(o);
        }
    }


}
