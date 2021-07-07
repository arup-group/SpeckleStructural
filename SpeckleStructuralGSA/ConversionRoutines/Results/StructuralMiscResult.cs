using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA
{
  [GSAObject("", new string[] { "ASSEMBLY.3" }, "results", true, false, new Type[] { typeof(GSAAssembly) }, new Type[] { })]
  public class GSAMiscResult : GSABase<StructuralMiscResult>
  {
  }

  public static partial class Conversions
  {
    public static SpeckleObject ToSpeckle(this GSAMiscResult dummyObject)
    {
      var keyword = typeof(GSAAssembly).GetGSAKeyword();
      var loadTaskKw = GsaRecord.GetKeyword<GsaLoadCase>();
      var comboKw = GsaRecord.GetKeyword<GsaCombination>();
      var typeName = dummyObject.GetType().Name;
      var axisStr = Initialiser.AppResources.Settings.ResultInLocalAxis ? "local" : "global";

      var resultTypes = Initialiser.AppResources.Settings.MiscResults.Keys.Where(r => r.ToLower().Contains("assembly")).ToList();
      var cases = Initialiser.AppResources.Settings.ResultCases;

      if (resultTypes.Count() == 0 
        || !Initialiser.AppResources.Cache.GetKeywordRecordsSummary(keyword, out var gwa, out var indices, out var applicationIds))
      {
        return new SpeckleNull();
      }

      var gsaMiscResults = new List<GSAMiscResult>();
      var gsaMiscResultsLock = new object();

      Initialiser.AppResources.Proxy.LoadResults(resultTypes, cases, indices);

#if DEBUG
      for (int i = 0; i < indices.Count(); i++)
#else
      Parallel.For(0, indices.Count, i =>
#endif
      {
        var entity = indices[i];
        var applicationId = applicationIds[i];

        try
        {
          var getResults = Initialiser.AppResources.Proxy.GetResults(keyword, entity, out var data);

          var results = SchemaConversion.Helper.GetSpeckleResultHierarchy(data, false);
          if (results != null)
          {
            var orderedLoadCases = results.Keys.OrderBy(k => k).ToList();
            foreach (var loadCase in orderedLoadCases)
            {
              var miscResult = new StructuralMiscResult()
              {
                IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
                Value = results[loadCase],
                TargetRef = applicationId
              };
              var loadCaseRef = SchemaConversion.Helper.GsaCaseToRef(loadCase, loadTaskKw, comboKw);
              if (!string.IsNullOrEmpty(loadCaseRef))
              {
                miscResult.LoadCaseRef = loadCase;
              }

              lock (gsaMiscResultsLock)
              {
                gsaMiscResults.Add(new GSAMiscResult { Value = miscResult, GSAId = entity });
              }
            }
          }

        }
        catch (Exception ex)
        {
          var contextDesc = string.Join(" ", typeName, entity);
          Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex, contextDesc, i.ToString());
        }
      }
#if !DEBUG
      );
#endif
      Initialiser.AppResources.Proxy.ClearResults(resultTypes);

      if (gsaMiscResults.Count > 0)
      {
        Initialiser.GsaKit.GSASenderObjects.AddRange(gsaMiscResults);
      }

      //Initialiser.GsaKit.GSASenderObjects.AddRange(gsaMiscResults);

      return new SpeckleObject();
    }
  }
}
