﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NUnit.Framework;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class SenderTests : TestBase
  {
    public SenderTests() : base(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\TestData\") { }

    public static string[] resultTypes = new[] { "Nodal Reaction", "1D Element Strain Energy Density", "1D Element Force", "Nodal Displacements", "1D Element Stress" };
    public static string[] loadCases = new[] { "A2", "C1" };
    public const string gsaFileNameWithResults = "20180906 - Existing structure GSA_V7_modified.gwb";
    public const string gsaFileNameWithoutResults = "Structural Demo 191004.gwb";

    [OneTimeSetUp]
    public void SetupTests()
    {
      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution
      SpeckleInitializer.Initialize();
      gsaInterfacer = new GSAProxy();
      gsaCache = new GSACache();

      Initialiser.Cache = gsaCache;
      Initialiser.Interface = gsaInterfacer;
      Initialiser.Settings = new Settings();
    }

    [TestCase("TxSpeckleObjectsDesignLayer.json", GSATargetLayer.Design, false, true, gsaFileNameWithResults)]
    [TestCase("TxSpeckleObjectsDesignLayerBeforeAnalysis.json", GSATargetLayer.Design, false, true, gsaFileNameWithoutResults)]
    [TestCase("TxSpeckleObjectsResultsOnly.json", GSATargetLayer.Analysis, true, false, gsaFileNameWithResults)]
    [TestCase("TxSpeckleObjectsEmbedded.json", GSATargetLayer.Analysis, false, true, gsaFileNameWithResults)]
    [TestCase("TxSpeckleObjectsNotEmbedded.json", GSATargetLayer.Analysis, false, false, gsaFileNameWithResults)]
    public void TransmissionTest(string inputJsonFileName, GSATargetLayer layer, bool resultsOnly, bool embedResults, string gsaFileName)
    {
      gsaInterfacer.OpenFile(Helper.ResolveFullPath(gsaFileName, TestDataDirectory));

      //Deserialise into Speckle Objects so that these can be compared in any order

      var expectedFullJson = Helper.ReadFile(inputJsonFileName, TestDataDirectory);

      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution
      var expectedObjects = JsonConvert.DeserializeObject<List<SpeckleObject>>(expectedFullJson, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

      expectedObjects = expectedObjects.OrderBy(a => a.ApplicationId).ToList();

      var expected = new Dictionary<SpeckleObject, string>();
      foreach (var expectedObject in expectedObjects)
      {
        var expectedJson = JsonConvert.SerializeObject(expectedObject, jsonSettings);

        expectedJson = Regex.Replace(expectedJson, jsonDecSearch, "$1");
        expectedJson = Regex.Replace(expectedJson, jsonHashSearch, jsonHashReplace);

        expected.Add(expectedObject, expectedJson);
      }

      var actualObjects = ModelToSpeckleObjects(layer, resultsOnly, embedResults, loadCases, resultTypes);
      Assert.IsNotNull(actualObjects);
      actualObjects = actualObjects.OrderBy(a => a.ApplicationId).ToList();

      var actual = new Dictionary<SpeckleObject, string>();
      foreach (var actualObject in actualObjects)
      {
        var actualJson = JsonConvert.SerializeObject(actualObject, jsonSettings);

        actualJson = Regex.Replace(actualJson, jsonDecSearch, "$1");
        actualJson = Regex.Replace(actualJson, jsonHashSearch, jsonHashReplace);

        actual.Add(actualObject, actualJson);
      }

      var unmatching = new List<Tuple<string, string, List<string>>>();
      //Compare each object
      foreach (var actualObject in actual.Keys)
      {
        var actualJson = actual[actualObject];
        var actualType = actualObject.GetType();

        var matchingTypeAndId = expected.Where(kvp => kvp.Key.GetType() == actualType && kvp.Key.ApplicationId == actualObject.ApplicationId);
        var matchingExpected = matchingTypeAndId.Where(kvp => JsonCompareAreEqual(kvp.Value, actualJson));

        if (matchingExpected == null || matchingExpected.Count() == 0)
        {
          var nearestMatching = new List<string>();
          if (!string.IsNullOrEmpty(actualObject.ApplicationId))
          {
            nearestMatching.AddRange(matchingTypeAndId.Select(kvp => kvp.Value));
          }
          
          unmatching.Add(new Tuple<string, string, List<string>>(actualObject.ApplicationId, actualJson, nearestMatching));
        }        
        else if (matchingExpected.Count() == 1)
        {
          expected.Remove(matchingExpected.First().Key);
        }
        else
        {
          //TO DO
        }
      }

      gsaInterfacer.Close();

      Assert.IsEmpty(unmatching, unmatching.Count().ToString() + " unmatched objects");
    }
  }
}
