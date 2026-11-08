// SPDX-License-Identifier: GPL-3.0-or-later
// Test-only viewer: validates arguments without opening Second Life.
using System;
class FakeViewer {
 static int Main(string[] args){return args.Length==2&&args[0]=="--url"&&args[1]==Environment.GetEnvironmentVariable("SLVIEWER_TEST_URL")?0:1;}
}
