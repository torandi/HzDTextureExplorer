﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HzdTextureLib
{
    public class HzDException : Exception
    {
        public HzDException(string error)
            : base(error)
        { }
    }
}
