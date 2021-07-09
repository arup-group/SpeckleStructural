﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  //Copied from the Receiver class in SpeckleGSA - this will be refactored to simplify and avoid dependency
  public class ReceiverProcessor : ProcessorBase
  {
    private List<Tuple<string, SpeckleObject>> receivedObjects;

    private Dictionary<Type, IGSASpeckleContainer> dummyObjectDict = new Dictionary<Type, IGSASpeckleContainer>();

    public ReceiverProcessor(string directory, GSATargetLayer layer = GSATargetLayer.Design) : base (directory)
    {
      //this.appResources = appResources;
      //GSAInterfacer = gsaInterfacer;
      //GSACache = gsaCache;
      //((MockSettings)this.appResources.Settings).TargetLayer = layer;
    }

    public void JsonSpeckleStreamsToGwaRecords(IEnumerable<string> savedJsonFileNames, out List<GwaRecord> gwaRecords, GSATargetLayer layer)
    {
      Initialiser.AppResources.Settings.TargetLayer = layer;

      gwaRecords = new List<GwaRecord>();

      receivedObjects = JsonSpeckleStreamsToSpeckleObjects(savedJsonFileNames);

      ScaleObjects();

      ConvertSpeckleObjectsToGsaInterfacerCache(layer);

      //var gwaCommands = ((IGSACacheForTesting) this.appResources.Cache).GetGwaSetCommands();
      var gwaCommands = ((IGSACacheForTesting)Initialiser.AppResources.Cache).GetGwaSetCommands();
      foreach (var gwaC in gwaCommands)
      {
        GSAProxy.ParseGeneralGwa(gwaC, out var keyword, out int? index, out var streamId, out var applicationId, out var gwaWithoutSet, out GwaSetCommandType? gwaSetType);
        gwaRecords.Add(new GwaRecord(string.IsNullOrEmpty(applicationId) ? null : applicationId, gwaC));
      }
    }

    #region private_methods    

    private List<Tuple<string,SpeckleObject>> JsonSpeckleStreamsToSpeckleObjects(IEnumerable<string> savedJsonFileNames)
    {
      //Read JSON files into objects
      return ExtractObjects(savedJsonFileNames.ToArray(), TestDataDirectory);
    }

    private void ScaleObjects()
    {
      //Status.ChangeStatus("Scaling objects");
      var units = Initialiser.AppResources.Settings.Units;
      var scaleFactor = (1.0).ConvertUnit(units, "m");
      foreach (var o in receivedObjects)
      {
        try
        {
          o.Item2.Scale(scaleFactor);
        }
        catch { }
      }
    }

    private void ConvertSpeckleObjectsToGsaInterfacerCache(GSATargetLayer layer)
    {
      Initialiser.AppResources.Settings.TargetLayer = layer;

      // Write objects
      var currentBatch = new List<Type>();
      var traversedTypes = new List<Type>();

      var TypePrerequisites = Initialiser.GsaKit.RxTypeDependencies;

      foreach (var tuple in receivedObjects)
      {
        tuple.Item2.Properties.Add("StreamId", tuple.Item1);
      }

      var rxObjsByType = CollateRxObjectsByType(receivedObjects.Select(tuple => tuple.Item2).ToList());

      do
      {
        currentBatch = TypePrerequisites.Where(i => i.Value.Count(x => !traversedTypes.Contains(x)) == 0).Select(i => i.Key).ToList();
        currentBatch.RemoveAll(i => traversedTypes.Contains(i));

        foreach (var t in currentBatch)
        {
          var dummyObject = Activator.CreateInstance(t);
          var keyword = dummyObject.GetAttribute<GSAObject>("GSAKeyword").ToString();
          var valueType = dummyObjectDict[t].SpeckleObject.GetType();
          var speckleTypeName = valueType.GetType().Name;
          var targetObjects = receivedObjects.Select(o => new { o, t = o.Item2.GetType() })
            .Where(x => (x.t == valueType || x.t.IsSubclassOf(valueType))).Select(x => x.o).ToList();

          for (var i = 0; i < targetObjects.Count(); i++)
          {
            var streamId = targetObjects[i].Item1;
            var obj = targetObjects[i].Item2;

            //DESERIALISE
            var deserialiseReturn = ((string)Converter.Deserialise(obj));
            var gwaCommands = deserialiseReturn.Split(new[] { '\n' }).Where(c => c.Length > 0).ToList();

            for (var j = 0; j < gwaCommands.Count(); j++)
            {
              GSAProxy.ParseGeneralGwa(gwaCommands[j], out keyword, out int? foundIndex, out var foundStreamId, out var foundApplicationId, 
                out var gwaWithoutSet, out var gwaSetCommandType);

              //Only cache the object against, the top-level GWA command, not the sub-commands
              ((IGSACache)Initialiser.AppResources.Cache).Upsert(keyword, foundIndex.Value, gwaWithoutSet, applicationId: foundApplicationId, 
                so: (foundApplicationId == obj.ApplicationId) ? obj : null, gwaSetCommandType: gwaSetCommandType.Value);
            }
          }

          traversedTypes.Add(t);
        }
      } while (currentBatch.Count > 0);

      var toBeAddedGwa = ((IGSACache)Initialiser.AppResources.Cache).GetNewGwaSetCommands();
      for (int i = 0; i < toBeAddedGwa.Count(); i++)
      {
        Initialiser.AppResources.Proxy.SetGwa(toBeAddedGwa[i]);
      }
    }

    private Dictionary<Type, List<SpeckleObject>> CollateRxObjectsByType(List<SpeckleObject> rxObjs)
    {
      var rxTypePrereqs = Initialiser.GsaKit.RxTypeDependencies;
      var rxSpeckleTypes = rxObjs.Select(k => k.GetType()).Distinct().ToList();

      //build up dictionary of old schema (IGSASpeckleContainer) types and dummy instances
      rxTypePrereqs.Keys.Where(t => !dummyObjectDict.ContainsKey(t)).ToList()
        .ForEach(t => dummyObjectDict[t] = (IGSASpeckleContainer)Activator.CreateInstance(t));

      ///[ GSA type , [ SpeckleObjects ]]
      var d = new Dictionary<Type, List<SpeckleObject>>();
      foreach (var o in rxObjs)
      {
        var speckleType = o.GetType();

        var matchingGsaTypes = rxTypePrereqs.Keys.Where(t => dummyObjectDict[t].SpeckleObject.GetType() == speckleType);
        if (matchingGsaTypes.Count() == 0)
        {
          matchingGsaTypes = rxTypePrereqs.Keys.Where(t => speckleType.IsSubclassOf(dummyObjectDict[t].SpeckleObject.GetType()));
        }

        if (matchingGsaTypes.Count() == 0)
        {
          continue;
        }

        var gsaType = matchingGsaTypes.First();
        if (!d.ContainsKey(gsaType))
        {
          d.Add(gsaType, new List<SpeckleObject>());
        }
        d[gsaType].Add(o);

      }

      return d;
    }

    public List<Tuple<string, SpeckleObject>> ExtractObjects(string fileName, string directory)
    {
      return ExtractObjects(new string[] { fileName }, directory);
    }

    public List<Tuple<string,SpeckleObject>> ExtractObjects(string[] fileNames, string directory)
    {
      var speckleObjects = new List<Tuple<string, SpeckleObject>>();
      foreach (var fileName in fileNames)
      {
        var json = Helper.ReadFile(fileName, directory);
        var streamId = fileName.Split(new[] { '.' }).First();

        var response = ResponseObject.FromJson(json);
        for (int i = 0; i < response.Resources.Count(); i++)
        {
          speckleObjects.Add(new Tuple<string, SpeckleObject>(streamId, response.Resources[i]));
        }
      }
      return speckleObjects;
    }

    #endregion
  }
}


