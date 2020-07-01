﻿using System;
using System.Collections.Generic;
using System.Linq;
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
    protected IComAuto comAuto;

    protected GSAProxy gsaInterfacer;
    protected GSACache gsaCache;

    protected JsonSerializerSettings jsonSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
    protected string jsonDecSearch = @"(\d*\.\d\d\d\d\d\d)\d*";
    protected string jsonHashSearch = @"""hash"":\s*""[^""]+?""";
    protected string jsonHashReplace = @"""hash"":""""";
    protected string TestDataDirectory;

    protected int NodeIndex = 0;

    protected TestBase(string directory)
    {
      TestDataDirectory = directory;
    }

    protected Mock<IComAuto> SetupMockGsaCom()
    {
      var mockGsaCom = new Mock<IComAuto>();

      //So far only these methods are actually called
      //The new cache is stricter about duplicates so just generate a new index every time so no duplicate entries with same index and different GWAs are tried to be cached
      mockGsaCom.Setup(x => x.Gen_NodeAt(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns((double x, double y, double z, double coin) => { NodeIndex++; return NodeIndex; });
      mockGsaCom.Setup(x => x.GwaCommand(It.IsAny<string>())).Returns((string x) => { return x.Contains("GET") ? (object)"" : (object)1; });
      mockGsaCom.Setup(x => x.VersionString()).Returns("Test\t1");
      mockGsaCom.Setup(x => x.LogFeatureUsage(It.IsAny<string>()));
      return mockGsaCom;
    }

    protected List<SpeckleObject> ModelToSpeckleObjects(GSATargetLayer layer, bool resultsOnly, bool embedResults, string[] cases = null, string[] resultsToSend = null)
    {
      gsaCache.Clear();

      //Clear out all sender objects that might be there from the last test preparation
      Initialiser.GSASenderObjects.Clear();

      //Compile all GWA commands with application IDs
      var senderProcessor = new SenderProcessor(TestDataDirectory, gsaInterfacer, gsaCache, layer, embedResults, cases, resultsToSend);

      var keywords = senderProcessor.GetKeywords(layer);
      var data = gsaInterfacer.GetGwaData(keywords, false);
      for (int i = 0; i < data.Count(); i++)
      {
        gsaCache.Upsert(
          data[i].Keyword, 
          data[i].Index, 
          data[i].GwaWithoutSet,
          //This needs to be revised as this logic is in the kit too
          applicationId: (string.IsNullOrEmpty(data[i].ApplicationId)) ? ("gsa/" + data[i].Keyword + "_" + data[i].Index.ToString()) : data[i].ApplicationId, 
          gwaSetCommandType: data[i].GwaSetType,
          streamId: data[i].StreamId
          );
      }

      senderProcessor.GsaInstanceToSpeckleObjects(layer, out var speckleObjects, resultsOnly);

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
          removeNullEmptyFields(jt1, new[] { "properties" });
          removeNullEmptyFields(jt2, new[] { "properties" });

          var newResult = JToken.DeepEquals(jt1, jt2);
        }

        return JToken.DeepEquals(jt1, jt2);
      }
      catch
      {
        return false;
      }
    }

    protected void removeNullEmptyFields(JToken token, string[] fields)
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
        removeNullEmptyFields(el, fields);
      }

      foreach (var el in removeList)
      {
        el.Remove();
      }
    }
  }
}
