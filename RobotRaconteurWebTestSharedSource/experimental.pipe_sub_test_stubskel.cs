//This file is automatically generated. DO NOT EDIT!
using System;
using RobotRaconteurWeb;
using RobotRaconteurWeb.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable 0108

namespace experimental.pipe_sub_test
{
public class experimental__pipe_sub_testFactory : ServiceFactory
{
    public override string DefString()
{
    const string s="service experimental.pipe_sub_test\n\nstdver 0.10\n\nobject testobj\npipe double testpipe1 [readonly]\npipe double testpipe2\nobjref testobj2 subobj\nend\n\nobject testobj2\npipe double testpipe3 [readonly]\nend\n";
    return s;
    }
    public override string GetServiceName() {return "experimental.pipe_sub_test";}
    public experimental__pipe_sub_testFactory() : this(null,null) {}
    public experimental__pipe_sub_testFactory(RobotRaconteurNode node = null, ClientContext context = null) : base(node,context)
    {
    }
    public override IStructureStub FindStructureStub(string objecttype)
    {
    throw new DataTypeException("Cannot find appropriate structure stub");
    }
    public override IPodStub FindPodStub(string objecttype)
    {
    throw new DataTypeException("Cannot find appropriate pod stub");
    }
    public override INamedArrayStub FindNamedArrayStub(string objecttype)
    {
    throw new DataTypeException("Cannot find appropriate pod stub");
    }
    public override ServiceStub CreateStub(string objecttype, string path, ClientContext context) {
    string objshort;
    if (CompareNamespace(objecttype, out objshort)) {
    switch (objshort) {
    case "testobj":
    return new testobj_stub(path, context);
    case "testobj2":
    return new testobj2_stub(path, context);
    default:
    break;
    }
    } else {
    return base.CreateStub(objecttype,path,context);
    }
    throw new ServiceException("Could not create stub");
    }
    public override ServiceSkel CreateSkel(string path,object obj,ServerContext context) {
    string objtype=ServiceDefinitionUtil.FindObjectRRType(obj);
    string objshort;
    if (CompareNamespace(objtype, out objshort)) {
    switch(objshort) {
    case "testobj":
    return new testobj_skel(path,(testobj)obj,context);
    case "testobj2":
    return new testobj2_skel(path,(testobj2)obj,context);
    default:
    break;
    }
    } else {
    return base.CreateSkel(path,obj,context);
    }
    throw new ServiceException("Could not create skel");
    }
    public override RobotRaconteurException DownCastException(RobotRaconteurException rr_exp)
    {
    if (rr_exp==null) return rr_exp;
    string rr_type=rr_exp.Error;
    if (!rr_type.Contains(".")) return rr_exp;
    string rr_stype;
    if (CompareNamespace(rr_type, out rr_stype)) {
    } else {
    return base.DownCastException(rr_exp); 
    }
    return rr_exp;
    }
}

public class testobj_stub : ServiceStub , testobj {
    private Pipe<double> rr_testpipe1;
    private Pipe<double> rr_testpipe2;
    public testobj_stub(string path, ClientContext c) : base(path, c) {
    rr_testpipe1=new PipeClient<double>("testpipe1", this);
    rr_testpipe2=new PipeClient<double>("testpipe2", this);
    }
    protected override void DispatchEvent(MessageEntry rr_m) {
    switch (rr_m.MemberName) {
    default:
    break;
    }
    }
    public async Task<testobj2> get_subobj(CancellationToken cancel=default(CancellationToken)) {
    return (testobj2)await FindObjRefTyped("subobj","experimental.pipe_sub_test.testobj2",cancel).ConfigureAwait(false);
    }
    public Pipe<double> testpipe1 {
    get { return rr_testpipe1;  }
    set { throw new InvalidOperationException();}
    }
    public Pipe<double> testpipe2 {
    get { return rr_testpipe2;  }
    set { throw new InvalidOperationException();}
    }
    protected override void DispatchPipeMessage(MessageEntry m)
    {
    switch (m.MemberName) {
    case "testpipe1":
    this.rr_testpipe1.PipePacketReceived(m);
    break;
    case "testpipe2":
    this.rr_testpipe2.PipePacketReceived(m);
    break;
    default:
    throw new Exception();
    }
    }
    protected override async Task<MessageEntry> CallbackCall(MessageEntry rr_m) {
    string rr_ename=rr_m.MemberName;
    MessageEntry rr_mr=new MessageEntry(MessageEntryType.CallbackCallRet, rr_ename);
    rr_mr.ServicePath=rr_m.ServicePath;
    rr_mr.RequestID=rr_m.RequestID;
    switch (rr_ename) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    return rr_mr;
    }
    protected override void DispatchWireMessage(MessageEntry m)
    {
    switch (m.MemberName) {
    default:
    throw new Exception();
    }
    }
}
public class testobj2_stub : ServiceStub , testobj2 {
    private Pipe<double> rr_testpipe3;
    public testobj2_stub(string path, ClientContext c) : base(path, c) {
    rr_testpipe3=new PipeClient<double>("testpipe3", this);
    }
    protected override void DispatchEvent(MessageEntry rr_m) {
    switch (rr_m.MemberName) {
    default:
    break;
    }
    }
    public Pipe<double> testpipe3 {
    get { return rr_testpipe3;  }
    set { throw new InvalidOperationException();}
    }
    protected override void DispatchPipeMessage(MessageEntry m)
    {
    switch (m.MemberName) {
    case "testpipe3":
    this.rr_testpipe3.PipePacketReceived(m);
    break;
    default:
    throw new Exception();
    }
    }
    protected override async Task<MessageEntry> CallbackCall(MessageEntry rr_m) {
    string rr_ename=rr_m.MemberName;
    MessageEntry rr_mr=new MessageEntry(MessageEntryType.CallbackCallRet, rr_ename);
    rr_mr.ServicePath=rr_m.ServicePath;
    rr_mr.RequestID=rr_m.RequestID;
    switch (rr_ename) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    return rr_mr;
    }
    protected override void DispatchWireMessage(MessageEntry m)
    {
    switch (m.MemberName) {
    default:
    throw new Exception();
    }
    }
}
public class testobj_skel : ServiceSkel {
    protected testobj obj;
    public testobj_skel(string p,testobj o,ServerContext c) : base(p,o,c) { obj=(testobj)o; }
    public override void ReleaseCastObject() { 
    }
    public override async Task<MessageEntry> CallGetProperty(MessageEntry m) {
    string ename=m.MemberName;
    MessageEntry mr=new MessageEntry(MessageEntryType.PropertyGetRes, ename);
    switch (ename) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    return mr;
    }
    public override async Task<MessageEntry> CallSetProperty(MessageEntry m) {
    string ename=m.MemberName;
    MessageElement me=m.FindElement("value");
    MessageEntry mr=new MessageEntry(MessageEntryType.PropertySetRes, ename);
    switch (ename) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    return mr;
    }
    public override async Task<MessageEntry> CallFunction(MessageEntry rr_m) {
    string rr_ename=rr_m.MemberName;
    MessageEntry rr_mr=new MessageEntry(MessageEntryType.FunctionCallRes, rr_ename);
    switch (rr_ename) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    return rr_mr;
    }
    public override async Task<object> GetSubObj(string name, string ind) {
    switch (name) {
    case "subobj": {
    return await obj.get_subobj().ConfigureAwait(false);
    }
    default:
    break;
    }
    throw new MemberNotFoundException("");
    }
    public override void RegisterEvents(object rrobj1) {
    obj=(testobj)rrobj1;
    }
    public override void UnregisterEvents(object rrobj1) {
    obj=(testobj)rrobj1;
    }
    public override object GetCallbackFunction(uint rr_endpoint, string rr_membername) {
    switch (rr_membername) {
    default:
    break;
    }
    throw new MemberNotFoundException("Member not found");
    }
    private PipeServer<double> rr_testpipe1;
    private PipeServer<double> rr_testpipe2;
    private bool rr_InitPipeServersRun=false;
    public override void InitPipeServers(object o) {
    if (this.rr_InitPipeServersRun) return;
    this.rr_InitPipeServersRun=true;
    testobj castobj=(testobj)o;
    this.rr_testpipe1=new PipeServer<double>("testpipe1",this);
    this.rr_testpipe2=new PipeServer<double>("testpipe2",this);
    castobj.testpipe1=this.rr_testpipe1;
    castobj.testpipe2=this.rr_testpipe2;
    }
    public override void InitCallbackServers(object rrobj1) {
    obj=(testobj)rrobj1;
    }
    public override async Task<MessageEntry> CallPipeFunction(MessageEntry m,Endpoint e) {
    string ename=m.MemberName;
    switch (ename) {
    case "testpipe1":
    return await this.rr_testpipe1.PipeCommand(m,e).ConfigureAwait(false);
    case "testpipe2":
    return await this.rr_testpipe2.PipeCommand(m,e).ConfigureAwait(false);
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override async Task<MessageEntry> CallWireFunction(MessageEntry m,Endpoint e) {
    string ename=m.MemberName;
    switch (ename) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override void DispatchPipeMessage(MessageEntry m, Endpoint e)
    {
    switch (m.MemberName) {
    case "testpipe1":
    this.rr_testpipe1.PipePacketReceived(m,e);
    break;
    case "testpipe2":
    this.rr_testpipe2.PipePacketReceived(m,e);
    break;
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override void DispatchWireMessage(MessageEntry m, Endpoint e)
    {
    switch (m.MemberName) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override async Task<MessageEntry> CallMemoryFunction(MessageEntry m,Endpoint e) {
    string ename=m.MemberName;
    switch (ename) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override bool IsRequestNoLock(MessageEntry m) {
    return false;
    }
}
public class testobj2_skel : ServiceSkel {
    protected testobj2 obj;
    public testobj2_skel(string p,testobj2 o,ServerContext c) : base(p,o,c) { obj=(testobj2)o; }
    public override void ReleaseCastObject() { 
    }
    public override async Task<MessageEntry> CallGetProperty(MessageEntry m) {
    string ename=m.MemberName;
    MessageEntry mr=new MessageEntry(MessageEntryType.PropertyGetRes, ename);
    switch (ename) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    return mr;
    }
    public override async Task<MessageEntry> CallSetProperty(MessageEntry m) {
    string ename=m.MemberName;
    MessageElement me=m.FindElement("value");
    MessageEntry mr=new MessageEntry(MessageEntryType.PropertySetRes, ename);
    switch (ename) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    return mr;
    }
    public override async Task<MessageEntry> CallFunction(MessageEntry rr_m) {
    string rr_ename=rr_m.MemberName;
    MessageEntry rr_mr=new MessageEntry(MessageEntryType.FunctionCallRes, rr_ename);
    switch (rr_ename) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    return rr_mr;
    }
    public override async Task<object> GetSubObj(string name, string ind) {
    switch (name) {
    default:
    break;
    }
    throw new MemberNotFoundException("");
    }
    public override void RegisterEvents(object rrobj1) {
    obj=(testobj2)rrobj1;
    }
    public override void UnregisterEvents(object rrobj1) {
    obj=(testobj2)rrobj1;
    }
    public override object GetCallbackFunction(uint rr_endpoint, string rr_membername) {
    switch (rr_membername) {
    default:
    break;
    }
    throw new MemberNotFoundException("Member not found");
    }
    private PipeServer<double> rr_testpipe3;
    private bool rr_InitPipeServersRun=false;
    public override void InitPipeServers(object o) {
    if (this.rr_InitPipeServersRun) return;
    this.rr_InitPipeServersRun=true;
    testobj2 castobj=(testobj2)o;
    this.rr_testpipe3=new PipeServer<double>("testpipe3",this);
    castobj.testpipe3=this.rr_testpipe3;
    }
    public override void InitCallbackServers(object rrobj1) {
    obj=(testobj2)rrobj1;
    }
    public override async Task<MessageEntry> CallPipeFunction(MessageEntry m,Endpoint e) {
    string ename=m.MemberName;
    switch (ename) {
    case "testpipe3":
    return await this.rr_testpipe3.PipeCommand(m,e).ConfigureAwait(false);
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override async Task<MessageEntry> CallWireFunction(MessageEntry m,Endpoint e) {
    string ename=m.MemberName;
    switch (ename) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override void DispatchPipeMessage(MessageEntry m, Endpoint e)
    {
    switch (m.MemberName) {
    case "testpipe3":
    this.rr_testpipe3.PipePacketReceived(m,e);
    break;
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override void DispatchWireMessage(MessageEntry m, Endpoint e)
    {
    switch (m.MemberName) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override async Task<MessageEntry> CallMemoryFunction(MessageEntry m,Endpoint e) {
    string ename=m.MemberName;
    switch (ename) {
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override bool IsRequestNoLock(MessageEntry m) {
    return false;
    }
}
public class testobj_default_impl : testobj{
    protected PipeBroadcaster<double> rrvar_testpipe1;
    public virtual Task<testobj2> get_subobj(CancellationToken cancel=default(CancellationToken)) {
    throw new NotImplementedException();
    }
    public virtual Pipe<double> testpipe1 {
    get { return rrvar_testpipe1.Pipe;  }
    set {
    if (rrvar_testpipe1!=null) throw new InvalidOperationException("Pipe already set");
    rrvar_testpipe1= new PipeBroadcaster<double>(value);
    }
    }
    public virtual Pipe<double> testpipe2 {
    get { throw new NotImplementedException(); }
    set { throw new InvalidOperationException();}
    }
}
public class testobj2_default_impl : testobj2{
    protected PipeBroadcaster<double> rrvar_testpipe3;
    public virtual Pipe<double> testpipe3 {
    get { return rrvar_testpipe3.Pipe;  }
    set {
    if (rrvar_testpipe3!=null) throw new InvalidOperationException("Pipe already set");
    rrvar_testpipe3= new PipeBroadcaster<double>(value);
    }
    }
}
public static class RRExtensions
{
}
}
