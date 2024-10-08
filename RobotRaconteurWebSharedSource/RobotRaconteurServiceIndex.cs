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

//This file is automatically generated. DO NOT EDIT!
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RobotRaconteurWeb;
namespace RobotRaconteurServiceIndex
{

#pragma warning disable 1591

    public class RobotRaconteurServiceIndexFactory : ServiceFactory
    {
        public override string DefString()
        {
            const string d = @"service RobotRaconteurServiceIndex
struct NodeInfo
field string NodeName
field uint8[16] NodeID
field string{int32} ServiceIndexConnectionURL
end struct
struct ServiceInfo
field string Name
field string RootObjectType
field string{int32} RootObjectImplements
field string{int32} ConnectionURL
field varvalue{string} Attributes
end struct
object ServiceIndex
function ServiceInfo{int32} GetLocalNodeServices()
function NodeInfo{int32} GetRoutedNodes()
function NodeInfo{int32} GetDetectedNodes()
event LocalNodeServicesChanged()
end object";
            return d;
        }
        //public RobotRaconteurServiceIndexFactory(RobotRaconteurNode node=null) : base(node) {}
        public override string GetServiceName() { return "RobotRaconteurServiceIndex"; }
        public NodeInfo_stub NodeInfo_stubentry;
        public ServiceInfo_stub ServiceInfo_stubentry;

        public RobotRaconteurServiceIndexFactory() : this(null) { }
        public RobotRaconteurServiceIndexFactory(RobotRaconteurNode node = null) : base(node)
        {
            NodeInfo_stubentry = new NodeInfo_stub(this);
            ServiceInfo_stubentry = new ServiceInfo_stub(this);
        }
        public override IStructureStub FindStructureStub(string objecttype)
        {
            string objshort = RemovePath(objecttype);
            switch (objshort)
            {
                case "NodeInfo":
                    return NodeInfo_stubentry;
                case "ServiceInfo":
                    return ServiceInfo_stubentry;
            }
            throw new DataTypeException("Cannot find appropriate structure stub");
        }
        public override MessageElementNestedElementList PackStructure(Object s)
        {
            if (s == null) return null;
            string objtype = ServiceDefinitionUtil.FindStructRRType(s.GetType());
            if (ServiceDefinitionUtil.SplitQualifiedName(objtype).Item1 == "RobotRaconteurServiceIndex")
            {
                string objshort = RemovePath(objtype);
                switch (objshort)
                {
                    case "NodeInfo":
                        return NodeInfo_stubentry.PackStructure(s);
                    case "ServiceInfo":
                        return ServiceInfo_stubentry.PackStructure(s);
                }
            }
            else
            {
                return base.PackStructure(s);
            }
            throw new Exception();
        }
        public override T UnpackStructure<T>(MessageElementNestedElementList l)
        {
            if (l == null) return default(T);
            if (ServiceDefinitionUtil.SplitQualifiedName(l.TypeName).Item1 == "RobotRaconteurServiceIndex")
            {
                string objshort = RemovePath(l.TypeName);
                switch (objshort)
                {
                    case "NodeInfo":
                        return NodeInfo_stubentry.UnpackStructure<T>(l);
                    case "ServiceInfo":
                        return ServiceInfo_stubentry.UnpackStructure<T>(l);
                }
            }
            else
            {
                return base.UnpackStructure<T>(l);
            }
            throw new DataTypeException("Could not unpack structure");
        }
        public override ServiceStub CreateStub(string objecttype, string path, ClientContext context)
        {
            if (ServiceDefinitionUtil.SplitQualifiedName(objecttype).Item1 == "RobotRaconteurServiceIndex")
            {
                string objshort = RemovePath(objecttype);
                switch (objshort)
                {
                    case "ServiceIndex":
                        return new ServiceIndex_stub(path, context);
                }
            }
            else
            {
                return base.CreateStub(objecttype, path, context);
            }
            throw new ServiceException("Could not create stub");
        }
        public override ServiceSkel CreateSkel(string path, object obj, ServerContext context)
        {
            string objtype = ServiceDefinitionUtil.FindObjectRRType(obj);
            if (ServiceDefinitionUtil.SplitQualifiedName(objtype).Item1 == "RobotRaconteurServiceIndex")
            {
                string sobjtype = RemovePath(objtype);
                switch (sobjtype)
                {
                    case "ServiceIndex":
                        return new ServiceIndex_skel(path, (ServiceIndex)obj, context);
                }
            }
            else
            {
                return base.CreateSkel(path, obj, context);
            }
            throw new ServiceException("Could not create skel");
        }


        public override IPodStub FindPodStub(string objecttype)
        {
            throw new NotImplementedException();
        }

        public override INamedArrayStub FindNamedArrayStub(string objecttype)
        {
            throw new NotImplementedException();
        }
    }

    [RobotRaconteurServiceStruct("RobotRaconteurServiceIndex.NodeInfo")]
    public class NodeInfo
    {
        public string NodeName;
        public byte[] NodeID;
        public Dictionary<int, string> ServiceIndexConnectionURL;
    }

    [RobotRaconteurServiceStruct("RobotRaconteurServiceIndex.ServiceInfo")]
    public class ServiceInfo
    {
        public string Name;
        public string RootObjectType;
        public Dictionary<int, string> RootObjectImplements;
        public Dictionary<int, string> ConnectionURL;
        public Dictionary<string, object> Attributes;
    }

    [RobotRaconteurServiceObjectInterface("RobotRaconteurServiceIndex.ServiceIndex")]
    public interface ServiceIndex
    {

        Task<Dictionary<int, ServiceInfo>> GetLocalNodeServices(CancellationToken rr_cancel = default(CancellationToken));
        Task<Dictionary<int, NodeInfo>> GetRoutedNodes(CancellationToken rr_cancel = default(CancellationToken));
        Task<Dictionary<int, NodeInfo>> GetDetectedNodes(CancellationToken rr_cancel = default(CancellationToken));

        event Action LocalNodeServicesChanged;



    }
}
