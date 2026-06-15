using dc.pr;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Utils
{
    internal static class LevelUtils
    {
        public static int GetSubLevelIndex(this Level lvl, Game? game = null)
        {
            game ??= lvl.game;

            int subLevelId = 0;

            for (int i = 0; i < game.subLevels.length; i++)
            {
                if ((Level)game.subLevels.getDyn(i) == lvl)
                {
                    subLevelId = i;
                }
            }
            return subLevelId;
        }
    }
}
