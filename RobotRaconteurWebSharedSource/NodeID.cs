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
using System.Threading.Tasks;

namespace RobotRaconteurWeb
{
    public class NodeID
    {
        private byte[] id;

        public NodeID(byte[] id)
        {
            if (id.Length != 16) throw new InvalidOperationException("Node ID must be 128 bits long");
            this.id = id;
        }

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

        public override string ToString()
        {
            return "{" + _ToStringD() + "}";
        }

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

        public static explicit operator byte[](NodeID i)
        {
            return i.ToByteArray();
        }

        public static NodeID NewUniqueID()
        {
            var guid = System.Guid.NewGuid().ToString("B");
            return new NodeID(guid);
        }

        public static bool operator ==(NodeID id1, NodeID id2)
        {
            if ((object)id1 == null && (object)id2 == null) return true;
            if (((object)id1 == null && (object)id2 != null) || ((object)id1 != null && (object)id2 == null)) return false;

            return id1.id.SequenceEqual(id2.id);
        }

        public static bool operator !=(NodeID id1, NodeID id2)
        {
            if ((object)id1 == null && (object)id2 == null) return false;
            if (((object)id1 == null && (object)id2 != null) || ((object)id1 != null && (object)id2 == null)) return true;

            return !id1.id.SequenceEqual(id2.id);
        }

        public bool IsAnyNode
        {
            get
            {
                return id.All(x => x == 0);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is NodeID)) return false;
            return this == ((NodeID)obj);
        }

        public static NodeID Any { get { return new NodeID(new byte[16]); } }

        public override int GetHashCode()
        {
            int sum = 0;
            foreach (byte b in id)
                sum += b;
            return sum;
        }

    }
}
