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
    /**
    <summary>
    The base class for Robot Raconteur exceptions.  These exception contain a Robot Raconteur error code
    </summary>
    */
    public class RobotRaconteurException : Exception
    {
        /**
        <summary>
        Initializes an empty exception
        </summary>
        */
        public RobotRaconteurException()
            : base()
        {
        }
        /**
        <summary>
        Initializes a new exception
        </summary>
        <param name="ErrorCode">The error code</param>
        <param name="error">The Robot Raconteur error name</param>
        <param name="message">The Robot Raconteur error message</param>
        */
        public RobotRaconteurException(MessageErrorType ErrorCode, string error, string message)
            : base(message)
        {
            Error = error;
            this.ErrorCode = ErrorCode;
        }
        /**
        <summary>
        Initializes a Robot Raconteur exception that contains a C# exception
        </summary>
        <param name="message">The message</param>
        <param name="innerexception">The C# contained by this exception</param>
        */
        public RobotRaconteurException(string message, Exception innerexception)
            : base(message, innerexception)
        {

        }
        /**
        <summary>
        The error code
        </summary>
        */
        public MessageErrorType ErrorCode = MessageErrorType.None;
        /**
        <summary>
        The error name
        </summary>
        */
        public string Error = "";
        /**
        <summary>
        Returns a string representation of this exception
        </summary>
        <returns>The string representation</returns>
        */
        public override string ToString()
        {
            return "RobotRaconteurException: " + Error + ": " + Message;
        }


    }


    /// <summary>
    /// Represents an exception that is thrown when a connection-related error occurs.
    /// </summary>
    public class ConnectionException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ConnectionException(string message)
            : base(MessageErrorType.ConnectionError, "RobotRaconteur.ConnectionError", message)
        {
        }
    }

    /// <summary>
    /// Represents an exception that is thrown when a protocol-related error occurs.
    /// </summary>
    public class ProtocolException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ProtocolException(string message)
            : base(MessageErrorType.ProtocolError, "RobotRaconteur.ProtocolError", message)
        {
        }
    }

    /// <summary>
    /// Represents an exception that is thrown when a requested service is not found.
    /// </summary>
    public class ServiceNotFoundException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ServiceNotFoundException(string message)
            : base(MessageErrorType.ServiceNotFound, "RobotRaconteur.ServiceNotFound", message)
        {
        }
    }

    /// <summary>
    /// Represents an exception that is thrown when a requested object is not found.
    /// </summary>
    public class ObjectNotFoundException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ObjectNotFoundException(string message)
            : base(MessageErrorType.ObjectNotFound, "RobotRaconteur.ObjectNotFound", message)
        {
        }
    }

    /// <summary>
    /// Represents an exception that is thrown when an invalid endpoint is encountered.
    /// </summary>
    public class InvalidEndpointException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidEndpointException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidEndpointException(string message)
            : base(MessageErrorType.InvalidEndpoint, "RobotRaconteur.InvalidEndpoint", message)
        {
        }
    }

    /// <summary>
    /// Represents an exception that indicates a fatal error occurred during endpoint communication.
    /// </summary>
    public class EndpointCommunicationFatalException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointCommunicationFatalException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public EndpointCommunicationFatalException(string message)
            : base(MessageErrorType.EndpointCommunicationFatalError, "RobotRaconteur.EndpointCommunicationFatalError", message)
        {
        }
    }

    /// <summary>
    /// Represents an exception that is thrown when a requested node is not found.
    /// </summary>
    public class NodeNotFoundException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NodeNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public NodeNotFoundException(string message)
            : base(MessageErrorType.NodeNotFound, "RobotRaconteur.NodeNotFound", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a service error occurs.
    /// </summary>
    public class ServiceException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ServiceException(string message)
            : base(MessageErrorType.ServiceError, "RobotRaconteur.ServiceError", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a member is not found.
    /// </summary>
    public class MemberNotFoundException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public MemberNotFoundException(string message)
            : base(MessageErrorType.MemberNotFound, "RobotRaconteur.MemberNotFound", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a member format does not match.
    /// </summary>
    public class MemberFormatMismatchException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberFormatMismatchException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public MemberFormatMismatchException(string message)
            : base(MessageErrorType.MemberFormatMismatch, "RobotRaconteur.MemberFormatMismatch", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a data type does not match.
    /// </summary>
    public class DataTypeMismatchException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeMismatchException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DataTypeMismatchException(string message)
            : base(MessageErrorType.DataTypeMismatch, "RobotRaconteur.DataTypeMismatch", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a data type is invalid.
    /// </summary>
    public class DataTypeException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTypeException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DataTypeException(string message)
            : base(MessageErrorType.DataTypeError, "RobotRaconteur.DataTypeError", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a data serialization error occurs.
    /// </summary>
    public class DataSerializationException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataSerializationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public DataSerializationException(string message)
            : base(MessageErrorType.DataSerializationError, "RobotRaconteur.DataSerializationError", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a message entry is not found.
    /// </summary>
    public class MessageEntryNotFoundException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageEntryNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public MessageEntryNotFoundException(string message)
            : base(MessageErrorType.MessageEntryNotFound, "RobotRaconteur.MessageEntryNotFound", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a message element is not found.
    /// </summary>
    public class MessageElementNotFoundException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageElementNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public MessageElementNotFoundException(string message)
            : base(MessageErrorType.MessageElementNotFound, "RobotRaconteur.MessageElementNotFound", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when an unknown error occurs.
    /// </summary>
    public class UnknownException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownException"/> class with a specified error message and error code.
        /// </summary>
        /// <param name="error">The error code that identifies the type of error.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public UnknownException(string error, string message)
            : base(MessageErrorType.UnknownError, error, message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when an operation fails.
    /// </summary>
    public class OperationFailedException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationFailedException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public OperationFailedException(string message)
            : base(MessageErrorType.OperationFailed, "RobotRaconteur.OperationFailed", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when an internal error occurs.
    /// </summary>
    public class InternalErrorException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InternalErrorException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public InternalErrorException(string message)
            : base(MessageErrorType.InternalError, "RobotRaconteur.InternalError", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a system resource permission is denied.
    /// </summary>
    public class SystemResourcePermissionDeniedException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemResourcePermissionDeniedException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public SystemResourcePermissionDeniedException(string message)
            : base(MessageErrorType.SystemResourcePermissionDenied, "RobotRaconteur.SystemResourcePermissionDenied", message)
        {
        }
    }
    /// <summary>
    /// The exception that is thrown when the system runs out of a critical resource.
    /// </summary>
    public class OutOfSystemResourceException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutOfSystemResourceException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public OutOfSystemResourceException(string message)
            : base(MessageErrorType.OutOfSystemResource, "RobotRaconteur.OutOfSystemResource", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a system resource error occurs.
    /// </summary>
    public class SystemResourceException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemResourceException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public SystemResourceException(string message)
            : base(MessageErrorType.SystemResourceError, "RobotRaconteur.SystemResourceError", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a requested resource is not found.
    /// </summary>
    public class ResourceNotFoundException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ResourceNotFoundException(string message)
            : base(MessageErrorType.ResourceNotFound, "RobotRaconteur.ResourceNotFound", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a buffer limit violation occurs.
    /// </summary>
    public class BufferLimitViolationException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BufferLimitViolationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public BufferLimitViolationException(string message)
            : base(MessageErrorType.BufferLimitViolation, "RobotRaconteur.BufferLimitViolation", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a service definition error occurs.
    /// </summary>
    public class ServiceDefinitionException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceDefinitionException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ServiceDefinitionException(string message)
            : base(MessageErrorType.ServiceDefinitionError, "RobotRaconteur.SystemDefinitionError", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a remote error occurs.
    /// </summary>
    public class RobotRaconteurRemoteException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RobotRaconteurRemoteException"/> class with a specified error message and error code.
        /// </summary>
        /// <param name="error">The error code that identifies the type of error.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public RobotRaconteurRemoteException(string error, string message)
            : base(MessageErrorType.RemoteError, error, message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a request times out.
    /// </summary>
    public class RequestTimeoutException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTimeoutException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public RequestTimeoutException(string message)
            : base(MessageErrorType.RequestTimeout, "RobotRaconteur.RequestTimeout", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a read-only member is modified.
    /// </summary>
    public class ReadOnlyMemberException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemberException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ReadOnlyMemberException(string message)
            : base(MessageErrorType.ReadOnlyMember, "RobotRaconteur.ReadOnlyMember", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a write-only member is accessed.
    /// </summary>
    public class WriteOnlyMemberException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteOnlyMemberException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public WriteOnlyMemberException(string message)
            : base(MessageErrorType.WriteOnlyMember, "RobotRaconteur.WriteOnlyMember", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a member is busy.
    /// </summary>
    public class MemberBusyException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberBusyException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public MemberBusyException(string message)
            : base(MessageErrorType.MemberBusy, "RobotRaconteur.MemberBusy", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when a value is not set.
    /// </summary>
    public class ValueNotSetException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueNotSetException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ValueNotSetException(string message)
            : base(MessageErrorType.ValueNotSet, "RobotRaconteur.ValueNotSet", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when an authentication error occurs.
    /// </summary>
    public class AuthenticationException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public AuthenticationException(string message)
            : base(MessageErrorType.AuthenticationError, "RobotRaconteur.AuthenticationError", message)
        {
        }
    }
    /// <summary>
    /// The exception that is thrown when an object is locked.
    /// </summary>
    public class ObjectLockedException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectLockedException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ObjectLockedException(string message)
            : base(MessageErrorType.ObjectLockedError, "RobotRaconteur.ObjectLockedError", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when permission is denied.
    /// </summary>
    public class PermissionDeniedException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionDeniedException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public PermissionDeniedException(string message)
            : base(MessageErrorType.PermissionDenied, "RobotRaconteur.PermissionDenied", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when an operation is aborted.
    /// </summary>
    public class AbortOperationException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbortOperationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public AbortOperationException(string message)
            : base(MessageErrorType.AbortOperation, "RobotRaconteur.AbortOperation", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when an operation is aborted.
    /// </summary>
    public class OperationAbortedException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationAbortedException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public OperationAbortedException(string message)
            : base(MessageErrorType.OperationAborted, "RobotRaconteur.OperationAborted", message)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when an iteration is stopped.
    /// </summary>
    public class StopIterationException : RobotRaconteurException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StopIterationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
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

            if (exception is System.IO.IOException)
            {
                entry.Error = MessageErrorType.IOError;
                entry.AddElement("errorname", "RobotRaconteur.IOError");
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
                    return new System.IO.IOException(errorstring);

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
