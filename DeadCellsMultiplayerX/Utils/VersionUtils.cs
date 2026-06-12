using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Utils
{
    internal class VersionUtils
    {
        public static Version ModVersion => typeof(VersionUtils).Assembly.GetName().Version!;
    }
}
