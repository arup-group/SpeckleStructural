using System;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("NODE.3", new string[] { }, "model", true, true, new Type[] { typeof(GSANode), typeof(GSASpringProperty) }, new Type[] { typeof(GSANode), typeof(GSASpringProperty) })]
  public class GSA0DSpring : GSABase<Structural0DSpring>
  {
    public int Member;
  }

  public static partial class Conversions
  {
    // The ToNative method is located in the relevant static class
    
    // There is no need for a ToSpeckle for this type in particular as the ToSpeckle for GsaNode creates Structural0DSprings as necessary
  }
}
