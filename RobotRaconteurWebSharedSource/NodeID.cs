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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RobotRaconteurWeb
{
    /**
    <summary>
    NodeID UUID storage and generation
    </summary>
    <remarks>
    <para>
    Robot Raconteur uses NodeID and NodeName to uniquely identify a node.
    NodeID is a UUID (Universally Unique ID), while NodeName is a string. The
    NodeID is expected to be unique, while the NodeName is set by the user
    and may not be unique. The NodeID class represents the UUID NodeID.
    </para>
    <para>
    A UUID is a 128-bit randomly generated number that is statistically guaranteed
    to be unique to a very high probability. NodeID uses the Boost.UUID library
    to generate, manage, and store the UUID.
    </para>
    <para>
    The UUID can be loaded from a string, bytes, or generated randomly at runtime.
    It can be converted to a string.
    </para>
    <para>
    The LocalTransport and ServerNodeSetup classes will automatically assign
    a NodeID to a node when the local transport is started with a specified node name.
    The generated NodeID is stored on the local system, and is associated with the node name.
    It will be loaded when a node is started with the same NodeName.
    </para>
    <para> NodeID with all zeros is considered "any" node.
    </para>
    </remarks>
    */

        [PublicApi]
    public class NodeID
    {
        private byte[] id;
        /**
        <summary>
        Construct a new NodeID with the specified UUID bytes
        </summary>
        <remarks>None</remarks>
        <param name="id">The UUID bytes</param>
        */

        [PublicApi]
        public NodeID(byte[] id)
        {
            if (id.Length != 16) throw new InvalidOperationException("Node ID must be 128 bits long");
            this.id = id;
        }
        /**
        <summary>
        Construct a new NodeID parsing a string UUID
        </summary>
        <remarks>None</remarks>
        <param name="id">The UUID as a string</param>
        */

        [PublicApi]
        public NodeID(string id)
        {
            byte[] id1 = null;
            if (TryParse(id, out id1))
            {
                this.id = id1;
                return;
            }
                        
            if (id1 == null)
            {
                throw new InvalidOperationException("Invalid format for NodeID");
            }
            this.id = id1;
        }
#pragma warning disable 1591
        protected static bool TryParse(string stringid, out byte[] bytes)
        {
            if (stringid == "{0}")
            {
                bytes = new byte[16];
                return true;
            }

            bytes = null;
            Regex r = new Regex(@"\{?([a-fA-F0-9]{8})-([a-fA-F0-9]{4})-([a-fA-F0-9]{4})-([a-fA-F0-9]{4})-([a-fA-F0-9]{12})\}?");
            var res = r.Match(stringid);
            if (!res.Success) return false;
            string res1 = "";
            for (int i = 1; i < 6; i++) res1 += res.Groups[i].Value;
            bytes = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                bytes[i] = Convert.ToByte(res1.Substring(i * 2, 2), 16);
            }
            return true;
        }


        public static bool TryParse(string stringid, out NodeID nodeid)
        {
            byte[] bytes;
            nodeid = null;
            if (!TryParse(stringid, out bytes))
            {
                return false;
            }
            nodeid = new NodeID(bytes);
            return true;
        }
#pragma warning restore 1591

        /**
        <summary>
        Convert the NodeID UUID to bytes
        </summary>
        <returns>The UUID as bytes</returns>
        */

        [PublicApi]
        public byte[] ToByteArray()
        {
            byte[] bid = new byte[16];
            Array.Copy(id, bid, 16);
            return bid;
        }

        private string _ToStringD()
        {
            string[] hexvals = id.Select(x => String.Format("{0:x2}", x)).ToArray();
            string g1 = String.Join("", hexvals, 0, 4);
            string g2 = String.Join("", hexvals, 4, 2);
            string g3 = String.Join("", hexvals, 6, 2);
            string g4 = String.Join("", hexvals, 8, 2);
            string g5 = String.Join("", hexvals, 10, 6);

            return String.Join("-", new string[] { g1, g2, g3, g4, g5 });
        }

        /**
        <summary> Convert the NodeID UUID to string with "B" format<br /> Convert the UUID string to
        8-4-4-4-12 "B" format (with brackets)<br /> {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx} </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public override string ToString()
        {
            return "{" + _ToStringD() + "}";
        }
        /**
        <summary> Convert the NodeID UUID to string with "B" format<br /> Convert the UUID string to
        8-4-4-4-12 "B" format (with brackets)<br /> {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}
        or "D" format with no brackets </summary>
        <remarks>None</remarks>
        */

        [PublicApi]
        public virtual string ToString(string format)
        {
            switch (format)
            {
                case "B":
                    return "{" + _ToStringD() + "}";
                case "D":
                    return _ToStringD();
                case "N":
                    return _ToStringD().Replace("-", "");
                default:
                    throw new ArgumentException("Invalid NodeID format");
            }
        }
        /// <summary>
        /// Convert to a byte array containing the UUID bytes
        /// </summary>
        /// <param name="i">The NodeID UUID as bytes</param>
        [PublicApi] 

        public static explicit operator byte[](NodeID i)
        {
            return i.ToByteArray();
        }
        /**
        <summary>
        Generate a new random NodeID UUID
        </summary>
        <remarks>
        Returned UUID is statistically guaranteed to be unique
        </remarks>
        <returns>The newly generated UUID</returns>
        */

        [PublicApi]
        public static NodeID NewUniqueID()
        {
            var guid = System.Guid.NewGuid().ToString("B");
            return new NodeID(guid);
        }

        /**
         * <summary>Test if NodeID is equal</summary>
         * <remarks>None</remarks>
         */
        [PublicApi]
        public static bool operator ==(NodeID id1, NodeID id2)
        {
            if ((object)id1 == null && (object)id2 == null) return true;
            if (((object)id1 == null && (object)id2 != null) || ((object)id1 != null && (object)id2 == null)) return false;

            return id1.id.SequenceEqual(id2.id);
        }

        /**
         * <summary>Test if NodeID is not equal</summary>
         * <remarks>None</remarks>
         */
        [PublicApi]
        public static bool operator !=(NodeID id1, NodeID id2)
        {
            if ((object)id1 == null && (object)id2 == null) return false;
            if (((object)id1 == null && (object)id2 != null) || ((object)id1 != null && (object)id2 == null)) return true;

            return !id1.id.SequenceEqual(id2.id);
        }
        /**
        <summary>
        Is the NodeID UUID all zeros
        </summary>
        <remarks>
        The all zero UUID respresents "any" node, or an unset NodeID
        </remarks>
        <returns>true The NodeID UUID is all zeros, representing any node, false The NodeID UUID is
        not all zeros</returns>
        */

        [PublicApi]
        public bool IsAnyNode
        {
            get
            {
                return id.All(x => x == 0);
            }
        }

        /**
         * <summary>Test if NodeID is equal</summary>
         * <remarks>None</remarks>
         */
        [PublicApi]
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is NodeID)) return false;
            return this == ((NodeID)obj);
        }
        /**
        <summary>
        Get the "any" NodeId
        </summary>
        <returns>The "any" NodeID</returns>
        */

        [PublicApi]
        public static NodeID Any { get { return new NodeID(new byte[16]); } }

        /// <summary>
        /// Get a hashcode for the NodeID
        /// </summary>
        /// <returns>The hash code</returns>
        [PublicApi] 
        public override int GetHashCode()
        {
            int sum = 0;
            foreach (byte b in id)
                sum += b;
            return sum;
        }

    }
}
