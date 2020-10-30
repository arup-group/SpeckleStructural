﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_NODE.2", new string[] { "NODE.3", "AXIS.1" }, "model", true, true, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSANode) })]
  public class GSA0DLoad : IGSASpeckleContainer
  {
    public int AxisId; // Store this temporarily to generate other loads

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural0DLoad();

    public void ParseGWACommand(List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural0DLoad();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      //Since this method is just called once to create an object, the application ID that the parent (single Structural0DLoad object) would have had.
      //This is based on the fact that one Strutural0DLoad object received previously would have created multiple LOAD_NODE lines in GSA
      obj.ApplicationId = SpeckleStructuralClasses.Helper.ExtractParentApplicationId(Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId));
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      var targetNodeRefs = Initialiser.Interface.ConvertGSAList(pieces[counter++], GSAEntity.NODE);

      if (nodes != null)
      {
        var targetNodes = nodes
            .Where(n => targetNodeRefs.Contains(n.GSAId)).ToList();

        obj.NodeRefs = targetNodes.Select(n => (string)n.Value.ApplicationId).OrderBy(n => n).ToList();
        this.SubGWACommand.AddRange(targetNodes.Select(n => n.GWACommand));

        foreach (var n in targetNodes)
          n.ForceSend = true;
      }

      obj.LoadCaseRef = Helper.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      var axis = pieces[counter++];
      this.AxisId = axis == "GLOBAL" ? 0 : Convert.ToInt32(axis);

      obj.Loading = new StructuralVectorSix(new double[6]);
      var direction = pieces[counter++].ToLower();
      switch (direction.ToUpper())
      {
        case "X":
          obj.Loading.Value[0] = Convert.ToDouble(pieces[counter++]);
          break;
        case "Y":
          obj.Loading.Value[1] = Convert.ToDouble(pieces[counter++]);
          break;
        case "Z":
          obj.Loading.Value[2] = Convert.ToDouble(pieces[counter++]);
          break;
        case "XX":
          obj.Loading.Value[3] = Convert.ToDouble(pieces[counter++]);
          break;
        case "YY":
          obj.Loading.Value[4] = Convert.ToDouble(pieces[counter++]);
          break;
        case "ZZ":
          obj.Loading.Value[5] = Convert.ToDouble(pieces[counter++]);
          break;
        default:
          // TODO: Error case maybe?
          break;
      }

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var load = this.Value as Structural0DLoad;

      if (load.Loading == null)
        return "";

      var keyword = typeof(GSA0DLoad).GetGSAKeyword();

      var nodeRefs = Initialiser.Cache.LookupIndices(typeof(GSANode).GetGSAKeyword(), load.NodeRefs).Where(x => x.HasValue).Select(x => x.Value).OrderBy(i => i).ToList();

      var loadCaseKeyword = typeof(GSALoadCase).GetGSAKeyword();
      var indexResult = Initialiser.Cache.LookupIndex(loadCaseKeyword, load.LoadCaseRef);
      var loadCaseRef = indexResult ?? Initialiser.Cache.ResolveIndex(loadCaseKeyword, load.LoadCaseRef);
      if (indexResult == null && load.ApplicationId != null)
      {
        if (load.LoadCaseRef == null)
        {
          Helper.SafeDisplay("Blank load case references found for these Application IDs:", load.ApplicationId);
        }
        else
        {
          Helper.SafeDisplay("Load case references not found:", load.ApplicationId + " referencing " + load.LoadCaseRef);
        }

      }

      var direction = new string[6] { "X", "Y", "Z", "XX", "YY", "ZZ" };

      var gwaCommands = new List<string>();

      for (var i = 0; i < load.Loading.Value.Count(); i++)
      {
        var ls = new List<string>();

        if (load.Loading.Value[i] == 0) continue;

        var loadDirectionAppId = SpeckleStructuralClasses.Helper.CreateChildApplicationId(direction[i], load.ApplicationId);
        var index = Initialiser.Cache.ResolveIndex(keyword, loadDirectionAppId);

        ls.Add("SET_AT");
        ls.Add(index.ToString());
        
        var sid = Helper.GenerateSID(loadDirectionAppId);
        ls.Add(keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid));
        ls.Add((load.Name == null || load.Name == "") ? " " : load.Name + (load.Name.All(char.IsDigit) ? " " : ""));
        ls.Add(string.Join(" ", nodeRefs));
        ls.Add(loadCaseRef.ToString());
        ls.Add("GLOBAL"); // Axis
        ls.Add(direction[i]);
        ls.Add(load.Loading.Value[i].ToString());

        gwaCommands.Add(string.Join("\t", ls));
      }
      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural0DLoad load)
    {
      return new GSA0DLoad() { Value = load }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA0DLoad dummyObject)
    {
      var newLines = ToSpeckleBase<GSA0DLoad>();
      var typeName = dummyObject.GetType().Name;
      var loads = new List<GSA0DLoad>();

      var nodes = Initialiser.GSASenderObjects.Get<GSANode>();

      //Multiple lines may result in just one 
      foreach (var k in newLines.Keys)
      {
        var p = newLines[k];
        var loadSubList = new List<GSA0DLoad>();

        // Placeholder load object to get list of nodes and load values
        // Need to transform to axis so one load definition may be transformed to many
        var initLoad = new GSA0DLoad() { GWACommand = p, GSAId = k };

        try
        {
          initLoad.ParseGWACommand(nodes);
        }
        catch (Exception ex)
        {
          Initialiser.AppUI.Message(typeName + ": " + ex.Message, k.ToString());
        }

        // Raise node flag to make sure it gets sent
        foreach (var n in nodes.Where(n => initLoad.Value.NodeRefs.Contains(n.Value.ApplicationId)))
        {
          n.ForceSend = true;
        }

        // Create load for each node applied
        foreach (string nRef in initLoad.Value.NodeRefs)
        {
          var load = new GSA0DLoad
          {
            GWACommand = initLoad.GWACommand,
            SubGWACommand = new List<string>(initLoad.SubGWACommand)
          };
          load.Value.Name = initLoad.Value.Name;
          load.Value.ApplicationId = initLoad.Value.ApplicationId;
          load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

          // Transform load to defined axis
          var node = nodes.Where(n => (n.Value.ApplicationId == nRef)).First();

          var loadAxis = Helper.Parse0DAxis(initLoad.AxisId, out string gwaRecord, node.Value.Value.ToArray());
          load.Value.Loading = initLoad.Value.Loading;
          load.Value.Loading.TransformOntoAxis(loadAxis);

          // If the loading already exists, add node ref to list
          var match = loadSubList.Count() > 0 ? loadSubList.Where(l => (l.Value.Loading.Value as List<double>).SequenceEqual(load.Value.Loading.Value as List<double>)).First() : null;
          if (match != null)
          {
            match.Value.NodeRefs.Add(nRef);
            if (gwaRecord != null)
            {
              match.SubGWACommand.Add(gwaRecord);
            }
          }
          else
          {
            load.Value.NodeRefs = new List<string>() { nRef };
            if (gwaRecord != null)
            {
              load.SubGWACommand.Add(gwaRecord);
            }
            loadSubList.Add(load);
          }
        }

        loads.AddRange(loadSubList);
      }

      Initialiser.GSASenderObjects.AddRange(loads);
      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
