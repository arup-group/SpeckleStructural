﻿using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class GsaSectionToSpeckle
  {
    public static SpeckleObject ToSpeckle(this GsaSection dummyObject)
    {
      var settings = Initialiser.AppResources.Settings;
      var element1dResults = settings.ResultTypes != null && settings.ResultTypes.Any(rt => rt.ToString().ToLower().Contains("1d"));
      if (settings.TargetLayer == GSATargetLayer.Analysis && settings.StreamSendConfig == StreamContentConfig.TabularResultsOnly && !element1dResults)
      {
        return new SpeckleNull();
      }

      var kw = GsaRecord.GetKeyword<GsaSection>();
      var newLines = Initialiser.AppResources.Cache.GetGwaToSerialise(kw);

      int numAdded = 0;

      var structural1DPropertyExplicits = new List<Structural1DPropertyExplicit>();
      
      var concreteMaterials = Initialiser.GsaKit.GSASenderObjects.Get<GSAMaterialConcrete>().ToDictionary(o => o.GSAId, o => ((StructuralMaterialConcrete) o.SpeckleObject).ApplicationId);
      var steelMaterials = Initialiser.GsaKit.GSASenderObjects.Get<GSAMaterialSteel>().ToDictionary(o => o.GSAId, o => ((StructuralMaterialSteel)o.SpeckleObject).ApplicationId);

      //Currently only handles explicit 1D properties
      //Filtering out all but explicit properties:
      //1.  First exclude any GWA lines with the exact string "EXP" - make first pass at filtering them out
      //2.  Call FromGwa for all and perform logic check of values of GsaSection (and subclass) instances
      var indicesContainingEXP = newLines.Keys.Where(k => newLines[k].Contains("EXP")).ToList();
      var gsaSectionsExp = new List<GsaSection>();
      foreach (var i in indicesContainingEXP)
      {
        var obj = Helper.ToSpeckleTryCatch(dummyObject.Keyword, i, () =>
        {
          var gsaSection = new GsaSection();
          if (gsaSection.FromGwa(newLines[i]) && FindExpDetails(gsaSection, out var comp, out var pde))
          {
            if (string.IsNullOrEmpty(gsaSection.ApplicationId))
            {
              gsaSection.ApplicationId = SpeckleStructuralGSA.Helper.FormatApplicationId(kw, i);
            }
            var structuralProp = new Structural1DPropertyExplicit()
            {
              Name = gsaSection.Name,
              ApplicationId = gsaSection.ApplicationId,
              Area = pde.Area,
              Iyy = pde.Iyy,
              Izz = pde.Izz,
              J = pde.J,
              Ky = pde.Ky,
              Kz = pde.Kz
            };

            //No support for any other material type at this stage
            if (comp.MaterialType == Section1dMaterialType.CONCRETE || comp.MaterialType == Section1dMaterialType.STEEL)
            {
              var materialIndex = comp.MaterialIndex ?? 0;
              var materialDict = (comp.MaterialType == Section1dMaterialType.CONCRETE) ? concreteMaterials : steelMaterials;
              structuralProp.MaterialRef = (materialIndex > 0 && materialDict.ContainsKey(materialIndex)) ? materialDict[materialIndex] : null;
            }
            return structuralProp;
          }
          return new SpeckleNull();
        });

        if (!(obj is SpeckleNull))
        {
          Initialiser.GsaKit.GSASenderObjects.Add(new GSA1DPropertyExplicit() { Value = (Structural1DPropertyExplicit)obj, GSAId = i });
          numAdded++;
        }
      }

      return (numAdded > 0) ? new SpeckleObject() : new SpeckleNull();
    }

    private static bool FindExpDetails(GsaSection gsaSection, out SectionComp comp, out ProfileDetailsExplicit profileDetailsExplicit)
    {
      profileDetailsExplicit = null;
      comp = null;
      if (gsaSection.Components != null && gsaSection.Components.Count() > 0)
      {
        var compExps = gsaSection.Components.Where(c => c is SectionComp && ((SectionComp)c).ProfileDetails != null && ((SectionComp)c).ProfileDetails is ProfileDetailsExplicit);
        if (compExps.Count() > 0)
        {
          comp = (SectionComp)compExps.First();
          profileDetailsExplicit = (ProfileDetailsExplicit)comp.ProfileDetails;
          return true;
        }
      }
      return false;
    }
  }
}
