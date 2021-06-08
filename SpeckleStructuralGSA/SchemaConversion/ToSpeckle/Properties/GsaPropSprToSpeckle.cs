using System.Collections.Generic;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class GsaPropSprToSpeckle
  {
    public static SpeckleObject ToSpeckle(this GsaPropSpr dummy)
    {
      var kw = GsaRecord.GetKeyword<GsaPropSpr>();
      var newLines = Initialiser.AppResources.Cache.GetGwaToSerialise(kw);

      int numAdded = 0;

      var structuralSpringProperties = new List<StructuralSpringProperty>();

#if DEBUG
      foreach (var i in newLines.Keys)
#else
      Parallel.ForEach(newLines.Keys, i =>
#endif
      {
        var obj = Helper.ToSpeckleTryCatch(kw, i, () =>
        {
          var gsaPropSpr = new GsaPropSpr();
          if (gsaPropSpr.FromGwa(newLines[i]))
          {
            var structuralProp = new StructuralSpringProperty()
            {
              Name = gsaPropSpr.Name,
              ApplicationId = SpeckleStructuralGSA.Helper.GetApplicationId(kw, i),
              DampingRatio = gsaPropSpr.DampingRatio,
              SpringType = gsaPropSpr.PropertyType,
              Stiffness = Helper.AxisDirDictToStructuralVectorSix(gsaPropSpr.Stiffnesses)
            };
            ((Dictionary<string, object>)structuralProp.Properties["structural"]).Add("NativeId", i.ToString());
            return structuralProp;
          }
          return new SpeckleNull();
        });

        if (!(obj is SpeckleNull))
        {
          Initialiser.GsaKit.GSASenderObjects.Add(new GSASpringProperty() { Value = (StructuralSpringProperty)obj, GSAId = i } );
          numAdded++;
        }
      }
#if !DEBUG
      );
#endif

      return (numAdded > 0) ? new SpeckleObject() : new SpeckleNull();
    }

  }
}
