using System.Collections.Generic;
using System.Linq;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class StructuralSpringPropertyToNative
  {
    public static string ToNative(this StructuralSpringProperty prop)
    {
      if (string.IsNullOrEmpty(prop.ApplicationId) || prop.SpringType == StructuralSpringPropertyType.NotSet)
      {
        return "";
      }

      return Helper.ToNativeTryCatch(prop, () =>
      {
        var keyword = GsaRecord.GetKeyword<GsaPropSpr>();
        var streamId = Initialiser.AppResources.Cache.LookupStream(prop.ApplicationId);
        var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, prop.ApplicationId);

        var existingGwa = Initialiser.AppResources.Cache.GetGwa(keyword, index);
        var gsaPropSpr = new GsaPropSpr()
        {
          Index = index,
          ApplicationId = prop.ApplicationId,
          StreamId = streamId,
          Name = prop.Name,
          PropertyType = prop.SpringType,
          DampingRatio = prop.DampingRatio
        };

        if (prop.Stiffness != null && prop.Stiffness.Value != null && prop.Stiffness.Value.Count() > 0)
        {
          gsaPropSpr.Stiffnesses = new Dictionary<AxisDirection6, double>();
          for (int i = 0; i < prop.Stiffness.Value.Count(); i++)
          {
            if (prop.Stiffness.Value[i] > 0)
            {
              gsaPropSpr.Stiffnesses.Add(Helper.AxisDirs[i], prop.Stiffness.Value[i]);
            }
          }
        }

        //Yes, the Axis member of the Speckle StructuralSpringProperty object is not used

        if (gsaPropSpr.Gwa(out var gwaLines, false))
        {
          Initialiser.AppResources.Cache.Upsert(keyword, gsaPropSpr.Index.Value, gwaLines.First(), streamId, gsaPropSpr.ApplicationId, GsaRecord.GetGwaSetCommandType<GsaNode>());
        }

        return "";
      }
      );
    }
  }
}
