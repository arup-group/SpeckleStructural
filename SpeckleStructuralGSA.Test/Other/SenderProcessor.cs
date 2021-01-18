﻿using System;
using System.Collections.Generic;
using System.Linq;
using Interop.Gsa_10_1;
using Moq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  public class SenderProcessor : ProcessorBase
  {
    public SenderProcessor(string directory, GSAProxy gsaInterfacer, GSACache gsaCache, GSATargetLayer layer, bool embedResults, string[] cases = null, string[] resultsToSend = null) : base(directory)
    {
      GSAInterfacer = gsaInterfacer;
      Initialiser.Instance.Settings.TargetLayer = layer;

      Initialiser.Instance.Settings.EmbedResults = embedResults;
      if (cases != null)
      {
        Initialiser.Instance.Settings.ResultCases = cases.ToList();
      }
      if (resultsToSend != null)
      {
        processResultLabels(resultsToSend);
      }
    }

    public void JsonGwaCacheFileToCacheRecords(string savedJsonFileName, string directory, out Dictionary<string, object> gwaCacheRecords)
    {
      gwaCacheRecords = new Dictionary<string, object>();

      var json = Helper.ReadFile(savedJsonFileName, directory);
      var readRecords = Helper.DeserialiseJson<Dictionary<string, string>>(json);
      foreach (var k in readRecords.Keys)
      {
        gwaCacheRecords[k] = (object)readRecords[k];
      }
    }

    public void GsaInstanceToSpeckleObjects(GSATargetLayer layer, out List<SpeckleObject> speckleObjects, bool resultOnly)
    {
      var TypePrerequisites = Helper.GetTypeCastPriority(ioDirection.Send, layer, resultOnly);

      var gwaCacheRecords = new Dictionary<string, object>();
      speckleObjects = new List<SpeckleObject>();

      // Read objects
      var currentBatch = new List<Type>();
      var traversedTypes = new List<Type>();

      do
      {
        currentBatch = TypePrerequisites.Where(i => i.Value.Count(x => !traversedTypes.Contains(x)) == 0).Select(i => i.Key).ToList();
        currentBatch.RemoveAll(i => traversedTypes.Contains(i));

        foreach (var t in currentBatch)
        {
          var dummyObject = Activator.CreateInstance(t);
          var result = Converter.Serialise(dummyObject);

          traversedTypes.Add(t);
        }
      } while (currentBatch.Count > 0);

      speckleObjects.AddRange(Initialiser.Instance.GSASenderObjects.GetAll().SelectMany(i => i.Value).Select(o => (SpeckleObject) ((IGSASpeckleContainer)o).SpeckleObject));
    }

    #region private_methods
    private Mock<IComAuto> SetupMockGsaCom()
    {
      var mockGsaCom = new Mock<IComAuto>();
      //So far only these methods are actually called
      mockGsaCom.Setup(x => x.Gen_NodeAt(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns((double x, double y, double z, double coin) => 1);
      mockGsaCom.Setup(x => x.GwaCommand(It.IsAny<string>())).Returns((string x) => { return x.Contains("GET") ? (object)"" : (object)1; });

      return mockGsaCom;
    }

    private bool processResultLabels(string[] resultLabels)
    {
      foreach (var resultLabel in resultLabels)
      {
        if (!processResultLabel(resultLabel))
        {
          return false;
        }
      }
      return true;
    }

    private bool processResultLabel(string resultLabel)
    {
      var matchingKey = Result.NodalResultMap.Keys.FirstOrDefault(k => string.Equals(k, resultLabel, StringComparison.OrdinalIgnoreCase));
      if (matchingKey != null)
      {
        Initialiser.Instance.Settings.NodalResults[matchingKey] = Result.NodalResultMap[matchingKey];
        return true;
      }

      matchingKey = Result.Element1DResultMap.Keys.FirstOrDefault(k => string.Equals(k, resultLabel, StringComparison.OrdinalIgnoreCase));
      if (matchingKey != null)
      {
        Initialiser.Instance.Settings.Element1DResults[matchingKey] = Result.Element1DResultMap[matchingKey];
        return true;
      }

      matchingKey = Result.Element2DResultMap.Keys.FirstOrDefault(k => string.Equals(k, resultLabel, StringComparison.OrdinalIgnoreCase));
      if (matchingKey != null)
      {
        Initialiser.Instance.Settings.Element2DResults[matchingKey] = Result.Element2DResultMap[matchingKey];
        return true;
      }

      matchingKey = Result.MiscResultMap.Keys.FirstOrDefault(k => string.Equals(k, resultLabel, StringComparison.OrdinalIgnoreCase));
      if (matchingKey != null)
      {
        Initialiser.Instance.Settings.MiscResults[matchingKey] = Result.MiscResultMap[matchingKey];
        return true;
      }
      return false;
    }
    #endregion
  }
}
