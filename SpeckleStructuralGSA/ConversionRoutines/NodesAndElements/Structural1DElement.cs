﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("EL.4", new string[] { "NODE.2" }, "elements", true, false, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSA1DProperty), typeof(GSASpringProperty) })]
  public class GSA1DElement : IGSASpeckleContainer
  {
    public string Member;
    
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DElement();

    public void ParseGWACommand(List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural1DElement();

      var pieces = this.GWACommand.ListSplit("\t");
      
      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);

      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Colour
      counter++; // Type
      obj.PropertyRef = Helper.GetApplicationId(typeof(GSA1DProperty).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));
      counter++; // Group

      obj.Value = new List<double>();
      for (var i = 0; i < 2; i++)
      {
        var key = pieces[counter++];
        var node = nodes.Where(n => n.GSAId == Convert.ToInt32(key)).FirstOrDefault();
        obj.Value.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }

      var orientationNodeRef = pieces[counter++];
      var rotationAngle = Convert.ToDouble(pieces[counter++]);

      if (orientationNodeRef != "0")
      {
        var node = nodes.Where(n => n.GSAId == Convert.ToInt32(orientationNodeRef)).FirstOrDefault();
        obj.ZAxis = Helper.Parse1DAxis(obj.Value.ToArray(),
            rotationAngle, node.Value.Value.ToArray()).Normal as StructuralVectorThree;
        this.SubGWACommand.Add(node.GWACommand);
      }
      else
        obj.ZAxis = Helper.Parse1DAxis(obj.Value.ToArray(), rotationAngle).Normal as StructuralVectorThree;


      if (pieces[counter++] != "NO_RLS")
      {
        var start = pieces[counter++];
        var end = pieces[counter++];

        var endReleases = new List<StructuralVectorBoolSix>
        {
          new StructuralVectorBoolSix(new bool[6]),
          new StructuralVectorBoolSix(new bool[6])
        };

        endReleases[0].Value[1] = ParseEndRelease(start[1], pieces, ref counter);
        endReleases[0].Value[2] = ParseEndRelease(start[2], pieces, ref counter);
        endReleases[0].Value[3] = ParseEndRelease(start[3], pieces, ref counter);
        endReleases[0].Value[4] = ParseEndRelease(start[4], pieces, ref counter);
        endReleases[0].Value[5] = ParseEndRelease(start[5], pieces, ref counter);

        endReleases[1].Value[0] = ParseEndRelease(end[0], pieces, ref counter);
        endReleases[1].Value[1] = ParseEndRelease(end[1], pieces, ref counter);
        endReleases[1].Value[2] = ParseEndRelease(end[2], pieces, ref counter);
        endReleases[1].Value[3] = ParseEndRelease(end[3], pieces, ref counter);
        endReleases[1].Value[4] = ParseEndRelease(end[4], pieces, ref counter);
        endReleases[1].Value[5] = ParseEndRelease(end[5], pieces, ref counter);

        obj.EndRelease = endReleases;
      }
      else
      {
        obj.EndRelease = new List<StructuralVectorBoolSix>
        {
          new StructuralVectorBoolSix(new bool[] { true, true, true, true, true, true }),
          new StructuralVectorBoolSix(new bool[] { true, true, true, true, true, true })
        };
      }

      var offsets = new List<StructuralVectorThree>
      {
        new StructuralVectorThree(new double[3]),
        new StructuralVectorThree(new double[3])
      };

      offsets[0].Value[0] = Convert.ToDouble(pieces[counter++]);
      offsets[1].Value[0] = Convert.ToDouble(pieces[counter++]);

      offsets[0].Value[1] = Convert.ToDouble(pieces[counter++]);
      offsets[1].Value[1] = offsets[0].Value[1];

      offsets[0].Value[2] = Convert.ToDouble(pieces[counter++]);
      offsets[1].Value[2] = offsets[0].Value[2];

      obj.Offset = offsets;

      //counter++; // Action // TODO: EL.4 SUPPORT
      counter++; // Dummy
      
      if (counter < pieces.Length)
        Member = pieces[counter++];

      this.Value = obj;
    }

    public string SetGWACommand(int group = 0)
    {
      if (this.Value == null)
        return "";

      var element = this.Value as Structural1DElement;

      var keyword = typeof(GSA1DElement).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(keyword, element.ApplicationId);

      var propKeyword = typeof(GSA1DProperty).GetGSAKeyword();
      var indexResult = Initialiser.Cache.LookupIndex(propKeyword, element.PropertyRef);
      //If the reference can't be found, then reserve a new index so that it at least doesn't point to any other existing record
      var propRef = indexResult ?? Initialiser.Cache.ResolveIndex(propKeyword, element.PropertyRef);
      if (indexResult == null && element.ApplicationId != null)
      {
        if (element.PropertyRef == null)
        {
          Helper.SafeDisplay("Blank property references found for these Application IDs:", element.ApplicationId);
        }
        else
        {
          Helper.SafeDisplay("Property references not found:", element.ApplicationId + " referencing " + element.PropertyRef);
        }
      }

      var ls = new List<string>
      {
        "SET",
        keyword + ":" + Helper.GenerateSID(element),
        index.ToString(),
        element.Name == null || element.Name == "" ? " " : element.Name,
        "NO_RGB",
        "BEAM", // Type
        propRef.ToString(),
        group.ToString()
      };
      for (var i = 0; i < element.Value.Count(); i += 3)
        ls.Add(Helper.NodeAt(element.Value[i], element.Value[i + 1], element.Value[i + 2], Initialiser.Settings.CoincidentNodeAllowance).ToString());
      ls.Add("0"); // Orientation Node
      try
      {
        ls.Add(Helper.Get1DAngle(element.Value.ToArray(), element.ZAxis).ToString());
      }
      catch { ls.Add("0"); }
      try
      {
        var subLs = new List<string>();
        if (element.EndRelease[0].Value.Any(x => x) || element.EndRelease[1].Value.Any(x => x))
        {
          subLs.Add("RLS");

          var end1 = "";

          end1 += element.EndRelease[0].Value[0] ? "R" : "F";
          end1 += element.EndRelease[0].Value[1] ? "R" : "F";
          end1 += element.EndRelease[0].Value[2] ? "R" : "F";
          end1 += element.EndRelease[0].Value[3] ? "R" : "F";
          end1 += element.EndRelease[0].Value[4] ? "R" : "F";
          end1 += element.EndRelease[0].Value[5] ? "R" : "F";

          subLs.Add(end1);

          var end2 = "";

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
        var subLs = new List<string>
        {
          element.Offset[0].Value[0].ToString(), // Offset x-start
          element.Offset[1].Value[0].ToString(), // Offset x-end

          element.Offset[0].Value[1].ToString(),
          element.Offset[0].Value[2].ToString()
        };

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
      ls.Add((element.GSADummy.HasValue && element.GSADummy.Value) ? "DUMMY" : "");

      return (string.Join("\t", ls));
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

  [GSAObject("MEMB.7", new string[] { "NODE.2" }, "elements", false, true, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSA1DProperty), typeof(GSASpringProperty) })]
  public class GSA1DMember : IGSASpeckleContainer
  {
    public int Group; // Keep for load targetting

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DElement();

    public void ParseGWACommand(List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural1DElement();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Color

      var type = pieces[counter++];
      if (type == "BEAM")
        obj.ElementType = Structural1DElementType.Beam;
      else if (type == "COLUMN")
        obj.ElementType = Structural1DElementType.Column;
      else if (type == "CANTILEVER")
        obj.ElementType = Structural1DElementType.Cantilever;
      else
        obj.ElementType = Structural1DElementType.Generic;

      obj.PropertyRef = Helper.GetApplicationId(typeof(GSA1DProperty).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));
      this.Group = Convert.ToInt32(pieces[counter++]); // Keep group for load targetting

      obj.Value = new List<double>();
      var nodeRefs = pieces[counter++].ListSplit(" ");
      for (var i = 0; i < nodeRefs.Length; i++)
      {
        var node = nodes.Where(n => n.GSAId == Convert.ToInt32(nodeRefs[i])).FirstOrDefault();
        if (node == null)
        {
          //TO DO: review how this is possible and prevent it
          continue;
        }
        obj.Value.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }

      var orientationNodeRef = pieces[counter++];
      var rotationAngle = Convert.ToDouble(pieces[counter++]);

      if (orientationNodeRef != "0")
      {
        var node = nodes.Where(n => n.GSAId == Convert.ToInt32(orientationNodeRef)).FirstOrDefault();
        obj.ZAxis = Helper.Parse1DAxis(obj.Value.ToArray(),
            rotationAngle, node.Value.ToArray()).Normal as StructuralVectorThree;
        this.SubGWACommand.Add(node.GWACommand);
      }
      else
        obj.ZAxis = Helper.Parse1DAxis(obj.Value.ToArray(), rotationAngle).Normal as StructuralVectorThree;

      counter += 9; //Skip to end conditions

      obj.EndRelease = new List<StructuralVectorBoolSix>
      {
        ParseEndReleases(Convert.ToInt32(pieces[counter++])),
        ParseEndReleases(Convert.ToInt32(pieces[counter++]))
      };

      // Skip to offsets at fifth to last
      counter = pieces.Length - 5;
      var offsets = new List<StructuralVectorThree>
      {
        new StructuralVectorThree(new double[3]),
        new StructuralVectorThree(new double[3])
      };

      offsets[0].Value[0] = Convert.ToDouble(pieces[counter++]);
      offsets[1].Value[0] = Convert.ToDouble(pieces[counter++]);

      offsets[0].Value[1] = Convert.ToDouble(pieces[counter++]);
      offsets[1].Value[1] = offsets[0].Value[1];

      offsets[0].Value[2] = Convert.ToDouble(pieces[counter++]);
      offsets[1].Value[2] = offsets[0].Value[2];

      obj.Offset = offsets;

      this.Value = obj;
    }

    public string SetGWACommand(int group = 0)
    {
      if (this.Value == null)
        return "";

      var member = this.Value as Structural1DElement;
      if (member.Value == null || member.Value.Count() == 0)
        return "";

      var keyword = typeof(GSA1DMember).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(typeof(GSA1DMember).GetGSAKeyword(), member.ApplicationId);

      var propKeyword = ((member.ElementType == Structural1DElementType.Spring) ? typeof(GSASpringProperty) : typeof(GSA1DProperty)).GetGSAKeyword();
      var indexResult = Initialiser.Cache.LookupIndex(propKeyword, member.PropertyRef);
      //If the reference can't be found, then reserve a new index so that it at least doesn't point to any other existing record
      var propRef = indexResult ?? Initialiser.Cache.ResolveIndex(propKeyword, member.PropertyRef);
      if (indexResult == null && member.ApplicationId != null)
      {
        if (member.PropertyRef == null)
        {
          Helper.SafeDisplay("Blank property references found for these Application IDs:", member.ApplicationId);
        }
        else
        {
          Helper.SafeDisplay("Property references not found:", member.ApplicationId + " referencing " + member.PropertyRef);
        }
      }

      var ls = new List<string>
      {
        "SET",
        keyword + ":" + Helper.GenerateSID(member),
        index.ToString(),
        member.Name == null || member.Name == "" ? " " : member.Name,
        "NO_RGB"
      };
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
      var topo = "";
      if (member.Value != null)
      {
        for (var i = 0; i < member.Value.Count(); i += 3)
        {
          topo += Helper.NodeAt(member.Value[i], member.Value[i + 1], member.Value[i + 2], Initialiser.Settings.CoincidentNodeAllowance).ToString() + " ";
        }
      }
      ls.Add(topo);
      ls.Add("0"); // Orientation node
      if (member.Value == null)
      {
        ls.Add("0");
      }
      else
      {
        try
        {
          ls.Add(Helper.Get1DAngle(member.Value.ToArray(), member.ZAxis ?? new StructuralVectorThree(0, 0, 1)).ToString());
        }
        catch { ls.Add("0"); }
      }
      //ls.Add(member.GSAMeshSize == 0 ? "0" : member.GSAMeshSize.ToString()); // Target mesh size
      ls.Add(member.GSAMeshSize == null ? "0" : member.GSAMeshSize.ToString()); // Target mesh size
      ls.Add("MESH"); // TODO: What is this?
      ls.Add((member.ElementType == Structural1DElementType.Spring) ? "SPRING" : "BEAM"); // Element type
      ls.Add("0"); // Fire
      ls.Add("0"); // Time 1
      ls.Add("0"); // Time 2
      ls.Add("0"); // Time 3
      ls.Add("0"); // Time 4
      ls.Add((member.GSADummy.HasValue && member.GSADummy.Value) ? "DUMMY" : "ACTIVE");

      if (member.EndRelease == null)
      {
        ls.AddRange(new[] { "2", "2" });
      }
      else
      {
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
      }

      ls.Add("AUTOMATIC"); // Effective length option
      ls.Add("0"); // Pool
      ls.Add("0"); // Height
      ls.Add("MAN"); // Auto offset 1
      ls.Add("MAN"); // Auto offset 2
      ls.Add("NO"); // Internal auto offset

      if (member.Offset == null)
      {
        ls.AddRange(new[] { "0", "0", "0", "0" });
      }
      else
      {
        try
        {
          var subLs = new List<string>
        {
          member.Offset[0].Value[0].ToString(), // Offset x-start
          member.Offset[1].Value[0].ToString(), // Offset x-end

          member.Offset[0].Value[1].ToString(),
          member.Offset[0].Value[2].ToString()
        };

          ls.AddRange(subLs);
        }
        catch
        {
          ls.AddRange(new[] { "0", "0", "0", "0" });
        }
      }
      ls.Add("ALL"); // Exposure

      return (string.Join("\t", ls));
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
    public static string ToNative(this SpeckleLine inputObject)
    {
      var convertedObject = new Structural1DElement();

      foreach (var p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
      {
        var inputProperty = inputObject.GetType().GetProperty(p.Name);
        if (inputProperty != null)
          p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
      }

      return convertedObject.ToNative();
    }

    public static string ToNative(this Structural1DElement beam)
    {
      return (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis) 
        ? new GSA1DElement() { Value = beam }.SetGWACommand()
        : new GSA1DMember() { Value = beam }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA1DElement dummyObject)
    {
      var newLines = ToSpeckleBase<GSA1DElement>();

      var elementsLock = new object();
      var elements = new List<GSA1DElement>();
      var nodes = Initialiser.GSASenderObjects.Get<GSANode>();

      Parallel.ForEach(newLines.Values, p =>
      {
        var pPieces = p.ListSplit("\t");

        if (pPieces[4] == "BEAM" && pPieces[4].ParseElementNumNodes() == 2)
        {
          try
          {
            var element = new GSA1DElement() { GWACommand = p };
            element.ParseGWACommand(nodes);
            lock (elementsLock)
            {
              elements.Add(element);
            }
          }
          catch { }
        }
      });

      Initialiser.GSASenderObjects.AddRange(elements);

      return (elements.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }

    public static SpeckleObject ToSpeckle(this GSA1DMember dummyObject)
    {

      var nodes = Initialiser.GSASenderObjects.Get<GSANode>();
      var membersLock = new object();
      var members = new List<GSA1DMember>();
      var newLines = ToSpeckleBase<GSA1DMember>();

      Parallel.ForEach(newLines.Values, p =>
      {
        var pPieces = p.ListSplit("\t");
        if (pPieces[4].Is1DMember())
        {
          try
          {
            var member = new GSA1DMember() { GWACommand = p };
            member.ParseGWACommand(nodes);
            lock (membersLock)
            {
              members.Add(member);
            }
          }
          catch { }
        }
      });

      Initialiser.GSASenderObjects.AddRange(members);

      return (members.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
