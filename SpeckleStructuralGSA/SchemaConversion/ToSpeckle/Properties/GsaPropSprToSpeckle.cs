using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class GsaPropSprToSpeckle
  {
    public static SpeckleObject ToSpeckle(GsaPropSpr dummy)
    {
      var kw = GsaRecord.GetKeyword<GsaPropSpr>();
      var newLines = Initialiser.AppResources.Cache.GetGwaToSerialise(kw);

      int numAdded = 0;

      var structuralSpringProperties = new List<StructuralSpringProperty>();

      foreach (var i in newLines.Keys)
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

      //var props = structuralSpringProperties.Select(pe => new GSASpringProperty() { Value = pe }).ToList();
      //Initialiser.GsaKit.GSASenderObjects.AddRange(props);
      //return (props.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
      return (numAdded > 0) ? new SpeckleObject() : new SpeckleNull();
    }

  }
}
