//This file is automatically generated. DO NOT EDIT!
using System;
using RobotRaconteurWeb;
using RobotRaconteurWeb.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable 0108

namespace com.robotraconteur.testing.TestService2
{
public class com__robotraconteur__testing__TestService2Factory : ServiceFactory
{
    public override string DefString()
{
    const string s="service com.robotraconteur.testing.TestService2\n\noption version 0.8\n\nexception testexception3\n\nstruct ostruct2\nfield double[] a1\nend struct\n\n\nobject baseobj\nproperty double d1\nproperty double[] d2\n\nfunction double func3(double d1, double d2)\n\nevent ev1()\n\nobjref subobj o5\n\npipe double[] p1\n\ncallback void cb2(double d1, double d2)\n\nwire double[] w1\n\nmemory double[] m1\n\n\nend object\n\nobject subobj\n\nfunction double add_val(double v)\n\nend object\n";
    return s;
    }
    public override string GetServiceName() {return "com.robotraconteur.testing.TestService2";}
    public ostruct2_stub ostruct2_stubentry;
    public com__robotraconteur__testing__TestService2Factory() : this(null,null) {}
    public com__robotraconteur__testing__TestService2Factory(RobotRaconteurNode node = null, ClientContext context = null) : base(node,context)
    {
    ostruct2_stubentry=new ostruct2_stub(this,this.node,this.context);
    }
    public override IStructureStub FindStructureStub(string objecttype)
    {
    if (objecttype=="ostruct2")
    return ostruct2_stubentry;
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
    case "baseobj":
    return new baseobj_stub(path, context);
    case "subobj":
    return new subobj_stub(path, context);
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
    case "baseobj":
    return new baseobj_skel(path,(baseobj)obj,context);
    case "subobj":
    return new subobj_skel(path,(subobj)obj,context);
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
    if (rr_stype=="testexception3") return new testexception3(rr_exp.Message);
    } else {
    return base.DownCastException(rr_exp); 
    }
    return rr_exp;
    }
}

public class ostruct2_stub : IStructureStub {
    public ostruct2_stub(com__robotraconteur__testing__TestService2Factory d, RobotRaconteurNode node, ClientContext context) {def=d; rr_node=node; rr_context=context;}
    private com__robotraconteur__testing__TestService2Factory def;
    private RobotRaconteurNode rr_node;
    private ClientContext rr_context;
    public MessageElementNestedElementList PackStructure(object s1) {
    List<MessageElement> m=new List<MessageElement>();
    if (s1 ==null) return null;
    ostruct2 s = (ostruct2)s1;
    MessageElementUtil.AddMessageElement(m,MessageElementUtil.PackArray<double>("a1",s.a1));
    return new MessageElementNestedElementList(DataTypes.structure_t,"com.robotraconteur.testing.TestService2.ostruct2",m);
    }
    public T UnpackStructure<T>(MessageElementNestedElementList m) {
    if (m == null ) return default(T);
    ostruct2 s=new ostruct2();
    s.a1 =MessageElementUtil.UnpackArray<double>(MessageElement.FindElement(m.Elements,"a1"));
    T st; try {st=(T)((object)s);} catch (InvalidCastException) {throw new DataTypeMismatchException("Wrong structuretype");}
    return st;
    }
}

public class baseobj_stub : ServiceStub , baseobj {
    private CallbackClient<Func<double, double, CancellationToken, Task>> rr_cb2;
    private Pipe<double[]> rr_p1;
    private Wire<double[]> rr_w1;
    private ArrayMemory<double> rr_m1;
    public baseobj_stub(string path, ClientContext c) : base(path, c) {
    rr_cb2=new CallbackClient<Func<double, double, CancellationToken, Task>>("cb2");
    rr_p1=new PipeClient<double[]>("p1", this);
    rr_w1=new WireClient<double[]>("w1", this);
    rr_m1=new ArrayMemoryClient<double>("m1",this, MemberDefinition_Direction.both);
    }
    public async Task<double> get_d1(CancellationToken cancel=default(CancellationToken)) {
        MessageEntry m = new MessageEntry(MessageEntryType.PropertyGetReq, "d1");
        MessageEntry mr=await ProcessRequest(m, cancel).ConfigureAwait(false);
        MessageElement me=mr.FindElement("value");
        return (MessageElementUtil.UnpackScalar<double>(me));
        }
    public async Task set_d1(double value, CancellationToken cancel=default(CancellationToken)) {
        MessageEntry m=new MessageEntry(MessageEntryType.PropertySetReq,"d1");
        MessageElementUtil.AddMessageElement(m,MessageElementUtil.PackScalar<double>("value",value));
        MessageEntry mr=await ProcessRequest(m, cancel).ConfigureAwait(false);
        }
    public async Task<double[]> get_d2(CancellationToken cancel=default(CancellationToken)) {
        MessageEntry m = new MessageEntry(MessageEntryType.PropertyGetReq, "d2");
        MessageEntry mr=await ProcessRequest(m, cancel).ConfigureAwait(false);
        MessageElement me=mr.FindElement("value");
        return MessageElementUtil.UnpackArray<double>(me);
        }
    public async Task set_d2(double[] value, CancellationToken cancel=default(CancellationToken)) {
        MessageEntry m=new MessageEntry(MessageEntryType.PropertySetReq,"d2");
        MessageElementUtil.AddMessageElement(m,MessageElementUtil.PackArray<double>("value",value));
        MessageEntry mr=await ProcessRequest(m, cancel).ConfigureAwait(false);
        }
    public async Task<double> func3(double d1, double d2, CancellationToken cancel = default(CancellationToken)) {
        MessageEntry rr_m=new MessageEntry(MessageEntryType.FunctionCallReq,"func3");
    MessageElementUtil.AddMessageElement(rr_m,MessageElementUtil.PackScalar<double>("d1",d1));
    MessageElementUtil.AddMessageElement(rr_m,MessageElementUtil.PackScalar<double>("d2",d2));
        MessageEntry rr_me=await ProcessRequest(rr_m, cancel).ConfigureAwait(false);
    return (MessageElementUtil.UnpackScalar<double>(rr_me.FindElement("return")));
    }
    public event Action ev1;
    protected override void DispatchEvent(MessageEntry rr_m) {
    switch (rr_m.MemberName) {
    case "ev1":
    {
    if (ev1 != null) { 
    ev1();
    }
    return;
    }
    default:
    break;
    }
    }
    public async Task<subobj> get_o5(CancellationToken cancel=default(CancellationToken)) {
    return (subobj)await FindObjRefTyped("o5","com.robotraconteur.testing.TestService2.subobj",cancel).ConfigureAwait(false);
    }
    public Pipe<double[]> p1 {
    get { return rr_p1;  }
    set { throw new InvalidOperationException();}
    }
    public Callback<Func<double, double, CancellationToken, Task>> cb2 {
    get { return rr_cb2;  }
    set { throw new InvalidOperationException();}
    }
    public Wire<double[]> w1 {
    get { return rr_w1;  }
    set { throw new InvalidOperationException();}
    }
    public ArrayMemory<double> m1 { 
    get { return rr_m1; }
    }
    protected override void DispatchPipeMessage(MessageEntry m)
    {
    switch (m.MemberName) {
    case "p1":
    this.rr_p1.PipePacketReceived(m);
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
    case "cb2": {
    double d1=(MessageElementUtil.UnpackScalar<double>(rr_m.FindElement("d1")));
    double d2=(MessageElementUtil.UnpackScalar<double>(rr_m.FindElement("d2")));
    await this.cb2.Function(d1, d2, default(CancellationToken)).ConfigureAwait(false);
    MessageElementUtil.AddMessageElement(rr_mr,MessageElementUtil.PackScalar<int>("return",0));
    break;
    }
    default:
    throw new MemberNotFoundException("Member not found");
    }
    return rr_mr;
    }
    protected override void DispatchWireMessage(MessageEntry m)
    {
    switch (m.MemberName) {
    case "w1":
    this.rr_w1.WirePacketReceived(m);
    break;
    default:
    throw new Exception();
    }
    }
}
public class subobj_stub : ServiceStub , subobj {
    public subobj_stub(string path, ClientContext c) : base(path, c) {
    }
    public async Task<double> add_val(double v, CancellationToken cancel = default(CancellationToken)) {
        MessageEntry rr_m=new MessageEntry(MessageEntryType.FunctionCallReq,"add_val");
    MessageElementUtil.AddMessageElement(rr_m,MessageElementUtil.PackScalar<double>("v",v));
        MessageEntry rr_me=await ProcessRequest(rr_m, cancel).ConfigureAwait(false);
    return (MessageElementUtil.UnpackScalar<double>(rr_me.FindElement("return")));
    }
    protected override void DispatchEvent(MessageEntry rr_m) {
    switch (rr_m.MemberName) {
    default:
    break;
    }
    }
    protected override void DispatchPipeMessage(MessageEntry m)
    {
    switch (m.MemberName) {
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
public class baseobj_skel : ServiceSkel {
    protected baseobj obj;
    public baseobj_skel(string p,baseobj o,ServerContext c) : base(p,o,c) { obj=(baseobj)o; }
    public override void ReleaseCastObject() { 
    }
    public override async Task<MessageEntry> CallGetProperty(MessageEntry m) {
    string ename=m.MemberName;
    MessageEntry mr=new MessageEntry(MessageEntryType.PropertyGetRes, ename);
    switch (ename) {
    case "d1":
    {
    double ret=await obj.get_d1().ConfigureAwait(false);
    mr.AddElement(MessageElementUtil.PackScalar<double>("value",ret));
    break;
    }
    case "d2":
    {
    double[] ret=await obj.get_d2().ConfigureAwait(false);
    mr.AddElement(MessageElementUtil.PackArray<double>("value",ret));
    break;
    }
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
    case "d1":
    {
    await obj.set_d1((MessageElementUtil.UnpackScalar<double>(me))).ConfigureAwait(false);
    break;
    }
    case "d2":
    {
    await obj.set_d2(MessageElementUtil.UnpackArray<double>(me)).ConfigureAwait(false);
    break;
    }
    default:
    throw new MemberNotFoundException("Member not found");
    }
    return mr;
    }
    public override async Task<MessageEntry> CallFunction(MessageEntry rr_m) {
    string rr_ename=rr_m.MemberName;
    MessageEntry rr_mr=new MessageEntry(MessageEntryType.FunctionCallRes, rr_ename);
    switch (rr_ename) {
    case "func3":
    {
    double d1=(MessageElementUtil.UnpackScalar<double>(MessageElementUtil.FindElement(rr_m,"d1")));
    double d2=(MessageElementUtil.UnpackScalar<double>(MessageElementUtil.FindElement(rr_m,"d2")));
    double rr_ret=await this.obj.func3(d1, d2, default(CancellationToken)).ConfigureAwait(false);
    rr_mr.AddElement(MessageElementUtil.PackScalar<double>("return",rr_ret));
    break;
    }
    default:
    throw new MemberNotFoundException("Member not found");
    }
    return rr_mr;
    }
    public override async Task<object> GetSubObj(string name, string ind) {
    switch (name) {
    case "o5": {
    return await obj.get_o5().ConfigureAwait(false);
    }
    default:
    break;
    }
    throw new MemberNotFoundException("");
    }
    public override void RegisterEvents(object rrobj1) {
    obj=(baseobj)rrobj1;
    obj.ev1+=rr_ev1;
    }
    public override void UnregisterEvents(object rrobj1) {
    obj=(baseobj)rrobj1;
    obj.ev1-=rr_ev1;
    }
    public void rr_ev1() {
    MessageEntry rr_mm=new MessageEntry(MessageEntryType.EventReq,"ev1");
    this.SendEvent(rr_mm);
    }
    public override object GetCallbackFunction(uint rr_endpoint, string rr_membername) {
    switch (rr_membername) {
    case "cb2": {
    return new Func<double, double, CancellationToken, Task>( async delegate(double d1, double d2, CancellationToken rr_cancel) {
    MessageEntry rr_mm=new MessageEntry(MessageEntryType.CallbackCallReq,"cb2");
    rr_mm.ServicePath=m_ServicePath;
    MessageElementUtil.AddMessageElement(rr_mm,MessageElementUtil.PackScalar<double>("d1",d1));
    MessageElementUtil.AddMessageElement(rr_mm,MessageElementUtil.PackScalar<double>("d2",d2));
    MessageEntry rr_mr=await RRContext.ProcessCallbackRequest(rr_mm,rr_endpoint,rr_cancel).ConfigureAwait(false);
    MessageElement rr_me = rr_mr.FindElement("return");
    });
    }
    default:
    break;
    }
    throw new MemberNotFoundException("Member not found");
    }
    private PipeServer<double[]> rr_p1;
    private WireServer<double[]> rr_w1;
    private bool rr_InitPipeServersRun=false;
    public override void InitPipeServers(object o) {
    if (this.rr_InitPipeServersRun) return;
    this.rr_InitPipeServersRun=true;
    baseobj castobj=(baseobj)o;
    this.rr_p1=new PipeServer<double[]>("p1",this);
    this.rr_w1=new WireServer<double[]>("w1",this);
    castobj.p1=this.rr_p1;
    castobj.w1=this.rr_w1;
    }
    public override void InitCallbackServers(object rrobj1) {
    obj=(baseobj)rrobj1;
    obj.cb2=new CallbackServer<Func<double, double, CancellationToken, Task>>("cb2",this);
    }
    public override async Task<MessageEntry> CallPipeFunction(MessageEntry m,Endpoint e) {
    string ename=m.MemberName;
    switch (ename) {
    case "p1":
    return await this.rr_p1.PipeCommand(m,e).ConfigureAwait(false);
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override async Task<MessageEntry> CallWireFunction(MessageEntry m,Endpoint e) {
    string ename=m.MemberName;
    switch (ename) {
    case "w1":
    return await this.rr_w1.WireCommand(m,e).ConfigureAwait(false);
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override void DispatchPipeMessage(MessageEntry m, Endpoint e)
    {
    switch (m.MemberName) {
    case "p1":
    this.rr_p1.PipePacketReceived(m,e);
    break;
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override void DispatchWireMessage(MessageEntry m, Endpoint e)
    {
    switch (m.MemberName) {
    case "w1":
    this.rr_w1.WirePacketReceived(m,e);
    break;
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override async Task<MessageEntry> CallMemoryFunction(MessageEntry m,Endpoint e) {
    string ename=m.MemberName;
    switch (ename) {
    case "m1":
     return await (new ArrayMemoryServiceSkel<double>("m1",this,MemberDefinition_Direction.both)).CallMemoryFunction(m,e,obj.m1).ConfigureAwait(false);
    break;
    default:
    throw new MemberNotFoundException("Member not found");
    }
    }
    public override bool IsRequestNoLock(MessageEntry m) {
    return false;
    }
}
public class subobj_skel : ServiceSkel {
    protected subobj obj;
    public subobj_skel(string p,subobj o,ServerContext c) : base(p,o,c) { obj=(subobj)o; }
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
    case "add_val":
    {
    double v=(MessageElementUtil.UnpackScalar<double>(MessageElementUtil.FindElement(rr_m,"v")));
    double rr_ret=await this.obj.add_val(v, default(CancellationToken)).ConfigureAwait(false);
    rr_mr.AddElement(MessageElementUtil.PackScalar<double>("return",rr_ret));
    break;
    }
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
    obj=(subobj)rrobj1;
    }
    public override void UnregisterEvents(object rrobj1) {
    obj=(subobj)rrobj1;
    }
    public override object GetCallbackFunction(uint rr_endpoint, string rr_membername) {
    switch (rr_membername) {
    default:
    break;
    }
    throw new MemberNotFoundException("Member not found");
    }
    private bool rr_InitPipeServersRun=false;
    public override void InitPipeServers(object o) {
    if (this.rr_InitPipeServersRun) return;
    this.rr_InitPipeServersRun=true;
    subobj castobj=(subobj)o;
    }
    public override void InitCallbackServers(object rrobj1) {
    obj=(subobj)rrobj1;
    }
    public override async Task<MessageEntry> CallPipeFunction(MessageEntry m,Endpoint e) {
    string ename=m.MemberName;
    switch (ename) {
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
public class baseobj_default_impl : baseobj{
    protected Callback<Func<double, double, CancellationToken, Task>> rrvar_cb2;
    public virtual Task<double> get_d1(CancellationToken cancel=default(CancellationToken)) {
    throw new NotImplementedException();
    }
    public virtual Task set_d1(double value, CancellationToken cancel=default(CancellationToken)) {
    throw new NotImplementedException();
    }
    public virtual Task<double[]> get_d2(CancellationToken cancel=default(CancellationToken)) {
    throw new NotImplementedException();
    }
    public virtual Task set_d2(double[] value, CancellationToken cancel=default(CancellationToken)) {
    throw new NotImplementedException();
    }
    public virtual Task<double> func3(double d1, double d2,CancellationToken rr_cancel=default(CancellationToken)) {
    throw new NotImplementedException();
    }
    public virtual event Action ev1;
    public virtual Task<subobj> get_o5(CancellationToken cancel=default(CancellationToken)) {
    throw new NotImplementedException();
    }
    public virtual Pipe<double[]> p1 {
    get { throw new NotImplementedException(); }
    set { throw new InvalidOperationException();}
    }
    public virtual Callback<Func<double, double, CancellationToken, Task>> cb2 {
    get { return rrvar_cb2;  }
    set {
    if (rrvar_cb2!=null) throw new InvalidOperationException("Callback already set");
    rrvar_cb2= value;
    }
    }
    public virtual Wire<double[]> w1 {
    get { throw new NotImplementedException(); }
    set { throw new NotImplementedException();}
    }
    public virtual ArrayMemory<double> m1 { 
    get { throw new NotImplementedException(); }
    }
}
public class subobj_default_impl : subobj{
    public virtual Task<double> add_val(double v,CancellationToken rr_cancel=default(CancellationToken)) {
    throw new NotImplementedException();
    }
}
public static class RRExtensions
{
}
}
