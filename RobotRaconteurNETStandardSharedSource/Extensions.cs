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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobotRaconteur.Extensions
{
    public static class Extensions
    {
        public static async Task AwaitWithTimeout(this Task task, int timeout)
        {
            if (timeout < 0)
            {
                await task;
                return;
            }

            var c = new CancellationTokenSource();
            Task timeout_task = RRTaskExtensions.Delay(timeout,c.Token);

            var r1 = await Task.WhenAny(task, timeout_task);
            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {

                var noop = timeout_task.IgnoreResult();
                c.Cancel();

                await task;
                return;
            }
            else
            {
                var noop = task.IgnoreResult();
                throw new TimeoutException("Operation timed out");
            }           
        }

        public static async Task<T> AwaitWithTimeout<T>(this Task<T> task, int timeout)
        {
            if (timeout < 0)
            {
                return await task;
            }

            var c = new CancellationTokenSource();
            Task timeout_task = RRTaskExtensions.Delay(timeout, c.Token);

            var r1 = await Task.WhenAny(task, timeout_task);
            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {
                var noop = timeout_task.IgnoreResult();
                c.Cancel();

                return await task;
            }
            else
            {
                var noop = task.IgnoreResult();
                throw new TimeoutException("Operation timed out");
            }
        }

        public static void AttachCancellationToken<T>(this TaskCompletionSource<T> source, CancellationToken cancel, Exception e=null)
        {
            cancel.Register(delegate()
            {
                if (e == null)
                {
                    source.TrySetCanceled();
                }
                else
                {
                    source.TrySetException(e);
                }
            });
        }

        public static Task IgnoreResult(this Task t)
        {
            return t.ContinueWith(delegate(Task t2)
            {
                try
                {
                    var e = t2.Exception;
                }
                catch (Exception) { }
            });
        }

        public static Task IgnoreResult<T>(this Task<T> t)
        {
            return t.ContinueWith(delegate(Task<T> t2)
            {
                try
                {
                    var e = t2.Result;
                }
                catch (Exception) { }
            });
        }

#if !ROBOTRACONTEUR_BRIDGE
        public static IAsyncResult AsApm<T>(this Task<T> task,
                                    AsyncCallback callback,
                                    object state)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            var tcs = new TaskCompletionSource<T>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(t.Result);

                if (callback != null)
                    callback(tcs.Task);
            }, TaskScheduler.Default);
            return tcs.Task;
        }

        public static IAsyncResult AsApm(this Task task,
                                    AsyncCallback callback,
                                    object state)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            var tcs = new TaskCompletionSource<int>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(0);

                if (callback != null)
                    callback(tcs.Task);
            }, TaskScheduler.Default);
            return tcs.Task;
        }
#else

        public class AsyncResultWrapper : IAsyncResult
        {
            
            public object state;
            public Exception exp;
            public int n;
            public AsyncResultWrapper(object s, Exception e, int n)
            {
                state = s;
                exp = e;
                this.n = n;
            }

            public object AsyncState
            {
                get
                {
                    if (exp!=null)
                    {
                        throw exp;
                    }
                    return state;
                }
            }

            public int Result
            {
                get
                {
                    if (exp != null)
                    {
                        throw exp;
                    }
                    return n;
                }
            }

            public bool CompletedSynchronously => false;

            public bool IsCompleted => true;
        }
        public static IAsyncResult AsApm(this Task<int> task,
                                    AsyncCallback callback,
                                    object state)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    callback(new AsyncResultWrapper(state, t.Exception.InnerException,0));
                }
                if (t.IsCanceled)
                {
                    callback(new AsyncResultWrapper(state, new OperationCanceledException(),0));
                }
                
                callback(new AsyncResultWrapper(state, null,t.Result));
            });
            return null;
        }
#endif
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

#if ROBOTRACONTEUR_BRIDGE
        public static Task<int> ReadAsync(this Stream stream,
                                   byte[] buffer, int offset,
                                   int count,
                                   CancellationToken cancel=default(CancellationToken))
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            var tcs = new TaskCompletionSource<int>();
            stream.BeginRead(buffer, offset, count, iar =>
            {
                try
                {
                    tcs.TrySetResult(stream.EndRead(iar));
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception exc)
                {
                    tcs.TrySetException(exc);
                }
            }, null);
            return tcs.Task;
        }

        public static Task<int> WriteAsync(this Stream stream,
                                   byte[] buffer, int offset,
                                   int count,
                                   CancellationToken cancel=default(CancellationToken))
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            var tcs = new TaskCompletionSource<int>();
            stream.BeginWrite(buffer, offset, count, iar =>
            {
                try
                {
                    tcs.TrySetResult(stream.EndRead(iar));
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception exc)
                {
                    tcs.TrySetException(exc);
                }
            }, null);            
            return tcs.Task;
        }

        public static Task<T> ConfigureAwait<T>(this Task<T> t, bool v)
        {
            return t;
        }
#endif
    }

    public static class RRTaskExtensions
    {
        public static Task Delay(int millisecondDelay, CancellationToken cancel)
        {
#if ROBOTRACONTEUR_BRIDGE
            return Task.Delay(millisecondDelay);
#else
            return Task.Delay(millisecondDelay, cancel);
#endif
        }
    }

    public static class RRArrayExtensions
    {
        public static long LongLength(Array a)
        {
#if ROBOTRACONTEUR_BRIDGE
            return a.Length;
#else
            return a.LongLength;
#endif
        }

        public static void Copy(Array sourceArray, long sourceIndex, Array destinationArray, long destinationIndex, long length)
        {
#if ROBOTRACONTEUR_BRIDGE
            Array.Copy(sourceArray, (int)sourceIndex, destinationArray, (int)destinationIndex, (int)length);
#else
            Array.Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
#endif
        }
    }

    public static class RRUriExtensions
    {
        public static string EscapeDataString(string s)
        {
#if ROBOTRACONTEUR_BRIDGE
            return Bridge.Script.EncodeURI(s);
#else
            return Uri.EscapeDataString(s);
#endif
        }

        public static string UnescapeDataString(string s)
        {
#if ROBOTRACONTEUR_BRIDGE
            return Bridge.Script.DecodeURI(s);
#else
            return Uri.UnescapeDataString(s);
#endif
        }
    }
#if ROBOTRACONTEUR_BRIDGE
    public static class Buffer
    {
        internal static void BlockCopy(byte[] recbuf, int v1, byte[] newbuf, int v2, int v3)
        {
            Array.Copy(recbuf, v1, newbuf, v2, v3);
        }

        internal static int ByteLength(Array a)
        {
            TypeCode t = RRTypeExtensions.GetTypeCode(a.GetType());
            switch (t)
            {
                case TypeCode.Double:
                    return 8 * a.Length;
                case TypeCode.Single:
                    return 4 *a.Length;
                case TypeCode.Byte:                    
                case TypeCode.SByte:
                    return a.Length;
                case TypeCode.UInt16:                    
                case TypeCode.Int16:
                    return 2* a.Length;
                case TypeCode.UInt32:                    
                case TypeCode.Int32:
                    return 4 * a.Length;
                case TypeCode.UInt64:                    
                case TypeCode.Int64:
                    return 8 * a.Length;
                case TypeCode.Boolean:
                    return a.Length;
                default:
                    throw new ArgumentException("Invalid array type");
            }
        }

        internal static void BlockCopy(Array a, int v, byte[] membuf, int position, int bl)
        {
            var b = new BinaryWriter(new MemoryStream(membuf));
            TypeCode t = RRTypeExtensions.GetTypeCode(a.GetType());
            switch (t)
            {
                case TypeCode.Double:
                    {
                        var a1 = (double[])a;
                        for (int i = 0; i < a1.Length; i++)
                            b.Write(a1[i]);
                        break;
                    }
                case TypeCode.Single:
                    {
                        var a1 = (float[])a;
                        for (int i = 0; i < a1.Length; i++)
                            b.Write(a1[i]);
                        break;
                    }
                case TypeCode.Byte:
                    {
                        var a1 = (byte[])a;
                        for (int i = 0; i < a1.Length; i++)
                            b.Write(a1[i]);
                        break;
                    }
                case TypeCode.SByte:
                    {
                        var a1 = (sbyte[])a;
                        for (int i = 0; i < a1.Length; i++)
                            b.Write(a1[i]);
                        break;
                    }
                case TypeCode.UInt16:
                    {
                        var a1 = (ushort[])a;
                        for (int i = 0; i < a1.Length; i++)
                            b.Write(a1[i]);
                        break;
                    }
                case TypeCode.Int16:
                    {
                        var a1 = (short[])a;
                        for (int i = 0; i < a1.Length; i++)
                            b.Write(a1[i]);
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        var a1 = (uint[])a;
                        for (int i = 0; i < a1.Length; i++)
                            b.Write(a1[i]);
                        break;
                    }
                case TypeCode.Int32:
                    {
                        var a1 = (int[])a;
                        for (int i = 0; i < a1.Length; i++)
                            b.Write(a1[i]);
                        break;
                    }
                case TypeCode.UInt64:
                    {
                        var a1 = (ulong[])a;
                        for (int i = 0; i < a1.Length; i++)
                            b.Write(a1[i]);
                        break;
                    }
                case TypeCode.Int64:
                    {
                        var a1 = (long[])a;
                        for (int i = 0; i < a1.Length; i++)
                            b.Write(a1[i]);
                        break;
                    }
                case TypeCode.Boolean:
                    {
                        var a1 = (bool[])a;
                        for (int i = 0; i < a1.Length; i++)
                            b.Write(a1[i]);
                        break;
                    }
                default:
                    throw new ArgumentException("Invalid array type");
            }
        }

        internal static void BlockCopy(byte[] membuf, int position, Array a, int v, int bl)
        {
            var b = new BinaryReader(new MemoryStream(membuf));
            TypeCode t = RRTypeExtensions.GetTypeCode(a.GetType());
            switch (t)
            {
                case TypeCode.Double:
                    {
                        var a1 = (double[])a;
                        for (int i = 0; i < a1.Length; i++)
                            a1[i] = b.ReadDouble();
                        break;
                    }
                case TypeCode.Single:
                    {
                        var a1 = (float[])a;
                        for (int i = 0; i < a1.Length; i++)
                            a1[i] = b.ReadSingle();
                        break;
                    }
                case TypeCode.Byte:
                    {
                        var a1 = (byte[])a;
                        for (int i = 0; i < a1.Length; i++)
                            a1[i] = b.ReadByte();
                        break;
                    }
                case TypeCode.SByte:
                    {
                        var a1 = (sbyte[])a;
                        for (int i = 0; i < a1.Length; i++)
                            a1[i] = b.ReadSByte();
                        break;
                    }
                case TypeCode.UInt16:
                    {
                        var a1 = (ushort[])a;
                        for (int i = 0; i < a1.Length; i++)
                            a1[i] = b.ReadUInt16();
                        break;
                    }
                case TypeCode.Int16:
                    {
                        var a1 = (short[])a;
                        for (int i = 0; i < a1.Length; i++)
                            a1[i] = b.ReadInt16();
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        var a1 = (uint[])a;
                        for (int i = 0; i < a1.Length; i++)
                            a1[i] = b.ReadUInt32();
                        break;
                    }
                case TypeCode.Int32:
                    {
                        var a1 = (int[])a;
                        for (int i = 0; i < a1.Length; i++)
                            a1[i] = b.ReadInt32();
                        break;
                    }
                case TypeCode.UInt64:
                    {
                        var a1 = (ulong[])a;
                        for (int i = 0; i < a1.Length; i++)
                            a1[i] = b.ReadUInt64();
                        break;
                    }
                case TypeCode.Int64:
                    {
                        var a1 = (long[])a;
                        for (int i = 0; i < a1.Length; i++)
                            a1[i] = b.ReadInt64();
                        break;
                    }
                case TypeCode.Boolean:
                    {
                        var a1 = (bool[])a;
                        for (int i = 0; i < a1.Length; i++)
                            a1[i] = b.ReadBoolean();
                        break;
                    }
                default:
                    throw new ArgumentException("Invalid array type");
            }
        }
    }

#endif

    public static class RRTypeExtensions
    {
        public static TypeCode GetTypeCode(Type t)
        {
#if ROBOTRACONTEUR_BRIDGE
            if (t == typeof(double))
            {
                return TypeCode.Double;
            }
            if (t == typeof(float))
            {
                return TypeCode.Single;
            }
            if (t == typeof(byte))
            {
                return TypeCode.Byte;
            }
            if (t == typeof(sbyte))
            {
                return TypeCode.SByte;
            }
            if (t == typeof(ushort))
            {
                return TypeCode.UInt16;
            }
            if (t == typeof(short))
            {
                return TypeCode.Int16;
            }
            if (t == typeof(uint))
            {
                return TypeCode.UInt32;
            }
            if (t == typeof(int))
            {
                return TypeCode.Int32;
            }
            if (t == typeof(ulong))
            {
                return TypeCode.UInt64;
            }
            if (t == typeof(long))
            {
                return TypeCode.Int64;
            }
            if (t == typeof(bool))
            {
                return TypeCode.Boolean;
            }
            return TypeCode.Object;
            
#else
            return Type.GetTypeCode(t);
#endif
        }
    }

    public static class RRConvertExtensions
    {
        public static object ChangeType(string v, Type t1)
        {


#if ROBOTRACONTEUR_BRIDGE
            TypeCode t = RRTypeExtensions.GetTypeCode(t1.GetType());
            switch (t)
            {
                case TypeCode.Double:
                    return Convert.ToDouble(v);
                case TypeCode.Single:
                    return Convert.ToSingle(v);
                case TypeCode.Byte:
                    return Convert.ToByte(v);
                case TypeCode.SByte:
                    return Convert.ToSByte(v);
                case TypeCode.UInt16:
                    return Convert.ToUInt16(v);
                case TypeCode.Int16:
                    return Convert.ToInt16(v);
                case TypeCode.UInt32:
                    return Convert.ToUInt32(v);
                case TypeCode.Int32:
                    return Convert.ToInt32(v);
                case TypeCode.UInt64:
                    return Convert.ToUInt64(v);
                case TypeCode.Int64:
                    return Convert.ToInt64(v);
                case TypeCode.Boolean:
                    return Convert.ToBoolean(v);
                default:
                    throw new ArgumentException("Invalid numeric type");
            }
#else
            return Convert.ChangeType(v,t1);
#endif
        }
    }
        


}
