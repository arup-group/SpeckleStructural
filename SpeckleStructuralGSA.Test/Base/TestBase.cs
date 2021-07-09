﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Interop.Gsa_10_1;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  public abstract class TestBase
  {
    public static string[] savedJsonFileNames = new[] { "lfsaIEYkR.json", "NaJD7d5kq.json", "U7ntEJkzdZ.json", "UNg87ieJG.json" };
    public static string expectedGwaPerIdsFileName = "TestGwaRecords.json";

    public static string[] savedBlankRefsJsonFileNames = new[] { "P40rt5c8I.json" };
    public static string expectedBlankRefsGwaPerIdsFileName = "BlankRefsGwaRecords.json";

    public static string[] savedSharedLoadPlaneJsonFileNames = new[] { "nagwSLyPE.json" };
    public static string expectedSharedLoadPlaneGwaPerIdsFileName = "SharedLoadPlaneGwaRefords.json";

    public static string[] simpleDataJsonFileNames = new[] { "gMu-Xgpc.json" };

    protected IComAuto comAuto;

    //protected IGSAAppResources appResources;

    //protected GSAProxy gsaInterfacer;
    //protected GSACache gsaCache;

    protected JsonSerializerSettings jsonSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
    protected string jsonDecSearch = @"(\d*\.\d\d\d\d\d\d)\d*";
    protected string jsonHashSearch = @"""hash"":\s*""[^""]+?""";
    protected string jsonHashReplace = @"""hash"":""""";
    protected string TestDataDirectory;

    protected int NodeIndex = 0;

    protected TestBase(string directory)
    {
      TestDataDirectory = directory;
      Initialiser.AppResources = new MockGSAApp();
    }

    protected Mock<IComAuto> SetupMockGsaCom()
    {
      var mockGsaCom = new Mock<IComAuto>();

      //So far only these methods are actually called
      //The new cache is stricter about duplicates so just generate a new index every time so no duplicate entries with same index and different GWAs are tried to be cached
      mockGsaCom.Setup(x => x.Gen_NodeAt(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns((double x, double y, double z, double coin) => { NodeIndex++; return NodeIndex; });
      mockGsaCom.Setup(x => x.GwaCommand(It.IsAny<string>())).Returns((string x) => { return x.Contains("GET") ? (object)"" : (object)1; });
      mockGsaCom.Setup(x => x.VersionString()).Returns("Test" + GSAProxy.GwaDelimiter + "1");
      mockGsaCom.Setup(x => x.LogFeatureUsage(It.IsAny<string>()));
      return mockGsaCom;
    }

    protected List<SpeckleObject> ModelToSpeckleObjects(GSATargetLayer layer, bool resultsOnly, bool embedResults, string[] cases, 
      string[] nodeResultsToSend = null, string[] elem1dResultsToSend = null, string[] elem2dResultsToSend = null, string[] miscResultsToSend = null)
    {
      bool sendResults = false;
      List<string> allResults = null;
      if (layer == GSATargetLayer.Analysis && cases != null && cases.Length > 0 && 
        ((nodeResultsToSend != null && nodeResultsToSend.Length > 0) || (elem1dResultsToSend != null && elem1dResultsToSend.Length > 0)
        || (elem2dResultsToSend != null && elem2dResultsToSend.Length > 0) || (miscResultsToSend != null && miscResultsToSend.Length > 0)))
      {
        sendResults = true;
        Initialiser.AppResources.Settings.ResultCases = cases.ToList();
        allResults = new List<string>();
        if (nodeResultsToSend != null)
        {
          Initialiser.AppResources.Settings.NodalResults = nodeResultsToSend.ToDictionary(nrts => nrts, nrts => (IGSAResultParams)null);
          allResults.AddRange(nodeResultsToSend);
        }
        if (elem1dResultsToSend != null)
        {
          Initialiser.AppResources.Settings.Element1DResults = elem1dResultsToSend.ToDictionary(nrts => nrts, nrts => (IGSAResultParams)null);
          allResults.AddRange(elem1dResultsToSend);
        }
        if (elem2dResultsToSend != null)
        {
          Initialiser.AppResources.Settings.Element2DResults = elem2dResultsToSend.ToDictionary(nrts => nrts, nrts => (IGSAResultParams)null);
          allResults.AddRange(elem2dResultsToSend);
        }
        if (miscResultsToSend != null)
        {
          Initialiser.AppResources.Settings.MiscResults = miscResultsToSend.ToDictionary(nrts => nrts, nrts => (IGSAResultParams)null);
          allResults.AddRange(miscResultsToSend);
        }

        if (resultsOnly)
        {
          Initialiser.AppResources.Settings.StreamSendConfig = StreamContentConfig.TabularResultsOnly;
        }
        else if (embedResults)
        {
          Initialiser.AppResources.Settings.StreamSendConfig = StreamContentConfig.ModelWithEmbeddedResults;
        }
        else
        {
          Initialiser.AppResources.Settings.StreamSendConfig = StreamContentConfig.ModelWithTabularResults;
        }
      }
      else
      {
        Initialiser.AppResources.Settings.StreamSendConfig = StreamContentConfig.ModelOnly;
      }
      Initialiser.AppResources.Settings.TargetLayer = layer;

      ((IGSACache)Initialiser.AppResources.Cache).Clear();

      ((GSAProxy)Initialiser.AppResources.Proxy).SetUnits("m");

      if (sendResults)
      {
        //Initialiser.AppResources.Proxy.LoadResults(allResults, cases.ToList());
        Initialiser.AppResources.Proxy.PrepareResults(Initialiser.AppResources.Settings.Result1DNumPosition + 2);
      }

      //Clear out all sender objects that might be there from the last test preparation
      Initialiser.GsaKit.GSASenderObjects.Clear();

      //Compile all GWA commands with application IDs
      var senderProcessor = new SenderProcessor(TestDataDirectory);

      var keywords = Initialiser.GsaKit.Keywords;
      var data = Initialiser.AppResources.Proxy.GetGwaData(keywords, false);
      for (int i = 0; i < data.Count(); i++)
      {
        var applicationId = string.IsNullOrEmpty(data[i].ApplicationId) ? null : data[i].ApplicationId;
        Initialiser.AppResources.Cache.Upsert(
          data[i].Keyword, 
          data[i].Index, 
          data[i].GwaWithoutSet,
          applicationId: applicationId,
          gwaSetCommandType: data[i].GwaSetType,
          streamId: data[i].StreamId
          );
      }

      senderProcessor.GsaInstanceToSpeckleObjects(out var speckleObjects);

      return speckleObjects;
    }

    protected bool JsonCompareAreEqual(string j1, string j2)
    {
      try
      {
        var jt1 = JToken.Parse(j1);
        var jt2 = JToken.Parse(j2);

        if (!JToken.DeepEquals(jt1, jt2))
        {
          //Required until SpeckleCoreGeometry has an updated such that its constructors create empty dictionaries for the "properties" property by default,
          //which would bring it in line with the default creation of empty dictionaries when they are created by other means
          RemoveNullEmptyFields(jt1, new[] { "properties" });
          RemoveNullEmptyFields(jt2, new[] { "properties" });

          RemoveNullEmptyFields(jt1, new[] { "name" });
          RemoveNullEmptyFields(jt2, new[] { "name" });

          RemoveNullEmptyFields(jt1, new[] { "hash" });
          RemoveNullEmptyFields(jt2, new[] { "hash" });

          var newResult = JToken.DeepEquals(jt1, jt2);
        }

        return JToken.DeepEquals(jt1, jt2);
      }
      catch
      {
        return false;
      }
    }

    protected void RemoveNullEmptyFields(JToken token, string[] fields)
    {
      var container = token as JContainer;
      if (container == null) return;

      var removeList = new List<JToken>();
      foreach (var el in container.Children())
      {
        var p = el as JProperty;
        if (p != null && fields.Contains(p.Name) && p.Value != null && !p.Value.HasValues)
        {
          removeList.Add(el);
        }
        RemoveNullEmptyFields(el, fields);
      }

      foreach (var el in removeList)
      {
        el.Remove();
      }
    }
  }
}
