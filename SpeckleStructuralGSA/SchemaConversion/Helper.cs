using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class Helper
  {
    public static List<AxisDirection6> AxisDirs = Enum.GetValues(typeof(AxisDirection6)).Cast<AxisDirection6>().Where(v => v != AxisDirection6.NotSet).ToList();

    /*
    //For insertion into the Result.Value property
    // [ load case [ result type [ column [ values ] ] ] ]
    public static Dictionary<string, Dictionary<string, object>> GetSpeckleResultHierarchy(Dictionary<string, Tuple<List<string>, object[,]>> data,
      bool simplifySingleItemLists = true, string elementIdCol = "id", string caseCol = "case_id")
    {
      //This stores ALL the data in this one pass
      var value = new Dictionary<string, Dictionary<string, object>>();
      //This stores where there is at least one non-zero/null/"null" value in the whole result type, across all columns
      var sendableValues = new Dictionary<string, Dictionary<string, bool>>();
      //This stores the number of values in each column: [ load case [ result type [ col, num values ] ] ]
      var numColValues = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();

      //This loop has been designed with the intention that the data is traversed *once*

      //Each result type (e.g. "Nodal Velocity")
      foreach (var rt in data.Keys)
      {
        int caseColIndex = data[rt].Item1.IndexOf(caseCol);
        int elementIdColIndex = data[rt].Item1.IndexOf(elementIdCol);
        for (var r = 0; r < data[rt].Item2.GetLength(0); r++)
        {
          var loadCase = data[rt].Item2[r, caseColIndex].ToString();
          if (!value.Keys.Contains(loadCase))
          {
            value.Add(loadCase, new Dictionary<string, object>());
          }
          if (!value[loadCase].ContainsKey(rt))
          {
            value[loadCase].Add(rt, new Dictionary<string, object>());
          }
          foreach (var c in Enumerable.Range(0, data[rt].Item1.Count()).Except(new[] { elementIdColIndex, caseColIndex }))
          {
            var col = data[rt].Item1[c];
            var val = data[rt].Item2[r, c];
            if (!((Dictionary<string, object>)value[loadCase][rt]).ContainsKey(col))
            {
              ((Dictionary<string, object>)value[loadCase][rt]).Add(col, new List<object>());
            }
            ((List<object>)((Dictionary<string, object>)value[loadCase][rt])[col]).Add(val);
            if (!sendableValues.ContainsKey(loadCase))
            {
              sendableValues.Add(loadCase, new Dictionary<string, bool>());
            }
            var sendable = SendableValue(val);
            if (!sendableValues[loadCase].ContainsKey(rt))
            {
              sendableValues[loadCase].Add(rt, sendable);
            }
            else if (!sendableValues[loadCase][rt])
            {
              sendableValues[loadCase][rt] = sendable;
            }
            if (!numColValues.ContainsKey(loadCase))
            {
              numColValues.Add(loadCase, new Dictionary<string, Dictionary<string, int>>());
            }
            if (!numColValues[loadCase].ContainsKey(rt))
            {
              numColValues[loadCase].Add(rt, new Dictionary<string, int>());
            }
            if (!numColValues[loadCase][rt].ContainsKey(col))
            {
              numColValues[loadCase][rt].Add(col, 1);
            }
            else
            {
              numColValues[loadCase][rt][col]++;
            }
          }
        }
      }

      var retValue = new Dictionary<string, Dictionary<string, object>>();
      foreach (var loadCase in sendableValues.Keys)
      {
        foreach (var rt in sendableValues[loadCase].Keys.Where(k => sendableValues[loadCase][k]))
        {
          if (!retValue.ContainsKey(loadCase))
          {
            retValue.Add(loadCase, new Dictionary<string, object>());
          }
          foreach (var col in ((Dictionary<string, object>)value[loadCase][rt]).Keys)
          {
            var colValues = ((List<object>)((Dictionary<string, object>)value[loadCase][rt])[col]);
          }
          retValue[loadCase].Add(rt, value[loadCase][rt]);
        }
      }

      if (simplifySingleItemLists)
      {
        foreach (var loadCase in retValue.Keys)
        {
          foreach (var rt in retValue[loadCase].Keys)
          {
            var singleValueCols = ((Dictionary<string, object>)retValue[loadCase][rt]).Keys.Where(k => numColValues[loadCase][rt][k] == 1).ToList();
            foreach (var col in singleValueCols)
            {
              ((Dictionary<string, object>)retValue[loadCase][rt])[col] = ((List<object>)((Dictionary<string, object>)value[loadCase][rt])[col]).First();
            }
          }
        }
      }

      return retValue;
    }
    */

    // [ result_type [ column [ values ] ] ]
    public static bool FilterResults(Dictionary<string, object> inValue, out Dictionary<string, object> outValue)
    {
      outValue = new Dictionary<string, object>();

      foreach (var rt in inValue.Keys)
      {
        var d = (Dictionary<string, object>)inValue[rt];

        var sendable = false;
        foreach (var c in d.Keys)
        {
          var colValues = (List<object>)d[c];

          foreach (var v in colValues)
          {
            if ((v is float && (float)v != 0) || (v is float? && ((float?)v).HasValue && (float?)v != 0))
            {
              sendable = true;
              break;
            }
          }
          if (sendable)
          {
            break;
          }
        }
        if (sendable)
        {
          outValue.Add(rt, inValue[rt]);
        }
      }

      return (outValue.Count > 0);
    }

    /*
    private static bool SendableValue(object v)
    {
      if (v == null)
      {
        return false;
      }
      if (v is int)
      {
        return ((int)v != 0);
      }
      else if (v is double)
      {
        return ((double)v != 0);
      }
      else if (v is string)
      {
        return (!string.IsNullOrEmpty((string)v) && !((string)v).Equals("null", StringComparison.InvariantCultureIgnoreCase));
      }
      return true;
    }
    */

    //This is necessary because SpeckleCore swallows exceptions thrown within ToNative methods
    public static string ToNativeTryCatch(SpeckleObject so, Func<object> toNativeMethod)
    {
      if (so == null)
      {
        return "";
      }

      var retValue = "";
      var streamId = "";
      var speckleType = "";
      var id = "";
      var url = "";

      try
      {
        if (so.Properties.ContainsKey("StreamId"))
        {
          streamId = so.Properties["StreamId"].ToString();
        }
        speckleType = so.Type;
        id = so._id;
        url = Initialiser.AppResources.Settings.ObjectUrl(id);
      }
      catch { }

      //In case of error
      var errContext = new List<string>() { "Receive", "StreamId=" + streamId, "SpeckleType=" + speckleType,
        "ApplicationId=" + (string.IsNullOrEmpty(so.ApplicationId) ? "" : so.ApplicationId),
        "_id=" + id, "Url=" + url };

      try
      {
        var methodReturnValue = toNativeMethod();

        if (methodReturnValue is string)
        {
          retValue = (string)methodReturnValue;
        }
        else if (methodReturnValue is Exception)
        {
          throw ((Exception)methodReturnValue);
        }
        else
        {
          throw new Exception("Unexpected type returned by conversion code: " + methodReturnValue.GetType().Name);
        }
      }
      catch (Exception ex)
      {
        //These messages will have more information added to them in the app before they are logged to file
        Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex, errContext.ToArray());
      }
      return retValue;
    }

    public static SpeckleObject ToSpeckleTryCatch(string keyword, int index, Func<SpeckleObject> toSpeckleMethod)
    {
      try
      {
        return toSpeckleMethod();
      }
      catch (Exception ex)
      {
        //These messages will have more information added to them in the app before they are logged to file
        Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex, "Keyword=" + keyword, "Index=" + index);
      }
      return new SpeckleNull();
    }

    #region loading
    //public static readonly AxisDirection6[] loadDirSeq = new AxisDirection6[] { AxisDirection6.X, AxisDirection6.Y, AxisDirection6.Z, AxisDirection6.XX, AxisDirection6.YY, AxisDirection6.ZZ };

    public static bool IsValidLoading(StructuralVectorSix loading)
    {
      return (loading != null && loading.Value != null && loading.Value.Count() == 6 && loading.Value.Any(v => v != 0));
    }

    public static Dictionary<AxisDirection6, double> ExplodeLoading(StructuralVectorSix loading)
    {
      var valueByDir = new Dictionary<AxisDirection6, double>();
      var loadDirSeq = Enum.GetValues(typeof(AxisDirection6)).Cast<AxisDirection6>().Where(v => v != AxisDirection6.NotSet).ToList();

      for (var i = 0; i < loadDirSeq.Count(); i++)
      {
        if (loading.Value[i] != 0)
        {
          valueByDir.Add(loadDirSeq[i], loading.Value[i]);
        }
      }

      return valueByDir;
    }
    #endregion

    public static void AddCustomStructuralProperty(SpeckleObject obj, string key, object value)
    {
      if (!obj.Properties.ContainsKey("structural"))
      {
        obj.Properties.Add("structural", new Dictionary<string, object>());
      }
      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeId", value);
    }

    public static bool ValidateCoordinates(List<double> coords, out List<int> nodeIndices)
    {
      nodeIndices = new List<int>();
      for (var i = 0; i < coords.Count(); i += 3)
      {
        var nodeIndex = Initialiser.AppResources.Proxy.NodeAt(coords[i], coords[i + 1], coords[i + 2], Initialiser.AppResources.Settings.CoincidentNodeAllowance);
        if (!nodeIndices.Contains(nodeIndex))
        {
          nodeIndices.Add(nodeIndex);
        }
      }
      return (nodeIndices.Count() > 1);
    }

    public static bool IsZeroAxis(StructuralAxis axis)
    {
      var bp = axis.basePlane;
      var zeroVector = new double[3] { 0, 0, 0 };
      return ((bp.Xdir == null && bp.Ydir == null) 
        || (bp.Xdir.Value.SequenceEqual(zeroVector) && bp.Ydir.Value.SequenceEqual(zeroVector)));
    }

    public static string GridExpansionToString(GridExpansion expansion)
    {
      switch(expansion)
      {
        case GridExpansion.PlaneAspect: return "PLANE_ASPECT";
        case GridExpansion.PlaneSmooth: return "PLANE_SMOOTH";
        case GridExpansion.PlaneCorner: return "PLANE_CORNER";
        default: return "LEGACY";
      }
    }

    public static GridExpansion StringToGridExpansion(string expansion)
    {
      switch (expansion)
      {
        case "PLANE_ASPECT": return GridExpansion.PlaneAspect;
        case "PLANE_SMOOTH":  return GridExpansion.PlaneSmooth;
        case "PLANE_CORNER": return GridExpansion.PlaneCorner;
        default: return GridExpansion.Legacy;
      }
    }

    public static StructuralLoadCaseType StringToLoadCaseType(string type)
    {
      switch (type)
      {
        case "DEAD":
        case "LC_PERM_SELF": 
          return StructuralLoadCaseType.Dead;
        case "LC_VAR_IMP": return StructuralLoadCaseType.Live;
        case "WIND": return StructuralLoadCaseType.Wind;
        case "SNOW": return StructuralLoadCaseType.Snow;
        case "SEISMIC": return StructuralLoadCaseType.Earthquake;
        case "LC_PERM_SOIL": return StructuralLoadCaseType.Soil;
        case "LC_VAR_TEMP": return StructuralLoadCaseType.Thermal;
        default: return StructuralLoadCaseType.Generic;
      }
    }

    public static string LoadCaseTypeToString(StructuralLoadCaseType caseType)
    {
      switch (caseType)
      {
        case StructuralLoadCaseType.Dead: return ("LC_PERM_SELF");
        case StructuralLoadCaseType.Live: return ("LC_VAR_IMP");
        case StructuralLoadCaseType.Wind: return ("WIND");
        case StructuralLoadCaseType.Snow: return ("SNOW");
        case StructuralLoadCaseType.Earthquake: return ("SEISMIC");
        case StructuralLoadCaseType.Soil: return ("LC_PERM_SOIL");
        case StructuralLoadCaseType.Thermal: return ("LC_VAR_TEMP");
        default: return ("LC_UNDEF");
      }
    }

    public static StructuralVectorSix AxisDirDictToStructuralVectorSix(Dictionary<AxisDirection6, double> d)
    {
      if (d == null || d.Keys.Count() == 0)
      {
        return null;
      }
      var v = new StructuralVectorSix();
      var values = new double[6];
      for (int i = 0; i < AxisDirs.Count(); i++)
      {
        if (d.ContainsKey(AxisDirs[i]))
        {
          values[i] = d[AxisDirs[i]];
        }
      }
      v.Value = values.ToList();
      return v;
    }

    public static StructuralVectorBoolSix AxisDirDictToStructuralVectorBoolSix(List<AxisDirection6> l)
    {
      if (l == null || l.Count() == 0)
      {
        return null;
      }
      var v = new StructuralVectorBoolSix();
      var values = new bool[6];
      for (int i = 0; i < AxisDirs.Count(); i++)
      {
        values[i] = l.Contains(AxisDirs[i]);
      }
      v.Value = values.ToList();
      return v;
    }

    public static List<U> GetNewFromCache<T, U>() where U : GsaRecord  //T = old type, U = new schema type
    {
      //Convert all raw GWA into GSA schema objects
      var keyword = typeof(T).GetGSAKeyword().Split('.').First();
      var newLines = Initialiser.AppResources.Cache.GetGwaToSerialise(keyword);
      var schemaObjs = new List<U>();
      foreach (var index in newLines.Keys)
      {
        var obj = (GsaRecord)Activator.CreateInstance(typeof(U));
        obj.Index = index;

        if (!obj.FromGwa(newLines[index]))
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, typeof(U).Name + ": Unable to parse GWA", index.ToString());
        }
        schemaObjs.Add((U)obj);
      }
      return schemaObjs;
    }

    public static List<U> GetAllFromCache<T, U>() where U : GsaRecord  //T = old type, U = new schema type
    {
      //Convert all raw GWA into GSA schema objects
      Initialiser.AppResources.Cache.GetKeywordRecordsSummary(typeof(T).GetGSAKeyword(), out var gwa, out var indices, out var appIds);
      var schemaObjs = new List<U>();
      for (var i = 0; i < gwa.Count(); i++)
      {
        var index = indices[i];

        var obj = (GsaRecord)Activator.CreateInstance(typeof(U));
        obj.Index = index;

        if (!obj.FromGwa(gwa[i]))
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, typeof(U).Name + ": Unable to parse GWA", index.ToString());
        }
        schemaObjs.Add((U)obj);
      }
      return schemaObjs;
    }

    public static StructuralVectorSix GsaLoadToLoading(AxisDirection6 ld, double value)
    {
      switch (ld)
      {
        case AxisDirection6.X: return new StructuralVectorSix(value, 0, 0, 0, 0, 0);
        case AxisDirection6.Y: return new StructuralVectorSix(0, value, 0, 0, 0, 0);
        case AxisDirection6.Z: return new StructuralVectorSix(0, 0, value, 0, 0, 0);
        case AxisDirection6.XX: return new StructuralVectorSix(0, 0, 0, value, 0, 0);
        case AxisDirection6.YY: return new StructuralVectorSix(0, 0, 0, 0, value, 0);
        case AxisDirection6.ZZ: return new StructuralVectorSix(0, 0, 0, 0, 0, value);
        default: return null;
      }
    }

    public static string GsaCaseToRef(string loadCase, string loadTaskKw, string comboKw)
    {
      string loadCaseRef = null;
      if (int.TryParse(loadCase.Substring(1), out int loadCaseIndex) && loadCaseIndex > 0)
      {
        if (loadCase.StartsWith("a", System.StringComparison.InvariantCultureIgnoreCase))
        {
          loadCaseRef = SpeckleStructuralGSA.Helper.GetApplicationId(loadTaskKw, loadCaseIndex);
        }
        else if (loadCase.StartsWith("c", System.StringComparison.InvariantCultureIgnoreCase))
        {
          loadCaseRef = SpeckleStructuralGSA.Helper.GetApplicationId(comboKw, loadCaseIndex);
        }
      }
      return string.IsNullOrEmpty(loadCaseRef) ? loadCase : loadCaseRef;
    }

  }
}
