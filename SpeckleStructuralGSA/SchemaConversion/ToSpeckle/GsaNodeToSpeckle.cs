using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using System.Threading.Tasks;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class GsaNodeToSpeckle
  {
    public static SpeckleObject ToSpeckle(this GsaNode dummyObject)
    {
      var nodeKw = GsaRecord.GetKeyword<GsaNode>();
      var springKw = GsaRecord.GetKeyword<GsaPropSpr>();
      var massKw = GsaRecord.GetKeyword<GsaPropMass>();
      var loadTaskKw = GsaRecord.GetKeyword<GsaLoadCase>();
      var comboKw = GsaRecord.GetKeyword<GsaCombination>();

      var newNodeLines = Initialiser.AppResources.Cache.GetGwaToSerialise(nodeKw);

      var sendResults = GetNodeResultSettings(out var embedResults, out var resultTypes, out var resultCases);

      if (sendResults)
      {
        Initialiser.AppResources.Proxy.LoadResults(resultTypes, resultCases, newNodeLines.Keys.ToList());
      }

      //This method produces two types of SpeckleStructural objects
      var structuralNodes = new List<StructuralNode>();
      var structural0dSprings = new List<Structural0DSpring>();

      int numToBeSent = 0;

#if DEBUG
      foreach (var i in newNodeLines.Keys)
#else
        Parallel.ForEach(newNodeLines.Keys, i =>
#endif
      {
        GsaNode gsaNode = null;
        var objNode = Helper.ToSpeckleTryCatch(nodeKw, i, () =>
        {
          gsaNode = new GsaNode();
          if (gsaNode.FromGwa(newNodeLines[i]))
          {
            var structuralNode = new StructuralNode()
            {
              Name = gsaNode.Name,
              ApplicationId = SpeckleStructuralGSA.Helper.GetApplicationId(nodeKw, i),
              Value = new List<double>() { gsaNode.X, gsaNode.Y, gsaNode.Z },
              Restraint = GetRestraint(gsaNode)
            };

            if (gsaNode.MeshSize.HasValue && gsaNode.MeshSize.Value > 0)
            {
              structuralNode.GSALocalMeshSize = gsaNode.MeshSize.Value;
            }

            if (gsaNode.MassPropertyIndex.HasValue && gsaNode.MassPropertyIndex.Value > 0)
            {
              var massGwas = Initialiser.AppResources.Cache.GetGwa(massKw, gsaNode.MassPropertyIndex.Value);
              if (massGwas != null && massGwas.Count() > 0 && !string.IsNullOrEmpty(massGwas.First()))
              {
                var gsaPropMass = new GsaPropMass();
                if (gsaPropMass.FromGwa(massGwas.First()) && gsaPropMass.Mass > 0)
                {
                  structuralNode.Mass = gsaPropMass.Mass;
                }
              }
            }
            return structuralNode;
          }
          return new SpeckleNull();
        });

        if (objNode !=null && !(objNode is SpeckleNull))
        {
          var structuralNode = (StructuralNode)objNode;
          GSANodeResult gsaNodeResult = null;

          //Embed results if appropriate as the last thing to do to the new Speckle object before being added to the collection of objects to be sent
          if (sendResults)
          {
            var getResults = Initialiser.AppResources.Proxy.GetResults(nodeKw, i, out var data);
            var results = Helper.GetSpeckleResultHierarchy(data, false);
            if (results != null && data.Keys.Count() > 0)
            {
              var orderedLoadCases = results.Keys.OrderBy(k => k).ToList();
              if (embedResults)
              {
                foreach (var loadCase in orderedLoadCases)
                {
                  if (!FormatResults(results[loadCase], out Dictionary<string, object> sendableResults))
                  {
                    continue;
                  }
                  var nodeResult = new StructuralNodeResult()
                  {
                    IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
                    TargetRef = structuralNode.ApplicationId,
                    Value = sendableResults
                  };
                  var loadCaseRef = Helper.GsaCaseToRef(loadCase, loadTaskKw, comboKw);
                  if (!string.IsNullOrEmpty(loadCaseRef))
                  {
                    //nodeResult.LoadCaseRef = loadCaseRef;
                    nodeResult.LoadCaseRef = loadCase;
                  }
                  if (structuralNode.Result == null)
                  {
                    //Can't just allocate an empty dictionary as the Result set property won't allow it
                    structuralNode.Result = new Dictionary<string, object>() { { loadCase, nodeResult } };
                  }
                  else
                  {
                    structuralNode.Result.Add(loadCase, nodeResult);
                  }
                }
              }
              else
              {
                foreach (var loadCase in orderedLoadCases)
                {
                  if (!FormatResults(results[loadCase], out Dictionary<string, object> sendableResults))
                  {
                    continue;
                  }
                  var nodeResult = new StructuralNodeResult()
                  {
                    IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
                    TargetRef = structuralNode.ApplicationId,
                    Value = sendableResults
                  };
                  var loadCaseRef = Helper.GsaCaseToRef(loadCase, loadTaskKw, comboKw);
                  if (!string.IsNullOrEmpty(loadCaseRef))
                  {
                    //nodeResult.LoadCaseRef = loadCaseRef;
                    nodeResult.LoadCaseRef = loadCase;
                  }
                  gsaNodeResult = new GSANodeResult { Value = nodeResult, GSAId = i };
                  Initialiser.GsaKit.GSASenderObjects.Add(gsaNodeResult);
                }
              }
            }
          }
          var senderObjectGsaNode = new GSANode()
          {
            Value = structuralNode,
            GSAId = i
          };
          if (gsaNodeResult != null)
          {
            senderObjectGsaNode.ForceSend = true;
          }
          Initialiser.GsaKit.GSASenderObjects.Add(senderObjectGsaNode);
          numToBeSent++;

          //Add spring object if appropriate
          if (gsaNode.SpringPropertyIndex.HasValue && gsaNode.SpringPropertyIndex.Value > 0)
          {
            var objSpring = Helper.ToSpeckleTryCatch(nodeKw, i, () =>
            {
              var springPropRef = SpeckleStructuralGSA.Helper.GetApplicationId(springKw, gsaNode.SpringPropertyIndex.Value);
              if (!string.IsNullOrEmpty(springPropRef))
              {
                var structural0dSpring = new Structural0DSpring()
                {
                //The application ID might need a better mechanism to allow a StructuralNode and Structural0DSpring previously received
                //that originally created the one node to be separated out again to produce the Application ID to use here for this spring
                //TO DO - review, for now just append a string to ensure the same Application ID value isn't used twice
                ApplicationId = gsaNode.ApplicationId + "_spring",
                  Name = gsaNode.Name,
                  Value = new List<double>() { gsaNode.X, gsaNode.Y, gsaNode.Z },
                  PropertyRef = springPropRef,
                  Dummy = false
                };
                return structural0dSpring;
              }
              return new SpeckleNull();
            });
            if (!(objSpring is SpeckleNull))
            {
              Initialiser.GsaKit.GSASenderObjects.Add(new GSA0DSpring() { Value = (Structural0DSpring)objSpring, GSAId = i });
              numToBeSent++;
            }
          } //if spring object needs to be added
        } //if node object was successfully created
      }
#if !DEBUG
      );
#endif

      if (sendResults)
      {
        Initialiser.AppResources.Proxy.ClearResults(resultTypes);
      }

      return (numToBeSent > 0) ? new SpeckleObject() : new SpeckleNull();
    }

    private static bool FormatResults(Dictionary<string, object> inValue, out Dictionary<string, object> outValue)
    {
      outValue = inValue;
      return true;
    }

    private static StructuralVectorBoolSix GetRestraint(GsaNode gsaNode)
    {
      if (gsaNode.NodeRestraint == NodeRestraint.Custom && gsaNode.Restraints != null && gsaNode.Restraints.Count() > 0)
      {
        return Helper.AxisDirDictToStructuralVectorBoolSix(gsaNode.Restraints);
      }
      else if (gsaNode.NodeRestraint == NodeRestraint.Fix)
      {
        return new StructuralVectorBoolSix(Enumerable.Repeat(true, 6));
      }
      else if (gsaNode.NodeRestraint == NodeRestraint.Pin)
      {
        return new StructuralVectorBoolSix(Enumerable.Repeat(true, 3).Concat(Enumerable.Repeat(false, 3)));
      }
      return null;
    }

    private static bool GetNodeResultSettings(out bool embedResults, out List<string> resultTypes, out List<string> resultCases)
    {
      var sendResults = ((Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis) 
        && (Initialiser.AppResources.Settings.StreamSendConfig == StreamContentConfig.ModelWithEmbeddedResults
        || Initialiser.AppResources.Settings.StreamSendConfig == StreamContentConfig.ModelWithTabularResults
        || Initialiser.AppResources.Settings.StreamSendConfig == StreamContentConfig.TabularResultsOnly));

      var sendNodeResults = (sendResults && Initialiser.AppResources.Settings.NodalResults.Keys.Count() > 0
        && Initialiser.AppResources.Settings.ResultCases != null && Initialiser.AppResources.Settings.ResultCases.Count() > 0);

      embedResults = sendNodeResults ? Initialiser.AppResources.Settings.StreamSendConfig == StreamContentConfig.ModelWithEmbeddedResults : false;
      resultTypes = sendNodeResults ? resultTypes = Initialiser.AppResources.Settings.NodalResults.Keys.ToList() : null;
      resultCases = sendNodeResults ? Initialiser.AppResources.Settings.ResultCases.ToList() : null;
      return sendNodeResults;
    }
  }
}
