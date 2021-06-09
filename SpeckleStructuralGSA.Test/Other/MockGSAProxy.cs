using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Interop.Gsa_10_1;
using Moq;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  /*
  public static class MockGSAProxy
  {
    public delegate void ParseCallback(string fullGwa, out string keyword, out int? index, out string streamId, out string applicationId, out string gwaWithoutSet, out GwaSetCommandType? gwaSetCommandType, bool includeKwVersion = false);

    public static int nodeIndex = 0;

    //Copied over from the GSAProxy
    public static Dictionary<string, string[]> IrregularKeywordGroups = new Dictionary<string, string[]> {
      { "LOAD_BEAM", new string[] { "LOAD_BEAM_POINT", "LOAD_BEAM_UDL", "LOAD_BEAM_LINE", "LOAD_BEAM_PATCH", "LOAD_BEAM_TRILIN" } }
    };
    private static readonly string SID_APPID_TAG = "speckle_app_id";
    private static readonly string SID_STRID_TAG = "speckle_stream_id";

    public static int[] ConvertGSAList(string list, GSAEntity type)
    {
      var elements = list.Split(new[] { ' ' });

      var indices = new List<int>();
      foreach (var e in elements)
      {
        if (e.All(c => char.IsDigit(c)) && int.TryParse(e, out int index))
        {
          indices.Add(index);
        }
      }

      //It's assumed for now that any list of GSA indices that would correspond to the App IDs in the list would be a sequence from 1
      return indices.ToArray();
    }

    public static int NodeAt(double x, double y, double z, double coincidenceTol) => ++nodeIndex;

    public static string FormatApplicationIdSidTag(string value)
    {
      return (string.IsNullOrEmpty(value) ? "" : "{" + SID_APPID_TAG + ":" + value.Replace(" ", "") + "}");
    }

    public static string FormatStreamIdSidTag(string value)
    {
      return (string.IsNullOrEmpty(value) ? "" : "{" + SID_STRID_TAG + ":" + value.Replace(" ", "") + "}");
    }

    public static string FormatSidTags(string streamId = "", string applicationId = "")
    {
      return FormatStreamIdSidTag(streamId) + FormatApplicationIdSidTag(applicationId);
    }

    public static void ParseGeneralGwa(string fullGwa, out string keyword, out int? index, out string streamId, out string applicationId, out string gwaWithoutSet, out GwaSetCommandType? gwaSetCommandType, bool includeKwVersion = false)
    {
      var pieces = fullGwa.ListSplit(GSAProxy.GwaDelimiter).ToList();
      keyword = "";
      streamId = "";
      applicationId = "";
      index = null;
      gwaWithoutSet = fullGwa;
      gwaSetCommandType = null;

      if (pieces.Count() < 2)
      {
        return;
      }

      //Remove the Set for the purpose of this method
      if (pieces[0].StartsWith("set", StringComparison.InvariantCultureIgnoreCase))
      {
        if (pieces[0].StartsWith("set_at", StringComparison.InvariantCultureIgnoreCase))
        {
          gwaSetCommandType = GwaSetCommandType.SetAt;

          if (int.TryParse(pieces[1], out int foundIndex))
          {
            index = foundIndex;
          }

          //For SET_ATs the format is SET_AT <index> <keyword> .., so remove the first two
          pieces.Remove(pieces[1]);
          pieces.Remove(pieces[0]);
        }
        else
        {
          gwaSetCommandType = GwaSetCommandType.Set;
          if (int.TryParse(pieces[2], out int foundIndex))
          {
            index = foundIndex;
          }

          pieces.Remove(pieces[0]);
        }
      }
      else
      {
        if (int.TryParse(pieces[1], out var foundIndex))
        {
          index = foundIndex;
        }
      }

      var delimIndex = pieces[0].IndexOf(':');
      if (delimIndex > 0)
      {
        //An SID has been found
        keyword = pieces[0].Substring(0, delimIndex);
        var sidTags = pieces[0].Substring(delimIndex);
        var match = Regex.Match(sidTags, "(?<={" + SID_STRID_TAG + ":).*?(?=})");
        streamId = (!string.IsNullOrEmpty(match.Value)) ? match.Value : "";
        match = Regex.Match(sidTags, "(?<={" + SID_APPID_TAG + ":).*?(?=})");
        applicationId = (!string.IsNullOrEmpty(match.Value)) ? match.Value : "";
      }
      else
      {
        keyword = pieces[0];
      }

      foreach (var groupKeyword in IrregularKeywordGroups.Keys)
      {
        if (IrregularKeywordGroups[groupKeyword].Contains(keyword))
        {
          keyword = groupKeyword;
          break;
        }
      }

      if (!includeKwVersion)
      {
        keyword = keyword.Split('.').First();
      }

      gwaWithoutSet = string.Join(GSAProxy.GwaDelimiter.ToString(), pieces);
      return;
    }
  }
  */

  public class TestProxy : GSAProxy, IGSAProxy
  {
    private readonly Dictionary<int, List<double>> nodes = new Dictionary<int, List<double>>();
    private readonly Mock<IComAuto> mockGSAObject = new Mock<IComAuto>();
    private readonly List<ProxyGwaLine> data = new List<ProxyGwaLine>();

    public TestProxy()
    {
      //So far only these methods are actually called
      mockGSAObject.Setup(x => x.GwaCommand(It.IsAny<string>())).Returns((string x) => { return x.Contains("GET") ? (object)"" : (object)1; });
      mockGSAObject.Setup(x => x.VersionString()).Returns("Test\t1");
      mockGSAObject.Setup(x => x.LogFeatureUsage(It.IsAny<string>()));
      mockGSAObject.Setup(x => x.SetLocale(It.IsAny<Locale>()));
      mockGSAObject.Setup(x => x.SetLocale(It.IsAny<Locale>()));
      mockGSAObject.Setup(x => x.NewFile());
      mockGSAObject.Setup(x => x.DisplayGsaWindow(It.IsAny<bool>()));
    }

    public new string GetUnits() => "m";

    public new void NewFile(bool showWindow = true, object gsaInstance = null)
    {
      base.NewFile(showWindow, gsaInstance: mockGSAObject.Object);
    }

    public new void OpenFile(string path, bool showWindow = true, object gsaInstance = null)
    {
      base.OpenFile(path, showWindow, gsaInstance: mockGSAObject.Object);
    }

    public new int NodeAt(double x, double y, double z, double coincidenceTol)
    {
      return ResolveIndex(x, y, z, coincidenceTol);
    }

    public void AddDataLine(string keyword, int index, string streamId, string applicationId, string gwaWithoutSet, GwaSetCommandType gwaSetType)
    {
      var line = new ProxyGwaLine() { Keyword = keyword, Index = index, StreamId = streamId, ApplicationId = applicationId, GwaWithoutSet = gwaWithoutSet, GwaSetType = gwaSetType };
      ExecuteWithLock(() => data.Add(line));
    }

    public new List<ProxyGwaLine> GetGwaData(IEnumerable<string> keywords, bool nodeApplicationIdFilter, IProgress<int> incrementProgress = null)
    {
      return data;
    }

    private int ResolveIndex(double x, double y, double z, double tol)
    {
      return ExecuteWithLock(() =>
      {
        int currMaxIndex = 1;
        if (nodes.Keys.Count() == 0)
        {
          nodes.Add(currMaxIndex, new List<double> { x, y, z });
          return currMaxIndex;
        }
        foreach (var i in nodes.Keys)
        {
          if ((WithinTol(x, nodes[i][0], tol)) && (WithinTol(y, nodes[i][1], tol)) && (WithinTol(z, nodes[i][2], tol)))
          {
            return i;
          }
          currMaxIndex = i;
        }
        for (int i = 1; i <= (currMaxIndex + 1); i++)
        {
          if (!nodes.Keys.Contains(i))
          {
            nodes.Add(i, new List<double> { x, y, z });
            return i;
          }
        }
        nodes.Add(currMaxIndex + 1, new List<double> { x, y, z });
        return (currMaxIndex + 1);
      });
    }

    public new void UpdateCasesAndTasks() { }
    public new void UpdateViews() { }

    private bool WithinTol(double x, double y, double tol)
    {
      return (Math.Abs(x - y) <= tol);
    }
  }
}
