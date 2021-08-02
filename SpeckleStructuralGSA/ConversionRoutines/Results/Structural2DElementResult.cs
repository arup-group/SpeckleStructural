using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

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
      var kw = GsaRecord.GetKeyword<GsaEl>();
      var loadTaskKw = GsaRecord.GetKeyword<GsaLoadCase>();
      var comboKw = GsaRecord.GetKeyword<GsaCombination>();

      //var resultTypes = Initialiser.AppResources.Settings.Element2DResults.Keys.ToList();
      var cases = Initialiser.AppResources.Settings.ResultCases;

      //if (Initialiser.AppResources.Settings.Element2DResults.Count() == 0
      if (!Initialiser.AppResources.Settings.ResultTypes.Any(rt => rt.ToString().ToLower().Contains("2d"))
        || (Initialiser.AppResources.Settings.StreamSendConfig == StreamContentConfig.ModelWithEmbeddedResults
          && Initialiser.GsaKit.GSASenderObjects.Count<GSA2DElement>() == 0))
      {
        return new SpeckleNull();
      }

      var axisStr = Initialiser.AppResources.Settings.ResultInLocalAxis ? "local" : "global";
      var typeName = dummyObject.GetType().Name;


      if (Initialiser.AppResources.Settings.StreamSendConfig == StreamContentConfig.ModelWithEmbeddedResults)
      {
        Embed2DResults(typeName, axisStr, kw, loadTaskKw, comboKw, cases);
      }
      else
      {
        if (!Create2DElementResultObjects(typeName, axisStr, kw, loadTaskKw, comboKw, cases))
        {
          return new SpeckleNull();
        }
      }

      return new SpeckleObject();
    }

    private static void Embed2DResults(string typeName, string axisStr, string keyword, string loadTaskKw, string comboKw, List<string> cases)
    {
      //Meshes aren't included as we only need quads and triangle *elements* here
      var elements = Initialiser.GsaKit.GSASenderObjects.Get<GSA2DElement>();

      var entities = elements.Cast<GSA2DElement>().ToList();
      var globalAxis = !Initialiser.AppResources.Settings.ResultInLocalAxis;

      Initialiser.AppResources.Proxy.LoadResults(ResultGroup.Element2d, out int numErrorRows, cases, entities.Select(e => e.GSAId).ToList());
      if (numErrorRows > 0)
      {
        Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "Unable to process " + numErrorRows + " rows of 2D element results");
        Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, "Unable to process " + numErrorRows + " rows of 2D element results");
      }

#if DEBUG
      foreach (var e in entities)
#else
      Parallel.ForEach(entities, e =>
#endif
      {
        var i = e.GSAId;
        var obj = e.Value;

        if (ResultObjectsByLoadCase(i, obj.ApplicationId, loadTaskKw, comboKw, out var resultObjectsByLoadCase))
        {
          foreach (var loadCase in resultObjectsByLoadCase.Keys)
          {
            if (obj.Result == null)
            {
              //Can't just allocate an empty dictionary as the Result set property won't allow it
              obj.Result = new Dictionary<string, object>() { { loadCase, resultObjectsByLoadCase[loadCase] } };
            }
            else
            {
              obj.Result.Add(loadCase, resultObjectsByLoadCase[loadCase]);
            }
          }
        }
      }
#if !DEBUG
      );
#endif
      Initialiser.AppResources.Proxy.ClearResults(ResultGroup.Element2d);
    }

    private static bool Create2DElementResultObjects(string typeName, string axisStr, string keyword, string loadTaskKw, string comboKw, List<string> cases)
    {
      var results = new List<GSA2DElementResult>();
      var resultsLock = new object();

      var memberKw = typeof(GSA1DMember).GetGSAKeyword();

      //Unlike embedding, separate results doesn't necessarily mean that there is a Speckle object created for each 2d element.  There is always though
      //some GWA loaded into the cache
      if (!Initialiser.AppResources.Cache.GetKeywordRecordsSummary(keyword, out var gwa, out var kwIndices, out var kwApplicationIds))
      {
        return false;
      }

      //Find relevant indices
      var indices = new List<int>();
      var applicationIds = new List<string>();
      for (var i = 0; i < kwIndices.Count(); i++)
      {
        var record = gwa[i];
        var pPieces = record.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
        if ((pPieces[4].ParseElementNumNodes() != 3 && pPieces[4].ParseElementNumNodes() != 4) || kwIndices[i] == 0)
        {
          continue;
        }
        indices.Add(kwIndices[i]);
        applicationIds.Add(kwApplicationIds[i]);
      }

      Initialiser.AppResources.Proxy.LoadResults(ResultGroup.Element2d, out int numErrorRows, cases, indices);
      if (numErrorRows > 0)
      {
        Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "Unable to process " + numErrorRows + " rows of 2D element results");
        Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, "Unable to process " + numErrorRows + " rows of 2D element results");
      }

#if DEBUG
      for (var i = 0; i < indices.Count(); i++)
#else
      Parallel.For(0, indices.Count(), (i) =>
#endif
      {
        var targetRef = applicationIds[i];
        if (string.IsNullOrEmpty(applicationIds[i]))
        {
          //The call to ToSpeckle() for 2D element would create application Ids in the cache, but when this isn't called (like for results-only sending)
          //then the cache would be filled with elements' and members' GWA commands but not their non-Speckle-originated (i.e. stored in SIDs) application IDs, 
          //and so in that case the application ID would need to be calculated in the same way as what would happen as a result of the ToSpeckle() call
          if (Helper.GetElementParentIdFromGwa(gwa[i], out var memberIndex) && memberIndex > 0)
          {
            targetRef = SpeckleStructuralClasses.Helper.CreateChildApplicationId(indices[i], Helper.GetApplicationId(memberKw, memberIndex));
          }
          else
          {
            targetRef = Helper.GetApplicationId(keyword, indices[i]);
          }
        }

        if (ResultObjectsByLoadCase(indices[i], targetRef, loadTaskKw, comboKw, out var resultObjectsByLoadCase))
        {
          lock (resultsLock)
          {
            foreach (var loadCase in resultObjectsByLoadCase.Keys)
            {
              results.Add(new GSA2DElementResult() { Value = resultObjectsByLoadCase[loadCase], GSAId = indices[i] });
            }
          }
        }
      }
#if !DEBUG
        );
#endif
      Initialiser.AppResources.Proxy.ClearResults(ResultGroup.Element2d);

      if (results.Count() > 0)
      {
        Initialiser.GsaKit.GSASenderObjects.AddRange(results);
      }

      return true;
    }

    private static bool ResultObjectsByLoadCase(int elementIndex, string applicationId, string loadTaskKw, string comboKw,
      out Dictionary<string, Structural2DElementResult> resultObjectsByLoadCase)
    {
      resultObjectsByLoadCase = new Dictionary<string, Structural2DElementResult>();

      if (Initialiser.AppResources.Proxy.GetResultHierarchy(ResultGroup.Element2d, elementIndex, out var results) && results != null)
      {
        var orderedLoadCases = results.Keys.OrderBy(k => k).ToList();
        foreach (var loadCase in orderedLoadCases)
        {
          var newResult = new Structural2DElementResult()
          {
            IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
            TargetRef = applicationId,
            Value = results[loadCase]
          };
          var loadCaseRef = SchemaConversion.Helper.GsaCaseToRef(loadCase, loadTaskKw, comboKw);
          if (!string.IsNullOrEmpty(loadCaseRef))
          {
            newResult.LoadCaseRef = loadCase;
          }
          newResult.GenerateHash();
          resultObjectsByLoadCase.Add(loadCase, newResult);
        }
      }
      
      return true;
    }


  }
}
