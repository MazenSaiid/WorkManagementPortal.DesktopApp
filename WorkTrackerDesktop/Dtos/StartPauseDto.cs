﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkTrackerDesktop.Dtos
{
    public class StartPauseDto
    {
        public int PauseType { get; set; }
        public int WorkLogId { get; set; }
    }
}