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

      if (Initialiser.AppResources.Settings.Element2DResults.Count() == 0
        || (Initialiser.AppResources.Settings.StreamSendConfig == StreamContentConfig.ModelWithEmbeddedResults
          && Initialiser.GsaKit.GSASenderObjects.Count<GSA2DElement>() == 0))
      {
        return new SpeckleNull();
      }

      var axisStr = Initialiser.AppResources.Settings.ResultInLocalAxis ? "local" : "global";
      var typeName = dummyObject.GetType().Name;

      if (Initialiser.AppResources.Settings.StreamSendConfig == StreamContentConfig.ModelWithEmbeddedResults)
      {
        Embed2DResults(typeName, axisStr, kw, loadTaskKw, comboKw);
      }
      else
      {
        if (!Create2DElementResultObjects(typeName, axisStr, kw, loadTaskKw, comboKw))
        {
          return new SpeckleNull();
        }
      }

      return new SpeckleObject();
    }

    private static void Embed2DResults(string typeName, string axisStr, string keyword, string loadTaskKw, string comboKw)
    {
      //Meshes aren't included as we only need quads and triangle *elements* here
      var elements = Initialiser.GsaKit.GSASenderObjects.Get<GSA2DElement>();

      var entities = elements.Cast<GSA2DElement>().ToList();
      var globalAxis = !Initialiser.AppResources.Settings.ResultInLocalAxis;

      //Assume the columns are the same for the data output for all entities
      var rtColInfos = new Dictionary<string, ResultTypeColumnInfo>();
      var orderedResultTypes = new Dictionary<string, List<string>>();

      foreach (var e in entities)
      {
        var i = e.GSAId;
        var obj = e.Value;

        if (ResultObjectsByLoadCase(keyword, i, obj.ApplicationId, loadTaskKw, comboKw, rtColInfos, out var resultObjectsByLoadCase))
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
    }

    private static bool Create2DElementResultObjects(string typeName, string axisStr, string keyword, string loadTaskKw, string comboKw)
    {
      var results = new List<GSA2DElementResult>();

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

      var rtColInfos = new Dictionary<string, ResultTypeColumnInfo>();

      for (var i = 0; i < indices.Count(); i++)
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

        if (ResultObjectsByLoadCase(keyword, i, targetRef, loadTaskKw, comboKw, rtColInfos, out var resultObjectsByLoadCase))
        {
          foreach (var loadCase in resultObjectsByLoadCase.Keys)
          {
            results.Add(new GSA2DElementResult() { Value = resultObjectsByLoadCase[loadCase], GSAId = indices[i] });
          }
        }
      }

      if (results.Count() > 0)
      {
        Initialiser.GsaKit.GSASenderObjects.AddRange(results);
      }

      return true;
    }

    /*
    private static bool Create2DElementResultObjects_old(string typeName, string axisStr)
    {
      var results = new List<GSA2DElementResult>();

      var keyword = typeof(GSA2DElement).GetGSAKeyword();
      var memberKw = typeof(GSA1DMember).GetGSAKeyword();

      //Unlike embedding, separate results doesn't necessarily mean that there is a Speckle object created for each 2d element.  There is always though
      //some GWA loaded into the cache
      if (!Initialiser.AppResources.Cache.GetKeywordRecordsSummary(keyword, out var gwa, out var indices, out var applicationIds))
      {
        return false;
      }

      foreach (var kvp in Initialiser.AppResources.Settings.Element2DResults)
      {
        foreach (var loadCase in Initialiser.AppResources.Settings.ResultCases.Where(rc => Initialiser.AppResources.Proxy.CaseExist(rc)))
        {
          for (var i = 0; i < indices.Count(); i++)
          {
            try
            {
              var record = gwa[i];

              var pPieces = record.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
              if ((pPieces[4].ParseElementNumNodes() != 3 && pPieces[4].ParseElementNumNodes() != 4) || indices[i] == 0)
              {
                continue;
              }

              var resultExport = Initialiser.AppResources.Proxy.GetGSAResult(indices[i], kvp.Value.ResHeader, kvp.Value.Flags, kvp.Value.Keys, loadCase, axisStr);

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

              var existingRes = results.FirstOrDefault(x => x.Value.TargetRef == targetRef && x.Value.LoadCaseRef == loadCase);

              if (existingRes == null)
              {
                var newRes = new Structural2DElementResult()
                {
                  Value = new Dictionary<string, object>(),
                  TargetRef = targetRef,
                  IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
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
            catch (Exception ex)
            {
              var contextDesc = string.Join(" ", typeName, kvp.Key, loadCase);
              Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex, contextDesc, i.ToString());
            }
          }
        }
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(results);

      return true;
    }

    private static void Embed2DResults_old(string typeName, string axisStr)
    {
      var elements = Initialiser.GsaKit.GSASenderObjects.Get<GSA2DElement>();

      foreach (var kvp in Initialiser.AppResources.Settings.Element2DResults)
      {
        foreach (var loadCase in Initialiser.AppResources.Settings.ResultCases.Where(rc => Initialiser.AppResources.Proxy.CaseExist(rc)))
        {
          foreach (var element in elements)
          {
            var id = element.GSAId;
            var obj = element.Value;

            try
            {
              if (obj.Result == null)
              {
                obj.Result = new Dictionary<string, object>();
              }
              var resultExport = Initialiser.AppResources.Proxy.GetGSAResult(id, kvp.Value.ResHeader, kvp.Value.Flags, kvp.Value.Keys, loadCase, axisStr);

              if (resultExport == null || resultExport.Count() == 0)
              {
                continue;
              }

              var newResult = new Structural2DElementResult()
              {
                TargetRef = obj.ApplicationId,
                Value = new Dictionary<string, object>(),
                IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
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
            catch (Exception ex)
            {
              var contextDesc = string.Join(" ", typeName, kvp.Key, loadCase);
              Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex, contextDesc, id.ToString());
            }
          }
        }
      }
    }
    */

    private struct ResultTypeColumnInfo
    {
      public List<int> relevantColIndices;
      public List<string> fields;
      public int numFields;
      public int rIndex;
      public int sIndex;
    }

    private static bool ResultObjectsByLoadCase(string keyword, int elementIndex, string applicationId, string loadTaskKw, string comboKw,
      Dictionary<string, ResultTypeColumnInfo> rtColInfos, out Dictionary<string, Structural2DElementResult> resultObjectsByLoadCase)
    {
      resultObjectsByLoadCase = new Dictionary<string, Structural2DElementResult>();

      var getResults = Initialiser.AppResources.Proxy.GetResults(keyword, elementIndex, out var data, 2);
      if (getResults)
      {
        var faceData = new Dictionary<string, Tuple<List<string>, object[,]>>();
        var vertexData = new Dictionary<string, Tuple<List<string>, object[,]>>();
        foreach (var rt in data.Keys)
        {
          ResultTypeColumnInfo rtColInfo;
          //Just determine the column indices on the first run and assume the same for all entities
          if (rtColInfos.ContainsKey(rt))
          {
            rtColInfo = rtColInfos[rt];
          }
          else
          {
            rtColInfo = new ResultTypeColumnInfo();
            for (var f = 0; f < data[rt].Item1.Count(); f++)
            {
              if (data[rt].Item1[f].Equals("position_r"))
              {
                rtColInfo.rIndex = f;
              }
              else if (data[rt].Item1[f].Equals("position_s"))
              {
                rtColInfo.sIndex = f;
              }
            }

            rtColInfo.relevantColIndices = Enumerable.Range(0, data[rt].Item1.Count()).Except(new[] { rtColInfo.rIndex, rtColInfo.sIndex }).ToList();
            rtColInfo.fields = rtColInfo.relevantColIndices.Select(f => data[rt].Item1[f]).ToList();
            rtColInfo.numFields = rtColInfo.relevantColIndices.Count();
            rtColInfos.Add(rt, rtColInfo);
          }

          var faceRowIndices = new HashSet<int>();
          var vertexRowIndices = new HashSet<int>();
          var totalNumRows = data[rt].Item2.GetLength(0);
          for (var r = 0; r < totalNumRows; r++)
          {
            var pos_r = data[rt].Item2[r, rtColInfo.rIndex];
            if (pos_r != null && pos_r is double && (double)pos_r > 0.1 && (double)pos_r < 0.9) //The intent here is to compare with 0 and 1 but avoid floating point issues
            {
              faceRowIndices.Add(r);
            }
            else
            {
              vertexRowIndices.Add(r);
            }
          }

          //Create two new bucket of data - one for face, one for vertex data - but assume the same columns for each
          var faceTable = new object[faceRowIndices.Count(), rtColInfo.numFields];
          var vertexTable = new object[vertexRowIndices.Count(), rtColInfo.numFields];
          var faceRowIndex = 0;
          var vertexRowIndex = 0;
          for (var r = 0; r < totalNumRows; r++)
          {
            var colIndex = 0;
            if (faceRowIndices.Contains(r))
            {
              foreach (var c in rtColInfo.relevantColIndices)
              {
                faceTable[faceRowIndex, colIndex++] = data[rt].Item2[r, c];
              }
              faceRowIndex++;
            }
            else
            {
              foreach (var c in rtColInfo.relevantColIndices)
              {
                vertexTable[vertexRowIndex, colIndex++] = data[rt].Item2[r, c];
              }
              vertexRowIndex++;
            }
          }
          if (faceRowIndex > 0) //This check is a proxy for whether any face data at all has been found
          {
            faceData.Add(rt + "_face", new Tuple<List<string>, object[,]>(rtColInfo.fields, faceTable));
          }
          if (vertexRowIndex > 0) //This check is a proxy for whether any vertex data at all has been found
          {
            vertexData.Add(rt + "_vertex", new Tuple<List<string>, object[,]>(rtColInfo.fields, vertexTable));
          }
        }

        var resultsFace = SchemaConversion.Helper.GetSpeckleResultHierarchy(faceData, false);
        var resultsVertex = SchemaConversion.Helper.GetSpeckleResultHierarchy(vertexData, false);
        if (resultsFace != null && resultsVertex != null)
        {
          //merge the results
          var orderedLoadCases = resultsFace.Keys.Union(resultsVertex.Keys).OrderBy(k => k).ToList();
          var results = new Dictionary<string, Dictionary<string, object>>();

          foreach (var loadCase in orderedLoadCases)
          {
            //Interlace the face and vertex result types
            var faceRtIndex = 0;
            var vertexRtIndex = 0;
            var maxNumRts = Math.Max(resultsFace.ContainsKey(loadCase) ? resultsFace[loadCase].Keys.Count : 0,
              resultsVertex.ContainsKey(loadCase) ? resultsVertex[loadCase].Keys.Count : 0);

            results.Add(loadCase, new Dictionary<string, object>());

            for (var rtIndex = 0; rtIndex < maxNumRts; rtIndex++)
            {
              if (faceRtIndex < resultsFace[loadCase].Count)
              {
                var rt = resultsFace[loadCase].Keys.ElementAt(faceRtIndex++);
                results[loadCase].Add(rt, resultsFace[loadCase][rt]);
              }
              if (vertexRtIndex < resultsVertex[loadCase].Count)
              {
                var rt = resultsVertex[loadCase].Keys.ElementAt(vertexRtIndex++);
                results[loadCase].Add(rt, resultsVertex[loadCase][rt]);
              }
            }
          }

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
      }

      return true;
    }

    
  }
}
