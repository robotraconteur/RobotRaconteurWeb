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
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb.Extensions;

namespace RobotRaconteurWeb
{
    public interface Generator1<ReturnType, ParamType>
    {
        Task<ReturnType> Next(ParamType param, CancellationToken cancel=default(CancellationToken));        
        Task Abort(CancellationToken cancel = default(CancellationToken));        
        Task Close(CancellationToken cancel = default(CancellationToken));        
    }

    public interface Generator2<ReturnType>
    {
        Task<ReturnType> Next(CancellationToken cancel = default(CancellationToken));
        Task Abort(CancellationToken cancel = default(CancellationToken));
        Task Close(CancellationToken cancel = default(CancellationToken));
        Task<ReturnType[]> NextAll(CancellationToken cancel = default(CancellationToken));
    }

    public interface Generator3<ParamType>
    {
        Task Next(ParamType param, CancellationToken cancel = default(CancellationToken));
        Task Abort(CancellationToken cancel = default(CancellationToken));
        Task Close(CancellationToken cancel = default(CancellationToken));
    }

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
            await stub.ProcessRequest(m, cancel);
        }
        public async Task Close(CancellationToken cancel = default(CancellationToken))
        {
            var m = new MessageEntry(MessageEntryType.GeneratorNextReq, MemberName);
            var err = new StopIterationException("Generator abort requested");
            RobotRaconteurExceptionUtil.ExceptionToMessageEntry(err, m);
            m.AddElement("index", id);
            await stub.ProcessRequest(m, cancel);
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
            var ret = await stub.ProcessRequest(m, cancel);
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
            var m = new MessageElement("param", stub.RRContext.PackVarType(param));
            var m_ret = await NextBase(m, cancel);
            var data = stub.RRContext.UnpackVarType(m_ret);
            if (data is Array)
            {
                if (typeof(ReturnType).IsArray)
                    return (ReturnType)data;
                else
                   return ((ReturnType[])data)[0];
            }
            else
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
            var m_ret = await NextBase(null, cancel);
            var data = stub.RRContext.UnpackVarType(m_ret);
            if (data is Array)
            {
                if (typeof(ReturnType).IsArray)
                    return (ReturnType)data;
                else
                    return ((ReturnType[])data)[0];
            }
            else
                return (ReturnType)data;
        }

        public async Task<ReturnType[]> NextAll(CancellationToken cancel = default(CancellationToken))
        {
            var ret = new List<ReturnType>();
            try
            {
                ret.Add(await Next(cancel));
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
            var m = new MessageElement("param", stub.RRContext.PackVarType(param));
            var m_ret = await NextBase(m, cancel);
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

        public Generator1Server(Generator1<ReturnType,ParamType> generator, string name, int id, ServiceSkel skel, ServerEndpoint ep) : base(name, id, skel, ep)
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
                    await generator.Close();                    
                }
                else
                {
                    await generator.Abort();
                }
                m_ret.AddElement("return", 0);
            }
            else
            {
                var p = (ParamType)skel.RRContext.UnpackVarType(m.FindElement("parameter"));
                var r = await generator.Next(p);
                m_ret.AddElement("return", skel.RRContext.PackVarType(r));
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
                    await generator.Close();
                }
                else
                {
                    await generator.Abort();
                }
                m_ret.AddElement("return", 0);
            }
            else
            {                
                var r = await generator.Next();
                m_ret.AddElement("return", skel.RRContext.PackVarType(r));
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
                    await generator.Close();
                }
                else
                {
                    await generator.Abort();
                }
                m_ret.AddElement("return", 0);
            }
            else
            {
                var p = (ParamType)skel.RRContext.UnpackVarType(m.FindElement("parameter"));
                await generator.Next(p);
                m_ret.AddElement("return", 0);
            }
            return m_ret;
        }
    }

    public class EnumeratorGenerator<T> : Generator2<T>
    {
        bool aborted = false;
        bool closed = false;
        IEnumerator<T> enumerator;

        public EnumeratorGenerator(IEnumerable<T> enumerable)
            : this(enumerable.GetEnumerator())
        { }

        public EnumeratorGenerator(IEnumerator<T> enumerator)
        {
            this.enumerator = enumerator;
        }

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
                    o.Add(await Next());
                }
            }
            catch (StopIterationException) { }
            return o.ToArray();
        }
    }

}