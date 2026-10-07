using dc;
using dc.en;
using dc.hl.types;
using DeadCellsMultiplayerX.Common.Data;
using Hashlink.Virtuals;

namespace DeadCellsMultiplayerX.Common.Serializers
{
    public static class AffectCodec
    {
        public static List<AffectEntry> Collect(Hero hero)
        {
            var result = new List<AffectEntry>();
            var affects = hero.affects;
            if (affects == null) return result;

            for (int i = 0; i < affects.length; i++)
            {
                var inner = affects.getDyn(i) as ArrayObj;
                if (inner == null) continue;

                for (int j = 0; j < inner.length; j++)
                {
                    var item = inner.getDyn(j) as virtual_a_t_uniqId_val_;
                    if (item == null) continue;

                    result.Add(new AffectEntry
                    {
                        OuterIndex = i,
                        InnerIndex = j,
                        A = item.a,
                        T = item.t,
                        UniqId = item.uniqId,
                        Val = item.val
                    });

                }
            }

            return result;
        }

        public static void Apply(Entity hero, List<AffectEntry> entries)
        {
            if (entries == null || entries.Count == 0) return;

            var affects = hero.affects;
            if (affects == null) return;

            foreach (var e in entries)
            {
                while (affects.length <= e.OuterIndex)
                {
                    affects.push(new ArrayObj());
                }

                if (!(affects.getDyn(e.OuterIndex) is ArrayObj inner))
                {
                    inner = new ArrayObj();
                    affects.array[e.OuterIndex] = inner;
                }

                while (inner.length <= e.InnerIndex)
                {
                    inner.push(null);
                }

                var v = new virtual_a_t_uniqId_val_
                {
                    a = e.A,
                    t = e.T,
                    uniqId = e.UniqId,
                    val = e.Val
                };

                inner.array[e.InnerIndex] = v;
            }
        }
    }
}