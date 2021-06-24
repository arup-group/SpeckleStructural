using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA
{
  //Because the application ID could come from the member (if the element is derived from a parent member)"
  // - GSA1DMember is also listed as a read prerequisite
  // - MEMB.8 is listed as a subkeyword
  [GSAObject("", new string[] { "EL.4", "MEMB.8" }, "results", true, false, new Type[] { typeof(GSA1DElement), typeof(GSA1DMember) }, new Type[] { })]
  public class GSA1DElementResult : GSABase<Structural1DElementResult>
  {
  }

  public static partial class Conversions
  {
    public static SpeckleObject ToSpeckle(this GSA1DElementResult dummyObject)
    {
      if (Initialiser.AppResources.Settings.Element1DResults.Count() == 0
        || Initialiser.AppResources.Settings.EmbedResults && Initialiser.GsaKit.GSASenderObjects.Count<GSA1DElement>() == 0)
      {
        return new SpeckleNull();
      }

      var kw = GsaRecord.GetKeyword<GsaEl>();
      var loadTaskKw = GsaRecord.GetKeyword<GsaLoadCase>();
      var comboKw = GsaRecord.GetKeyword<GsaCombination>();

      var axisStr = Initialiser.AppResources.Settings.ResultInLocalAxis ? "local" : "global";
      var num1dPos = Initialiser.AppResources.Settings.Result1DNumPosition;
      var typeName = dummyObject.GetType().Name;

      if (Initialiser.AppResources.Settings.EmbedResults)
      {
        Embed1DResults(typeName, axisStr, num1dPos, kw, loadTaskKw, comboKw);
      }
      else
      {
        if (!Create1DElementResultObjects(typeName, axisStr, num1dPos, loadTaskKw, comboKw))
        {
          return new SpeckleNull();
        }
      }

      return new SpeckleObject();
    }

    private static bool Create1DElementResultObjects(string typeName, string axisStr, int num1dPos, string loadTaskKw, string comboKw)
    {
      var gsaResults = new List<GSA1DElementResult>();
      var memberKw = typeof(GSA1DMember).GetGSAKeyword();
      var keyword = typeof(GSA1DElement).GetGSAKeyword();
      var globalAxis = !Initialiser.AppResources.Settings.ResultInLocalAxis;

      //Unlike embedding, separate results doesn't necessarily mean that there is a Speckle object created for each 1d element.  There is always though
      //some GWA loaded into the cache
      if (!Initialiser.AppResources.Cache.GetKeywordRecordsSummary(keyword, out var gwa, out var indices, out var applicationIds))
      {
        return false;
      }

      for (int i = 0; i < indices.Count(); i++)
      {
        var entity = indices[i];
        var applicationId = applicationIds[i];

        try
        {
          var pPieces = gwa[i].ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
          if (pPieces[4].ParseElementNumNodes() != 2 || entity == 0)
          {
            continue;
          }

          var getResults = Initialiser.AppResources.Proxy.GetResults(keyword, entity, out var data);

          var results = SchemaConversion.Helper.GetSpeckleResultHierarchy(data, false);
          if (results != null)
          {
            var orderedLoadCases = results.Keys.OrderBy(k => k).ToList();
            foreach (var loadCase in orderedLoadCases)
            {
              var elem1dResult = new Structural1DElementResult()
              {
                IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
                Value = results[loadCase],
                TargetRef = applicationId
              };
              var loadCaseRef = SchemaConversion.Helper.GsaCaseToRef(loadCase, loadTaskKw, comboKw);
              if (!string.IsNullOrEmpty(loadCaseRef))
              {
                elem1dResult.LoadCaseRef = loadCase;
              }

              Initialiser.GsaKit.GSASenderObjects.Add(new GSA1DElementResult { Value = elem1dResult, GSAId = entity });
            }
          }

        }
        catch (Exception ex)
        {
          var contextDesc = string.Join(" ", typeName, entity);
          Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex, contextDesc, i.ToString());
        }
      }
      
      return true;
    }

    private static void Embed1DResults(string typeName, string axisStr, int num1dPos, string keyword, string loadTaskKw, string comboKw)
    {
      var elements = Initialiser.GsaKit.GSASenderObjects.Get<GSA1DElement>();

      var entities = elements.Cast<GSA1DElement>().ToList();
      var globalAxis = !Initialiser.AppResources.Settings.ResultInLocalAxis;

      foreach (var e in entities)
      {
        var i = e.GSAId;
        var obj = e.Value;
        var getResults = Initialiser.AppResources.Proxy.GetResults(keyword, i, out var data);
        var results = SchemaConversion.Helper.GetSpeckleResultHierarchy(data, false);
        if (results != null)
        {
          var orderedLoadCases = results.Keys.OrderBy(k => k).ToList();
          foreach (var loadCase in orderedLoadCases)
          {
            var nodeResult = new Structural1DElementResult()
            {
              IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
              TargetRef = obj.ApplicationId,
              Value = results[loadCase]
            };
            var loadCaseRef = SchemaConversion.Helper.GsaCaseToRef(loadCase, loadTaskKw, comboKw);
            if (!string.IsNullOrEmpty(loadCaseRef))
            {
              nodeResult.LoadCaseRef = loadCase;
            }
            if (obj.Result == null)
            {
              //Can't just allocate an empty dictionary as the Result set property won't allow it
              obj.Result = new Dictionary<string, object>() { { loadCase, nodeResult } };
            }
            else
            {
              obj.Result.Add(loadCase, nodeResult);
            }
          }
        }
      }

      // Linear interpolate the line values
      foreach (var entity in entities)
      {
        var obj = entity.Value;

        var dX = (obj.Value[3] - obj.Value[0]) / (Initialiser.AppResources.Settings.Result1DNumPosition + 1);
        var dY = (obj.Value[4] - obj.Value[1]) / (Initialiser.AppResources.Settings.Result1DNumPosition + 1);
        var dZ = (obj.Value[5] - obj.Value[2]) / (Initialiser.AppResources.Settings.Result1DNumPosition + 1);

        var interpolatedVertices = new List<double>();
        interpolatedVertices.AddRange(obj.Value.Take(3));

        for (var i = 1; i <= Initialiser.AppResources.Settings.Result1DNumPosition; i++)
        {
          interpolatedVertices.Add(interpolatedVertices[0] + dX * i);
          interpolatedVertices.Add(interpolatedVertices[1] + dY * i);
          interpolatedVertices.Add(interpolatedVertices[2] + dZ * i);
        }

        interpolatedVertices.AddRange(obj.Value.Skip(3).Take(3));

        obj.ResultVertices = interpolatedVertices;
      }
    }
  }
}
