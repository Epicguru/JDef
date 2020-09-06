using System.Collections.Generic;
using JDef;

namespace Testbed.Defs
{
    public class ProductionBase : Base
    {
        public List<string> Products;
        public float Rate = 15f;
        public Def OtherDef;

        public override string ToString()
        {
            return base.ToString() + $"\nProducts:\n  *{string.Join(",\n  *", Products)}\nRate: {Rate}\nDef: {OtherDef}";
        }
    }
}
