using System;
using System.Linq;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleStructuralGSA.SchemaConversion;

namespace SpeckleStructuralGSA
{
  [GSAObject("NODE.3", new string[] { "AXIS.1", "PROP_SPR.4", "PROP_MASS.2" }, "model", true, true, new Type[] { }, new Type[] { })]
  public class GSANode : GSABase<StructuralNode>
  {
    public bool ForceSend; // This is to filter only "important" nodes
  }

  public static partial class Conversions
  {
    public static string ToNative(this SpecklePoint inputObject)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(inputObject, () =>
      {
        var convertedObject = new StructuralNode();

        foreach (var p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
        {
          var inputProperty = inputObject.GetType().GetProperty(p.Name);
          if (inputProperty != null)
            p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
        }

        return convertedObject.ToNative();
      });
    }

    public static SpeckleObject ToSpeckle(this GSANode dummyObject) => (new GsaNode()).ToSpeckle();
  }
}
