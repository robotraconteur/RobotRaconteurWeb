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

namespace RobotRaconteurWeb.Extensions
{
    public static class Extensions
    {
        public static async Task AwaitWithTimeout(this Task task, int timeout)
        {
            if (timeout < 0)
            {
                await task.ConfigureAwait(false);
                return;
            }

            var c = new CancellationTokenSource();
            Task timeout_task = Task.Delay(timeout, c.Token);

            var r1 = await Task.WhenAny(task, timeout_task).ConfigureAwait(false);
            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {

                var noop = timeout_task.IgnoreResult();
                c.Cancel();

                await task.ConfigureAwait(false);
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
                return await task.ConfigureAwait(false);
            }

            var c = new CancellationTokenSource();
            Task timeout_task = Task.Delay(timeout, c.Token);

            var r1 = await Task.WhenAny(task, timeout_task).ConfigureAwait(false);
            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {
                var noop = timeout_task.IgnoreResult();
                c.Cancel();

                return await task.ConfigureAwait(false);
            }
            else
            {
                var noop = task.IgnoreResult();
                throw new TimeoutException("Operation timed out");
            }
        }

        public static void AttachCancellationToken<T>(this TaskCompletionSource<T> source, CancellationToken cancel, Exception e = null)
        {
            cancel.Register(delegate ()
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
            return t.ContinueWith(delegate (Task t2)
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
            return t.ContinueWith(delegate (Task<T> t2)
            {
                try
                {
                    var e = t2.Result;
                }
                catch (Exception) { }
            });
        }

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
            });
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
            });
            return tcs.Task;
        }

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
                                   CancellationToken cancel = default(CancellationToken))
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
                                   CancellationToken cancel = default(CancellationToken))
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

        public static Task ConfigureAwait(this Task t, bool v)
        {
            return t;
        }
#endif

    }

    public static class RRUriExtensions
    {
        public static string EscapeDataString(string s)
        {
#if ROBOTRACONTEUR_BRIDGE
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(new char[] { c });
                    foreach (byte b in bytes)
                    {
                        sb.AppendFormat("%{0:X2}", b);
                    }
                }
            }
            return sb.ToString();
#else
            return Uri.EscapeDataString(s);
#endif
        }

        public static string UnescapeDataString(string s)
        {
#if ROBOTRACONTEUR_BRIDGE
            return H5.Script.DecodeURI(s);
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
            TypeCode t = Type.GetTypeCode(a.GetType().GetElementType());
            switch (t)
            {
                case TypeCode.Double:
                    return 8 * a.Length;
                case TypeCode.Single:
                    return 4 * a.Length;
                case TypeCode.Byte:
                case TypeCode.SByte:
                    return a.Length;
                case TypeCode.UInt16:
                case TypeCode.Int16:
                    return 2 * a.Length;
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
            var b = new BinaryWriter(new MemoryStream(membuf,position,bl));
            TypeCode t = Type.GetTypeCode(a.GetType().GetElementType());
            switch (t)
            {
                case TypeCode.Double:
                    {
                        var a1 = (double[])a;
                        for (int i = 0; i < bl/8; i++)
                            b.Write(a1[i+v]);
                        break;
                    }
                case TypeCode.Single:
                    {
                        var a1 = (float[])a;
                        for (int i = 0; i < bl/4; i++)
                            b.Write(a1[i+v]);
                        break;
                    }
                case TypeCode.Byte:
                    {
                        var a1 = (byte[])a;
                        for (int i = 0; i < bl; i++)
                            b.Write(a1[i+v]);
                        break;
                    }
                case TypeCode.SByte:
                    {
                        var a1 = (sbyte[])a;
                        for (int i = 0; i < bl; i++)
                            b.Write(a1[i+v]);
                        break;
                    }
                case TypeCode.UInt16:
                    {
                        var a1 = (ushort[])a;
                        for (int i = 0; i < bl/2; i++)
                            b.Write(a1[i+v]);
                        break;
                    }
                case TypeCode.Int16:
                    {
                        var a1 = (short[])a;
                        for (int i = 0; i <bl/2; i++)
                            b.Write(a1[i+v]);
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        var a1 = (uint[])a;
                        for (int i = 0; i < bl/4; i++)
                            b.Write(a1[i+v]);
                        break;
                    }
                case TypeCode.Int32:
                    {
                        var a1 = (int[])a;
                        for (int i = 0; i < bl/4; i++)
                            b.Write(a1[i+v]);
                        break;
                    }
                case TypeCode.UInt64:
                    {
                        var a1 = (ulong[])a;
                        for (int i = 0; i < bl/8; i++)
                            b.Write(a1[i+v]);
                        break;
                    }
                case TypeCode.Int64:
                    {
                        var a1 = (long[])a;
                        for (int i = 0; i < bl/8; i++)
                            b.Write(a1[i+v]);
                        break;
                    }
                case TypeCode.Boolean:
                    {
                        var a1 = (bool[])a;
                        for (int i = 0; i < bl; i++)
                            b.Write(a1[i+v]);
                        break;
                    }
                default:
                    throw new ArgumentException("Invalid array type");
            }
        }

        internal static void BlockCopy(byte[] membuf, int position, Array a, int v, int bl)
        {
            var b = new BinaryReader(new MemoryStream(membuf,position,bl));
            TypeCode t = Type.GetTypeCode(a.GetType().GetElementType());
            switch (t)
            {
                case TypeCode.Double:
                    {
                        var a1 = (double[])a;
                        for (int i = 0; i < bl/8; i++)
                            a1[i+v] = b.ReadDouble();
                        break;
                    }
                case TypeCode.Single:
                    {
                        var a1 = (float[])a;
                        for (int i = 0; i < bl/4; i++)
                            a1[i+v] = b.ReadSingle();
                        break;
                    }
                case TypeCode.Byte:
                    {
                        var a1 = (byte[])a;
                        for (int i = 0; i < bl; i++)
                            a1[i+v] = b.ReadByte();
                        break;
                    }
                case TypeCode.SByte:
                    {
                        var a1 = (sbyte[])a;
                        for (int i = 0; i < bl; i++)
                            a1[i+v] = b.ReadSByte();
                        break;
                    }
                case TypeCode.UInt16:
                    {
                        var a1 = (ushort[])a;
                        for (int i = 0; i < bl/2; i++)
                            a1[i+v] = b.ReadUInt16();
                        break;
                    }
                case TypeCode.Int16:
                    {
                        var a1 = (short[])a;
                        for (int i = 0; i < bl/2; i++)
                            a1[i+v] = b.ReadInt16();
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        var a1 = (uint[])a;
                        for (int i = 0; i < bl/4; i++)
                            a1[i+v] = b.ReadUInt32();
                        break;
                    }
                case TypeCode.Int32:
                    {
                        var a1 = (int[])a;
                        for (int i = 0; i < bl/4; i++)
                            a1[i+v] = b.ReadInt32();
                        break;
                    }
                case TypeCode.UInt64:
                    {
                        var a1 = (ulong[])a;
                        for (int i = 0; i < bl/8; i++)
                            a1[i+v] = b.ReadUInt64();
                        break;
                    }
                case TypeCode.Int64:
                    {
                        var a1 = (long[])a;
                        for (int i = 0; i < bl/8; i++)
                            a1[i+v] = b.ReadInt64();
                        break;
                    }
                case TypeCode.Boolean:
                    {
                        var a1 = (bool[])a;
                        for (int i = 0; i < bl; i++)
                            a1[i+v] = b.ReadBoolean();
                        break;
                    }
                default:
                    throw new ArgumentException("Invalid array type");
            }
        }
    }

    class WeakReference<T>
    {
        T target;

        public WeakReference(T target)
        {
            this.target = target;
        }

        public T Target
        {
            get { return target; }
        }

        public bool IsAlive
        {
            get { return target != null; }
        }

        public void SetTarget(T target)
        {
            this.target = target;
        }

        public void Free()
        {
            target = default(T);
        }

        public static implicit operator WeakReference<T>(T target)
        {
            return new WeakReference<T>(target);
        }

        public static implicit operator T(WeakReference<T> reference)
        {
            return reference.target;
        }

        public override string ToString()
        {
            return target.ToString();
        }

        public bool TryGetTarget(out T target)
        {
            target = this.target;
            return target != null;
        }
    }

#endif
}
