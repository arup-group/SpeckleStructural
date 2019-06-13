﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleElements;

namespace SpeckleElementsGSA
{
  [GSAObject("EL.4", new string[] { "NODE.2" }, "elements", true, false, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSA1DProperty) })]
  public class GSA1DElement : IGSASpeckleContainer
  {
    public string Member;

    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DElement();

    public void ParseGWACommand(GSAInterfacer GSA, List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      Structural1DElement obj = new Structural1DElement();

      string[] pieces = this.GWACommand.ListSplit(",");

      int counter = 1; // Skip identifier
      obj.StructuralId = pieces[counter++];
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Colour
      counter++; // Type
      obj.PropertyRef = pieces[counter++];
      counter++; // Group

      obj.Value = new List<double>();
      for (int i = 0; i < 2; i++)
      {
        string key = pieces[counter++];
        GSANode node = nodes.Where(n => n.Value.StructuralId == key).FirstOrDefault();
        obj.Value.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }

      string orientationNodeRef = pieces[counter++];
      double rotationAngle = Convert.ToDouble(pieces[counter++]);

      if (orientationNodeRef != "0")
      {
        GSANode node = nodes.Where(n => n.Value.StructuralId == orientationNodeRef).FirstOrDefault();
        obj.ZAxis = GSA.Parse1DAxis(obj.Value.ToArray(),
            rotationAngle, node.Value.ToArray()).Normal as StructuralVectorThree;
        this.SubGWACommand.Add(node.GWACommand);
      }
      else
        obj.ZAxis = GSA.Parse1DAxis(obj.Value.ToArray(), rotationAngle).Normal as StructuralVectorThree;


      if (pieces[counter++] != "NO_RLS")
      {
        string start = pieces[counter++];
        string end = pieces[counter++];

        obj.EndRelease = new List<StructuralVectorBoolSix>();
        obj.EndRelease.Add(new StructuralVectorBoolSix(new bool[6]));
        obj.EndRelease.Add(new StructuralVectorBoolSix(new bool[6]));

        obj.EndRelease[0].Value[0] = ParseEndRelease(start[0], pieces, ref counter);
        obj.EndRelease[0].Value[1] = ParseEndRelease(start[1], pieces, ref counter);
        obj.EndRelease[0].Value[2] = ParseEndRelease(start[2], pieces, ref counter);
        obj.EndRelease[0].Value[3] = ParseEndRelease(start[3], pieces, ref counter);
        obj.EndRelease[0].Value[4] = ParseEndRelease(start[4], pieces, ref counter);
        obj.EndRelease[0].Value[5] = ParseEndRelease(start[5], pieces, ref counter);

        obj.EndRelease[1].Value[0] = ParseEndRelease(end[0], pieces, ref counter);
        obj.EndRelease[1].Value[1] = ParseEndRelease(end[1], pieces, ref counter);
        obj.EndRelease[1].Value[2] = ParseEndRelease(end[2], pieces, ref counter);
        obj.EndRelease[1].Value[3] = ParseEndRelease(end[3], pieces, ref counter);
        obj.EndRelease[1].Value[4] = ParseEndRelease(end[4], pieces, ref counter);
        obj.EndRelease[1].Value[5] = ParseEndRelease(end[5], pieces, ref counter);
      }
      else
      {
        obj.EndRelease = new List<StructuralVectorBoolSix>();
        obj.EndRelease.Add(new StructuralVectorBoolSix(new bool[] { true, true, true, true, true, true }));
        obj.EndRelease.Add(new StructuralVectorBoolSix(new bool[] { true, true, true, true, true, true }));
      }

      obj.Offset = new List<StructuralVectorThree>();
      obj.Offset.Add(new StructuralVectorThree(new double[3]));
      obj.Offset.Add(new StructuralVectorThree(new double[3]));

      obj.Offset[0].Value[0] = Convert.ToDouble(pieces[counter++]);
      obj.Offset[1].Value[0] = Convert.ToDouble(pieces[counter++]);

      obj.Offset[0].Value[1] = Convert.ToDouble(pieces[counter++]);
      obj.Offset[1].Value[1] = obj.Offset[0].Value[1];

      obj.Offset[0].Value[2] = Convert.ToDouble(pieces[counter++]);
      obj.Offset[1].Value[2] = obj.Offset[0].Value[2];

      //counter++; // Action // TODO: EL.4 SUPPORT
      counter++; // Dummy
      
      Member = pieces[counter++];

      this.Value = obj;
    }

    public void SetGWACommand(GSAInterfacer GSA, int group = 0)
    {
      if (this.Value == null)
        return;

      Structural1DElement element = this.Value as Structural1DElement;

      string keyword = typeof(GSA1DElement).GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(typeof(GSA1DElement), element);
      int propRef = 0;
      try
      {
        propRef = GSA.Indexer.LookupIndex(typeof(GSA1DProperty), element.PropertyRef).Value;
      }
      catch { }

      List<string> ls = new List<string>();

      ls.Add("SET");
      ls.Add(keyword + ":" + GSA.GenerateSID(element));
      ls.Add(index.ToString());
      ls.Add(element.Name == null || element.Name == "" ? " " : element.Name);
      ls.Add("NO_RGB");
      ls.Add("BEAM"); // Type
      ls.Add(propRef.ToString());
      ls.Add(group.ToString());
      for (int i = 0; i < element.Value.Count(); i += 3)
        ls.Add(GSA.NodeAt(element.Value[i], element.Value[i + 1], element.Value[i + 2], Conversions.GSACoincidentNodeAllowance).ToString());
      ls.Add("0"); // Orientation Node
      try
      {
        ls.Add(GSA.Get1DAngle(element.Value.ToArray(), element.ZAxis).ToString());
      }
      catch { ls.Add("0"); }
      try
      {
        List<string> subLs = new List<string>();
        if (element.EndRelease[0].Value.Any(x => x) || element.EndRelease[1].Value.Any(x => x))
        {
          subLs.Add("RLS");

          string end1 = "";

          end1 += element.EndRelease[0].Value[0] ? "R" : "F";
          end1 += element.EndRelease[0].Value[1] ? "R" : "F";
          end1 += element.EndRelease[0].Value[2] ? "R" : "F";
          end1 += element.EndRelease[0].Value[3] ? "R" : "F";
          end1 += element.EndRelease[0].Value[4] ? "R" : "F";
          end1 += element.EndRelease[0].Value[5] ? "R" : "F";

          subLs.Add(end1);

          string end2 = "";

          end2 += element.EndRelease[1].Value[0] ? "R" : "F";
          end2 += element.EndRelease[1].Value[1] ? "R" : "F";
          end2 += element.EndRelease[1].Value[2] ? "R" : "F";
          end2 += element.EndRelease[1].Value[3] ? "R" : "F";
          end2 += element.EndRelease[1].Value[4] ? "R" : "F";
          end2 += element.EndRelease[1].Value[5] ? "R" : "F";

          subLs.Add(end2);

          ls.AddRange(subLs);
        }
        else
          ls.Add("NO_RLS");
      }
      catch { ls.Add("NO_RLS"); }

      try
      {
        List<string> subLs = new List<string>();
        subLs.Add(element.Offset[0].Value[0].ToString()); // Offset x-start
        subLs.Add(element.Offset[1].Value[0].ToString()); // Offset x-end

        subLs.Add(element.Offset[0].Value[1].ToString());
        subLs.Add(element.Offset[0].Value[2].ToString());

        ls.AddRange(subLs);
      }
      catch
      {
        ls.Add("0");
        ls.Add("0");
        ls.Add("0");
        ls.Add("0");
      }

      //ls.Add("NORMAL"); // Action // TODO: EL.4 SUPPORT
      ls.Add(element.Dummy ? "DUMMY" : "");

      GSA.RunGWACommand(string.Join("\t", ls));
    }

    private static bool ParseEndRelease(char code, string[] pieces, ref int counter)
    {
      switch (code)
      {
        case 'F':
          return false;
        case 'R':
          return true;
        default:
          // TODO
          counter++;
          return true;
      }
    }
  }

  [GSAObject("MEMB.7", new string[] { "NODE.2" }, "elements", false, true, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSA1DProperty) })]
  public class GSA1DMember : IGSASpeckleContainer
  {
    public int Group; // Keep for load targetting

    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DElement();

    public void ParseGWACommand(GSAInterfacer GSA, List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      Structural1DElement obj = new Structural1DElement();

      string[] pieces = this.GWACommand.ListSplit(",");

      int counter = 1; // Skip identifier
      obj.StructuralId = pieces[counter++];
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Color

      string type = pieces[counter++];
      if (type == "BEAM")
        obj.ElementType = Structural1DElementType.Beam;
      else if (type == "COLUMN")
        obj.ElementType = Structural1DElementType.Column;
      else if (type == "CANTILEVER")
        obj.ElementType = Structural1DElementType.Cantilever;
      else
        obj.ElementType = Structural1DElementType.Generic;

      obj.PropertyRef = pieces[counter++];
      this.Group = Convert.ToInt32(pieces[counter++]); // Keep group for load targetting

      obj.Value = new List<double>();
      string[] nodeRefs = pieces[counter++].ListSplit(" ");
      for (int i = 0; i < nodeRefs.Length; i++)
      {
        GSANode node = nodes.Where(n => n.Value.StructuralId == nodeRefs[i]).FirstOrDefault();
        obj.Value.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }

      string orientationNodeRef = pieces[counter++];
      double rotationAngle = Convert.ToDouble(pieces[counter++]);

      if (orientationNodeRef != "0")
      {
        GSANode node = nodes.Where(n => n.Value.StructuralId == orientationNodeRef).FirstOrDefault();
        obj.ZAxis = GSA.Parse1DAxis(obj.Value.ToArray(),
            rotationAngle, node.Value.ToArray()).Normal as StructuralVectorThree;
        this.SubGWACommand.Add(node.GWACommand);
      }
      else
        obj.ZAxis = GSA.Parse1DAxis(obj.Value.ToArray(), rotationAngle).Normal as StructuralVectorThree;

      counter += 9; //Skip to end conditions

      obj.EndRelease = new List<StructuralVectorBoolSix>();
      obj.EndRelease.Add(ParseEndReleases(Convert.ToInt32(pieces[counter++])));
      obj.EndRelease.Add(ParseEndReleases(Convert.ToInt32(pieces[counter++])));

      // Skip to offsets at fifth to last
      counter = pieces.Length - 5;
      obj.Offset = new List<StructuralVectorThree>();
      obj.Offset.Add(new StructuralVectorThree(new double[3]));
      obj.Offset.Add(new StructuralVectorThree(new double[3]));

      obj.Offset[0].Value[0] = Convert.ToDouble(pieces[counter++]);
      obj.Offset[1].Value[0] = Convert.ToDouble(pieces[counter++]);

      obj.Offset[0].Value[1] = Convert.ToDouble(pieces[counter++]);
      obj.Offset[1].Value[1] = obj.Offset[0].Value[1];

      obj.Offset[0].Value[2] = Convert.ToDouble(pieces[counter++]);
      obj.Offset[1].Value[2] = obj.Offset[0].Value[2];

      this.Value = obj;
    }

    public void SetGWACommand(GSAInterfacer GSA, int group = 0)
    {
      if (this.Value == null)
        return;

      Structural1DElement member = this.Value as Structural1DElement;

      string keyword = typeof(GSA1DMember).GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(typeof(GSA1DMember), member);
      int propRef = 0;
      try
      {
        propRef = GSA.Indexer.LookupIndex(typeof(GSA1DProperty), member.PropertyRef).Value;
      }
      catch { }

      List<string> ls = new List<string>();

      ls.Add("SET");
      ls.Add(keyword + ":" + GSA.GenerateSID(member));
      ls.Add(index.ToString());
      ls.Add(member.Name == null || member.Name == "" ? " " : member.Name);
      ls.Add("NO_RGB");
      if (member.ElementType == Structural1DElementType.Beam)
        ls.Add("BEAM");
      else if (member.ElementType == Structural1DElementType.Column)
        ls.Add("COLUMN");
      else if (member.ElementType == Structural1DElementType.Cantilever)
        ls.Add("CANTILEVER");
      else
        ls.Add("1D_GENERIC");
      ls.Add(propRef.ToString());
      ls.Add(group != 0 ? group.ToString() : index.ToString()); // TODO: This allows for targeting of elements from members group
      string topo = "";
      for (int i = 0; i < member.Value.Count(); i += 3)
        topo += GSA.NodeAt(member.Value[i], member.Value[i + 1], member.Value[i + 2], Conversions.GSACoincidentNodeAllowance).ToString() + " ";
      ls.Add(topo);
      ls.Add("0"); // Orientation node
      try
      {
        ls.Add(GSA.Get1DAngle(member.Value.ToArray(), member.ZAxis).ToString());
      }
      catch { ls.Add("0"); }
      ls.Add(member.MeshSize == 0 ? "0" : member.MeshSize.ToString()); // Target mesh size
      ls.Add("MESH"); // TODO: What is this?
      ls.Add("BEAM"); // Element type
      ls.Add("0"); // Fire
      ls.Add("0"); // Time 1
      ls.Add("0"); // Time 2
      ls.Add("0"); // Time 3
      ls.Add("0"); // TODO: What is this?
      ls.Add(member.Dummy ? "DUMMY" : "ACTIVE");

      try
      {
        if (member.EndRelease[0].Value.SequenceEqual(ParseEndReleases(1).Value))
          ls.Add("1");
        else if (member.EndRelease[0].Value.SequenceEqual(ParseEndReleases(2).Value))
          ls.Add("2");
        else if (member.EndRelease[0].Value.SequenceEqual(ParseEndReleases(3).Value))
          ls.Add("3");
        else
        {
          if (member.EndRelease[0].Value.Skip(3).Take(3).SequenceEqual(new bool[] { false, false, false }))
            ls.Add("2");
          else
            ls.Add("1");
        }
      }
      catch { ls.Add("2"); }

      try
      {
        if (member.EndRelease[1].Value.SequenceEqual(ParseEndReleases(1).Value))
          ls.Add("1");
        else if (member.EndRelease[1].Value.SequenceEqual(ParseEndReleases(2).Value))
          ls.Add("2");
        else if (member.EndRelease[1].Value.SequenceEqual(ParseEndReleases(3).Value))
          ls.Add("3");
        else
        {
          if (member.EndRelease[1].Value.Skip(3).Take(3).SequenceEqual(new bool[] { false, false, false }))
            ls.Add("2");
          else
            ls.Add("1");
        }
      }
      catch { ls.Add("2"); }

      ls.Add("AUTOMATIC"); // Effective length option
      ls.Add("0"); // Pool
      ls.Add("0"); // Height
      ls.Add("MAN"); // Auto offset 1
      ls.Add("MAN"); // Auto offset 2
      ls.Add("NO"); // Internal auto offset

      try
      {
        List<string> subLs = new List<string>();
        subLs.Add(member.Offset[0].Value[0].ToString()); // Offset x-start
        subLs.Add(member.Offset[1].Value[0].ToString()); // Offset x-end

        subLs.Add(member.Offset[0].Value[1].ToString());
        subLs.Add(member.Offset[0].Value[2].ToString());

        ls.AddRange(subLs);
      }
      catch
      {
        ls.Add("0");
        ls.Add("0");
        ls.Add("0");
        ls.Add("0");
      }
      ls.Add("ALL"); // Exposure

      GSA.RunGWACommand(string.Join("\t", ls));
    }

    private static StructuralVectorBoolSix ParseEndReleases(int option)
    {
      switch (option)
      {
        case 1:
          // Pinned
          return new StructuralVectorBoolSix(false, false, false, false, true, true);
        case 2:
          // Fixed
          return new StructuralVectorBoolSix(false, false, false, false, false, false);
        case 3:
          // Free
          return new StructuralVectorBoolSix(true, true, true, true, true, true);
        case 4:
          // Full rotational
          return new StructuralVectorBoolSix(false, false, false, false, false, false);
        case 5:
          // Partial rotational
          return new StructuralVectorBoolSix(false, false, false, false, true, true);
        case 6:
          // Top flange lateral
          return new StructuralVectorBoolSix(false, false, false, false, false, false);
        default:
          // Pinned
          return new StructuralVectorBoolSix(false, false, false, false, true, true);
      }
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this SpeckleLine inputObject)
    {
      Structural1DElement convertedObject = new Structural1DElement();

      foreach (PropertyInfo p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
      {
        PropertyInfo inputProperty = inputObject.GetType().GetProperty(p.Name);
        if (inputProperty != null)
          p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
      }

      return convertedObject.ToNative();
    }

    public static bool ToNative(this Structural1DElement beam)
    {
      if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
        new GSA1DElement() { Value = beam }.SetGWACommand(GSA);
      else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
        new GSA1DMember() { Value = beam }.SetGWACommand(GSA);

      return true;
    }

    public static bool ToNative(this Beam beam)
    {
      return true;
    }

    public static bool ToNative(this Column column)
    {
      return true;
    }

    public static SpeckleObject ToSpeckle(this GSA1DElement dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSA1DElement)))
        GSASenderObjects[typeof(GSA1DElement)] = new List<object>();

      if (!GSASenderObjects.ContainsKey(typeof(GSA1DElementPolyline)))
        GSASenderObjects[typeof(GSA1DElementPolyline)] = new List<object>();

      List<GSA1DElement> elements = new List<GSA1DElement>();
      List<GSANode> nodes = GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      string keyword = typeof(GSA1DElement).GetGSAKeyword();
      string[] subKeywords = typeof(GSA1DElement).GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL," + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL," + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL," + k));

      // Remove deleted lines
      GSASenderObjects[typeof(GSA1DElement)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSA1DElement)]
        .Select(l => (l as IGSASpeckleContainer).GWACommand)
        .Concat(GSASenderObjects[typeof(GSA1DElementPolyline)].SelectMany(l => (l as IGSASpeckleContainer).SubGWACommand))
        .ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        string[] pPieces = p.ListSplit(",");
        if (pPieces[4].ParseElementNumNodes() == 2)
        {
          GSA1DElement element = new GSA1DElement() { GWACommand = p };
          element.ParseGWACommand(GSA, nodes);
          elements.Add(element);
        }
      }

      GSASenderObjects[typeof(GSA1DElement)].AddRange(elements);

      if (elements.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }

    public static SpeckleObject ToSpeckle(this GSA1DMember dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSA1DMember)))
        GSASenderObjects[typeof(GSA1DMember)] = new List<object>();

      List<GSA1DMember> members = new List<GSA1DMember>();
      List<GSANode> nodes = GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      string keyword = typeof(GSA1DMember).GetGSAKeyword();
      string[] subKeywords = typeof(GSA1DMember).GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL," + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL," + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL," + k));

      // Remove deleted lines
      GSASenderObjects[typeof(GSA1DMember)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSA1DMember)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        string[] pPieces = p.ListSplit(",");
        if (pPieces[4].MemberIs1D())
        {
          GSA1DMember member = new GSA1DMember() { GWACommand = p };
          member.ParseGWACommand(GSA, nodes);
          members.Add(member);
        }
      }

      GSASenderObjects[typeof(GSA1DMember)].AddRange(members);

      if (members.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
