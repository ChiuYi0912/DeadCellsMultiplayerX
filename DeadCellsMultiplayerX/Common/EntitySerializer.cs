using dc;
using DeadCellsMultiplayerX.Common.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DeadCellsMultiplayerX.Common
{
    internal static class EntitySerializer
    {
        private enum SFieldKind
        {
            Unknown,
            Int,
            Bool,
            Double
        }
        private record class SFieldInfo(string Name, PropertyInfo Field, SFieldKind Kind);

        private readonly static List<SFieldInfo> fields = [];

        static EntitySerializer()
        {
            var fs = typeof(Entity).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            foreach(var v in fs)
            {
                SFieldKind kind;
                if(v.PropertyType == typeof(int))
                {
                    kind = SFieldKind.Int;
                }
                else if(v.PropertyType == typeof(bool))
                {
                    kind = SFieldKind.Bool;
                }
                else if(v.PropertyType == typeof(double))
                {
                    kind = SFieldKind.Double;
                }
                else
                {
                    continue;
                }

                fields.Add(new(v.Name, v, kind));
            }
        }
    
        public static void Deserialize(EntityInfo info, Entity entity)
        {
            foreach (var v in fields)
            {
                object val;

                if (v.Kind == SFieldKind.Int)
                {
                    val = info.IntValues[v.Name];
                }
                else if (v.Kind == SFieldKind.Bool)
                {
                    val = info.BoolValues[v.Name];
                }
                else if (v.Kind == SFieldKind.Double)
                {
                    val = info.DoubleValues[v.Name];
                }
                else
                {
                    continue;
                }

                v.Field.SetValue(entity, val);
            }
        }

        public static void Serialize(EntityInfo info, Entity entity)
        {
            info.IntValues.Clear();
            info.DoubleValues.Clear();
            info.BoolValues.Clear();

            foreach(var v in fields)
            {
                var val = v.Field.GetValue(entity);

                if(val == null)
                {
                    continue;
                }

                if(v.Kind == SFieldKind.Int)
                {
                    info.IntValues[v.Name] = (int)val;
                }
                else if(v.Kind == SFieldKind.Bool)
                {
                    info.BoolValues[v.Name] = (bool)val;
                }
                else if(v.Kind == SFieldKind.Double)
                {
                    info.DoubleValues[v.Name] = (double)val;
                }
            }
        }
    }
}
