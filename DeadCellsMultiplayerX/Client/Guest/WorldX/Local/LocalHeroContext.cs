namespace DeadCellsMultiplayerX.Client.Guest.WorldX.LocalHero
{
    /// <summary>
    /// 本地 Hero 的状态快照，只读
    /// </summary>
    internal class LocalHeroContext
    {
        public bool InSubLevel;     //是否正在子关卡中

        public string CurrentLevelId = string.Empty;    //当前 level id


        public bool InCinematic;    //是否在过场动画

        public int SubLevelIndex = -1;  //当前 sublevel 索引

        public bool HasHero;    //本地 Hero 是否存在

        public bool IsAlive;    //本地 Hero 是否活着
    }
}