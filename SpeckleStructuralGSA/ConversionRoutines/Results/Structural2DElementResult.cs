﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  //Because the application ID could come from the member (if the element is derived from a parent member)"
  // - GSADMember is also listed as a read prerequisite
  // - MEMB.8 is listed as a subkeyword
  [GSAObject("", new string[] { "EL.4", "MEMB.8" }, "results", true, false, new Type[] { typeof(GSA2DElement), typeof(GSA2DMember) }, new Type[] { })]
  public class GSA2DElementResult : GSABase<Structural2DElementResult>
  {
  }

  public static partial class Conversions
  {
    public static SpeckleObject ToSpeckle(this GSA2DElementResult dummyObject)
    {
      if (Initialiser.Settings.Element2DResults.Count() == 0
        || Initialiser.Settings.EmbedResults && Initialiser.GSASenderObjects.Count<GSA2DElement>() == 0)
      {
        return new SpeckleNull();
      }
      
      if (Initialiser.Settings.EmbedResults)
      {
        var elements = Initialiser.GSASenderObjects.Get<GSA2DElement>();

        foreach (var kvp in Initialiser.Settings.Element2DResults)
        {
          foreach (var loadCase in Initialiser.Settings.ResultCases.Where(rc => Initialiser.Interface.CaseExist(rc)))
          {
            foreach (var element in elements)
            {
              var id = element.GSAId;
              var obj = (Structural2DElement)element.Value;

              if (obj.Result == null)
              {
                obj.Result = new Dictionary<string, object>();
              }
              var resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, 
                Initialiser.Settings.ResultInLocalAxis ? "local" : "global");

              if (resultExport == null || resultExport.Count() == 0)
              {
                continue;
              }

              var newResult = new Structural2DElementResult()
              {
                TargetRef = obj.ApplicationId,
                Value = new Dictionary<string, object>(),
                IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
                LoadCaseRef = loadCase
              };

              //The setter of entity.Value.Result won't accept a value if there are no keys (to avoid issues during merging), so
              //setting a value here needs to be done with at least one key in it
              if (obj.Result == null)
              {
                obj.Result = new Dictionary<string, object>() { { loadCase, newResult } };
              }
              else if (!obj.Result.ContainsKey(loadCase))
              {
                obj.Result[loadCase] = newResult;
              }

              // Let's split the dictionary into xxx_face and xxx_vertex
              var faceDictionary = resultExport.ToDictionary(
                x => x.Key,
                x => new List<double>() { (x.Value as List<double>).Last() } as object);
              var vertexDictionary = resultExport.ToDictionary(
                x => x.Key,
                x => (x.Value as List<double>).Take((x.Value as List<double>).Count - 1).ToList() as object);

              (obj.Result[loadCase] as Structural2DElementResult).Value[kvp.Key + "_face"] = faceDictionary;
              (obj.Result[loadCase] as Structural2DElementResult).Value[kvp.Key + "_vertex"] = vertexDictionary;
            }
          }
        }
      }
      else
      {
        var results = new List<GSA2DElementResult>();

        var keyword = typeof(GSA2DElement).GetGSAKeyword();

        //Unlike embedding, separate results doesn't necessarily mean that there is a Speckle object created for each 1d element.  There is always though
        //some GWA loaded into the cache
        if (!Initialiser.Cache.GetKeywordRecordsSummary(keyword, out var gwa, out var indices, out var applicationIds))
        {
          return new SpeckleNull();
        }

        foreach (var kvp in Initialiser.Settings.Element2DResults)
        {
          foreach (var loadCase in Initialiser.Settings.ResultCases.Where(rc => Initialiser.Interface.CaseExist(rc)))
          {
            for (var i = 0; i < indices.Count(); i++)
            {
              var record = gwa[i];

              var pPieces = record.ListSplit(Initialiser.Interface.GwaDelimiter);
              if ((pPieces[4].ParseElementNumNodes() != 3 && pPieces[4].ParseElementNumNodes() != 4) || indices[i] == 0)
              {
                continue;
              }

              var resultExport = Initialiser.Interface.GetGSAResult(indices[i], kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, 
                Initialiser.Settings.ResultInLocalAxis ? "local" : "global");

              if (resultExport == null)
              {
                continue;
              }

              // Let's split the dictionary into xxx_face and xxx_vertex
              var faceDictionary = resultExport.ToDictionary(
                x => x.Key,
                x => new List<double>() { (x.Value as List<double>).Last() } as object);
              var vertexDictionary = resultExport.ToDictionary(
                x => x.Key,
                x => (x.Value as List<double>).Take((x.Value as List<double>).Count - 1).ToList() as object);

              var targetRef = applicationIds[i];
              if (string.IsNullOrEmpty(applicationIds[i]))
              {
                //The call to ToSpeckle() for 1D element would create application Ids in the cache, but when this isn't called (like for results-only sending)
                //then the cache would be filled with elements' and members' GWA commands but not their non-Speckle-originated (i.e. stored in SIDs) application IDs, 
                //and so in that case the application ID would need to be calculated in the same way as what would happen as a result of the ToSpeckle() call
                if (Helper.GetElementParentIdFromGwa(gwa[i], out var memberIndex) && memberIndex > 0)
                {
                  targetRef = SpeckleStructuralClasses.Helper.CreateChildApplicationId(indices[i], Helper.GetApplicationId(typeof(GSA1DMember).GetGSAKeyword(), memberIndex));
                }
                else
                {
                  targetRef = Helper.GetApplicationId(keyword, indices[i]);
                }
              }

              var existingRes = results.FirstOrDefault(x => ((StructuralResultBase)x.Value).TargetRef == targetRef
                && ((StructuralResultBase)x.Value).LoadCaseRef == loadCase);

              if (existingRes == null)
              {
                var newRes = new Structural2DElementResult()
                {
                  Value = new Dictionary<string, object>(),
                  TargetRef = targetRef,
                  IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
                  LoadCaseRef = loadCase
                };
                newRes.Value[kvp.Key + "_face"] = faceDictionary;
                newRes.Value[kvp.Key + "_vertex"] = vertexDictionary;

                newRes.GenerateHash();

                results.Add(new GSA2DElementResult() { Value = newRes, GSAId = indices[i] });
              }
              else
              {
                existingRes.Value.Value[kvp.Key + "_face"] = faceDictionary;
                existingRes.Value.Value[kvp.Key + "_vertex"] = vertexDictionary;
              }
            }
          }
        }

        Initialiser.GSASenderObjects.AddRange(results);
      }

      return new SpeckleObject();
    }
  }
}
