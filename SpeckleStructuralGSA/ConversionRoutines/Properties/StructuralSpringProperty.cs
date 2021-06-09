using System;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleStructuralGSA.SchemaConversion;

namespace SpeckleStructuralGSA
{
  [GSAObject("PROP_SPR.4", new string[] { }, "model", true, true, new Type[] { }, new Type[] { })]
  public class GSASpringProperty : GSABase<StructuralSpringProperty>
  {
    
  }

  public static partial class Conversions
  {
    public static SpeckleObject ToSpeckle(this GSASpringProperty dummyObject) => (new GsaPropSpr()).ToSpeckle();
  }
}
