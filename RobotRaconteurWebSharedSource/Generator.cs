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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;

namespace RobotRaconteurWeb
{
    /**
    <summary>
    Generator type for use with generator functions, with parameter and return
    </summary>
    <remarks>
    <para>
    Generators are used with generator functions to implement simple coroutines. They are
    returned by function members with a parameter and/or return marked with the
    generator container type. Robot Raconteur generators are modeled on Python generators,
    and are intended to be used in two scenarios:
    1. Transferring large parameter values or return values that would be over the message
    transfer limit (typically around 10 MB).
    2. Long running operations that return updates or require periodic input. Generators
    are used to implement functionality similar to "actions" in ROS.
    </para>
    <para>
    Generators are a generalization of iterators, where a value is returned every time
    the iterator is advanced until there are no more values. Python and Robot Raconteur iterators
    add the option of passing a parameter every advance, allowing for simple coroutines. The
    generator is advanced by calling the Next() function. These functions
    will either return a value or throw StopIterationException if there are no more values. Next()
    may also throw any valid Robot Raconteur exception.
    </para>
    <para>
    Generators can be terminated with either the Close() or Abort() functions. Close() should be
    used to cleanly close the generator, and is not considered an error condition. Next(), if called
    after close, should throw StopIterationException. Abort() is considered an error condition, and
    will cause any action associated with the generator to be aborted as quickly as possible (ie faulting
    a robot). If Next() is called after Abort(), OperationAbortedException should be thrown.
    </para>
    <para>
    Robot Raconteur clients will return a populated stub generator that calls the service. Services
    are expected to return a subclass of Generator.
    </para>
    </remarks>
    <typeparam name="ReturnType">The type of value returned by Next() </typeparam>
    <typeparam name="ParamType">The type of the parameter passed to Next() </typeparam>
    */

    [PublicApi]
    public interface Generator1<ReturnType, ParamType>
    {
        /**
        <summary>
        Advance the generator
        </summary>
        <remarks>
        Next() advances the generator to retrieve the next value. This version of
        Generator includes passing a parameter v to the generator.
        </remarks>
        <param name="param">Parameter to pass to generator</param>
        <param name="cancel">Cancellation token for operation</param>
        <returns>Return value from generator</returns>
        */

        [PublicApi]
        Task<ReturnType> Next(ParamType param, CancellationToken cancel = default(CancellationToken));
        /**
        <summary>
        Abort the generator
        </summary>
        <remarks>
        Aborts and destroys the generator. This is assumed to be an error condition. Next() should throw
        OperationAbortedException if called after Abort(). Any ongoing operations should be terminated with an error
        condition, for example a moving robot should be immediately halted.
        </remarks>
        */


        [PublicApi]
        Task Abort(CancellationToken cancel = default(CancellationToken));
        /**
        <summary>
        Close the generator
        </summary>
        <remarks>
        Closes the generator. Closing the generator terminates iteration and destroys the generator.
        This operation cleanly closes the generator, and is not considered to be an error condition. Next()
        should throw StopIterationException if called after Close().
        </remarks>
        */
        [PublicApi]

        Task Close(CancellationToken cancel = default(CancellationToken));
    }
    /**
    <summary>
    Generator type for use with generator functions, with return
    </summary>
    <remarks>
    <para>
    Generators are used with generator functions to implement simple coroutines. They are
    returned by function members with a parameter and/or return marked with the
    generator container type. Robot Raconteur generators are modeled on Python generators,
    and are intended to be used in two scenarios:
    1. Transferring large parameter values or return values that would be over the message
    transfer limit (typically around 10 MB).
    2. Long running operations that return updates or require periodic input. Generators
    are used to implement functionality similar to "actions" in ROS.
    </para>
    <para>
    Generators are a generalization of iterators, where a value is returned every time
    the iterator is advanced until there are no more values. Python and Robot Raconteur iterators
    add the option of passing a parameter every advance, allowing for simple coroutines. The
    generator is advanced by calling the Next() function. These functions
    will either return a value or throw StopIterationException if there are no more values. Next()
    may also throw any valid Robot Raconteur exception.
    </para>
    <para>
    Generators can be terminated with either the Close() or Abort() functions. Close() should be
    used to cleanly close the generator, and is not considered an error condition. Next(), if called
    after close, should throw StopIterationException. Abort() is considered an error condition, and
    will cause any action associated with the generator to be aborted as quickly as possible (ie faulting
    a robot). If Next() is called after Abort(), OperationAbortedException should be thrown.
    </para>
    <para>
    Robot Raconteur clients will return a populated stub generator that calls the service. Services
    are expected to return a subclass of Generator.
    </para>
    </remarks>
    <typeparam name="ReturnType">Return The type of value returned by Next()</typeparam>
    */

    [PublicApi]
    public interface Generator2<ReturnType>
    {
        /**
        <summary>
        Advance the generator
        </summary>
        <remarks>
        Next() advances the generator to retrieve the next value. This version of
        Generator does not include passing a parameter to the generator.
        </remarks>
        <returns>Return Return value from generator</returns>
        */

        [PublicApi]
        Task<ReturnType> Next(CancellationToken cancel = default(CancellationToken));
        /**
        <summary>
        Abort the generator
        </summary>
        <remarks>
        Aborts and destroys the generator. This is assumed to be an error condition. Next() should throw
        OperationAbortedException if called after Abort(). Any ongoing operations should be terminated with an error
        condition, for example a moving robot should be immediately halted.
        </remarks>
        */

        [PublicApi]
        Task Abort(CancellationToken cancel = default(CancellationToken));
        /**
        <summary>
        Close the generator
        </summary>
        <remarks>
        Closes the generator. Closing the generator terminates iteration and destroys the generator.
        This operation cleanly closes the generator, and is not considered to be an error condition. Next()
        should throw StopIterationException if called after Close().
        </remarks>
        */

        [PublicApi]
        Task Close(CancellationToken cancel = default(CancellationToken));
        /**
        <summary>
        Automatically call Next() repeatedly and return array of results
        </summary>
        <returns>All values returned by generator Next()</returns>
        */

        [PublicApi]
        Task<ReturnType[]> NextAll(CancellationToken cancel = default(CancellationToken));
    }
    /**
    <summary>
    Generator type for use with generator functions, with parameter
    </summary>
    <remarks>
    <para>
    Generators are used with generator functions to implement simple coroutines. They are
    returned by function members with a parameter and/or return marked with the
    generator container type. Robot Raconteur generators are modeled on Python generators,
    and are intended to be used in two scenarios:
    1. Transferring large parameter values or return values that would be over the message
    transfer limit (typically around 10 MB).
    2. Long running operations that return updates or require periodic input. Generators
    are used to implement functionality similar to "actions" in ROS.
    </para>
    <para>
    Generators are a generalization of iterators, where a value is returned every time
    the iterator is advanced until there are no more values. Python and Robot Raconteur iterators
    add the option of passing a parameter every advance, allowing for simple coroutines. The
    generator is advanced by calling the Next() function. These functions
    will either return a value or throw StopIterationException if there are no more values. Next()
    may also throw any valid Robot Raconteur exception.
    </para>
    <para>
    Generators can be terminated with either the Close() or Abort() functions. Close() should be
    used to cleanly close the generator, and is not considered an error condition. Next(), if called
    after close, should throw StopIterationException. Abort() is considered an error condition, and
    will cause any action associated with the generator to be aborted as quickly as possible (ie faulting
    a robot). If Next() is called after Abort(), OperationAbortedException should be thrown.
    </para>
    <para>
    Robot Raconteur clients will return a populated stub generator that calls the service. Services
    are expected to return a subclass of Generator.
    </para>
    </remarks>
    <typeparam name="ParamType">The type of the parameter passed to Next()</typeparam>
    */

    [PublicApi]
    public interface Generator3<ParamType>
    {
        /**
        <summary>
        Advance the generator
        </summary>
        <remarks>
        Next() advances the generator to retrieve the next value. This version of
        Generator includes passing a parameter to the generator but no return.
        </remarks>
        <param name="param">Parameter to pass to generator</param>
        <param name="cancel">The cancellation token for the operation</param>
        */

        [PublicApi]
        Task Next(ParamType param, CancellationToken cancel = default(CancellationToken));
        /**
        <summary>
        Abort the generator
        </summary>
        <remarks>
        Aborts and destroys the generator. This is assumed to be an error condition. Next() should throw
        OperationAbortedException if called after Abort(). Any ongoing operations should be terminated with an error
        condition, for example a moving robot should be immediately halted.
        </remarks>
        */

        [PublicApi]
        Task Abort(CancellationToken cancel = default(CancellationToken));
        /**
        <summary>
        Close the generator
        </summary>
        <remarks>
        Closes the generator. Closing the generator terminates iteration and destroys the generator.
        This operation cleanly closes the generator, and is not considered to be an error condition. Next()
        should throw StopIterationException if called after Close().
        </remarks>
        */

        [PublicApi]
        Task Close(CancellationToken cancel = default(CancellationToken));
    }
#pragma warning disable 1591
    public abstract class GeneratorClientBase
    {
        protected ServiceStub stub;
        protected string membername;
        internal int id;

        protected GeneratorClientBase(string membername, ServiceStub stub, int id)
        {
            this.stub = stub;
            this.membername = membername;
            this.id = id;
        }

        public string MemberName { get => membername; }

        public async Task Abort(CancellationToken cancel = default(CancellationToken))
        {
            var m = new MessageEntry(MessageEntryType.GeneratorNextReq, MemberName);
            var err = new AbortOperationException("Generator abort requested");
            RobotRaconteurExceptionUtil.ExceptionToMessageEntry(err, m);
            m.AddElement("index", id);
            await stub.ProcessRequest(m, cancel).ConfigureAwait(false);
        }
        public async Task Close(CancellationToken cancel = default(CancellationToken))
        {
            var m = new MessageEntry(MessageEntryType.GeneratorNextReq, MemberName);
            var err = new StopIterationException("Generator abort requested");
            RobotRaconteurExceptionUtil.ExceptionToMessageEntry(err, m);
            m.AddElement("index", id);
            await stub.ProcessRequest(m, cancel).ConfigureAwait(false);
        }

        public async Task<MessageElement> NextBase(MessageElement v, CancellationToken cancel)
        {
            var m = new MessageEntry(MessageEntryType.GeneratorNextReq, MemberName);
            m.AddElement("index", id);
            if (v != null)
            {
                v.ElementName = "parameter";
                m.elements.Add(v);
            }
            var ret = await stub.ProcessRequest(m, cancel).ConfigureAwait(false);
            MessageElement mret;
            ret.TryFindElement("return", out mret);
            return mret;
        }
    }

    public class Generator1Client<ReturnType, ParamType> : GeneratorClientBase, Generator1<ReturnType, ParamType>
    {
        public Generator1Client(string membername, ServiceStub stub, int id) : base(membername, stub, id)
        {
        }

        public async Task<ReturnType> Next(ParamType param, CancellationToken cancel = default(CancellationToken))
        {
            var m = new MessageElement("param", stub.RRContext.PackAnyType<ParamType>(ref param));
            var m_ret = await NextBase(m, cancel).ConfigureAwait(false);
            var data = stub.RRContext.UnpackAnyType<ReturnType>(m_ret);
            return (ReturnType)data;
        }
    }

    public class Generator2Client<ReturnType> : GeneratorClientBase, Generator2<ReturnType>
    {
        public Generator2Client(string membername, ServiceStub stub, int id) : base(membername, stub, id)
        {
        }

        public async Task<ReturnType> Next(CancellationToken cancel = default(CancellationToken))
        {
            var m_ret = await NextBase(null, cancel).ConfigureAwait(false);
            var data = stub.RRContext.UnpackAnyType<ReturnType>(m_ret);
            return (ReturnType)data;
        }

        public async Task<ReturnType[]> NextAll(CancellationToken cancel = default(CancellationToken))
        {
            var ret = new List<ReturnType>();
            try
            {
                ret.Add(await Next(cancel).ConfigureAwait(false));
            }
            catch (StopIterationException) { }
            return ret.ToArray();
        }
    }

    public class Generator3Client<ParamType> : GeneratorClientBase, Generator3<ParamType>
    {
        public Generator3Client(string membername, ServiceStub stub, int id) : base(membername, stub, id)
        {
        }

        public async Task Next(ParamType param, CancellationToken cancel = default(CancellationToken))
        {
            var m = new MessageElement("param", stub.RRContext.PackAnyType<ParamType>(ref param));
            var m_ret = await NextBase(m, cancel).ConfigureAwait(false);
            stub.RRContext.UnpackVarType(m_ret);
        }
    }

    public abstract class GeneratorServerBase
    {
        protected string name;
        protected int index;
        protected ServiceSkel skel;
        protected ServerEndpoint ep;

        protected internal DateTime last_access_time;

        protected GeneratorServerBase(string name, int index, ServiceSkel skel, ServerEndpoint ep)
        {
            this.name = name;
            this.index = index;
            this.skel = skel;
            this.ep = ep;
        }

        public uint Endpoint { get => ep.LocalEndpoint; }

        public abstract Task<MessageEntry> CallNext(MessageEntry m);

    }

    public class Generator1Server<ReturnType, ParamType> : GeneratorServerBase
    {
        protected Generator1<ReturnType, ParamType> generator;

        public Generator1Server(Generator1<ReturnType, ParamType> generator, string name, int id, ServiceSkel skel, ServerEndpoint ep) : base(name, id, skel, ep)
        {
            this.generator = generator;
        }

        public override async Task<MessageEntry> CallNext(MessageEntry m)
        {
            var m_ret = new MessageEntry(MessageEntryType.GeneratorNextRes, m.MemberName);
            m_ret.RequestID = m.RequestID;
            m_ret.ServicePath = m.ServicePath;
            if (m.Error != MessageErrorType.None)
            {
                if (m.Error == MessageErrorType.StopIteration)
                {
                    await generator.Close().ConfigureAwait(false);
                }
                else
                {
                    await generator.Abort().ConfigureAwait(false);
                }
                m_ret.AddElement("return", 0);
            }
            else
            {
                var p = (ParamType)skel.RRContext.UnpackAnyType<ParamType>(m.FindElement("parameter"));
                var r = await generator.Next(p).ConfigureAwait(false);
                m_ret.AddElement("return", skel.RRContext.PackAnyType<ReturnType>(ref r));
            }
            return m_ret;
        }
    }

    public class Generator2Server<ReturnType> : GeneratorServerBase
    {
        protected Generator2<ReturnType> generator;

        public Generator2Server(Generator2<ReturnType> generator, string name, int id, ServiceSkel skel, ServerEndpoint ep) : base(name, id, skel, ep)
        {
            this.generator = generator;
        }

        public override async Task<MessageEntry> CallNext(MessageEntry m)
        {
            var m_ret = new MessageEntry(MessageEntryType.GeneratorNextRes, m.MemberName);
            m_ret.RequestID = m.RequestID;
            m_ret.ServicePath = m.ServicePath;
            if (m.Error != MessageErrorType.None)
            {
                if (m.Error == MessageErrorType.StopIteration)
                {
                    await generator.Close().ConfigureAwait(false);
                }
                else
                {
                    await generator.Abort().ConfigureAwait(false);
                }
                m_ret.AddElement("return", 0);
            }
            else
            {
                var r = await generator.Next().ConfigureAwait(false);
                m_ret.AddElement("return", skel.RRContext.PackAnyType<ReturnType>(ref r));
            }
            return m_ret;
        }
    }

    public class Generator3Server<ParamType> : GeneratorServerBase
    {
        protected Generator3<ParamType> generator;

        public Generator3Server(Generator3<ParamType> generator, string name, int id, ServiceSkel skel, ServerEndpoint ep) : base(name, id, skel, ep)
        {
            this.generator = generator;
        }

        public override async Task<MessageEntry> CallNext(MessageEntry m)
        {
            var m_ret = new MessageEntry(MessageEntryType.GeneratorNextRes, m.MemberName);
            m_ret.RequestID = m.RequestID;
            m_ret.ServicePath = m.ServicePath;
            if (m.Error != MessageErrorType.None)
            {
                if (m.Error == MessageErrorType.StopIteration)
                {
                    await generator.Close().ConfigureAwait(false);
                }
                else
                {
                    await generator.Abort().ConfigureAwait(false);
                }
                m_ret.AddElement("return", 0);
            }
            else
            {
                var p = (ParamType)skel.RRContext.UnpackAnyType<ParamType>(m.FindElement("parameter"));
                await generator.Next(p).ConfigureAwait(false);
                m_ret.AddElement("return", 0);
            }
            return m_ret;
        }
    }
#pragma warning restore 1591
    /**
    <summary>
    Adapter class to create a generator from an enumerator
    </summary>
    <remarks>
    Next calls will be mapped to the supplied enumerator
    </remarks>
    <typeparam name="T">The enumerator value type</typeparam>
    */

    [PublicApi]
    public class EnumeratorGenerator<T> : Generator2<T>
    {
        bool aborted = false;
        bool closed = false;
        IEnumerator<T> enumerator;
        /**
        <summary>
        Construct a generator from an IEnumerable
        </summary>
        <remarks>None</remarks>
        <param name="enumerable" />
        <returns />
        */
        [PublicApi]
        public EnumeratorGenerator(IEnumerable<T> enumerable)
            : this(enumerable.GetEnumerator())
        { }
        /**
        <summary>
        Construct a generator from an IEnumerator
        </summary>
        <remarks>None</remarks>
        <param name="enumerator" />
        */
        [PublicApi]
        public EnumeratorGenerator(IEnumerator<T> enumerator)
        {
            this.enumerator = enumerator;
        }
#pragma warning disable 1591
        public Task Abort(CancellationToken cancel = default(CancellationToken))
        {
            lock (this)
            {
                aborted = true;
            }
            return Task.FromResult(0);
        }

        public Task Close(CancellationToken cancel = default(CancellationToken))
        {
            lock (this)
            {
                closed = true;
            }
            return Task.FromResult(0);
        }

        public Task<T> Next(CancellationToken cancel = default(CancellationToken))
        {
            lock (this)
            {
                if (aborted) throw new OperationAbortedException("Generator aborted");
                if (closed) throw new StopIterationException("");
                if (!enumerator.MoveNext()) throw new StopIterationException("");
                return Task.FromResult(enumerator.Current);
            }
        }

        public async Task<T[]> NextAll(CancellationToken cancel = default(CancellationToken))
        {
            List<T> o = new List<T>();
            try
            {
                while (true)
                {
                    o.Add(await Next().ConfigureAwait(false));
                }
            }
            catch (StopIterationException) { }
            return o.ToArray();
        }
    }
#pragma warning restore 1591
}
