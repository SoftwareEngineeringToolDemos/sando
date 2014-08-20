﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sando.ExtensionContracts.IndexerContracts
{
    public interface IMissingFilesIncluder
    {
        void EnsureNoMissingFilesAndNoDeletedFiles();
    }
}
