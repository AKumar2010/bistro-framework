﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using System.Runtime.InteropServices;

namespace FSharp.ProjectExtender
{
    [ComVisible(true)]
    public interface IProjectManager
    {
        Project MSBuildProject { get; }
    }
}
