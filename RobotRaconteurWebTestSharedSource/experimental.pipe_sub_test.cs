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
[RobotRaconteurServiceObjectInterface("experimental.pipe_sub_test.testobj")]
public interface testobj
{
    Task<testobj2> get_subobj(CancellationToken rr_cancel=default(CancellationToken));
    Pipe<double> testpipe1{ get; set; }
    Pipe<double> testpipe2{ get; set; }
}

[RobotRaconteurServiceObjectInterface("experimental.pipe_sub_test.testobj2")]
public interface testobj2
{
    Pipe<double> testpipe3{ get; set; }
}

}