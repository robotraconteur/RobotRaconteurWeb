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

namespace RobotRaconteurWeb
{
    public class RobotRaconteurException : Exception
    {
        public RobotRaconteurException()
            : base()
        {
        }

        public RobotRaconteurException(MessageErrorType ErrorCode, string error, string message)
            : base(message)
        {
            Error = error;
            this.ErrorCode = ErrorCode;
        }

        public RobotRaconteurException(string message, Exception innerexception)
            : base(message, innerexception)
        {

        }

        public MessageErrorType ErrorCode = MessageErrorType.None;

        public string Error = "";

        public override string ToString()
        {
            return "RobotRaconteurException: " + Error + ": " + Message;
        }


    }


    public class ConnectionException : RobotRaconteurException
    {
        public ConnectionException(string message)
            : base(MessageErrorType.ConnectionError, "RobotRaconteur.ConnectionError", message)
        {
        }
    }
    public class ProtocolException : RobotRaconteurException
    {
        public ProtocolException(string message)
            : base(MessageErrorType.ProtocolError, "RobotRaconteur.ProtocolError", message)
        {
        }
    }
    public class ServiceNotFoundException : RobotRaconteurException
    {
        public ServiceNotFoundException(string message)
            : base(MessageErrorType.ServiceNotFound, "RobotRaconteur.ServiceNotFound", message)
        {
        }
    }
    public class ObjectNotFoundException : RobotRaconteurException
    {
        public ObjectNotFoundException(string message)
            : base(MessageErrorType.ObjectNotFound, "RobotRaconteur.ObjectNotFound", message)
        {
        }
    }
    public class InvalidEndpointException : RobotRaconteurException
    {
        public InvalidEndpointException(string message)
            : base(MessageErrorType.InvalidEndpoint, "RobotRaconteur.InvalidEndpoint", message)
        {
        }
    }
    public class EndpointCommunicationFatalException : RobotRaconteurException
    {
        public EndpointCommunicationFatalException(string message)
            : base(MessageErrorType.EndpointCommunicationFatalError, "RobotRaconteur.EndpointCommunicationFatalError", message)
        {
        }
    }
    public class NodeNotFoundException : RobotRaconteurException
    {
        public NodeNotFoundException(string message)
            : base(MessageErrorType.NodeNotFound, "RobotRaconteur.NodeNotFound", message)
        {
        }
    }
    public class ServiceException : RobotRaconteurException
    {
        public ServiceException(string message)
            : base(MessageErrorType.ServiceError, "RobotRaconteur.ServiceError", message)
        {
        }
    }
    public class MemberNotFoundException : RobotRaconteurException
    {
        public MemberNotFoundException(string message)
            : base(MessageErrorType.MemberNotFound, "RobotRaconteur.MemberNotFound", message)
        {
        }
    }
    public class MemberFormatMismatchException : RobotRaconteurException
    {
        public MemberFormatMismatchException(string message)
            : base(MessageErrorType.MemberFormatMismatch, "RobotRaconteur.MemberFormatMismatch", message)
        {
        }
    }
    public class DataTypeMismatchException : RobotRaconteurException
    {
        public DataTypeMismatchException(string message)
            : base(MessageErrorType.DataTypeMismatch, "RobotRaconteur.DataTypeMismatch", message)
        {
        }
    }
    public class DataTypeException : RobotRaconteurException
    {
        public DataTypeException(string message)
            : base(MessageErrorType.DataTypeError, "RobotRaconteur.DataTypeError", message)
        {
        }
    }
    public class DataSerializationException : RobotRaconteurException
    {
        public DataSerializationException(string message)
            : base(MessageErrorType.DataSerializationError, "RobotRaconteur.DataSerializationError", message)
        {
        }
    }
    public class MessageEntryNotFoundException : RobotRaconteurException
    {
        public MessageEntryNotFoundException(string message)
            : base(MessageErrorType.MessageEntryNotFound, "RobotRaconteur.MessageEntryNotFound", message)
        {
        }
    }
    public class MessageElementNotFoundException : RobotRaconteurException
    {
        public MessageElementNotFoundException(string message)
            : base(MessageErrorType.MessageElementNotFound, "RobotRaconteur.MessageElementNotFound", message)
        {
        }
    }
    public class UnknownException : RobotRaconteurException
    {
        public UnknownException(string error, string message)
            : base(MessageErrorType.UnknownError, error, message)
        {
        }
    }
    public class OperationFailedException : RobotRaconteurException
    {
        public OperationFailedException(string message)
            : base(MessageErrorType.OperationFailed, "RobotRaconteur.OperationFailed", message)
        {
        }
    }
    public class InternalErrorException : RobotRaconteurException
    {
        public InternalErrorException(string message)
            : base(MessageErrorType.InternalError, "RobotRaconteur.InternalError", message)
        {
        }
    }
    public class SystemResourcePermissionDeniedException : RobotRaconteurException
    {
        public SystemResourcePermissionDeniedException(string message)
            : base(MessageErrorType.SystemResourcePermissionDenied, "RobotRaconteur.SystemResourcePermissionDenied", message)
        {
        }
    }
    public class OutOfSystemResourceException : RobotRaconteurException
    {
        public OutOfSystemResourceException(string message)
            : base(MessageErrorType.OutOfSystemResource, "RobotRaconteur.OutOfSystemResource", message)
        {
        }
    }
    public class SystemResourceException : RobotRaconteurException
    {
        public SystemResourceException(string message)
            : base(MessageErrorType.SystemResourceError, "RobotRaconteur.SystemResourceError", message)
        {
        }
    }
    public class ResourceNotFoundException : RobotRaconteurException
    {
        public ResourceNotFoundException(string message)
            : base(MessageErrorType.ResourceNotFound, "RobotRaconteur.ResourceNotFound", message)
        {
        }
    }
    public class IOException : RobotRaconteurException
    {
        public IOException(string message)
            : base(MessageErrorType.IOError, "RobotRaconteur.IOError", message)
        {
        }
    }
    public class BufferLimitViolationException : RobotRaconteurException
    {
        public BufferLimitViolationException(string message)
            : base(MessageErrorType.BufferLimitViolation, "RobotRaconteur.BufferLimitViolation", message)
        {
        }
    }
    public class ServiceDefinitionException : RobotRaconteurException
    {
        public ServiceDefinitionException(string message)
            : base(MessageErrorType.ServiceDefinitionError, "RobotRaconteur.SystemDefinitionError", message)
        {
        }
    }
    
    public class RobotRaconteurRemoteException : RobotRaconteurException
    {
        public RobotRaconteurRemoteException(string error, string message)
            : base(MessageErrorType.RemoteError, error, message)
        {
        }
    }
    public class RequestTimeoutException : RobotRaconteurException
    {
        public RequestTimeoutException(string message)
            : base(MessageErrorType.RequestTimeout, "RobotRaconteur.RequestTimeout", message)
        {
        }
    }
    public class ReadOnlyMemberException : RobotRaconteurException
    {
        public ReadOnlyMemberException(string message)
            : base(MessageErrorType.ReadOnlyMember, "RobotRaconteur.ReadOnlyMember", message)
        {
        }
    }
    public class WriteOnlyMemberException : RobotRaconteurException
    {
        public WriteOnlyMemberException(string message)
            : base(MessageErrorType.WriteOnlyMember, "RobotRaconteur.WriteOnlyMember", message)
        {
        }
    }
    public class MemberBusyException : RobotRaconteurException
    {
        public MemberBusyException(string message)
            : base(MessageErrorType.MemberBusy, "RobotRaconteur.MemberBusy", message)
        {
        }
    }
    public class ValueNotSetException : RobotRaconteurException
    {
        public ValueNotSetException(string message)
            : base(MessageErrorType.ValueNotSet, "RobotRaconteur.ValueNotSet", message)
        {
        }
    }
    public class AuthenticationException : RobotRaconteurException
    {
        public AuthenticationException(string message)
            : base(MessageErrorType.AuthenticationError, "RobotRaconteur.AuthenticationError", message)
        {
        }
    }
    public class ObjectLockedException : RobotRaconteurException
    {
        public ObjectLockedException(string message)
            : base(MessageErrorType.ObjectLockedError, "RobotRaconteur.ObjectLockedError", message)
        {
        }
    }
    public class PermissionDeniedException : RobotRaconteurException
    {
        public PermissionDeniedException(string message)
            : base(MessageErrorType.PermissionDenied, "RobotRaconteur.PermissionDenied", message)
        {
        }
    }
    public class AbortOperationException : RobotRaconteurException
    {
        public AbortOperationException(string message)
            : base(MessageErrorType.AbortOperation, "RobotRaconteur.AbortOperation", message)
        {
        }
    }
    public class OperationAbortedException : RobotRaconteurException
    {
        public OperationAbortedException(string message)
            : base(MessageErrorType.OperationAborted, "RobotRaconteur.OperationAborted", message)
        {
        }
    }
    public class StopIterationException : RobotRaconteurException
    {
        public StopIterationException(string message)
            : base(MessageErrorType.StopIteration, "RobotRaconteur.StopIteration", message)
        {
        }
    }

    public class RobotRaconteurExceptionUtil
    {
        public static void ExceptionToMessageEntry(Exception exception, MessageEntry entry)
        {


            if (exception is InvalidOperationException)
            {
                entry.Error = MessageErrorType.InvalidOperation;
                entry.AddElement("errorname", "RobotRaconteur.InvalidOperation");
                entry.AddElement("errorstring", exception.Message);
                return;
            }

            if (exception is ArgumentException)
            {
                entry.Error = MessageErrorType.InvalidArgument;
                entry.AddElement("errorname", "RobotRaconteur.InvalidArgument");
                entry.AddElement("errorstring", exception.Message);
                return;
            }

            if (exception is NullReferenceException)
            {
                entry.Error = MessageErrorType.NullValue;
                entry.AddElement("errorname", "RobotRaconteur.NullValue");
                entry.AddElement("errorstring", exception.Message);
                return;
            }

            if (exception is NotImplementedException)
            {
                entry.Error = MessageErrorType.NotImplementedError;
                entry.AddElement("errorname", "RobotRaconteur.NotImplementedError");
                entry.AddElement("errorstring", exception.Message);
                return;
            }

            if (exception is ArgumentOutOfRangeException)
            {
                entry.Error = MessageErrorType.OutOfRange;
                entry.AddElement("errorname", "RobotRaconteur.OutOfRange");
                entry.AddElement("errorstring", exception.Message);
                return;
            }

            if (exception is KeyNotFoundException)
            {
                entry.Error = MessageErrorType.KeyNotFound;
                entry.AddElement("errorname", "RobotRaconteur.KeyNotFound");
                entry.AddElement("errorstring", exception.Message);
                return;
            }

            if (exception is RobotRaconteurException)
            {
                RobotRaconteurException r = (RobotRaconteurException)exception;
                entry.Error = r.ErrorCode;
                entry.AddElement("errorname", r.Error);
                entry.AddElement("errorstring", r.Message);

            }
            else
            {
                entry.Error = MessageErrorType.RemoteError;
                entry.AddElement("errorname", exception.GetType().ToString());
                entry.AddElement("errorstring", exception.Message);


            }

        }

        public static Exception MessageEntryToException(MessageEntry entry)
        {
            var error_code = entry.Error;
            var errorname = entry.FindElement("errorname").CastData<string>();
            var errorstring = entry.FindElement("errorstring").CastData<string>();

            switch (error_code)
            {
                case MessageErrorType.RemoteError:
                    RobotRaconteurException e1 = new RobotRaconteurRemoteException(errorname, errorstring);
                    return e1;

                case MessageErrorType.ConnectionError:
                    return new ConnectionException(errorstring);

                case MessageErrorType.ProtocolError:
                    return new ProtocolException(errorstring);

                case MessageErrorType.ServiceNotFound:
                    return new ServiceNotFoundException(errorstring);

                case MessageErrorType.ObjectNotFound:
                    return new ObjectNotFoundException(errorstring);

                case MessageErrorType.InvalidEndpoint:
                    return new InvalidEndpointException(errorstring);

                case MessageErrorType.EndpointCommunicationFatalError:
                    return new EndpointCommunicationFatalException(errorstring);

                case MessageErrorType.NodeNotFound:
                    return new NodeNotFoundException(errorstring);

                case MessageErrorType.ServiceError:
                    return new ServiceException(errorstring);

                case MessageErrorType.MemberNotFound:
                    return new MemberNotFoundException(errorstring);

                case MessageErrorType.MemberFormatMismatch:
                    return new MemberFormatMismatchException(errorstring);

                case MessageErrorType.DataTypeMismatch:
                    return new DataTypeMismatchException(errorstring);

                case MessageErrorType.DataTypeError:
                    return new DataTypeException(errorstring);

                case MessageErrorType.DataSerializationError:
                    return new DataSerializationException(errorstring);

                case MessageErrorType.MessageEntryNotFound:
                    return new MessageEntryNotFoundException(errorstring);

                case MessageErrorType.MessageElementNotFound:
                    return new MessageElementNotFoundException(errorstring);

                case MessageErrorType.UnknownError:
                    return new UnknownException(errorname, errorstring);

                case MessageErrorType.InvalidOperation:
                    return new InvalidOperationException(errorstring);

                case MessageErrorType.InvalidArgument:
                    return new ArgumentException(errorstring);

                case MessageErrorType.OperationFailed:
                    return new OperationFailedException(errorstring);

                case MessageErrorType.NullValue:
                    return new NullReferenceException(errorstring);

                case MessageErrorType.InternalError:
                    return new InternalErrorException(errorstring);

                case MessageErrorType.SystemResourcePermissionDenied:
                    return new SystemResourcePermissionDeniedException(errorstring);

                case MessageErrorType.OutOfSystemResource:
                    return new OutOfSystemResourceException(errorstring);

                case MessageErrorType.SystemResourceError:
                    return new SystemResourceException(errorstring);

                case MessageErrorType.ResourceNotFound:
                    return new ResourceNotFoundException(errorstring);

                case MessageErrorType.IOError:
                    return new IOException(errorstring);

                case MessageErrorType.BufferLimitViolation:
                    return new BufferLimitViolationException(errorstring);

                case MessageErrorType.ServiceDefinitionError:
                    return new ServiceDefinitionException(errorstring);

                case MessageErrorType.OutOfRange:
                    return new ArgumentOutOfRangeException(errorstring);

                case MessageErrorType.KeyNotFound:
                    return new KeyNotFoundException(errorstring);

                case MessageErrorType.RequestTimeout:
                    return new RequestTimeoutException(errorstring);

                case MessageErrorType.ReadOnlyMember:
                    return new ReadOnlyMemberException(errorstring);

                case MessageErrorType.WriteOnlyMember:
                    return new WriteOnlyMemberException(errorstring);

                case MessageErrorType.NotImplementedError:
                    return new NotImplementedException(errorstring);

                case MessageErrorType.MemberBusy:
                    return new MemberBusyException(errorstring);

                case MessageErrorType.ValueNotSet:
                    return new ValueNotSetException(errorstring);

                case MessageErrorType.AuthenticationError:
                    return new AuthenticationException(errorstring);

                case MessageErrorType.ObjectLockedError:
                    return new ObjectLockedException(errorstring);

                case MessageErrorType.PermissionDenied:
                    return new PermissionDeniedException(errorstring);

                case MessageErrorType.AbortOperation:
                    return new AbortOperationException(errorstring);

                case MessageErrorType.OperationAborted:
                    return new OperationAbortedException(errorstring);

                case MessageErrorType.StopIteration:
                    return new StopIterationException(errorstring);

            }

            return new RobotRaconteurException(error_code, errorname, errorstring);

        }
    }
}
