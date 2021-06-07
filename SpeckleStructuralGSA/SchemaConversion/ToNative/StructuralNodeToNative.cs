using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class StructuralNodeToNative
  {
    private static double massEpsilon = 0.0001;

    public static string ToNative(this StructuralNode node)
    {
      if (string.IsNullOrEmpty(node.ApplicationId) || node.Value == null || node.Value.Count < 3)
      {
        return "";
      }

      return Helper.ToNativeTryCatch(node, () =>
      {
        var keyword = GsaRecord.GetKeyword<GsaNode>();
        var massKeyword = GsaRecord.GetKeyword<GsaPropMass>();

        var index = Initialiser.AppResources.Proxy.NodeAt(node.Value[0], node.Value[1], node.Value[2], Initialiser.AppResources.Settings.CoincidentNodeAllowance);
        var streamId = Initialiser.AppResources.Cache.LookupStream(node.ApplicationId);

        var existingGwa = Initialiser.AppResources.Cache.GetGwa(keyword, index);
        GsaNode gsaNode;
        if (existingGwa == null || existingGwa.Count() == 0 || string.IsNullOrEmpty(existingGwa.First()))
        {
          gsaNode = new GsaNode()
          {
            Index = index,
            ApplicationId = node.ApplicationId,
            Name = node.Name,
            StreamId = streamId,
            X = node.Value[0],
            Y = node.Value[1],
            Z = node.Value[2],
          };
        }
        else
        {
          gsaNode = new GsaNode();
          if (!gsaNode.FromGwa(existingGwa.First()))
          {
            //TO DO: add error mesage
            return "";
          }
        }

        if (node.Mass.HasValue && node.Mass.Value > 0)
        {
          int? massIndex = null;
          GsaPropMass gsaPropMass = null;

          //Assume the PROP_MASS has the same Application ID as this node
          //Check if the existing mass with the App ID still has the mass required by this StructuralNode
          massIndex = Initialiser.AppResources.Cache.LookupIndex(massKeyword, node.ApplicationId);
          if (massIndex.HasValue)
          {
            var massGwa = Initialiser.AppResources.Cache.GetGwa(massKeyword, massIndex.Value);
            if (massGwa != null && massGwa.Count() > 0 && !string.IsNullOrEmpty(massGwa.First()))
            {
              gsaPropMass = new GsaPropMass();
              gsaPropMass.FromGwa(massGwa.First());

              if (Math.Abs(gsaPropMass.Mass - node.Mass.Value) > massEpsilon)
              {
                gsaPropMass.Mass = node.Mass.Value;
              }
            }
          }

          if (gsaPropMass == null)
          {
            gsaPropMass = new GsaPropMass() 
            { 
              ApplicationId = gsaNode.ApplicationId,
              StreamId = streamId,
              Index = Initialiser.AppResources.Cache.ResolveIndex(massKeyword, gsaNode.ApplicationId),
              Mass = node.Mass.Value
            };
            if (!string.IsNullOrEmpty(node.Name))
            {
              gsaPropMass.Name = "Mass for " + node.Name;
            }
          }
          if (gsaPropMass.Gwa(out var massGwaLines, false))
          {
            Initialiser.AppResources.Cache.Upsert(massKeyword, gsaPropMass.Index.Value, massGwaLines.First(), streamId, gsaPropMass.ApplicationId, GsaRecord.GetGwaSetCommandType<GsaPropMass>());
          }
        }
        else
        {
          //Remove it if, for some reason, it's not valid
          gsaNode.MassPropertyIndex = null;
        }

        if (node.Restraint == null || node.Restraint.Value == null || node.Restraint.Value.Count() < 6)
        {
          gsaNode.NodeRestraint = NodeRestraint.Free;
        }
        else if (node.Restraint.Value.SequenceEqual(new[] { true, true, true, false, false, false }))
        {
          gsaNode.NodeRestraint = NodeRestraint.Pin;
        }
        else if (node.Restraint.Value.All(b => b))
        {
          gsaNode.NodeRestraint = NodeRestraint.Fix;
        }
        else
        {
          gsaNode.NodeRestraint = NodeRestraint.Custom;
          gsaNode.Restraints = new List<AxisDirection6>();
          for (var i = 0; i < node.Restraint.Value.Count(); i++)
          {
            if (node.Restraint.Value[i])
            {
              gsaNode.Restraints.Add(Helper.AxisDirs[i]);
            }
          }
        }

        if (node.Axis == null)
        {
          gsaNode.AxisRefType = AxisRefType.Global;
        }
        else
        {
          var gsaAxis = StructuralAxisToNative.ToNativeSchema(node.Axis);
          gsaAxis.StreamId = streamId;
          StructuralAxisToNative.ToNative(gsaAxis);

          gsaNode.AxisRefType = AxisRefType.Reference;
          gsaNode.AxisIndex = gsaAxis.Index;
        }

        if (node.GSALocalMeshSize.HasValue)
        {
          gsaNode.MeshSize = node.GSALocalMeshSize.Value;
        }

        if (gsaNode.Gwa(out var gwaLines, false))
        {
          Initialiser.AppResources.Cache.Upsert(keyword, gsaNode.Index.Value, gwaLines.First(), streamId, gsaNode.ApplicationId, GsaRecord.GetGwaSetCommandType<GsaNode>());
        }

        return "";
      });
    }
  }
}
