﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NUnit.Framework;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class SenderTests : TestBase
  {
    public SenderTests() : base(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\TestData\") { }

    public static List<ResultType> nodeResultTypes = new List<ResultType> { ResultType.NodalDisplacements, ResultType.NodalReaction };
    public static List<ResultType> elem1dResultTypes = new List<ResultType> { ResultType.Element1dDisplacement, ResultType.Element1dForce };
    public static List<ResultType> elem2dResultTypes = new List<ResultType> { ResultType.Element2dDisplacement, ResultType.Element2dProjectedMoment, ResultType.Element2dProjectedForce, 
      ResultType.Element2dProjectedStressBottom, ResultType.Element2dProjectedStressMiddle, ResultType.Element2dProjectedStressTop };
    public static List<ResultType> miscResultTypes = new List<ResultType> { ResultType.AssemblyForcesAndMoments };
    public static string[] loadCases = new[] { "A2", "C1" };
    //public const string gsaFileNameWithResults = "20180906 - Existing structure GSA_V7_modified.gwb";
    public const string gsaFileNameWithResults = "Structural Demo 200630 Results.gwb";
    public const string gsaFileNameWithoutResults = "Structural Demo 200630.gwb";
    public static List<ResultType> allResultTypes => nodeResultTypes.Union(elem1dResultTypes).Union(elem2dResultTypes).Union(miscResultTypes).ToList();

    [OneTimeSetUp]
    public void SetupTests()
    {
      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution
      SpeckleInitializer.Initialize();
      Initialiser.AppResources = new MockGSAApp(proxy: new GSAProxy());
      Initialiser.GsaKit.Clear();
    }

    [TestCase("TxSpeckleObjectsDesignLayer.json", GSATargetLayer.Design, false, true, gsaFileNameWithResults)]
    [TestCase("TxSpeckleObjectsDesignLayerBeforeAnalysis.json", GSATargetLayer.Design, false, true, gsaFileNameWithoutResults)]
    [TestCase("TxSpeckleObjectsResultsOnly.json", GSATargetLayer.Analysis, true, false, gsaFileNameWithResults)]
    [TestCase("TxSpeckleObjectsEmbedded.json", GSATargetLayer.Analysis, false, true, gsaFileNameWithResults)]
    [TestCase("TxSpeckleObjectsNotEmbedded.json", GSATargetLayer.Analysis, false, false, gsaFileNameWithResults)]
    public void TransmissionTest(string inputJsonFileName, GSATargetLayer layer, bool resultsOnly, bool embedResults, string gsaFileName)
    {
      Initialiser.AppResources.Proxy.OpenFile(Path.Combine(TestDataDirectory, gsaFileName), true);

      //Deserialise into Speckle Objects so that these can be compared in any order

      var expectedFullJson = Helper.ReadFile(inputJsonFileName, TestDataDirectory);

      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution
      var expectedObjects = JsonConvert.DeserializeObject<List<SpeckleObject>>(expectedFullJson, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

      expectedObjects = expectedObjects.OrderBy(a => a.ApplicationId).ToList();

      JsonConvert.DefaultSettings = () => new JsonSerializerSettings
      {
        FloatParseHandling = FloatParseHandling.Decimal,
        Culture = new CultureInfo(string.Empty)
        {
          NumberFormat = new NumberFormatInfo
          {
            CurrencyDecimalDigits = 2
          }
        }
      };

      var expected = new Dictionary<Type, List<Tuple<string, SpeckleObject, string>>>();
      var expectedLock = new object();
      //Parallel.ForEach(expectedObjects, expectedObject =>
      foreach(var expectedObject in expectedObjects)
      {
        var expectedJson = JsonConvert.SerializeObject(expectedObject, jsonSettings);

        expectedJson = Regex.Replace(expectedJson, jsonDecSearch, "$1");
        expectedJson = Regex.Replace(expectedJson, jsonHashSearch, jsonHashReplace);
        expectedJson = Helper.RemoveKeywordVersionFromApplicationIds(expectedJson);

        var type = expectedObject.GetType();
        lock (expectedLock)
        {
          if (!expected.ContainsKey(type))
          {
            expected[type] = new List<Tuple<string, SpeckleObject, string>>();
          }
          var expectedObjectAppId = SafeApplicationId(expectedObject);
          expected[type].Add(new Tuple<string, SpeckleObject, string>(expectedObjectAppId, expectedObject, expectedJson));
        }
      }
      //);

      var allResultTypes = nodeResultTypes.Union(elem1dResultTypes).Union(elem2dResultTypes).Union(miscResultTypes).ToList();
      var actualObjects = ModelToSpeckleObjects(layer, resultsOnly, embedResults, loadCases, allResultTypes);
      Assert.IsNotNull(actualObjects);

      //This replaces what the real sender does in terms of stream buckets
      if (resultsOnly)
      {
        actualObjects = actualObjects.Where(o => o.Type.Contains("Result")).ToList();
      }

      actualObjects = actualObjects.OrderBy(a => a.ApplicationId).ToList();

      var actual = new Dictionary<SpeckleObject, string>();
      foreach (var actualObject in actualObjects)
      {
        var actualJson = JsonConvert.SerializeObject(actualObject, jsonSettings);

        actualJson = Regex.Replace(actualJson, jsonDecSearch, "$1");
        actualJson = Regex.Replace(actualJson, jsonHashSearch, jsonHashReplace);
        actualJson = Helper.RemoveKeywordVersionFromApplicationIds(actualJson);

        actual.Add(actualObject, actualJson);
      }

      var matched = new List<SpeckleObject>();
      var matchedLock = new object();
      var unmatching = new List<Tuple<string, string, List<string>>>();
      var unmatchingLock = new object();

      //Compare each object
      foreach(var actualObject in actual.Keys)
      //Parallel.ForEach(actual.Keys, actualObject =>
      {
        var actualJson = actual[actualObject];
        var actualType = actualObject.GetType();

        List<Tuple<string, SpeckleObject, string>> matchingExpected;
        bool containsKey;
        lock (expectedLock)
        {
          containsKey = expected.ContainsKey(actualType);
        }
        if (containsKey)
        {
          List<Tuple<string, SpeckleObject, string>> matchingTypeAndId;
          var actualObjectAppId = SafeApplicationId(actualObject);
          lock (expectedLock)
          {
            matchingTypeAndId = expected[actualType].Where(tup => tup.Item1 == actualObjectAppId).ToList();
          }
          matchingExpected = matchingTypeAndId.Where(tup => JsonCompareAreEqual(tup.Item3, actualJson)).ToList();

          if (matchingExpected.Count() == 0)
          {
            var nearestMatching = matchingTypeAndId.Select(kvp => kvp.Item3).ToList();

            lock (unmatchingLock)
            {
              unmatching.Add(new Tuple<string, string, List<string>>(actualObject.ApplicationId, actualJson, nearestMatching));
            }
          }
          else if (matchingExpected.Count() == 1)
          {
            lock (expectedLock)
            {
              expected[actualType].Remove(matchingExpected.First());
            }
            lock (matchedLock)
            {
              matched.Add(actualObject);
            }
          }
          else
          {
            //TO DO
          }
        }
        else
        {
          lock (unmatchingLock)
          {
            unmatching.Add(new Tuple<string, string, List<string>>(actualObject.ApplicationId, actualJson, new List<string>()));
          }
        }
      }
      //);

      Initialiser.AppResources.Proxy.Close();
      Assert.IsFalse(actual.Keys.Any(a => !a.Type.ToLower().EndsWith("result") && string.IsNullOrEmpty(a.ApplicationId)));
      Assert.LessOrEqual(actual.Count() - matched.Count(), 5);   //To account for floating point variations
      Assert.LessOrEqual(unmatching.Count(), 5, unmatching.Count().ToString() + " unmatched objects");
    }

    //To cope with result objects not having an application Id
    private string SafeApplicationId(SpeckleObject so)
    {
      var appId = "";
      if (so is StructuralResultBase)
      {
        var resultObj = (StructuralResultBase)so;
        appId = (resultObj.TargetRef ?? "") + (resultObj.LoadCaseRef ?? "") + (resultObj.ResultSource ?? "") + (resultObj.Description ?? "");
      }
      else
      {
        appId = so.ApplicationId ?? "";
      }
      return Helper.RemoveKeywordVersionFromApplicationIds(appId);
    }

    [Test]
    public void ResultTypeDependencies()
    {
      var resultTypes = new List<Type> { typeof(GSAMiscResult), typeof(GSANodeResult), typeof(GSA1DElementResult), typeof(GSA2DElementResult) };
      ((MockSettings)Initialiser.AppResources.Settings).TargetLayer = GSATargetLayer.Analysis;
      ((MockSettings)Initialiser.AppResources.Settings).StreamSendConfig = StreamContentConfig.ModelOnly;
      resultTypes.ForEach(rt => Assert.IsFalse(Initialiser.GsaKit.TxTypeDependencies.ContainsKey(rt)));

      ((MockSettings)Initialiser.AppResources.Settings).StreamSendConfig = StreamContentConfig.ModelWithTabularResults;
      foreach (var rt in resultTypes)
      {
        Assert.IsTrue(Initialiser.GsaKit.TxTypeDependencies.ContainsKey(rt));
        Assert.IsTrue(Initialiser.GsaKit.TxTypeDependencies[rt].Count() > 0);
      }
    }

    [Ignore("There is an equivalent test in SpeckleGSA repo, so this one might be removed")]
    //[TestCase(GSATargetLayer.Design, false, false, "sjc.gwb")]
    [TestCase(GSATargetLayer.Analysis, false, false, @"C:\Temp\ResultsTest.gwb", "", "")]
    //[TestCase(GSATargetLayer.Analysis, false, true, @"C:\Users\Nic.Burgers\OneDrive - Arup\Issues\Nguyen Le\2D result\shear wall system-seismic v10.1.gwb", 
    //  "2D Element Projected Force", "A1 A2" )]
    public void TransmissionTestForDebug(GSATargetLayer layer, bool resultsOnly, bool embedResults, string gsaFileName, 
      ResultType? overrideResultType = null, string loadCasesOverride = null)
    {
      Initialiser.AppResources.Proxy.OpenFile(gsaFileName.Contains("\\") ? gsaFileName : Path.Combine(TestDataDirectory, gsaFileName));

      var actualObjects = ModelToSpeckleObjects(layer, resultsOnly, embedResults,
        (loadCasesOverride == null) ? loadCases : loadCasesOverride.ListSplit(" "),
        (overrideResultType.HasValue) ? new List<ResultType> { overrideResultType.Value } : nodeResultTypes);

      Assert.IsNotNull(actualObjects);
      actualObjects = actualObjects.OrderBy(a => a.ApplicationId).ToList();

      var objectsByType = actualObjects.GroupBy(o => o.GetType());

      var actual = new Dictionary<SpeckleObject, string>();
      foreach (var actualObject in actualObjects)
      {
        var actualJson = JsonConvert.SerializeObject(actualObject, jsonSettings);

        actualJson = Regex.Replace(actualJson, jsonDecSearch, "$1");
        actualJson = Regex.Replace(actualJson, jsonHashSearch, jsonHashReplace);

        actual.Add(actualObject, actualJson);
      }

      var matched = new List<SpeckleObject>();

      Initialiser.AppResources.Proxy.Close();
    }
  }
}
