//This file is automatically generated. DO NOT EDIT!
using System;
using RobotRaconteurWeb;
using RobotRaconteurWeb.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 0108

namespace experimental.testing.subtestfilter
{
[RobotRaconteurServiceObjectInterface("experimental.testing.subtestfilter.sub_testroot")]
public interface sub_testroot
{
    Task<double> get_d1(CancellationToken cancel=default(CancellationToken));
    Task set_d1(double value, CancellationToken cancel=default(CancellationToken));
}

[RobotRaconteurServiceObjectInterface("experimental.testing.subtestfilter.sub_testroot2")]
public interface sub_testroot2
{
    Task<double> get_d1(CancellationToken cancel=default(CancellationToken));
    Task set_d1(double value, CancellationToken cancel=default(CancellationToken));
}

}