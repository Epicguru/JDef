using JDef;

namespace Testbed.Defs
{
    public abstract class Base : Def
    {
        public float Size;

        public override string ToString()
        {
            return base.ToString() + $"\nSize: {Size}";
        }
    }
}
