//This file is automatically generated. DO NOT EDIT!
using System;
using RobotRaconteurWeb;
using RobotRaconteurWeb.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 0108

namespace experimental.sub_test
{
[RobotRaconteurServiceObjectInterface("experimental.sub_test.testobj")]
public interface testobj
{
    Task<double> add_two_numbers(double a, double b,CancellationToken rr_cancel=default(CancellationToken));
}

}
