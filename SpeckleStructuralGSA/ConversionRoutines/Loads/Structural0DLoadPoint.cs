﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_GRID_POINT.2", new string[] { "NODE.2", "AXIS.1" }, "loads", true, true, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSANode) })]
  public class GSA0DLoadPoint : IGSASpeckleContainer
  {
    public int Axis; // Store this temporarily to generate other loads
    
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural0DLoadPoint();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural0DLoadPoint();
      this.GetAttribute("loadAxis");
      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      
      obj.LoadCaseRef = HelperClass.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      var axis = pieces[counter++];
      this.Axis = axis == "GLOBAL" ? 0 : Convert.ToInt32(axis);






      //HelperClass.GetGridPlaneRef(Convert.ToInt32(pieces[counter++]), out int gridPlaneRefRet, out string gridSurfaceRec);
      //HelperClass.GetGridPlaneData(gridPlaneRefRet, out int gridPlaneAxis, out double gridPlaneElevation, out string gridPlaneRec);

      //this.SubGWACommand.Add(gridSurfaceRec);
      //this.SubGWACommand.Add(gridPlaneRec);

      //string gwaRec = null;
      //var planeAxis = HelperClass.Parse0DAxis(gridPlaneAxis, Initialiser.Interface, out gwaRec);
      //if (gwaRec != null)
      //  this.SubGWACommand.Add(gwaRec);
      //double elevation = gridPlaneElevation;

      //var planeLoadAxisId = 0;
      //var planeLoadAxisData = pieces[counter++];
      //StructuralAxis planeLoadAxis;
      //if (planeLoadAxisData == "LOCAL")
      //  planeLoadAxis = planeAxis;
      //else
      //{
      //  planeLoadAxisId = planeLoadAxisData == "GLOBAL" ? 0 : Convert.ToInt32(planeLoadAxisData);
      //  planeLoadAxis = HelperClass.Parse0DAxis(planeLoadAxisId, Initialiser.Interface, out gwaRec);
      //  if (gwaRec != null)
      //    this.SubGWACommand.Add(gwaRec);
      //}
      //var planeProjected = pieces[counter++] == "YES";
      //var planeDirection = pieces[counter++];
      //var value = Convert.ToDouble(pieces[counter++]);







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

      var load = this.Value as Structural0DLoadPoint;
      
      if (load.Loading == null)
        return "";
      
      var keyword = typeof(GSA0DLoadPoint).GetGSAKeyword();

      var gridSurfaceIndex = Initialiser.Cache.ResolveIndex("GRID_SURFACE.1");
      var gridPlaneIndex = Initialiser.Cache.ResolveIndex("GRID_PLANE.4");

      var loadCaseRef = 0;
      try
      {
        loadCaseRef = Initialiser.Cache.LookupIndex(typeof(GSALoadCase).GetGSAKeyword(), load.LoadCaseRef).Value;
      }
      catch {
        loadCaseRef = Initialiser.Cache.ResolveIndex(typeof(GSALoadCase).GetGSAKeyword(), load.LoadCaseRef);
      }

      var axis = load.LoadAxis;

      // Calculate elevation
      //var elevation = (load.Value[0] * axis.Normal.Value[0] +
      //    load.Value[1] * axis.Normal.Value[1] +
      //    load.Value[2] * axis.Normal.Value[2]) /
      //    Math.Sqrt(axis.Normal.Value[0] * axis.Normal.Value[0] +
      //        axis.Normal.Value[1] * axis.Normal.Value[1] +
      //        axis.Normal.Value[2] * axis.Normal.Value[2]);
      var direction = new string[6] { "X", "Y", "Z", "XX", "YY", "ZZ" };

      var gwaAxisCommand = "";
      var gwaCommands = new List<string>();

      var axisRef = "GLOBAL";

      if (load.LoadAxis == null)
      {
        //Default value
        axisRef = "GLOBAL";
      }
      else
      {
        if (load.LoadAxis.Xdir.Value.SequenceEqual(new double[] { 1, 0, 0 }) &&
          load.LoadAxis.Ydir.Value.SequenceEqual(new double[] { 0, 1, 0 }) &&
          load.LoadAxis.Normal.Value.SequenceEqual(new double[] { 0, 0, 1 }))
        {
          axisRef = "GLOBAL";
        }
        else if (load.LoadAxis.Xdir.Value.SequenceEqual(new double[] { 0, 0, 1 }) &&
          load.LoadAxis.Ydir.Value.SequenceEqual(new double[] { 1, 0, 0 }) &&
          load.LoadAxis.Normal.Value.SequenceEqual(new double[] { 0, 1, 0 }))
        {
          axisRef = "VERTICAL";
        }
        else
          try
          {
            HelperClass.SetAxis(load.LoadAxis, out var axisIndex, out gwaAxisCommand, load.Name);
            if (gwaAxisCommand.Length > 0)
            {
              gwaCommands.Add(gwaAxisCommand);
              axisRef = axisIndex.ToString();
            }
          }
          catch { axisRef = "GLOBAL"; }
      }

      var ls = new List<string>();
      for (var i = 0; i < load.Loading.Value.Count(); i++)
      {

        if (load.Loading.Value[i] == 0) continue;

        var index = Initialiser.Cache.ResolveIndex(typeof(GSA0DLoadPoint).GetGSAKeyword());

        ls.Add("SET_AT");
        ls.Add(index.ToString());
        ls.Add(keyword + ":" + HelperClass.GenerateSID(load));
        ls.Add(load.Name == null || load.Name == "" ? " " : load.Name);
        ls.Add("1"); // Grid Surface
        ls.Add("0"); // X coordinate
        ls.Add("0"); // Y coordinate
        ls.Add(loadCaseRef.ToString());
        ls.Add(axisRef); // Axis
        ls.Add(direction[i]);
        ls.Add(load.Loading.Value[i].ToString());

        gwaCommands.Add(string.Join("\t", ls));
      }

      ls.Clear();
      ls.AddRange(new[] {
        "SET",
        "GRID_SURFACE.1",
        gridSurfaceIndex.ToString(),
        load.Name == null || load.Name == "" ? " " : load.Name,
        gridPlaneIndex.ToString(),
        "1", // Dimension of elements to target
        "all", // List of elements to target
        "0.01", // Tolerance
        "ONE", // Span option
        "0"}); // Span angle
      gwaCommands.Add(string.Join("\t", ls));

      ls.Clear();
      HelperClass.SetAxis(axis, out int planeAxisIndex, out string planeAxisGwa, load.Name);
      if (planeAxisGwa.Length > 0)
      {
        gwaCommands.Add(planeAxisGwa);
      }

      ls.AddRange(new[] {
        "SET",
        "GRID_PLANE.4",
        gridPlaneIndex.ToString(),
        load.Name == null || load.Name == "" ? " " : load.Name,
        "GENERAL", // Type
        planeAxisIndex.ToString(),
        "0",
        "0", // Elevation above
        "0"}); // Elevation below
      gwaCommands.Add(string.Join("\t", ls));
      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural0DLoadPoint load)
    {
      return new GSA0DLoadPoint() { Value = load }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA0DLoadPoint dummyObject)
    {
      var newPoints = ToSpeckleBase<GSA0DLoadPoint>();

      var loads = new List<GSA0DLoadPoint>();

      var nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();


      foreach (var p in newPoints.Values)
      {
        var loadSubList = new List<GSA0DLoadPoint>();

        // Placeholder load object to get list of nodes and load values
        // Need to transform to axis so one load definition may be transformed to many
        var initLoad = new GSA0DLoadPoint() { GWACommand = p };
        initLoad.ParseGWACommand();

        // Raise node flag to make sure it gets sent
        foreach (var n in nodes.Where(n => initLoad.Value.NodeRefs.Contains(n.Value.ApplicationId)))
        {
          n.ForceSend = true;
        }

        //// Create load for each node applied
        //foreach (string nRef in initLoad.Value.NodeRefs)
        //{
        //  var load = new GSA0DLoadPoint
        //  {
        //    GWACommand = initLoad.GWACommand,
        //    SubGWACommand = new List<string>(initLoad.SubGWACommand)
        //  };
        //  load.Value.Name = initLoad.Value.Name;
        //  load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

        //  // Transform load to defined axis
        //  var node = nodes.Where(n => (n.Value.ApplicationId == nRef)).First();
        //  string gwaRecord = null;
        //  StructuralAxis loadAxis = HelperClass.Parse0DAxis(initload.LoadAxis, Initialiser.Interface, out gwaRecord, node.Value.Value.ToArray());
        //  load.Value.Loading = initLoad.Value.Loading;
        //  load.Value.Loading.TransformOntoAxis(loadAxis);

        //  // If the loading already exists, add node ref to list
        //  var match = loadSubList.Count() > 0 ? loadSubList.Where(l => (l.Value.Loading.Value as List<double>).SequenceEqual(load.Value.Loading.Value as List<double>)).First() : null;
        //  if (match != null)
        //  {
        //    match.Value.NodeRefs.Add(nRef);
        //    if (gwaRecord != null)
        //      match.SubGWACommand.Add(gwaRecord);
        //  }
        //  else
        //  {
        //    load.Value.NodeRefs = new List<string>() { nRef };
        //    if (gwaRecord != null)
        //      load.SubGWACommand.Add(gwaRecord);
        //    loadSubList.Add(load);
        //  }
        //}

        loads.AddRange(loadSubList);
      }

      Initialiser.GSASenderObjects[typeof(GSA0DLoadPoint)].AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
