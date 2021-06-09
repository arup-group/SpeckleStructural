using System.Linq;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class Structural0DSpringToNative
  {
    public static string ToNative(this Structural0DSpring spring)
    {
      if (string.IsNullOrEmpty(spring.ApplicationId) || spring.Value == null || string.IsNullOrEmpty(spring.PropertyRef))
      {
        return "";
      }

      return Helper.ToNativeTryCatch(spring, () =>
      {
        var propIndex = Initialiser.AppResources.Cache.LookupIndex(GsaRecord.GetKeyword<GsaPropSpr>(), spring.PropertyRef);
        if (propIndex == null)
        {
          //TO DO : add error message
          return "";
        }

        var keyword = GsaRecord.GetKeyword<GsaNode>();
        var streamId = Initialiser.AppResources.Cache.LookupStream(spring.ApplicationId);
        var index = Initialiser.AppResources.Proxy.NodeAt(spring.Value[0], spring.Value[1], spring.Value[2], Initialiser.AppResources.Settings.CoincidentNodeAllowance);
        
        var existingGwa = Initialiser.AppResources.Cache.GetGwa(keyword, index);
        GsaNode gsaNode;
        if (existingGwa == null || existingGwa.Count() == 0 || string.IsNullOrEmpty(existingGwa.First()))
        {
          gsaNode = new GsaNode()
          {
            Index = index,
            ApplicationId = spring.ApplicationId,
            Name = spring.Name,
            StreamId = streamId,
            X = spring.Value[0],
            Y = spring.Value[1],
            Z = spring.Value[2],
            SpringPropertyIndex = propIndex
          };
        }
        else
        {
          gsaNode = new GsaNode();
          if (!gsaNode.FromGwa(existingGwa.First()))
          {
            //TO DO: add error mesage
            return "";
          }
          gsaNode.SpringPropertyIndex = propIndex;
        }

        //Yes, the Axis member of the Speckle StructuralSpringProperty object is not used

        if (gsaNode.Gwa(out var gwaLines, false))
        {
          Initialiser.AppResources.Cache.Upsert(keyword, gsaNode.Index.Value, gwaLines.First(), streamId, gsaNode.ApplicationId, GsaRecord.GetGwaSetCommandType<GsaNode>());
        }

        return "";
      }
      );
    }
  }
}
